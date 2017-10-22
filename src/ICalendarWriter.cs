using System.Collections.Generic;
using System.Threading.Tasks;

namespace makecal
{
  public interface ICalendarWriter
  {
    Task PrepareAsync();
    Task WriteAsync(IList<CalendarEvent> events);
  }
}