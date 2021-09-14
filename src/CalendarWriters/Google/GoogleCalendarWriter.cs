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
  public class GoogleCalendarWriter : ICalendarWriter, IDisposable
  {
    private const string CalendarId = "primary";
    private const string AppName = "makecal";

    private const string EventColour = "5";
    private const string DutyEventColour = "8";
    private const string MeetingEventColour = "2";

    private static readonly Event.ExtendedPropertiesData eventProperties = new()
    {
      Private__ = new Dictionary<string, string> { { AppName, "true" } }
    };

    private static readonly EventComparer<Event> comparer = new(e => e.Start?.DateTime, e => e.End?.DateTime, e => e.Summary, e => e.Location);

    private readonly CalendarService _service;
    private bool _disposedValue;

    public GoogleCalendarWriter(string email, string serviceAccountKey)
    {
      _service = GetCalendarService(serviceAccountKey, email);
    }

    public async Task WriteAsync(IList<CalendarEvent> events)
    {
      var existingEvents = await GetExistingEventsAsync();
      
      var expectedEvents = events.Select(o => new Event
      {
        Summary = o.Title,
        Location = o.Location,
        Start = new EventDateTime { DateTime = o.Start },
        End = new EventDateTime { DateTime = o.End }
      }).ToList();

      await DeleteEventsAsync(existingEvents.Except(expectedEvents, comparer));
      await AddEventsAsync(expectedEvents.Except(existingEvents, comparer));
    }

    private static CalendarService GetCalendarService(string serviceAccountKey, string email)
    {
      var credential = GoogleCredential.FromJson(serviceAccountKey).CreateScoped(CalendarService.Scope.Calendar).CreateWithUser(email);

      return new CalendarService(new BaseClientService.Initializer
      {
        HttpClientInitializer = credential,
        ApplicationName = AppName
      });
    }

    private async Task<IList<Event>> GetExistingEventsAsync()
    {
      var listRequest = _service.Events.List(CalendarId);
      listRequest.PrivateExtendedProperty = $"{AppName}=true";
      listRequest.Fields = "items(id,summary,location,start(dateTime),end(dateTime)),nextPageToken";
      return await listRequest.FetchAllWithRetryAsync(after: DateTime.Today);
    }

    private async Task DeleteEventsAsync(IEnumerable<Event> events)
    {
      var deleteBatch = new GoogleUnlimitedBatch(_service);
      foreach (var ev in events)
      {
        deleteBatch.Queue(_service.Events.Delete(CalendarId, ev.Id));
      }
      await deleteBatch.ExecuteWithRetryAsync();
    }

    private async Task AddEventsAsync(IEnumerable<Event> events)
    {
      var insertBatch = new GoogleUnlimitedBatch(_service);
      foreach (var ev in events)
      {
        var isDuty = ev.Summary.Contains("duty", StringComparison.OrdinalIgnoreCase) || ev.Summary.Contains("duties", StringComparison.OrdinalIgnoreCase);
        var isMeeting = ev.Summary.Contains("meet", StringComparison.OrdinalIgnoreCase) || ev.Summary.Contains("line management", StringComparison.OrdinalIgnoreCase);
        ev.ColorId = isDuty ? DutyEventColour : (isMeeting ? MeetingEventColour : EventColour);
        ev.Reminders = new Event.RemindersData { UseDefault = isDuty };
        ev.ExtendedProperties = eventProperties;
        var insertRequest = _service.Events.Insert(ev, CalendarId);
        insertRequest.Fields = "id";
        insertBatch.Queue(insertRequest);
      }
      await insertBatch.ExecuteWithRetryAsync();
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!_disposedValue)
      {
        if (disposing)
        {
          _service?.Dispose();
        }
        _disposedValue = true;
      }
    }

    public void Dispose()
    {
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }
  }
}
