using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace makecal
{
  public class MicrosoftUnlimitedBatch
  {
    private readonly IGraphServiceClient _service;
    private readonly IList<BatchRequestContent> _batches;

    private int _counter;

    private const int BatchSizeLimit = 20;
    private const int MaxAttempts = 4;
    private const int RetryFirst = 5000;
    private const int RetryMultiplier = 4;

    public MicrosoftUnlimitedBatch(IGraphServiceClient service)
    {
      _service = service ?? throw new ArgumentNullException(nameof(service));
      _batches = new List<BatchRequestContent> { new() };
    }

    public void Queue(HttpRequestMessage request)
    {
      if (request == null)
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

      var step = new BatchRequestStep(_counter.ToString(), request, _counter == 0 ? null : new List<string> { (_counter - 1).ToString() });
      currentBatch.AddBatchRequestStep(step);
      _counter++;
    }

    public async Task ExecuteWithRetryAsync()
    {
      foreach (var batch in _batches)
      {
        var current = batch;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++) {
          if (current.BatchRequestSteps.Count == 0) break;
          if (attempt > 1)
          {
            var backoff = RetryFirst * (int)Math.Pow(RetryMultiplier, attempt - 1);
            await Task.Delay(backoff);
          }
          var result = await _service.Batch.Request().PostAsync(current);
          var responses = await result.GetResponsesAsync();

          current = new BatchRequestContent();
          var i = 0;
          foreach (var (stepKey, stepResponse) in responses)
          {
            if (!stepResponse.IsSuccessStatusCode)
            {
              var step = new BatchRequestStep(i.ToString(), batch.BatchRequestSteps[stepKey].Request, i == 0 ? null : new List<string> { (i - 1).ToString() });
              current.AddBatchRequestStep(step);
              i++;
            }
            stepResponse.Dispose();
          }
        }

      }
    }
  }
}
