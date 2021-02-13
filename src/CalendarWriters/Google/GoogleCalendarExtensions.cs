using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Requests;

namespace makecal
{
  public static class GoogleCalendarExtensions
  {
    private static readonly int maxAttempts = 4;
    private static readonly int retryFirst = 5000;
    private static readonly int retryMultiplier = 4;

    private static readonly int maxPageSize = 2500;

    private static async Task<TResponse> ExecuteWithRetryAsync<TResponse>(Func<Task<TResponse>> func)
    {
      TResponse response = default;
      for (var attempt = 1; attempt <= maxAttempts; attempt++)
      {
        try
        {
          response = await func();
          break;
        }
        catch (Google.GoogleApiException) when (attempt < maxAttempts)
        {
          var backoff = retryFirst * (int)Math.Pow(retryMultiplier, attempt - 1);
          await Task.Delay(backoff);
        }
      }
      return response;
    }

    public static async Task<IList<Event>> FetchAllWithRetryAsync(this EventsResource.ListRequest listRequest, DateTime? after = null, DateTime? before = null)
    {
      listRequest.TimeMin = after;
      listRequest.TimeMax = before;
      listRequest.MaxResults = maxPageSize;
      var pageStreamer = new PageStreamer<Event, EventsResource.ListRequest, Events, string>(
        (request, token) => request.PageToken = token,
        response => response.NextPageToken,
        response => response.Items
      );
      return await ExecuteWithRetryAsync(() => pageStreamer.FetchAllAsync(listRequest, CancellationToken.None));
    }

    public static async Task ExecuteWithRetryAsync(this BatchRequest request)
      => await ExecuteWithRetryAsync(async () => { await request.ExecuteAsync(); return 0; });

  }
}
