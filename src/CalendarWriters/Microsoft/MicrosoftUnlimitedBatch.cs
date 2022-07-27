using System.Globalization;
using Microsoft.Graph;

namespace TimetableCalendarGenerator;

internal class MicrosoftUnlimitedBatch : IDisposable
{
  private readonly GraphServiceClient _service;
  private readonly IList<BatchRequestContent> _batches = new List<BatchRequestContent> { new() };

  private int _counter;
  private bool disposedValue;

  private const int BatchSizeLimit = 20;
  private const int MaxAttempts = 4;
  private const int RetryFirst = 5000;
  private const int RetryMultiplier = 4;

  public MicrosoftUnlimitedBatch(GraphServiceClient service)
  {
    _service = service ?? throw new ArgumentNullException(nameof(service));
  }

  public void Queue(HttpRequestMessage request)
  {
    if (request is null)
    {
      throw new ArgumentNullException(nameof(request));
    }

    var currentBatch = _batches.Last();
    if (currentBatch.BatchRequestSteps.Count == BatchSizeLimit)
    {
      currentBatch = new BatchRequestContent();
      _batches.Add(currentBatch);
      _counter = 0;
    }

    var step = new BatchRequestStep(_counter.ToString(CultureInfo.InvariantCulture),request,
      _counter == 0 ? null : new List<string> { (_counter - 1).ToString(CultureInfo.InvariantCulture) });
    currentBatch.AddBatchRequestStep(step);
    _counter++;
  }

  public async Task ExecuteWithRetryAsync()
  {
    foreach (var batch in _batches)
    {
      if (batch.BatchRequestSteps.Count == 0) continue;
      var current = batch;

      for (var attempt = 1; attempt <= MaxAttempts; attempt++) {
        int firstFailure = 0;
        List<KeyValuePair<string, HttpResponseMessage>> responses = null;
        try
        {
          firstFailure = 0;
          var result = await _service.Batch.Request().PostAsync(current);
          responses = (await result.GetResponsesAsync()).ToList();
          firstFailure = responses.FindIndex(o => !o.Value.IsSuccessStatusCode);
          if (firstFailure < 0)
          {
            current.Dispose();
            break;
          }
          throw new HttpRequestException("Batch requests failed.");
        }
        catch when (attempt < 3)
        {
          var retry = new BatchRequestContent();
          var i = 0;
          foreach (var step in current.BatchRequestSteps.Values.Skip(firstFailure))
          {
            var clone = await CloneRequestAsync(step.Request);
            var retryStep = new BatchRequestStep(i.ToString(CultureInfo.InvariantCulture), clone, i == 0 ? null : new List<string> { (i - 1).ToString(CultureInfo.InvariantCulture) });
            retry.AddBatchRequestStep(retryStep);
            i++;
          }
          var backoff = RetryFirst * (int)Math.Pow(RetryMultiplier, attempt - 1);
          await Task.Delay(backoff);
          current.Dispose();
          current = retry;
        }
        finally
        {
          if (responses is not null)
          {
            foreach (var response in responses) response.Value?.Dispose();
          }
        }
      }

    }
  }

  private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage req)
  {
    var clone = new HttpRequestMessage(req.Method, req.RequestUri) {Content = await CloneContentAsync(req.Content).ConfigureAwait(false), Version = req.Version};
    foreach (var (key, value) in req.Options) clone.Options.Set(new HttpRequestOptionsKey<object>(key), value);
    foreach (var (key, value) in req.Headers) clone.Headers.TryAddWithoutValidation(key, value);
    return clone;
  }

  private static async Task<HttpContent> CloneContentAsync(HttpContent content)
  {
    if (content is null) return null;
    var ms = new MemoryStream();
    await content.CopyToAsync(ms).ConfigureAwait(false);
    ms.Position = 0;
    var clone = new StreamContent(ms);
    foreach (var (key, value) in content.Headers) clone.Headers.Add(key, value);
    return clone;
  }

  protected virtual void Dispose(bool disposing)
  {
    if (!disposedValue)
    {
      if (disposing)
      {
        if (_batches is not null)
        {
          foreach (var batchRequestContent in _batches)
          {
            batchRequestContent?.Dispose();
          }
        }
      }

      disposedValue = true;
    }
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }
}