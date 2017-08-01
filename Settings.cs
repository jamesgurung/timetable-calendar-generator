using System;
using System.Collections.Generic;
using System.Linq;

namespace makecal
{
  internal class Settings
  {
    public IList<LessonTime> LessonTimes { get; set; }
    public IList<StudyLeave> StudyLeave { get; set; }
    public IList<Override> Overrides { set {
        OverrideDictionary = value.ToDictionary(o => (o.Date, o.Period), o => o.Title);
      }
    }
    public IList<Rename> Renames { set {
        RenameDictionary = value.ToDictionary(o => o.OriginalTitle, o => o.NewTitle);
      }
    }

    public string ServiceAccountKey { get; set; }
    public IDictionary<DateTime, string> DayTypes { get; set; }
    public IDictionary<(DateTime, int), string> OverrideDictionary { get; private set; }
    public IDictionary<string, string> RenameDictionary { get; private set; }

    internal class LessonTime
    {
      public string StartTime {
        set {
          var parts = value.Split(':');
          StartHour = Convert.ToInt32(parts[0]);
          StartMinute = Convert.ToInt32(parts[1]);
        }
      }
      public int StartHour { get; private set; }
      public int StartMinute { get; private set; }
      public int Duration { get; set; }
    }
  }

  internal class StudyLeave
  {
    public int Year { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
  }

  internal class Override
  {
    public DateTime Date { get; set; }
    public int Period { get; set; }
    public string Title { get; set; }
  }

  internal class Rename
  {
    public string OriginalTitle { get; set; }
    public string NewTitle { get; set; }
  }
}