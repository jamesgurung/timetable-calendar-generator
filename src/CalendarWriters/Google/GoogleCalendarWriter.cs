using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;

namespace TimetableCalendarGenerator;

public class GoogleCalendarWriter(string email, string serviceAccountKey, DateTime startDate, DateTime endDate) : ICalendarWriter, IDisposable
{
  private const string CalendarId = "primary";
  private const string AppName = "makecal";

  private const string EventColour = "5";
  private const string DutyEventColour = "8";
  private const string MeetingEventColour = "2";

  private static readonly string[] MeetingKeywords = ["meet", "line management", "brief", "mentor", "lmm", "review", "planning", "discuss"];
  private static readonly string[] DutyKeywords = ["duty", "duties"];

  private static readonly Event.ExtendedPropertiesData EventProperties = new()
  {
    Private__ = new Dictionary<string, string> { { AppName, "true" } }
  };

  private static readonly EventComparer<Event> Comparer = new(e => e.Start?.DateTimeDateTimeOffset, e => e.End?.DateTimeDateTimeOffset, e => e.Summary, e => e.Location);

  private readonly CalendarService _service = GetCalendarService(serviceAccountKey, email);
  private bool _disposedValue;

  public async Task WriteAsync(IList<CalendarEvent> events)
  {
    var existingEvents = await GetExistingEventsAsync();

    var expectedEvents = events.Select(o => new Event
    {
      Summary = o.Title,
      Location = o.Location,
      Start = new EventDateTime { DateTimeDateTimeOffset = o.Start },
      End = new EventDateTime { DateTimeDateTimeOffset = o.End }
    }).ToList();

    var removedEvents = existingEvents.Except(expectedEvents, Comparer);
    var duplicateEvents = existingEvents.GroupBy(o => o, Comparer).Where(g => g.Count() > 1).SelectMany(g => g.Skip(1));
    var eventsToDelete = removedEvents.Union(duplicateEvents, Comparer).OrderBy(o => o.Start?.DateTimeDateTimeOffset);

    await DeleteEventsAsync(eventsToDelete);
    await AddEventsAsync(expectedEvents.Except(existingEvents, Comparer).OrderBy(o => o.Start?.DateTimeDateTimeOffset));
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
    return await listRequest.FetchAllWithRetryAsync(after: startDate, before: endDate);
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
      var isDuty = DutyKeywords.Any(word => ev.Summary.Contains(word, StringComparison.OrdinalIgnoreCase));
      var isMeeting = MeetingKeywords.Any(word => ev.Summary.Contains(word, StringComparison.OrdinalIgnoreCase));
      ev.ColorId = isDuty ? DutyEventColour : (isMeeting ? MeetingEventColour : EventColour);
      ev.Reminders = new Event.RemindersData { UseDefault = isDuty };
      ev.ExtendedProperties = EventProperties;
      var insertRequest = _service.Events.Insert(ev, CalendarId);
      insertRequest.Fields = "id";
      insertBatch.Queue(insertRequest);
    }
    await insertBatch.ExecuteWithRetryAsync();
  }

  protected virtual void Dispose(bool disposing)
  {
    if (_disposedValue) return;
    if (disposing)
    {
      _service?.Dispose();
    }
    _disposedValue = true;
  }

  public void Dispose()
  {
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }
}