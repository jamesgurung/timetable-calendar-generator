using System.Collections.Generic;
using System.Threading.Tasks;

namespace TimetableCalendarGenerator;

public interface ICalendarWriter
{
  Task WriteAsync(IList<CalendarEvent> events);
}