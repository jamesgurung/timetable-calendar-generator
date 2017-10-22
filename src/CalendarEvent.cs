using System;

namespace makecal
{
  public class CalendarEvent
  {
    public string Title { get; set; }
    public string Location { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
  }
}