using System;

namespace makecal
{
  public class CalendarEvent
  {
    public string Title { get; init; }
    public string Location { get; init; }
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
  }
}
