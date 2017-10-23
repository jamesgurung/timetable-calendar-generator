using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace makecal
{
  class IcalCalendarWriter : ICalendarWriter
  {
    private static readonly string dateFormat = "yyyyMMdd'T'HHmmss";

    private string OutputFileName { get; }

    public IcalCalendarWriter(string outputFileName)
    {
      OutputFileName = outputFileName;
    }

    public async Task WriteAsync(IList<CalendarEvent> events)
    {
      var sb = new StringBuilder();
      sb.AppendLine("BEGIN:VCALENDAR");
      sb.AppendLine("PRODID:-//github.com/jamesgurung/timetable-calendar-generator//makecal//EN");
      sb.AppendLine("VERSION:2.0");
      foreach (var calendarEvent in events)
      {
        sb.AppendLine("BEGIN:VEVENT");
        sb.AppendLine("SUMMARY:" + calendarEvent.Title);
        sb.AppendLine("DTSTART:" + calendarEvent.Start.ToString(dateFormat));
        sb.AppendLine("DTEND:" + calendarEvent.End.ToString(dateFormat));
        if (!string.IsNullOrWhiteSpace(calendarEvent.Location))
        {
          sb.AppendLine("LOCATION:" + calendarEvent.Location);
        }
        sb.AppendLine("END:VEVENT");
      }
      sb.AppendLine("END:VCALENDAR");
      await File.WriteAllTextAsync(OutputFileName, sb.ToString(), Encoding.UTF8);
    }
  }
}