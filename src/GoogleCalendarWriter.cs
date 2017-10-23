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
    private string CalendarId { get; set; }

    public GoogleCalendarWriter(string email, string serviceAccountKey)
    {
      Service = GetCalendarService(serviceAccountKey, email);
    }

    private async Task PrepareAsync()
    {
      CalendarId = await GetCalendarIdAsync();

      if (CalendarId == null)
      {
        await CreateNewCalendarAsync();
      }
      else
      {
        await DeleteExistingEventsAsync();
      }
    }

    public async Task WriteAsync(IList<CalendarEvent> events)
    {
      await PrepareAsync();

      var insertBatch = new UnlimitedBatch(Service);
      foreach (var calendarEvent in events)
      {
        var googleCalendarEvent = new Event
        {
          Summary = calendarEvent.Title,
          Location = calendarEvent.Location,
          Start = new EventDateTime { DateTime = calendarEvent.Start },
          End = new EventDateTime { DateTime = calendarEvent.End }
        };
        var insertRequest = Service.Events.Insert(googleCalendarEvent, CalendarId);
        insertRequest.Fields = "id";
        insertBatch.Queue(insertRequest);
      }
      await insertBatch.ExecuteWithRetryAsync();
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

    private async Task CreateNewCalendarAsync()
    {
      var newCalendar = new Calendar { Summary = calendarName };
      var insertRequest = Service.Calendars.Insert(newCalendar);
      insertRequest.Fields = "id";
      newCalendar = await insertRequest.ExecuteWithRetryAsync();
      CalendarId = newCalendar.Id;
      await Service.CalendarList.SetColor(CalendarId, calendarColor).ExecuteWithRetryAsync();
    }

    private async Task DeleteExistingEventsAsync()
    {
      var listRequest = Service.Events.List(CalendarId);
      listRequest.Fields = "items(id),nextPageToken";
      var existingFutureEvents = await listRequest.FetchAllWithRetryAsync(after: DateTime.Today);
      var deleteBatch = new UnlimitedBatch(Service);
      foreach (var existingEvent in existingFutureEvents)
      {
        deleteBatch.Queue(Service.Events.Delete(CalendarId, existingEvent.Id));
      }
      await deleteBatch.ExecuteWithRetryAsync();
    }
  }
}