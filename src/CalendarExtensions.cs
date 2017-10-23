using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Requests;

namespace makecal
{
  public static class CalendarExtensions
  {
    private static readonly int maxAttempts = 4;
    private static readonly int retryFirst = 5000;
    private static readonly int retryExponent = 4;

    public static async Task<IList<Event>> FetchAllWithRetryAsync(this EventsResource.ListRequest listRequest, DateTime? after = null, DateTime? before = null)
    {
      listRequest.TimeMin = after;
      listRequest.TimeMax = before;
      var pageStreamer = new PageStreamer<Event, EventsResource.ListRequest, Events, string>((request, token) => request.PageToken = token, response => response.NextPageToken, response => response.Items);
      IList<Event> events = null;
      for (var attempt = 1; attempt <= maxAttempts; attempt++)
      {
        try
        {
          events = await pageStreamer.FetchAllAsync(listRequest, CancellationToken.None);
          break;
        }
        catch (Google.GoogleApiException) when (attempt < maxAttempts)
        {
          var backoff = retryFirst * (int)Math.Pow(retryExponent, attempt - 1);
          await Task.Delay(backoff);
        }
      }
      return events;
    }

    public static CalendarListResource.PatchRequest SetColor(this CalendarListResource calendarList, string calendarId, string colorId)
    {
      var calListEntry = new CalendarListEntry { ColorId = colorId };
      var setColourRequest = calendarList.Patch(calListEntry, calendarId);
      return setColourRequest;
    }

    public static async Task<TResponse> ExecuteWithRetryAsync<TResponse>(this IClientServiceRequest<TResponse> request)
    {
      TResponse response = default;
      for (var attempt = 1; attempt <= maxAttempts; attempt++)
      {
        try
        {
          response = await request.ExecuteAsync();
          break;
        }
        catch (Google.GoogleApiException) when (attempt < maxAttempts)
        {
          var backoff = retryFirst * (int)Math.Pow(retryExponent, attempt - 1);
          await Task.Delay(backoff);
        }
      }
      return response;
    }

    public static async Task ExecuteWithRetryAsync(this BatchRequest request)
    {
      for (var attempt = 1; attempt <= maxAttempts; attempt++)
      {
        try
        {
          await request.ExecuteAsync();
          return;
        }
        catch (Google.GoogleApiException) when (attempt < maxAttempts)
        {
          var backoff = retryFirst * (int)Math.Pow(retryExponent, attempt - 1);
          await Task.Delay(backoff);
        }
      }
    }

  }
}
