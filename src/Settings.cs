using System.Globalization;
using System.Text.Json.Serialization;

namespace TimetableCalendarGenerator;

public class Settings
{
  public IList<Timing> Timings { get; set; }
  public IList<Absence> Absences { get; set; } = [];
  public IList<Override> Overrides { get; set; } = [];
  public IList<Rename> Renames { get; set; }
  public bool WeekTypeAsSuffix { get; set; }

  [JsonIgnore]
  public IDictionary<DateOnly, string> DayTypes { get; set; }

  private IDictionary<string, (string Title, string Room)> _renameDictionary;
  [JsonIgnore]
  public IDictionary<string, (string Title, string Room)> RenameDictionary =>
    _renameDictionary ??= Renames?.ToDictionary(o => o.OriginalTitle, o => (o.NewTitle, o.NewRoom)) ?? [];

  private IList<IGrouping<string, Timing>> _timingsByPeriod;
  [JsonIgnore]
  public IList<IGrouping<string, Timing>> TimingsByPeriod =>
    _timingsByPeriod ??= [.. Timings.GroupBy(o => o.Period)];

  [JsonIgnore]
  public DateOnly StartDate => DayTypes.Keys.Min();
  [JsonIgnore]
  public DateOnly EndDate => DayTypes.Keys.Max();
}

public class Absence
{
  public IList<int> YearGroups { get; set; }
  [JsonConverter(typeof(JsonDateConverter))]
  public DateOnly StartDate { get; set; }
  [JsonConverter(typeof(JsonDateConverter))]
  public DateOnly EndDate { get; set; }
}

public class Override
{
  [JsonConverter(typeof(JsonDateConverter))]
  public DateOnly Date { get; set; }
  public string Period { get; set; }
  public string Title { get; set; }
  public IList<int?> YearGroups { get; set; }
  public string CopyFromPeriod { get; set; }
}

public class Rename
{
  public string OriginalTitle { get; set; }
  public string NewTitle { get; set; }
  public string NewRoom { get; set; }
}

public class Timing
{
  public string Period { get; set; }
  public int Duration { get; set; }
  public IList<int?> YearGroups { get; set; }
  public IList<string> Days { get; set; }

  public string StartTime
  {
    set
    {
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

[JsonSerializable(typeof(Settings))]
[JsonSerializable(typeof(MicrosoftClientKey))]
[JsonConverter(typeof(JsonDateConverter))]
internal sealed partial class SourceGenerationContext : JsonSerializerContext { }
