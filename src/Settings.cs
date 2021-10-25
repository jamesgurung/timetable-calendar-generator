using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;

namespace TimetableCalendarGenerator;

public class Settings
{
  public IList<Timing> Timings { get; set; }
  public IList<Absence> Absences { get; set; } = new List<Absence>();
  public IList<Override> Overrides { get; set; } = new List<Override>();
  public IList<Rename> Renames { get; set; }

  [JsonIgnore]
  public IDictionary<DateTime, string> DayTypes { get; set; }

  private IDictionary<string, string> _renameDictionary;
  [JsonIgnore]
  public IDictionary<string, string> RenameDictionary
  {
    get => _renameDictionary ??= Renames?.ToDictionary(o => o.OriginalTitle, o => o.NewTitle) ?? new();
  }

  private IList<IGrouping<string, Timing>> _timingsByPeriod;
  [JsonIgnore]
  public IList<IGrouping<string, Timing>> TimingsByPeriod
  {
    get => _timingsByPeriod ??= Timings.GroupBy(o => o.Period).ToList();
  }
}

public class Absence
{
  public IList<int> YearGroups { get; set; }
  public DateTime StartDate { get; set; }
  public DateTime EndDate { get; set; }
}

public class Override
{
  public DateTime Date { get; set; }
  public string Period { get; set; }
  public string Title { get; set; }
  public IList<int?> YearGroups { get; set; }
}

public class Rename
{
  public string OriginalTitle { get; set; }
  public string NewTitle { get; set; }
}

public class Timing
{
  public string Period { get; set; }
  public int Duration { get; set; }
  public IList<int?> YearGroups { get; set; }
  public IList<string> Days { get; set; }

  public string StartTime
  {
    set {
      if (value is null) throw new ArgumentNullException(nameof(value), "Timing start time is required.");
      var parts = value.Split(':');
      StartHour = int.Parse(parts[0], CultureInfo.InvariantCulture);
      StartMinute = int.Parse(parts[1], CultureInfo.InvariantCulture);
    }
    get => $"{StartHour:00}:{StartMinute:00}";
  }

  [JsonIgnore]
  public int StartHour { get; private set; }
  [JsonIgnore]
  public int StartMinute { get; private set; }
}