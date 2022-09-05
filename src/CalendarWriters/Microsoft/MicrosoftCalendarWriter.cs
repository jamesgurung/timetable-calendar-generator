using System.Globalization;
using Microsoft.Graph;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using Microsoft.Graph.Beta.Users.Item;

namespace TimetableCalendarGenerator;

public class MicrosoftCalendarWriter : ICalendarWriter
{
  private const string Tag = "timetable-calendar-generator";

  private const string CategoryName = "Timetable";
  private const CategoryColor CategoryColour = CategoryColor.Preset2;

  private const string DutyCategoryName = "Duty";
  private const CategoryColor DutyCategoryColour = CategoryColor.Preset10;

  private const string MeetingCategoryName = "Meeting";
  private const CategoryColor MeetingCategoryColour = CategoryColor.Preset4;

  private static readonly EventComparer<CalendarEvent> comparer = new(e => e.Start, e => e.End, e => e.Title, e => e.Location);

  private readonly GraphServiceClient _client;
  private readonly UserItemRequestBuilder _userClient;

  [CLSCompliant(false)]
  public MicrosoftCalendarWriter(string email, GraphServiceClient client)
  {
    _client = client;
    _userClient = _client.Users[email];
  }

  public async Task WriteAsync(IList<CalendarEvent> events)
  {
    await SetupCategoryAsync();
    var existingEvents = await GetExistingEventsAsync();

    var removedEvents = existingEvents.Except(events, comparer);
    var duplicateEvents = existingEvents.GroupBy(o => o, comparer).Where(g => g.Count() > 1).SelectMany(g => g.Skip(1));
    var eventsToDelete = removedEvents.Union(duplicateEvents, comparer).Cast<CalendarEventWithId>().OrderBy(o => o.Start);

    await DeleteEventsAsync(eventsToDelete.Select(o => o.Id).ToList());
    await AddEventsAsync(events.Except(existingEvents, comparer).OrderBy(o => o.Start));
  }

  private async Task SetupCategoryAsync()
  {
    var categories = await _userClient.Outlook.MasterCategories.GetAsync(config =>
    {
      config.QueryParameters.Top = 999;
      config.QueryParameters.Select = new[] { "DisplayName" };
    });
    if (!categories.Value.Any(o => string.Equals(o.DisplayName, CategoryName, StringComparison.OrdinalIgnoreCase)))
    {
      var category = new OutlookCategory { DisplayName = CategoryName, Color = CategoryColour };
      await _userClient.Outlook.MasterCategories.PostAsync(category);
    }
    if (!categories.Value.Any(o => string.Equals(o.DisplayName, DutyCategoryName, StringComparison.OrdinalIgnoreCase)))
    {
      var dutyCategory = new OutlookCategory { DisplayName = DutyCategoryName, Color = DutyCategoryColour };
      await _userClient.Outlook.MasterCategories.PostAsync(dutyCategory);
    }
    if (!categories.Value.Any(o => string.Equals(o.DisplayName, MeetingCategoryName, StringComparison.OrdinalIgnoreCase)))
    {
      var meetingCategory = new OutlookCategory { DisplayName = MeetingCategoryName, Color = MeetingCategoryColour };
      await _userClient.Outlook.MasterCategories.PostAsync(meetingCategory);
    }
  }

  private async Task<IList<CalendarEventWithId>> GetExistingEventsAsync()
  {
    var events = new List<Event>();
    var response = await _userClient.Calendar.Events.GetAsync(config =>
    {
      config.QueryParameters.Top = 999;
      config.Headers.Add("Prefer", "outlook.timezone=\"Europe/London\"");
      config.QueryParameters.Filter = $"Start/DateTime gt '{DateTime.Today:s}' and Extensions/any(f:f/id eq '{Tag}')";
      config.QueryParameters.Select = new[] { "Id", "Start", "End", "Subject", "Location" };
    });
    var iterator = PageIterator<Event, EventCollectionResponse>.CreatePageIterator(_client, response,
      ev => { events.Add(ev); return true; },
      config => { config.Headers.Add("Prefer", "outlook.timezone=\"Europe/London\""); return config; }
    );
    await iterator.IterateAsync();
    return events.Select(ev => new CalendarEventWithId() {
      Id = ev.Id,
      Title = ev.Subject,
      Location = ev.Location.DisplayName,
      Start = ev.Start?.DateTime is null ? default : DateTime.ParseExact(ev.Start.DateTime[..19], "s", CultureInfo.InvariantCulture),
      End = ev.End?.DateTime is null ? default : DateTime.ParseExact(ev.End.DateTime[..19], "s", CultureInfo.InvariantCulture)
    }).ToList();
  }

  private async Task DeleteEventsAsync(IEnumerable<string> eventIds)
  {
    if (!eventIds.Any()) return;
    using var deleteBatch = new MicrosoftUnlimitedBatch<string>(_client, id => _userClient.Events[id].CreateDeleteRequestInformation());
    foreach (var id in eventIds)
    {
      deleteBatch.Queue(id);
    }
    await deleteBatch.ExecuteWithRetryAsync();
  }

  private async Task AddEventsAsync(IEnumerable<CalendarEvent> events)
  {
    if (!events.Any()) return;
    using var insertBatch = new MicrosoftUnlimitedBatch<CalendarEvent>(_client, o =>
    {
      var isDuty = o.Title.Contains("duty", StringComparison.OrdinalIgnoreCase) || o.Title.Contains("duties", StringComparison.OrdinalIgnoreCase);
      var isMeeting = o.Title.Contains("meet", StringComparison.OrdinalIgnoreCase) || o.Title.Contains("line management", StringComparison.OrdinalIgnoreCase)
        || o.Title.Contains("brief", StringComparison.OrdinalIgnoreCase) || o.Title.Contains("mentor", StringComparison.OrdinalIgnoreCase)
        || o.Title.Contains("LMM", StringComparison.OrdinalIgnoreCase);
      var ev = new Event
      {
        Subject = o.Title,
        Location = new Location { DisplayName = o.Location },
        Start = new DateTimeTimeZone { DateTime = o.Start.ToString("s"), TimeZone = "Europe/London" },
        End = new DateTimeTimeZone { DateTime = o.End.ToString("s"), TimeZone = "Europe/London" },
        Extensions = new() { new OpenTypeExtension { ExtensionName = Tag } },
        Categories = new() { isDuty ? DutyCategoryName : (isMeeting ? MeetingCategoryName : CategoryName) },
        IsReminderOn = isDuty
      };
      return _userClient.Calendar.Events.CreatePostRequestInformation(ev);
    });
    foreach (var ev in events)
    {
      insertBatch.Queue(ev);
    }
    await insertBatch.ExecuteWithRetryAsync();
  }

  private class CalendarEventWithId : CalendarEvent
  {
    public string Id { get; set; }
  }
}