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
    private static readonly string calendarColor = "5";
    private static readonly EventComparer<Event> comparer = new(e => e.Start?.DateTime?.ToString("s"), e => e.End?.DateTime?.ToString("s"), e => e.Summary, e => e.Location);

    private readonly CalendarService service;
    private readonly bool removeCalendars;

    public GoogleCalendarWriter(string email, string serviceAccountKey, bool removeCalendars)
    {
      service = GetCalendarService(serviceAccountKey, email);
      this.removeCalendars = removeCalendars;
    }

    public async Task WriteAsync(IList<CalendarEvent> events)
    {
      if (removeCalendars)
      {
        await DeleteTimetableCalendarAsync();
        return;
      }

      var calendarId = await GetCalendarIdAsync();
      var existingEvents = await GetExistingEventsAsync(calendarId);
      if (calendarId is null)
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

      await DeleteEventsAsync(calendarId, existingEvents.Except(expectedEvents, comparer));
      await AddEventsAsync(calendarId, expectedEvents.Except(existingEvents, comparer));
    }

    public async Task DeleteTimetableCalendarAsync()
    {
      var calendarId = await GetCalendarIdAsync();
      if (calendarId is null)
      {
        return;
      }
      await service.Calendars.Delete(calendarId).ExecuteWithRetryAsync();
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
      var listRequest = service.CalendarList.List();
      listRequest.Fields = "items(id,summary)";
      var calendars = await listRequest.ExecuteWithRetryAsync();
      return calendars.Items.FirstOrDefault(o => o.Summary == calendarName)?.Id;
    }

    private async Task<string> CreateNewCalendarAsync()
    {
      var newCalendar = new Calendar { Summary = calendarName };
      var insertRequest = service.Calendars.Insert(newCalendar);
      insertRequest.Fields = "id";
      newCalendar = await insertRequest.ExecuteWithRetryAsync();
      await service.CalendarList.SetColor(newCalendar.Id, calendarColor).ExecuteWithRetryAsync();
      return newCalendar.Id;
    }

    private async Task<IList<Event>> GetExistingEventsAsync(string calendarId)
    {
      if (calendarId == null)
      {
        return new List<Event>();
      }
      var listRequest = service.Events.List(calendarId);
      listRequest.Fields = "items(id,summary,location,start(dateTime),end(dateTime)),nextPageToken";
      return await listRequest.FetchAllWithRetryAsync(after: DateTime.Today);
    }

    private async Task DeleteEventsAsync(string calendarId, IEnumerable<Event> events)
    {
      var deleteBatch = new GoogleUnlimitedBatch(service);
      foreach (var ev in events)
      {
        deleteBatch.Queue(service.Events.Delete(calendarId, ev.Id));
      }
      await deleteBatch.ExecuteWithRetryAsync();
    }

    private async Task AddEventsAsync(string calendarId, IEnumerable<Event> events)
    {
      var insertBatch = new GoogleUnlimitedBatch(service);
      foreach (var ev in events)
      {
        var insertRequest = service.Events.Insert(ev, calendarId);
        insertRequest.Fields = "id";
        insertBatch.Queue(insertRequest);
      }
      await insertBatch.ExecuteWithRetryAsync();
    }
  }
}
