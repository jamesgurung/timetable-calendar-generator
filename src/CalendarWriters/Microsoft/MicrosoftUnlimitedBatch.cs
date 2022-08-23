using Microsoft.Graph;
using Microsoft.Graph.Beta;
using Microsoft.Kiota.Abstractions;

namespace TimetableCalendarGenerator;

internal class MicrosoftUnlimitedBatch<T> : IDisposable
{
  private readonly GraphServiceClient _service;
  private readonly List<BatchRequestContent> _batches;
  private readonly Func<T, RequestInformation> _requestInfoFunction;
  private readonly Dictionary<string, T> _originalData = new();

  private bool disposedValue;

  private const int BatchSizeLimit = 4; // Mailbox concurrency limit
  private const int MaxAttempts = 4;
  private const int RetryFirst = 1000;
  private const int RetryMultiplier = 4;

  public MicrosoftUnlimitedBatch(GraphServiceClient service, Func<T, RequestInformation> requestInfoFunction)
  {
    _service = service ?? throw new ArgumentNullException(nameof(service));
    _batches = new() { new(_service) };
    _requestInfoFunction = requestInfoFunction;
  }

  public void Queue(T request)
  {
    if (request is null)
    {
      throw new ArgumentNullException(nameof(request));
    }

    var currentBatch = _batches.Last();
    if (currentBatch.BatchRequestSteps.Count == BatchSizeLimit)
    {
      currentBatch = new BatchRequestContent(_service);
      _batches.Add(currentBatch);
    }

    var id = currentBatch.AddBatchRequestStep(_requestInfoFunction(request));
    _originalData.Add(id, request);
  }

  public async Task ExecuteWithRetryAsync()
  {
    foreach (var batch in _batches)
    {
      if (batch.BatchRequestSteps.Count == 0) continue;
      var current = batch;

      for (var attempt = 1; attempt <= MaxAttempts; attempt++) {
        var stepsToRetry = current.BatchRequestSteps.Select(o => o.Key).ToList();
        List<KeyValuePair<string, HttpResponseMessage>> responses = null;
        var wait = RetryFirst * (int)Math.Pow(RetryMultiplier, attempt - 1);
        try
        {
          var result = await _service.Batch.PostAsync(current);
          responses = (await result.GetResponsesAsync()).ToList();
          var failures = responses.Where(o => !o.Value.IsSuccessStatusCode);
          stepsToRetry = failures.Select(o => o.Key).ToList();
          if (!stepsToRetry.Any())
          {
            current.Dispose();
            break;
          }
          wait = (int)failures.Max(o => o.Value.Headers.RetryAfter?.Delta?.TotalMilliseconds ?? wait);
          throw new HttpRequestException("Batch requests failed.");
        }
        catch when (attempt < MaxAttempts)
        {
          current.Dispose();
          current = new BatchRequestContent(_service);
          foreach (var stepId in stepsToRetry)
          {
            var data = _originalData[stepId];
            var newId = current.AddBatchRequestStep(_requestInfoFunction(data));
            _originalData.Add(newId, data);
          }
          await Task.Delay(wait);
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