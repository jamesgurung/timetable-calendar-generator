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
  class MicrosoftCalendarWriter : ICalendarWriter
  {
    private static readonly string tag = "timetable-calendar-generator";
    private static readonly EventComparer<Event> comparer = new(e => e.Start?.DateTime, e => e.End?.DateTime, e => e.Subject, e => e.Location.DisplayName);
    private static readonly EventExtensionsCollectionPage extensions = new() { new OpenTypeExtension { ExtensionName = tag } };

    private readonly GraphServiceClient client;
    private readonly ICalendarEventsCollectionRequestBuilder eventsClient;
    private readonly Serializer serializer = new();

    public MicrosoftCalendarWriter(string email, MicrosoftClientKey clientKey)
    {
      var confidentialClient = ConfidentialClientApplicationBuilder
        .Create(clientKey.ClientId)
        .WithTenantId(clientKey.TenantId)
        .WithClientSecret(clientKey.ClientSecret)
        .WithAuthority(AadAuthorityAudience.AzureAdMyOrg)
        .Build();
      client = new GraphServiceClient(new ClientCredentialProvider(confidentialClient));
      eventsClient = client.Users[email].Calendar.Events;
    }

    public async Task WriteAsync(IList<CalendarEvent> events)
    {
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

    private async Task<IList<Event>> GetExistingEventsAsync()
    {
      var events = new List<Event>();
      var request = eventsClient.Request();
      do
      {
        var response = await request.Top(1000)
          .Filter($"Start/DateTime gt '{DateTime.Today:s}' and Extensions/any(f:f/id eq '{tag}')")
          .Select("Id,Start,End,Subject,Location")
          .GetAsync();
        events.AddRange(response.CurrentPage);
        request = response.NextPageRequest;
      } while (request is not null);

      return events;
    }

    private async Task DeleteEventsAsync(IEnumerable<Event> events)
    {
      var deleteBatch = new MicrosoftUnlimitedBatch(client);
      foreach (var ev in events)
      {
        ev.Extensions = extensions;
        var deleteRequest = eventsClient[ev.Id].Request().GetHttpRequestMessage();
        deleteRequest.Method = HttpMethod.Delete;
        deleteBatch.Queue(deleteRequest);
      }
      await deleteBatch.ExecuteAsync();
    }

    private async Task AddEventsAsync(IEnumerable<Event> events)
    {
      var insertBatch = new MicrosoftUnlimitedBatch(client);
      foreach (var ev in events)
      {
        ev.Extensions = extensions;
        var insertRequest = eventsClient.Request().Select("Id").GetHttpRequestMessage();
        insertRequest.Method = HttpMethod.Post;
        insertRequest.Content = serializer.SerializeAsJsonContent(ev);
        insertBatch.Queue(insertRequest);
      }
      await insertBatch.ExecuteAsync();
    }
  }
}
