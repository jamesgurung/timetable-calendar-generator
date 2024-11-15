namespace TimetableCalendarGenerator;

public class EventComparer<TEvent>(Func<TEvent, DateTimeOffset?> start, Func<TEvent, DateTimeOffset?> end, Func<TEvent, string> title, Func<TEvent, string> location) : IEqualityComparer<TEvent>
{
  private Func<TEvent, DateTimeOffset?> Start { get; } = start;
  private Func<TEvent, DateTimeOffset?> End { get; } = end;
  private Func<TEvent, string> Title { get; } = title;
  private Func<TEvent, string> Location { get; } = location;

  public bool Equals(TEvent x, TEvent y)
  {
    if (x is null && y is null)
    {
      return true;
    }
    if (x is null || y is null)
    {
      return false;
    }
    return
      Start(x) == Start(y) &&
      End(x) == End(y) &&
      GetOriginalTitle(Title(x)) == GetOriginalTitle(Title(y)) &&
      (Location(x) == Location(y) || (string.IsNullOrEmpty(Location(x)) && string.IsNullOrEmpty(Location(y))));
  }

  public int GetHashCode(TEvent obj)
  {
    if (obj is null)
    {
      throw new ArgumentNullException(nameof(obj));
    }
    unchecked
    {
      var hash = 17;
      hash = (hash * 23) + (GetOriginalTitle(Title(obj))?.GetHashCode(StringComparison.Ordinal) ?? 0);
      hash = (hash * 23) + (Start(obj)?.GetHashCode() ?? 0);
      hash = (hash * 23) + (End(obj)?.GetHashCode() ?? 0);
      hash = (hash * 23) + (Location(obj) ?? string.Empty).GetHashCode(StringComparison.Ordinal);
      return hash;
    }
  }

  private static string GetOriginalTitle(string title)
  {
    if (title is null) return null;
    var index = title.IndexOf('-', StringComparison.Ordinal);
    return index < 0 ? title : title[..index].TrimEnd(' ');
  }
}