﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    private static readonly string categoryName = "Timetable";
    private static readonly CategoryColor categoryColour = CategoryColor.Preset2;

    private readonly GraphServiceClient client;
    private readonly IUserRequestBuilder userClient;
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
      userClient = client.Users[email];
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
      var categories = await userClient.Outlook.MasterCategories.Request().GetAsync();
      if (categories.Any(o => o.DisplayName == categoryName)) return;
      var category = new OutlookCategory { DisplayName = categoryName, Color = categoryColour };
      await userClient.Outlook.MasterCategories.Request().AddAsync(category);
    }

    private async Task<IList<Event>> GetExistingEventsAsync()
    {
      var events = new List<Event>();
      var request = userClient.Calendar.Events.Request();
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
        var deleteRequest = userClient.Events[ev.Id].Request().GetHttpRequestMessage();
        deleteRequest.Method = HttpMethod.Delete;
        deleteBatch.Queue(deleteRequest);
      }
      await deleteBatch.ExecuteWithRetryAsync();
    }

    private async Task AddEventsAsync(IEnumerable<Event> events)
    {
      var insertBatch = new MicrosoftUnlimitedBatch(client);
      foreach (var ev in events)
      {
        ev.Extensions = extensions;
        ev.Categories = new[] { categoryName };
        var insertRequest = userClient.Calendar.Events.Request().Select("Id").GetHttpRequestMessage();
        insertRequest.Method = HttpMethod.Post;
        insertRequest.Content = serializer.SerializeAsJsonContent(ev);
        insertBatch.Queue(insertRequest);
      }
      await insertBatch.ExecuteWithRetryAsync();
    }
  }
}
