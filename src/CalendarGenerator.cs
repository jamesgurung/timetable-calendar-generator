using System;
using System.Collections.Generic;
using System.Linq;

namespace makecal
{
  public class CalendarGenerator
  {
    private const string BlankingCode = "Blanking Code";

    private Settings Settings { get; }

    public CalendarGenerator(Settings settings)
    {
      Settings = settings;
    }

    public IList<CalendarEvent> Generate(Person person)
    {
      var myLessons = person.Lessons.GroupBy(o => o.PeriodCode).ToDictionary(o => o.Key, o => o.First());

      var events = new List<CalendarEvent>();

      foreach (var (date, dayCode) in Settings.DayTypes.Where(o => o.Key >= DateTime.Today))
      {
        for (var period = 0; period < Settings.Timings.Count; period++)
        {
          var lessonTime = Settings.Timings[period];
          var periodName = lessonTime.Period;

          if (myLessons.TryGetValue($"{dayCode}:{periodName}", out var lesson) && lesson.Class == BlankingCode)
          {
            continue;
          }

          var title = !string.IsNullOrEmpty(periodName) && char.IsDigit(periodName[0]) ? $"P{periodName}. " : string.Empty;

          string room;

          if (Settings.OverrideDictionary.TryGetValue((date, periodName), out var overrideTitle))
          {
            if (string.IsNullOrEmpty(overrideTitle))
            {
              continue;
            }
            title += overrideTitle;
            room = null;
          }
          else if (lesson is not null)
          {
            if (lesson.YearGroup is not null && Settings.StudyLeave.Any(o => o.YearGroups.Contains(lesson.YearGroup.Value) && o.StartDate <= date && o.EndDate >= date))
            {
              continue;
            }
            var clsName = lesson.Class;
            if (Settings.RenameDictionary.TryGetValue(clsName, out var newTitle))
            {
              if (string.IsNullOrEmpty(newTitle))
              {
                continue;
              }
              clsName = newTitle;
            }
            if (clsName == BlankingCode)
            {
              continue;
            }
            title += string.IsNullOrEmpty(lesson.Teacher) ? clsName : $"{clsName} ({lesson.Teacher})";
            room = lesson.Room;
          }
          else
          {
            continue;
          }
          
          var start = new DateTime(date.Year, date.Month, date.Day, lessonTime.StartHour, lessonTime.StartMinute, 0);
          var end = start.AddMinutes(lessonTime.Duration);

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
  }
}
