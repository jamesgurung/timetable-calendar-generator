using System;
using System.Collections.Generic;
using System.Linq;

namespace makecal
{
  public class ArgumentParser
  {
    public (OutputType Type, string Name, int SimultaneousRequests) OutputFormat { get; private set; }

    private bool OutputFormatSet { set; get; } = false;
    
    public ArgumentParser(string[] args)
    {
      var flags = args?.Select(o => o.ToLowerInvariant()).Distinct().ToList() ?? new List<string>();

      foreach (var flag in flags)
      {
        switch (flag)
        {
          case "--csv":
          case "-c":
            SetOutputFormat(OutputType.Csv, "CSV", 4);
            break;
          case "--ical":
          case "-i":
            SetOutputFormat(OutputType.Ical, "iCal", 4);
            break;
          case "--google":
          case "-g":
            SetOutputFormat(OutputType.GoogleCalendar, "Google", 40);
            break;
	  case "--primarygoogle":
          case "-p":
            Console.WriteLine("WARNING: The --primaygoogle option deletes any calendars named 'My timetable'.\nIt will also input events into users' primary calendars...\n\nHit ctrl+c or close the window if you're uncertain!\n");
            System.Threading.Thread.Sleep(12000);
            SetOutputFormat(OutputType.PrimaryGoogle, "PrimaryGoogle", 40);
            break;
          default:
            throw new ArgumentException("Flag not recognised: " + flag);
        }
      }
      
      if (!OutputFormatSet)
      {
        throw new ArgumentException("You must specify an output type: --csv or --ical or --google or --primarygoogle");
      }
    }

    private void SetOutputFormat(OutputType type, string name, int simultaneousRequests)
    {
      if (OutputFormatSet)
      {
        throw new ArgumentException("Use only one flag: --csv or --ical or --google or --primary-google");
      }
      OutputFormat = (type, name, simultaneousRequests);
      OutputFormatSet = true;
    }
  }
}
