﻿using Azure.Identity;
using Microsoft.Graph.Beta;

namespace TimetableCalendarGenerator;

public class CalendarWriterFactory
{
  public int SimultaneousRequests { get; }
  public string DisplayText { get; }

  private OutputType OutputType { get; }
  private string GoogleServiceAccountKey { get; }
  private GraphServiceClient MicrosoftClient { get; }
  private string OutputDirectory { get; }

  public CalendarWriterFactory(OutputType outputType, string googleServiceAccountKey, MicrosoftClientKey microsoftClientKey)
  {
    OutputType = outputType;

    switch (OutputType)
    {
      case OutputType.Csv:
        SimultaneousRequests = 4;
        DisplayText = "Generating CSV calendars:";
        OutputDirectory = CreateOutputDirectory("csv");
        break;
      case OutputType.Ical:
        SimultaneousRequests = 4;
        DisplayText = "Generating iCal calendars:";
        OutputDirectory = CreateOutputDirectory("ical");
        break;
      case OutputType.GoogleWorkspace:
        SimultaneousRequests = 40;
        DisplayText = "Writing to Google Workspace calendars:";
        GoogleServiceAccountKey = googleServiceAccountKey;
        break;
      case OutputType.Microsoft365:
        SimultaneousRequests = 25;
        DisplayText = "Writing to Microsoft 365 calendars:";
        ArgumentNullException.ThrowIfNull(microsoftClientKey);
        var credential = new ClientSecretCredential(microsoftClientKey.TenantId, microsoftClientKey.ClientId, microsoftClientKey.ClientSecret);
        MicrosoftClient = new GraphServiceClient(credential);
        break;
      default:
        throw new NotImplementedException();
    }
  }

  public ICalendarWriter GetCalendarWriter(string email)
  {
    ArgumentNullException.ThrowIfNull(email);
    switch (OutputType)
    {
      case OutputType.GoogleWorkspace:
        return new GoogleCalendarWriter(email, GoogleServiceAccountKey);
      case OutputType.Microsoft365:
        return new MicrosoftCalendarWriter(email, MicrosoftClient);
      default:
        var userName = email.Split('@')[0];
        var outputFileName = Path.Combine(OutputDirectory, userName);

        return OutputType switch
        {
          OutputType.Csv => new CsvCalendarWriter(outputFileName + ".csv"),
          OutputType.Ical => new IcalCalendarWriter(outputFileName + ".ics"),
          _ => throw new NotImplementedException()
        };
    }
  }

  private static string CreateOutputDirectory(string subfolder)
  {
    var directory = Path.Combine(AppContext.BaseDirectory, "calendars", subfolder);
    Directory.CreateDirectory(directory);
    return directory;
  }
}