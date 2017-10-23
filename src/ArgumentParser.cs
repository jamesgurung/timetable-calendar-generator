using System;
using System.Collections.Generic;
using System.Linq;

namespace makecal
{
  public class ArgumentParser
  {
    private bool UseGoogle { get; }
    private bool UseCsv { get; }
    private bool UseIcal { get; }
    
    public ArgumentParser(string[] args)
    {
      var flags = args?.Select(o => o.ToLowerInvariant()).ToList() ?? new List<string>();
      foreach (var flag in flags)
      {
        switch (flag)
        {
          case "--csv":
          case "-c":
            UseCsv = true;
            break;
          case "--ical":
          case "-i":
            UseIcal = true;
            break;
          case "--google":
          case "-g":
            UseGoogle = true;
            break;
          default:
            throw new ArgumentException("Flag not recognised: " + flag);
        }
      }
      if (new[] { UseCsv, UseIcal, UseGoogle }.Count(b => b) > 1)
      {
        throw new ArgumentException("Use only one flag: --csv or --ical or --google");
      }
    }

    public (OutputType Type, string Name, int SimultaneousRequests) GetOutputFormat()
    {
      if (UseGoogle) return (OutputType.GoogleCalendar, "Google", 40);
      if (UseIcal) return (OutputType.Ical, "iCal", 4);
      return (OutputType.Csv, "CSV", 4);
    }
  }
}
