using Microsoft.Graph;
using Microsoft.Graph.Beta;
using Microsoft.Kiota.Abstractions;
using System.Net;

namespace TimetableCalendarGenerator;

internal class MicrosoftUnlimitedBatch<T> : IDisposable
{
  private readonly GraphServiceClient _service;
  private readonly List<BatchRequestContentCollection> _batches;
  private readonly Func<T, RequestInformation> _requestInfoFunction;
  private readonly Dictionary<string, T> _originalData = [];

  private const int BatchSizeLimit = 4; // Mailbox concurrency limit
  private const int MaxAttempts = 4;
  private const int RetryFirst = 1000;
  private const int RetryMultiplier = 4;

  public MicrosoftUnlimitedBatch(GraphServiceClient service, Func<T, RequestInformation> requestInfoFunction)
  {
    _service = service ?? throw new ArgumentNullException(nameof(service));
    _batches = [new(_service)];
    _requestInfoFunction = requestInfoFunction;
  }

  public async Task QueueAsync(T request)
  {
    if (request is null)
    {
      throw new ArgumentNullException(nameof(request));
    }

    var currentBatch = _batches.Last();
    if (currentBatch.BatchRequestSteps.Count == BatchSizeLimit)
    {
      currentBatch = new BatchRequestContentCollection(_service);
      _batches.Add(currentBatch);
    }

    var id = currentBatch.AddBatchRequestStep(await _service.RequestAdapter.ConvertToNativeRequestAsync<HttpRequestMessage>(_requestInfoFunction(request)));
    _originalData.Add(id, request);
  }

  public async Task ExecuteWithRetryAsync()
  {
    foreach (var batch in _batches)
    {
      if (batch.BatchRequestSteps.Count == 0) continue;
      var current = batch;

      for (var attempt = 1; attempt <= MaxAttempts; attempt++)
      {
        var stepsToRetry = current.BatchRequestSteps.Select(o => o.Key).ToList();
        List<KeyValuePair<string, HttpStatusCode>> responses = null;
        var wait = RetryFirst * (int)Math.Pow(RetryMultiplier, attempt - 1);
        try
        {
          var result = await _service.Batch.PostAsync(current);
          responses = [.. await result.GetResponsesStatusCodesAsync()];
          var failures = responses.Where(o => (int)o.Value is < 200 or > 299).ToList();
          stepsToRetry = [.. failures.Select(o => o.Key)];
          if (stepsToRetry.Count == 0)
          {
            break;
          }
          throw new HttpRequestException("Batch requests failed.");
        }
        catch when (attempt < MaxAttempts)
        {
          current = new BatchRequestContentCollection(_service);
          foreach (var stepId in stepsToRetry)
          {
            var data = _originalData[stepId];
            var newId = current.AddBatchRequestStep(await _service.RequestAdapter.ConvertToNativeRequestAsync<HttpRequestMessage>(_requestInfoFunction(data)));
            _originalData.Add(newId, data);
          }
          await Task.Delay(wait);
        }
      }

    }
  }

  public void Dispose()
  {
    GC.SuppressFinalize(this);
  }
}