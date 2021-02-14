using System;

namespace makecal
{
  public static class ArgumentParser
  {
    public static (OutputType Type, string Text, int SimultaneousRequests) Parse(string[] args)
    {
      if (args is null || args.Length == 0)
      {
        throw new ArgumentException("You must specify --csv or --ical or --google or --microsoft");
      }

      var flags = string.Join(' ', args).ToLowerInvariant();

      return flags switch
      {
        "--csv" => (OutputType.Csv, "Generating CSV calendars", 4),
        "--ical" => (OutputType.Ical, "Generating iCal calendars", 4),
        "--google" => (OutputType.GoogleWorkspace, "Writing to Google Workspace calendars", 40),
        "--microsoft" => (OutputType.Microsoft365, "Writing to Microsoft 365 calendars", 40),
        _ => throw new ArgumentException("Flag combination not recognised: " + flags)
      };
    }

  }
}
