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
    private readonly IGraphServiceClient service;
    private readonly IList<BatchRequestContent> batches;

    private int counter = 0;

    private static readonly int batchSizeLimit = 20;
    private static readonly int maxAttempts = 4;
    private static readonly int retryFirst = 5000;
    private static readonly int retryMultiplier = 4;

    public int BatchCount => batches.Count;

    public MicrosoftUnlimitedBatch(IGraphServiceClient service)
    {
      this.service = service ?? throw new ArgumentNullException(nameof(service));
      batches = new List<BatchRequestContent> { new BatchRequestContent() };
    }

    public void Queue(HttpRequestMessage request)
    {
      if (request == null)
      {
        throw new ArgumentNullException(nameof(request));
      }

      var currentBatch = batches.Last();
      if (currentBatch.BatchRequestSteps.Count == batchSizeLimit)
      {
        currentBatch = new BatchRequestContent();
        batches.Add(currentBatch);
        counter = 0;
      }

      var step = new BatchRequestStep(counter.ToString(), request, counter == 0 ? null : new List<string> { (counter - 1).ToString() });
      currentBatch.AddBatchRequestStep(step);
      counter++;
    }

    public async Task ExecuteWithRetryAsync()
    {
      foreach (var batch in batches)
      {
        var current = batch;

        for (var attempt = 1; attempt <= maxAttempts; attempt++) {
          if (current.BatchRequestSteps.Count == 0) break;
          if (attempt > 1)
          {
            var backoff = retryFirst * (int)Math.Pow(retryMultiplier, attempt - 1);
            await Task.Delay(backoff);
          }
          var result = await service.Batch.Request().PostAsync(current);
          var responses = await result.GetResponsesAsync();

          current = new BatchRequestContent();
          var i = 0;
          foreach (var response in responses)
          {
            if (!response.Value.IsSuccessStatusCode)
            {
              var step = new BatchRequestStep(i.ToString(), batch.BatchRequestSteps[response.Key].Request, i == 0 ? null : new List<string> { (i - 1).ToString() });
              current.AddBatchRequestStep(step);
              i++;
            }
            response.Value.Dispose();
          }
        }

      }
    }
  }
}
