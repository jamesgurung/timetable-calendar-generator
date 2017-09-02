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

    public static async Task<IList<Event>> FetchAllAsync(this EventsResource.ListRequest listRequest, DateTime? after = null, DateTime? before = null)
    {
      listRequest.TimeMin = after;
      listRequest.TimeMax = before;
      var pageStreamer = new PageStreamer<Event, EventsResource.ListRequest, Events, string>((request, token) => request.PageToken = token, response => response.NextPageToken, response => response.Items);
      return await pageStreamer.FetchAllAsync(listRequest, CancellationToken.None);
    }

    public static CalendarListResource.PatchRequest SetColor(this CalendarListResource calendarList, string calendarId, string color)
    {
      var calListEntry = new CalendarListEntry { BackgroundColor = color };
      var setColourRequest = calendarList.Patch(calListEntry, calendarId);
      setColourRequest.ColorRgbFormat = true;
      return setColourRequest;
    }

  }
}