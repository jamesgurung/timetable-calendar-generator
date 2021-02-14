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
    private const int MaxAttempts = 4;
    private const int RetryFirst = 5000;
    private const int RetryMultiplier = 4;
    private const int MaxPageSize = 2500;

    private static async Task<TResponse> ExecuteWithRetryAsync<TResponse>(Func<Task<TResponse>> func)
    {
      TResponse response = default;
      for (var attempt = 1; attempt <= MaxAttempts; attempt++)
      {
        try
        {
          response = await func();
          break;
        }
        catch (Google.GoogleApiException) when (attempt < MaxAttempts)
        {
          var backoff = RetryFirst * (int)Math.Pow(RetryMultiplier, attempt - 1);
          await Task.Delay(backoff);
        }
      }
      return response;
    }

    public static async Task<IList<Event>> FetchAllWithRetryAsync(this EventsResource.ListRequest listRequest, DateTime? after = null, DateTime? before = null)
    {
      listRequest.TimeMin = after;
      listRequest.TimeMax = before;
      listRequest.MaxResults = MaxPageSize;
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
