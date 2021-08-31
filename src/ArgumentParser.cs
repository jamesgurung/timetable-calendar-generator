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

      var flags = string.Join(' ', args).ToUpperInvariant();

      return flags switch
      {
        "--CSV" => OutputType.Csv,
        "--ICAL" => OutputType.Ical,
        "--GOOGLE" => OutputType.GoogleWorkspace,
        "--MICROSOFT" => OutputType.Microsoft365,
        _ => throw new ArgumentException("Flag combination not recognised: " + flags)
      };
    }

  }
}
