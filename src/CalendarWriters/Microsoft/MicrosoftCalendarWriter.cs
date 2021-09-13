using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Azure.Identity;

namespace makecal
{
  public class MicrosoftCalendarWriter : ICalendarWriter
  {
    private const string Tag = "timetable-calendar-generator";

    private const string CategoryName = "Timetable";
    private const CategoryColor CategoryColour = CategoryColor.Preset2;

    private const string DutyCategoryName = "Duty";
    private const CategoryColor DutyCategoryColour = CategoryColor.Preset10;

    private const string MeetingCategoryName = "Meeting";
    private const CategoryColor MeetingCategoryColour = CategoryColor.Preset4;

    private static readonly EventExtensionsCollectionPage extensions = new() { new OpenTypeExtension { ExtensionName = Tag } };

    private static readonly EventComparer<Event> comparer = new(
      e => e.Start?.DateTime is null ? null : DateTime.ParseExact(e.Start.DateTime[..19], "s", CultureInfo.InvariantCulture),
      e => e.End?.DateTime is null ? null : DateTime.ParseExact(e.End.DateTime[..19], "s", CultureInfo.InvariantCulture),
      e => e.Subject,
      e => e.Location.DisplayName
    );

    private readonly GraphServiceClient _client;
    private readonly IUserRequestBuilder _userClient;
    private readonly Serializer _serializer = new();

    public MicrosoftCalendarWriter(string email, MicrosoftClientKey clientKey)
    {
      var credential = new ClientSecretCredential(clientKey.TenantId, clientKey.ClientId, clientKey.ClientSecret);
      _client = new GraphServiceClient(credential);
      _userClient = _client.Users[email];
    }

    public async Task WriteAsync(IList<CalendarEvent> events)
    {
      await SetupCategoryAsync();
      var existingEvents = await GetExistingEventsAsync();
      
      var expectedEvents = events.Select(o => new Event
      {
        Subject = o.Title,
        Location = new Location { DisplayName = o.Location },
        Start = new DateTimeTimeZone { DateTime = o.Start.ToString("s"), TimeZone = "Europe/London" },
        End = new DateTimeTimeZone { DateTime = o.End.ToString("s"), TimeZone = "Europe/London" }
      }).ToList();

      await DeleteEventsAsync(existingEvents.Except(expectedEvents, comparer));
      await AddEventsAsync(expectedEvents.Except(existingEvents, comparer));
    }

    private async Task SetupCategoryAsync()
    {
      var categories = await _userClient.Outlook.MasterCategories.Request().GetAsync();
      if (!categories.Any(o => o.DisplayName.ToLowerInvariant() == CategoryName.ToLowerInvariant()))
      {
        var category = new OutlookCategory { DisplayName = CategoryName, Color = CategoryColour };
        await _userClient.Outlook.MasterCategories.Request().AddAsync(category);
      }
      if (!categories.Any(o => o.DisplayName.ToLowerInvariant() == DutyCategoryName.ToLowerInvariant()))
      {
        var dutyCategory = new OutlookCategory { DisplayName = DutyCategoryName, Color = DutyCategoryColour };
        await _userClient.Outlook.MasterCategories.Request().AddAsync(dutyCategory);
      }
      if (!categories.Any(o => o.DisplayName.ToLowerInvariant() == MeetingCategoryName.ToLowerInvariant()))
      {
        var meetingCategory = new OutlookCategory { DisplayName = MeetingCategoryName, Color = MeetingCategoryColour };
        await _userClient.Outlook.MasterCategories.Request().AddAsync(meetingCategory);
      }
    }

    private async Task<IList<Event>> GetExistingEventsAsync()
    {
      var events = new List<Event>();
      var request = _userClient.Calendar.Events.Request()
        .Filter($"Start/DateTime gt '{DateTime.Today:s}' and Extensions/any(f:f/id eq '{Tag}')")
        .Select("Id,Start,End,Subject,Location");
      do
      {
        request.Headers.Add(new HeaderOption("Prefer", "outlook.timezone=\"Europe/London\""));
        var response = await request.GetAsync();
        events.AddRange(response.CurrentPage);
        request = response.NextPageRequest;
      } while (request is not null);

      return events;
    }

    private async Task DeleteEventsAsync(IEnumerable<Event> events)
    {
      var deleteBatch = new MicrosoftUnlimitedBatch(_client);
      foreach (var ev in events)
      {
        var deleteRequest = _userClient.Events[ev.Id].Request().GetHttpRequestMessage();
        deleteRequest.Method = HttpMethod.Delete;
        deleteBatch.Queue(deleteRequest);
      }
      await deleteBatch.ExecuteWithRetryAsync();
    }

    private async Task AddEventsAsync(IEnumerable<Event> events)
    {
      var insertBatch = new MicrosoftUnlimitedBatch(_client);
      foreach (var ev in events)
      {
        ev.Extensions = extensions;
        var isDuty = ev.Subject.Contains("duty", StringComparison.OrdinalIgnoreCase) || ev.Subject.Contains("duties", StringComparison.OrdinalIgnoreCase);
        var isMeeting = ev.Subject.Contains("meet", StringComparison.OrdinalIgnoreCase) || ev.Subject.Contains("line management", StringComparison.OrdinalIgnoreCase);
        ev.Categories = new[] { isDuty ? DutyCategoryName : (isMeeting ? MeetingCategoryName : CategoryName) };
        ev.IsReminderOn = isDuty;
        var insertRequest = _userClient.Calendar.Events.Request().Select("Id").GetHttpRequestMessage();
        insertRequest.Method = HttpMethod.Post;
        insertRequest.Content = _serializer.SerializeAsJsonContent(ev);
        insertBatch.Queue(insertRequest);
      }
      await insertBatch.ExecuteWithRetryAsync();
    }
  }
}
