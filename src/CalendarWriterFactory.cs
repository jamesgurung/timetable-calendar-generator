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
        OutputDirectory = CreateOutputDirectory("csv");
      }
      else if (OutputType == OutputType.Ical)
      {
        OutputDirectory = CreateOutputDirectory("ical");
      }
       else if (OutputType == OutputType.GoogleCalendar || OutputType == OutputType.PrimaryGoogle)
      {
        ServiceAccountKey = serviceAccountKey;
      }
    }

    public ICalendarWriter GetCalendarWriter(string email)
    {
      if (OutputType == OutputType.GoogleCalendar)
      {
        return new GoogleCalendarWriter(email, ServiceAccountKey);
      }
      if (OutputType == OutputType.PrimaryGoogle)
      {
        return new PrimaryGoogleCalendarWriter(email, ServiceAccountKey);
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

    private static string CreateOutputDirectory(string subfolder)
    {
      var directory = Path.Combine(AppContext.BaseDirectory, "calendars", subfolder);
      Directory.CreateDirectory(directory);
      return directory;
    }
  }
}
