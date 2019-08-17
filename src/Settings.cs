using System;
using System.Collections.Generic;
using System.Linq;

namespace makecal
{
  public class Settings
  {
    public IList<LessonTime> LessonTimes { get; set; }
    public LessonTime WeirdFriday4Time { get; set; }
    public IList<StudyLeave> StudyLeave { get; set; }
    public IList<Override> Overrides { set {
        OverrideDictionary = value.ToDictionary(o => (o.Date, o.Period), o => o.Title);
      }
    }
    public IList<Rename> Renames { set {
        RenameDictionary = value.ToDictionary(o => o.OriginalTitle, o => o.NewTitle);
      }
    }
    public IDictionary<DateTime, string> DayTypes { get; set; }
    public IDictionary<(DateTime, string), string> OverrideDictionary { get; private set; }
    public IDictionary<string, string> RenameDictionary { get; private set; }
  }

  public class StudyLeave
  {
    public int Year { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
  }

  public class Override
  {
    public DateTime Date { get; set; }
    public string Period { get; set; }
    public string Title { get; set; }
  }

  public class Rename
  {
    public string OriginalTitle { get; set; }
    public string NewTitle { get; set; }
  }

  public class LessonTime
  {
    public string StartTime
    {
      set
      {
        var parts = value.Split(':');
        StartHour = int.Parse(parts[0]);
        StartMinute = int.Parse(parts[1]);
      }
    }
    public int StartHour { get; private set; }
    public int StartMinute { get; private set; }
    public int Duration { get; set; }
    public string Lesson {get; set; }
  }
}
