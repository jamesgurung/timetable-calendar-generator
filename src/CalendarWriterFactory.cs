using System;
using System.IO;

namespace makecal
{
  public class CalendarWriterFactory
  {
    public OutputType OutputType { get; set; }
    private string GoogleServiceAccountKey { get; }
    private MicrosoftClientKey MicrosoftClientKey { get; }
    private string OutputDirectory { get; }

    public CalendarWriterFactory(OutputType outputType, string googleServiceAccountKey, MicrosoftClientKey microsoftClientKey)
    {
      OutputType = outputType;

      switch (OutputType)
      {
        case OutputType.Csv:
          OutputDirectory = CreateOutputDirectory("csv");
          break;
        case OutputType.Ical:
          OutputDirectory = CreateOutputDirectory("ical");
          break;
        case OutputType.GoogleWorkspace:
          GoogleServiceAccountKey = googleServiceAccountKey;
          break;
        case OutputType.Microsoft365:
          MicrosoftClientKey = microsoftClientKey;
          break;
      }
    }

    public ICalendarWriter GetCalendarWriter(string email)
    {
      switch (OutputType)
      {
        case OutputType.GoogleWorkspace:
          return new GoogleCalendarWriter(email, GoogleServiceAccountKey);
        case OutputType.Microsoft365:
          return new MicrosoftCalendarWriter(email, MicrosoftClientKey);
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
}
