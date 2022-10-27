namespace TimetableCalendarGenerator;

public class CalendarGenerator
{
  private Settings Settings { get; }

  public CalendarGenerator(Settings settings)
  {
    Settings = settings;
  }

  public IList<CalendarEvent> Generate(Person person)
  {
    ArgumentNullException.ThrowIfNull(person);

    var myLessons = person.Lessons.GroupBy(o => o.PeriodCode).ToDictionary(o => o.Key, o => o.First());
    var events = new List<CalendarEvent>();

    foreach (var (date, dayCode) in Settings.DayTypes.Where(o => o.Key >= DateTime.Today))
    {
      foreach (var periodTimings in Settings.TimingsByPeriod)
      {
        var period = periodTimings.Key;

        myLessons.TryGetValue($"{dayCode}:{period}", out var lesson);
        var yearGroup = person.YearGroup ?? lesson?.YearGroup;

        var overridePeriod = Settings.Overrides.FirstOrDefault(o => o.Date == date && o.Period == period && (o.YearGroups?.Contains(yearGroup) ?? true));

        string title = null, room = null;
        
        if (overridePeriod is not null)
        {
          if (overridePeriod.CopyFromPeriod is not null && myLessons.TryGetValue($"{dayCode}:{overridePeriod.CopyFromPeriod}", out var lessonToCopy))
          {
            (title, room) = GetTitleAndRoom(lessonToCopy, person.YearGroup ?? lessonToCopy.YearGroup, date);
            if (title is not null && person.YearGroup is null) yearGroup = lessonToCopy.YearGroup;
          }
          if (title is null)
          {
            if (string.IsNullOrEmpty(overridePeriod.Title)) continue;
            title = overridePeriod.Title;
            room = null;
          }
        }
        else if (lesson is not null)
        {
          (title, room) = GetTitleAndRoom(lesson, yearGroup, date);
          if (title is null) continue;
        }
        else
        {
          continue;
        }

        var lessonTime = periodTimings.FirstOrDefault(timingEntry => (timingEntry.YearGroups?.Contains(yearGroup) ?? true) && (timingEntry.Days?.Contains(dayCode) ?? true));

        if (lessonTime is null)
        {
          throw new InvalidOperationException($"Period {period} requires fallback timings.");
        }
          
        var start = new DateTime(date.Year, date.Month, date.Day, lessonTime.StartHour, lessonTime.StartMinute, 0);
        var end = start.AddMinutes(lessonTime.Duration);

        if (char.IsDigit(period[0])) {
          title =  $"P{period}. {title}";
        }
        else if (period is "AM" or "PM")
        {
          title = $"{period}. {title}";
        }

        var calendarEvent = new CalendarEvent
        {
          Title = title,
          Location = room,
          Start = start,
          End = end
        };
        events.Add(calendarEvent);
      }
    }

    return events;
  }
  
  private (string Title, string Room) GetTitleAndRoom(Lesson lesson, int? yearGroup, DateTime date)
  {
    if (yearGroup is not null && Settings.Absences.Any(o => o.YearGroups.Contains(yearGroup.Value) && o.StartDate <= date && o.EndDate >= date)) return (null, null);

    var clsName = lesson.Class;
    if (Settings.RenameDictionary.TryGetValue(clsName, out var newTitle))
    {
      if (string.IsNullOrEmpty(newTitle)) return (null, null);
      clsName = newTitle;
    }
    return (string.IsNullOrEmpty(lesson.Teacher) ? clsName : $"{clsName} ({lesson.Teacher})", lesson.Room);
  }
}