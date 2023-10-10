using Google.Apis.Requests;
using Google.Apis.Services;

namespace TimetableCalendarGenerator;

internal sealed class GoogleUnlimitedBatch
{
  private readonly IClientService _service;
  private readonly List<BatchRequest> _batches;

  public int BatchSizeLimit { get; }

  public GoogleUnlimitedBatch(IClientService service, int batchSizeLimit = 50)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSizeLimit);
    _service = service ?? throw new ArgumentNullException(nameof(service));
    _batches = [new(_service)];
    BatchSizeLimit = batchSizeLimit;
  }

  public void Queue<TResponse>(IClientServiceRequest request, BatchRequest.OnResponse<TResponse> callback) where TResponse : class
  {
    ArgumentNullException.ThrowIfNull(request);
    ArgumentNullException.ThrowIfNull(callback);

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