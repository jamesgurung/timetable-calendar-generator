using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;

namespace makecal
{
  class GoogleCalendarWriter : ICalendarWriter
  {
    private static readonly string appName = "makecal";
    private static readonly string calendarName = "My timetable";
    private static readonly string calendarColor = "15";

    private CalendarService Service { get; }

    public GoogleCalendarWriter(string email, string serviceAccountKey)
    {
      Service = GetCalendarService(serviceAccountKey, email);
    }

    public async Task WriteAsync(IList<CalendarEvent> events)
    {
      var calendarId = await GetCalendarIdAsync();
      var existingEvents = await GetExistingEventsAsync(calendarId);
      if (calendarId == null)
      {
        calendarId = await CreateNewCalendarAsync();
      }

      var expectedEvents = events.Select(o => new Event
      {
        Summary = o.Title,
        Location = o.Location,
        Start = new EventDateTime { DateTime = o.Start },
        End = new EventDateTime { DateTime = o.End }
      }).ToList();

      var comparer = new GoogleCalendarEventComparer();
      await DeleteEventsAsync(calendarId, existingEvents.Except(expectedEvents, comparer));
      await AddEventsAsync(calendarId, expectedEvents.Except(existingEvents, comparer));
    }

    private static CalendarService GetCalendarService(string serviceAccountKey, string email)
    {
      var credential = GoogleCredential.FromJson(serviceAccountKey).CreateScoped(CalendarService.Scope.Calendar).CreateWithUser(email);

      return new CalendarService(new BaseClientService.Initializer
      {
        HttpClientInitializer = credential,
        ApplicationName = appName
      });
    }

    private async Task<string> GetCalendarIdAsync()
    {
      var listRequest = Service.CalendarList.List();
      listRequest.Fields = "items(id,summary)";
      var calendars = await listRequest.ExecuteWithRetryAsync();
      return calendars.Items.FirstOrDefault(o => o.Summary == calendarName)?.Id;
    }

    private async Task<string> CreateNewCalendarAsync()
    {
      var newCalendar = new Calendar { Summary = calendarName };
      var insertRequest = Service.Calendars.Insert(newCalendar);
      insertRequest.Fields = "id";
      newCalendar = await insertRequest.ExecuteWithRetryAsync();
      await Service.CalendarList.SetColor(newCalendar.Id, calendarColor).ExecuteWithRetryAsync();
      return newCalendar.Id;
    }

    private async Task<IList<Event>> GetExistingEventsAsync(string calendarId)
    {
      if (calendarId == null)
      {
        return new List<Event>();
      }
      var listRequest = Service.Events.List(calendarId);
      listRequest.Fields = "items(id,summary,location,start(dateTime),end(dateTime)),nextPageToken";
      return await listRequest.FetchAllWithRetryAsync(after: DateTime.Today);
    }

    private async Task DeleteEventsAsync(string calendarId, IEnumerable<Event> events)
    {
      var deleteBatch = new UnlimitedBatch(Service);
      foreach (var ev in events)
      {
        deleteBatch.Queue(Service.Events.Delete(calendarId, ev.Id));
      }
      await deleteBatch.ExecuteWithRetryAsync();
    }

    private async Task AddEventsAsync(string calendarId, IEnumerable<Event> events)
    {
      var insertBatch = new UnlimitedBatch(Service);
      foreach (var ev in events)
      {
        var insertRequest = Service.Events.Insert(ev, calendarId);
        insertRequest.Fields = "id";
        insertBatch.Queue(insertRequest);
      }
      await insertBatch.ExecuteWithRetryAsync();
    }
  }
}
