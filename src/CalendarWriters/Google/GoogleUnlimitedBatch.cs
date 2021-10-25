using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Requests;
using Google.Apis.Services;

namespace TimetableCalendarGenerator;

internal class GoogleUnlimitedBatch
{
  private readonly IClientService _service;
  private readonly IList<BatchRequest> _batches;

  public int BatchSizeLimit { get; }

  public GoogleUnlimitedBatch(IClientService service, int batchSizeLimit = 50)
  {
    if (batchSizeLimit <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(batchSizeLimit));
    }
    _service = service ?? throw new ArgumentNullException(nameof(service));
    _batches = new List<BatchRequest> { new(_service) };
    BatchSizeLimit = batchSizeLimit;
  }

  public void Queue<TResponse>(IClientServiceRequest request, BatchRequest.OnResponse<TResponse> callback) where TResponse : class
  {
    if (request is null)
    {
      throw new ArgumentNullException(nameof(request));
    }

    if (callback is null)
    {
      throw new ArgumentNullException(nameof(callback));
    }

    var currentBatch = _batches.Last();
    if (currentBatch.Count == BatchSizeLimit)
    {
      currentBatch = new BatchRequest(_service);
      _batches.Add(currentBatch);
    }

    currentBatch.Queue(request, callback);
  }

  public void Queue(IClientServiceRequest request)
  {
    Queue<object>(request, (_, _, _, _) => { });
  }

  public async Task ExecuteWithRetryAsync()
  {
    foreach (var batch in _batches)
    {
      if (batch.Count == 0) continue;
      await batch.ExecuteWithRetryAsync();
    }
  }
}