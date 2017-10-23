using System;
using System.Collections.Generic;
using Google.Apis.Calendar.v3.Data;

namespace makecal
{
  public class GoogleCalendarEventComparer : IEqualityComparer<Event>
  {
    public bool Equals(Event x, Event y)
    {
      if (x == null && y == null) return true;
      if (x == null || y == null) return false;
      return 
        x.Summary == y.Summary &&
        x.Start?.DateTime == y.Start?.DateTime &&
        x.End?.DateTime == y.End?.DateTime &&
        (x.Location == y.Location || (String.IsNullOrEmpty(x.Location) && String.IsNullOrEmpty(y.Location)));
    }

    public int GetHashCode(Event ev)
    {
      if (ev == null) return default;
      unchecked
      {
        var hash = 17;
        hash = hash * 23 + (ev.Summary?.GetHashCode() ?? 0);
        hash = hash * 23 + (ev.Start?.DateTime?.GetHashCode() ?? 0);
        hash = hash * 23 + (ev.End?.DateTime?.GetHashCode() ?? 0);
        hash = hash * 23 + (ev.Location ?? string.Empty).GetHashCode();
        return hash;
      }
    }
  }
}
