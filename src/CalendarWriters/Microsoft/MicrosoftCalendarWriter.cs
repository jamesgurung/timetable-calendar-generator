using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;

namespace makecal
{
  public class MicrosoftCalendarWriter : ICalendarWriter
  {
    private const string Tag = "timetable-calendar-generator";
    private static readonly EventComparer<Event> comparer = new(e => e.Start?.DateTime, e => e.End?.DateTime, e => e.Subject, e => e.Location.DisplayName);
    private static readonly EventExtensionsCollectionPage extensions = new() { new OpenTypeExtension { ExtensionName = Tag } };
    private const string CategoryName = "Timetable";
    private const CategoryColor CategoryColour = CategoryColor.Preset2;

    private readonly GraphServiceClient _client;
    private readonly IUserRequestBuilder _userClient;
    private readonly Serializer _serializer = new();

    public MicrosoftCalendarWriter(string email, MicrosoftClientKey clientKey)
    {
      var confidentialClient = ConfidentialClientApplicationBuilder
        .Create(clientKey.ClientId)
        .WithTenantId(clientKey.TenantId)
        .WithClientSecret(clientKey.ClientSecret)
        .WithAuthority(AadAuthorityAudience.AzureAdMyOrg)
        .Build();
      _client = new GraphServiceClient(new ClientCredentialProvider(confidentialClient));
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
      if (categories.Any(o => o.DisplayName == CategoryName)) return;
      var category = new OutlookCategory { DisplayName = CategoryName, Color = CategoryColour };
      await _userClient.Outlook.MasterCategories.Request().AddAsync(category);
    }

    private async Task<IList<Event>> GetExistingEventsAsync()
    {
      var events = new List<Event>();
      var request = _userClient.Calendar.Events.Request();
      do
      {
        request.Headers.Add(new HeaderOption("Prefer", "outlook.timezone=\"Europe/London\""));
        var response = await request.Top(1000)
          .Filter($"Start/DateTime gt '{DateTime.Today:s}' and Extensions/any(f:f/id eq '{Tag}')")
          .Select("Id,Start,End,Subject,Location")
          .GetAsync();
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
        ev.Categories = new[] { CategoryName };
        var insertRequest = _userClient.Calendar.Events.Request().Select("Id").GetHttpRequestMessage();
        insertRequest.Method = HttpMethod.Post;
        insertRequest.Content = _serializer.SerializeAsJsonContent(ev);
        insertBatch.Queue(insertRequest);
      }
      await insertBatch.ExecuteWithRetryAsync();
    }
  }
}
