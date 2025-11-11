using Azure.Identity;
using Microsoft.Graph.Beta;

namespace TimetableCalendarGenerator;

public class CalendarWriterFactory
{
  public int SimultaneousRequests { get; }
  public string DisplayText { get; }

  private OutputType OutputType { get; }
  private DateTime StartDate { get; }
  private DateTime EndDate { get; }
  private byte[] GoogleServiceAccountKey { get; }
  private GraphServiceClient MicrosoftClient { get; }
  private string OutputDirectory { get; }

  public CalendarWriterFactory(OutputType outputType, byte[] googleServiceAccountKey, MicrosoftClientKey microsoftClientKey, DateOnly startDate, DateOnly endDate)
  {
    OutputType = outputType;
    StartDate = startDate.ToDateTime(new TimeOnly(0, 0, 0));
    if (StartDate < DateTime.Today) StartDate = DateTime.Today;
    EndDate = endDate.ToDateTime(new TimeOnly(23, 59, 59));

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
        return new GoogleCalendarWriter(email, GoogleServiceAccountKey, StartDate, EndDate);
      case OutputType.Microsoft365:
        return new MicrosoftCalendarWriter(email, MicrosoftClient, StartDate, EndDate);
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