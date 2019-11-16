using System;

namespace makecal
{
  public class ArgumentParser
  {
    public (OutputType Type, string Text, int SimultaneousRequests) Parse(string[] args)
    {
      if (args is null || args.Length == 0)
      {
        throw new ArgumentException("You must specify an output type: --csv or --ical or --google");
      }

      var flags = args == null ? null : string.Join(' ', args).ToLowerInvariant();

      return flags switch
      {
        "--csv" => (OutputType.Csv, "Generating CSV calendars", 4),
        "--ical" => (OutputType.Ical, "Generating iCal calendars", 4),
        "--google" => throw new ArgumentException("You must specify --google --primary or --google --secondary"),
        "--google --primary" => (OutputType.GoogleCalendarPrimary, "Writing to primary Google calendars", 40),
        "--google --secondary" => (OutputType.GoogleCalendar, "Writing to Google \"My timetable\" calendars", 40),
        "--google --remove-secondary" => (OutputType.GoogleCalendarRemoveSecondary, "Removing \"My timetable\" Google calendars", 40),
        _ => throw new ArgumentException("Flag combination not recognised: " + flags)
      };
    }

  }
}
