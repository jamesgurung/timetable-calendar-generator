using System;
using System.Collections.Generic;

namespace makecal
{
  public class EventComparer<TEvent> : IEqualityComparer<TEvent>
  {
    private Func<TEvent, DateTime?> Start { get; }
    private Func<TEvent, DateTime?> End { get; }
    private Func<TEvent, string> Title { get; }
    private Func<TEvent, string> Location { get; }

    public EventComparer(Func<TEvent, DateTime?> start, Func<TEvent, DateTime?> end, Func<TEvent, string> title, Func<TEvent, string> location)
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
        Start(x) == Start(y) &&
        End(x) == End(y) &&
        GetOriginalTitle(Title(x)) == GetOriginalTitle(Title(y)) &&
        (Location(x) == Location(y) || (string.IsNullOrEmpty(Location(x)) && string.IsNullOrEmpty(Location(y))));
    }

    public int GetHashCode(TEvent ev)
    {
      if (ev is null)
      {
        throw new ArgumentNullException(nameof(ev));
      }
      unchecked
      {
        var hash = 17;
        hash = hash * 23 + (GetOriginalTitle(Title(ev))?.GetHashCode() ?? 0);
        hash = hash * 23 + (Start(ev)?.GetHashCode() ?? 0);
        hash = hash * 23 + (End(ev)?.GetHashCode() ?? 0);
        hash = hash * 23 + (Location(ev) ?? string.Empty).GetHashCode();
        return hash;
      }
    }

    private static string GetOriginalTitle(string title)
    {
      if (title is null) return null;
      var index = title.IndexOf('-');
      return index < 0 ? title : title[..index].TrimEnd(' ');
    }
  }
}
