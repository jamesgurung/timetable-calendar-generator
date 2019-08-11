using System;
using System.Collections.Generic;
using Google.Apis.Calendar.v3.Data;

namespace makecal
{
  public class GoogleCalendarEventComparer : IEqualityComparer<Event>
  {
    public bool Equals(Event x, Event y)
    {
      if (x == null && y == null)
      {
        return true;
      }
      if (x == null || y == null)
      {
        return false;
      }
      return 
        x.Start?.DateTime == y.Start?.DateTime &&
        x.End?.DateTime == y.End?.DateTime &&
        GetOriginalTitle(x.Summary) == GetOriginalTitle(y.Summary) &&
        (x.Location == y.Location || (string.IsNullOrEmpty(x.Location) && string.IsNullOrEmpty(y.Location)));
    }

    public int GetHashCode(Event ev)
    {
      if (ev == null)
      {
        return default;
      }
      unchecked
      {
        var hash = 17;
        hash = hash * 23 + (GetOriginalTitle(ev.Summary)?.GetHashCode() ?? 0);
        hash = hash * 23 + (ev.Start?.DateTime?.GetHashCode() ?? 0);
        hash = hash * 23 + (ev.End?.DateTime?.GetHashCode() ?? 0);
        hash = hash * 23 + (ev.Location ?? string.Empty).GetHashCode();
        return hash;
      }
    }

    private static string GetOriginalTitle(string title) {
      if (title is null) return null;
      var index = title.IndexOf('-');
      if (index < 0) return title;
      return title.Substring(0, index).Trim();
    }
  }
}
