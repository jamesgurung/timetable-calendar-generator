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
    public IList<Override> Overrides { get; set; }
    public IList<Rename> Renames { get; set; }
    public IDictionary<DateTime, string> DayTypes { get; set; }

    private IDictionary<(DateTime, string), string> _overrideDictionary = null;
    public IDictionary<(DateTime, string), string> OverrideDictionary
    {
      get
      {
        if (_overrideDictionary is null) _overrideDictionary = Overrides?.ToDictionary(o => (o.Date, o.Period), o => o.Title);
        return _overrideDictionary;
      }
    }

    private IDictionary<string, string> _renameDictionary = null;
    public IDictionary<string, string> RenameDictionary
    {
      get
      {
        if (_renameDictionary is null) _renameDictionary = Renames?.ToDictionary(o => o.OriginalTitle, o => o.NewTitle);
        return _renameDictionary;
      }
    }
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
      set {
        var parts = value.Split(':');
        StartHour = int.Parse(parts[0]);
        StartMinute = int.Parse(parts[1]);
      }
    }
    public int StartHour { get; private set; }
    public int StartMinute { get; private set; }
    public int Duration { get; set; }
    public string Lesson { get; set; }
  }
}
