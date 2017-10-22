using System;
using System.Collections.Generic;
using System.Linq;

namespace makecal
{
  public class ArgumentParser
  {
    private bool UseGoogle { get; }
    private bool UseCsv { get; }

    public ArgumentParser(string[] args)
    {
      var flags = args?.Select(o => o.ToLowerInvariant()).ToList() ?? new List<string>();
      foreach (var flag in flags)
      {
        switch (flag)
        {
          case "--online":
          case "-o":
            UseGoogle = true;
            break;
          case "--csv":
          case "-c":
            UseCsv = true;
            break;
          default:
            throw new ArgumentException("Flag not recognised: " + flag);
        }
      }
      if (UseGoogle && UseCsv)
      {
        throw new ArgumentException("Use only one flag: --online or --csv");
      }
    }

    public (OutputType Type, string Name, int SimultaneousRequests) GetOutputFormat()
    {
      if (UseGoogle) return (OutputType.GoogleCalendar, "Google", 40);
      return (OutputType.Csv, "CSV", 4);
    }
  }
}
