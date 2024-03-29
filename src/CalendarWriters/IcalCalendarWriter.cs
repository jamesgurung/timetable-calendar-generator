﻿using System.Globalization;
using System.Text;

namespace TimetableCalendarGenerator;

public class IcalCalendarWriter(string outputFileName) : ICalendarWriter
{
  private const string DateFormat = "yyyyMMdd'T'HHmmss";

  private string OutputFileName { get; } = outputFileName;

  public async Task WriteAsync(IList<CalendarEvent> events)
  {
    ArgumentNullException.ThrowIfNull(events);
    var sb = new StringBuilder();
    sb.AppendLine("BEGIN:VCALENDAR");
    sb.AppendLine("PRODID:-//github.com/jamesgurung/timetable-calendar-generator//makecal//EN");
    sb.AppendLine("VERSION:2.0");
    sb.AppendLine("BEGIN:VTIMEZONE\nTZID:Europe/London\nBEGIN:DAYLIGHT\nTZOFFSETFROM:+0000\nTZOFFSETTO:+0100\nTZNAME:BST\nDTSTART:19700329T010000\n" +
                  "RRULE:FREQ=YEARLY;BYMONTH=3;BYDAY=-1SU\nEND:DAYLIGHT\nBEGIN:STANDARD\nTZOFFSETFROM:+0100\nTZOFFSETTO:+0000\nTZNAME:GMT\nDTSTART:19701025T020000\n" +
                  "RRULE:FREQ=YEARLY;BYMONTH=10;BYDAY=-1SU\nEND:STANDARD\nEND:VTIMEZONE");
    foreach (var calendarEvent in events)
    {
      sb.AppendLine("BEGIN:VEVENT");
      sb.AppendLine("UID:" + Guid.NewGuid());
      sb.AppendLine("SUMMARY:" + calendarEvent.Title);
      sb.AppendLine("DTSTART;TZID=Europe/London:" + calendarEvent.Start.ToString(DateFormat, CultureInfo.InvariantCulture));
      sb.AppendLine("DTEND;TZID=Europe/London:" + calendarEvent.End.ToString(DateFormat, CultureInfo.InvariantCulture));
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