using System;

namespace makecal
{
  public static class ArgumentParser
  {
    public static OutputType Parse(string[] args)
    {
      if (args is null || args.Length == 0)
      {
        throw new ArgumentException("You must specify --csv or --ical or --google or --microsoft");
      }

      var flags = string.Join(' ', args).ToLowerInvariant();

      return flags switch
      {
        "--csv" => OutputType.Csv,
        "--ical" => OutputType.Ical,
        "--google" => OutputType.GoogleWorkspace,
        "--microsoft" => OutputType.Microsoft365,
        _ => throw new ArgumentException("Flag combination not recognised: " + flags)
      };
    }

  }
}
