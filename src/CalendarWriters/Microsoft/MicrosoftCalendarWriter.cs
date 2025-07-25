﻿using System.Globalization;
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

  private static readonly EventComparer<CalendarEvent> Comparer = new(e => e.Start, e => e.End, e => e.Title, e => e.Location);
  private static readonly string[] DisplayName = ["DisplayName"];
  private static readonly string[] SelectFields = ["Id", "Start", "End", "Subject", "Location"];
  private static readonly string[] MeetingKeywords = ["meet", "line management", "brief", "mentor", "lmm", "review", "planning", "discuss"];
  private static readonly string[] DutyKeywords = ["duty", "duties"];

  private readonly GraphServiceClient _client;
  private readonly UserItemRequestBuilder _userClient;
  private readonly DateTime _startDate;
  private readonly DateTime _endDate;

  [CLSCompliant(false)]
  public MicrosoftCalendarWriter(string email, GraphServiceClient client, DateTime startDate, DateTime endDate)
  {
    _client = client;
    _userClient = _client.Users[email];
    _startDate = startDate;
    _endDate = endDate;
  }

  public async Task WriteAsync(IList<CalendarEvent> events)
  {
    await SetupCategoryAsync();
    List<CalendarEvent> eventsToAdd;
    do
    {
      var existingEvents = await GetExistingEventsAsync();
      var removedEvents = existingEvents.Except(events, Comparer);
      var duplicateEvents = existingEvents.GroupBy(o => o, Comparer).Where(g => g.Count() > 1).SelectMany(g => g.Skip(1));
      var eventsToDelete = removedEvents.Union(duplicateEvents, Comparer).Cast<CalendarEventWithId>().OrderBy(o => o.Start).Select(o => o.Id).ToList();
      eventsToAdd = [.. events.Except(existingEvents, Comparer).OrderBy(o => o.Start)];

      await DeleteEventsAsync(eventsToDelete);
      await AddEventsAsync(eventsToAdd);
    }
    while (eventsToAdd.Count > 0);
    /* Graph API sometimes returns 503 Service Unavailable errors but adds the events anyway.
     * This can result in events being added twice, so we re-run the sync to remove duplicates. */
  }

  private async Task SetupCategoryAsync()
  {
    var categories = await _userClient.Outlook.MasterCategories.GetAsync(config =>
    {
      config.QueryParameters.Top = 999;
      config.QueryParameters.Select = DisplayName;
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
      config.QueryParameters.Filter = $"Start/DateTime gt '{_startDate:s}' and Start/DateTime lt '{_endDate:s}' and Extensions/any(f:f/id eq '{Tag}')";
      config.QueryParameters.Select = SelectFields;
    });
    var iterator = PageIterator<Event, EventCollectionResponse>.CreatePageIterator(_client, response,
      ev => { events.Add(ev); return true; },
      config => { config.Headers.Add("Prefer", "outlook.timezone=\"Europe/London\""); return config; }
    );
    await iterator.IterateAsync();
    return [.. events.Select(ev => new CalendarEventWithId
    {
      Id = ev.Id,
      Title = ev.Subject,
      Location = ev.Location.DisplayName,
      Start = ev.Start?.DateTime is null ? default : DateTime.ParseExact(ev.Start.DateTime[..19], "s", CultureInfo.InvariantCulture),
      End = ev.End?.DateTime is null ? default : DateTime.ParseExact(ev.End.DateTime[..19], "s", CultureInfo.InvariantCulture)
    })];
  }

  private async Task DeleteEventsAsync(IList<string> eventIds)
  {
    if (!eventIds.Any()) return;
    using var deleteBatch = new MicrosoftUnlimitedBatch<string>(_client, id => _userClient.Events[id].ToDeleteRequestInformation());
    foreach (var id in eventIds)
    {
      await deleteBatch.QueueAsync(id);
    }
    await deleteBatch.ExecuteWithRetryAsync();
  }

  private async Task AddEventsAsync(IList<CalendarEvent> events)
  {
    if (!events.Any()) return;
    using var insertBatch = new MicrosoftUnlimitedBatch<CalendarEvent>(_client, o =>
    {
      var isDuty = DutyKeywords.Any(word => o.Title.Contains(word, StringComparison.OrdinalIgnoreCase));
      var isMeeting = MeetingKeywords.Any(word => o.Title.Contains(word, StringComparison.OrdinalIgnoreCase));
      var ev = new Event
      {
        Subject = o.Title,
        Location = new Location { DisplayName = o.Location },
        Start = new DateTimeTimeZone { DateTime = o.Start.ToString("s"), TimeZone = "Europe/London" },
        End = new DateTimeTimeZone { DateTime = o.End.ToString("s"), TimeZone = "Europe/London" },
        Extensions = [new OpenTypeExtension { ExtensionName = Tag }],
        Categories = [isDuty ? DutyCategoryName : (isMeeting ? MeetingCategoryName : CategoryName)],
        IsReminderOn = isDuty
      };
      return _userClient.Calendar.Events.ToPostRequestInformation(ev);
    });
    foreach (var ev in events)
    {
      await insertBatch.QueueAsync(ev);
    }
    await insertBatch.ExecuteWithRetryAsync();
  }

  private sealed class CalendarEventWithId : CalendarEvent
  {
    public string Id { get; init; }
  }
}