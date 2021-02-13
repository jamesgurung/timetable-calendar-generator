using System;
using System.Collections.Generic;

namespace makecal
{
  public class EventComparer<TEvent> : IEqualityComparer<TEvent>
  {
    private Func<TEvent, string> Start { get; }
    private Func<TEvent, string> End { get; }
    private Func<TEvent, string> Title { get; }
    private Func<TEvent, string> Location { get; }

    public EventComparer(Func<TEvent, string> start, Func<TEvent, string> end, Func<TEvent, string> title, Func<TEvent, string> location)
    {
      Start = start;
      End = end;
      Title = title;
      Location = location;
    }

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
        Start(x)?.Substring(0, 19) == Start(y)?.Substring(0, 19) &&
        End(y)?.Substring(0, 19) == End(y)?.Substring(0, 19) &&
        GetOriginalTitle(Title(x)) == GetOriginalTitle(Title(y)) &&
        (Location(x) == Location(y) || (string.IsNullOrEmpty(Location(x)) && string.IsNullOrEmpty(Location(y))));
    }

    public int GetHashCode(TEvent ev)
    {
      if (ev is null)
      {
        return default;
      }
      unchecked
      {
        var hash = 17;
        hash = hash * 23 + (GetOriginalTitle(Title(ev))?.GetHashCode() ?? 0);
        hash = hash * 23 + (Start(ev)?.Substring(0, 19)?.GetHashCode() ?? 0);
        hash = hash * 23 + (End(ev)?.Substring(0, 19)?.GetHashCode() ?? 0);
        hash = hash * 23 + (Location(ev) ?? string.Empty).GetHashCode();
        return hash;
      }
    }

    private static string GetOriginalTitle(string title)
    {
      if (title is null) return null;
      var index = title.IndexOf('-');
      if (index < 0) return title;
      return title.Substring(0, index).TrimEnd(' ');
    }
  }
}
