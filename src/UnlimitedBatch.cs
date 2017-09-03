using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Requests;
using Google.Apis.Services;

namespace makecal
{
  public class UnlimitedBatch
  {
    private readonly IClientService _service;
    private readonly IList<BatchRequest> _batches;

    public int BatchSizeLimit { get; private set; }
    public int BatchCount => _batches.Count;

    public UnlimitedBatch(IClientService service, int batchSizeLimit = 50)
    {
      if (batchSizeLimit <= 0)
      {
        throw new ArgumentOutOfRangeException(nameof(batchSizeLimit));
      }
      _service = service ?? throw new ArgumentNullException(nameof(service));
      _batches = new List<BatchRequest> { new BatchRequest(_service) };
      BatchSizeLimit = batchSizeLimit;
    }

    public void Queue<TResponse>(IClientServiceRequest request, BatchRequest.OnResponse<TResponse> callback) where TResponse : class
    {
      if (request == null)
      {
        throw new ArgumentNullException(nameof(request));
      }

      if (callback == null)
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
      Queue<object>(request, (content, error, index, message) => { });
    }

    public async Task ExecuteAsync()
    {
      foreach (var batch in _batches)
      {
        await batch.ExecuteAsync();
      }
    }
  }
}
