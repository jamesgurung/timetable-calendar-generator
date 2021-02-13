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

    public int BatchSizeLimit { get; private set; }
    public int BatchCount => _batches.Count;

    public MicrosoftUnlimitedBatch(IGraphServiceClient service, int batchSizeLimit = 20)
    {
      if (batchSizeLimit <= 0)
      {
        throw new ArgumentOutOfRangeException(nameof(batchSizeLimit));
      }
      _service = service ?? throw new ArgumentNullException(nameof(service));
      _batches = new List<BatchRequestContent> { new BatchRequestContent() };
      BatchSizeLimit = batchSizeLimit;
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
      }

      currentBatch.AddBatchRequestStep(request);
    }

    public async Task ExecuteAsync()
    {
      foreach (var batch in _batches)
      {
        if (batch.BatchRequestSteps.Count == 0) continue;
        await _service.Batch.Request().PostAsync(batch);
      }
    }
  }
}
