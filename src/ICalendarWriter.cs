namespace TimetableCalendarGenerator;

public interface ICalendarWriter
{
  Task WriteAsync(IList<CalendarEvent> events);
}