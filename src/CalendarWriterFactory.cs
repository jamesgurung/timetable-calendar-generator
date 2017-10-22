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
      if (OutputType == OutputType.Csv)
      {
        OutputDirectory = CreateOutputDirectory();
      }
      else if (OutputType == OutputType.GoogleCalendar)
      {
        ServiceAccountKey = serviceAccountKey;
      }
    }

    public ICalendarWriter GetCalendarWriter(string email)
    {
      switch (OutputType)
      {
        case OutputType.Csv:
          var userName = email.Split('@')[0];
          var outputFileName = Path.Combine(OutputDirectory, userName + ".csv");
          return new CsvCalendarWriter(outputFileName);
        case OutputType.GoogleCalendar:
          return new GoogleCalendarWriter(email, ServiceAccountKey);
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