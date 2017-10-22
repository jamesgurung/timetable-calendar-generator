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

    public async Task PrepareAsync()
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
        insertBatch.Queue(Service.Events.Insert(googleCalendarEvent, CalendarId));
      }
      await insertBatch.ExecuteAsync();
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
      var calendars = await Service.CalendarList.List().ExecuteAsync();
      return calendars.Items.FirstOrDefault(o => o.Summary == calendarName)?.Id;
    }

    private async Task CreateNewCalendarAsync()
    {
      var newCalendar = new Calendar { Summary = calendarName };
      newCalendar = await Service.Calendars.Insert(newCalendar).ExecuteAsync();
      CalendarId = newCalendar.Id;
      await Service.CalendarList.SetColor(CalendarId, calendarColor).ExecuteAsync();
    }

    private async Task DeleteExistingEventsAsync()
    {
      var existingFutureEvents = await Service.Events.List(CalendarId).FetchAllAsync(after: DateTime.Today);
      var deleteBatch = new UnlimitedBatch(Service);
      foreach (var existingEvent in existingFutureEvents)
      {
        deleteBatch.Queue(Service.Events.Delete(CalendarId, existingEvent.Id));
      }
      await deleteBatch.ExecuteAsync();
    }
  }
}