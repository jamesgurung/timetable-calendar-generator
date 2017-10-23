using System;
using System.IO;

namespace makecal
{
  public class CalendarWriterFactory
  {
    public OutputType OutputType { get; set; }
    private string ServiceAccountKey { get; set; }
    private string OutputDirectory { get; set; }

    public CalendarWriterFactory(OutputType outputType, string serviceAccountKey)
    {
      OutputType = outputType;
      if (OutputType == OutputType.GoogleCalendar)
      {
        ServiceAccountKey = serviceAccountKey;
      }
      else
      {
        OutputDirectory = CreateOutputDirectory();
      }
    }

    public ICalendarWriter GetCalendarWriter(string email)
    {
      if (OutputType == OutputType.GoogleCalendar)
      {
        return new GoogleCalendarWriter(email, ServiceAccountKey);
      }
      var userName = email.Split('@')[0];
      var outputFileName = Path.Combine(OutputDirectory, userName);
      switch (OutputType)
      {
        case OutputType.Csv:
          return new CsvCalendarWriter(outputFileName + ".csv");
        case OutputType.Ical:
          return new IcalCalendarWriter(outputFileName + ".ics");
        default:
          throw new NotImplementedException();
      }
    }

    private static string CreateOutputDirectory()
    {
      var directory = Path.Combine(AppContext.BaseDirectory, "calendars");
      Directory.CreateDirectory(directory);
      return directory;
    }
  }
}
