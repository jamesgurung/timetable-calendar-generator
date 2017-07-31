using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KBCsv;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Google.Apis.Calendar.v3.Data;
using System.Threading.Tasks;

namespace makecal
{
  public class Program
  {
    private static readonly string settingsFileName = @"inputs\settings.json";
    private static readonly string keyFileName = @"inputs\key.json";
    private static readonly string daysFileName = @"inputs\days.csv";
    private static readonly string studentsFileName = @"inputs\students.csv";
    private static readonly string teachersFileName = @"inputs\teachers.csv";
    private static readonly string calendarName = "My timetable";
    private static readonly string calendarColour = "#fbe983";
    private static readonly string appName = "makecal";

    static void Main()
    {
      MainAsync().GetAwaiter().GetResult();
    }

    private static async Task MainAsync()
    {
      try {

        Console.Clear();
        Console.WriteLine("TIMETABLE CALENDAR GENERATOR\n");

        var settings = await LoadSettingsAsync();
        var students = await LoadStudentsAsync();
        var teachers = await LoadTeachersAsync();

        Console.WriteLine("\nSetting up calendars:\n");

        var people = students.Concat(teachers).ToList();
        for (var i = 0; i < people.Count; i++) {
          var person = people[i];
          Console.WriteLine($"({i+1}/{people.Count}) {person.Email}\n  - Connecting to Google Calendar...");
          await WriteTimetableAsync(person, settings);
        }

        Console.WriteLine("\nCalendar generation complete.\n");

      } catch (Exception exc) {

        DisplayError(exc.Message);

      }
    }

    private static async Task<Settings> LoadSettingsAsync()
    {
      Console.WriteLine($"Reading {settingsFileName}");
      var settings = JsonConvert.DeserializeObject<Settings>(await File.ReadAllTextAsync(settingsFileName), new IsoDateTimeConverter { DateTimeFormat = "dd-MMM-yy" });

      Console.WriteLine($"Reading {keyFileName}");
      settings.ServiceAccountKey = await File.ReadAllTextAsync(keyFileName);

      Console.WriteLine($"Reading {daysFileName}");
      settings.DayTypes = new Dictionary<DateTime, string>();

      using (var fs = File.Open(daysFileName, FileMode.Open))
      using (var reader = new CsvReader(fs)) {

        while (reader.HasMoreRecords) {
          var record = await reader.ReadDataRecordAsync();
          if (string.IsNullOrEmpty(record[0])) continue;
          var date = DateTime.ParseExact(record[0], "dd-MMM-yy", null);
          settings.DayTypes.Add(date, record[1] + date.DayOfWeek.ToString("G").Substring(0, 3));
        }

      }

      return settings;
    }

    private static async Task<IEnumerable<Person>> LoadStudentsAsync()
    {
      Console.WriteLine($"Reading {studentsFileName}");

      var students = new List<Person>();

      using (var fs = File.Open(studentsFileName, FileMode.Open)) {
        using (var reader = new CsvReader(fs)) {

          Person currentStudent = null;
          string currentSubject = null;

          while (reader.HasMoreRecords) {

            var record = await reader.ReadDataRecordAsync();
            if (record[StudentFields.Email] == nameof(StudentFields.Email)) continue;

            if (!string.IsNullOrEmpty(record[StudentFields.Email])) {
              var newEmail = record[StudentFields.Email].ToLower();
              if (currentStudent?.Email != newEmail) {
                currentStudent = new Person { Email = newEmail, YearGroup = Int32.Parse(record[StudentFields.Year]), Lessons = new List<Lesson>() };
                currentSubject = null;
                students.Add(currentStudent);
              }
            }

            if (!string.IsNullOrEmpty(record[StudentFields.Subject])) currentSubject = record[StudentFields.Subject];
            if (currentStudent == null || currentSubject == null) throw new Exception("Incorrectly formatted timetable.");

            currentStudent.Lessons.Add(new Lesson {
              PeriodCode = record[StudentFields.Period],
              Class = currentSubject,
              Room = record[StudentFields.Room],
              Teacher = record[StudentFields.Teacher]
            });

          }
        }
      }
      return students;
    }

    private static async Task<IEnumerable<Person>> LoadTeachersAsync()
    {
      Console.WriteLine($"Reading {teachersFileName}");

      var teachers = new List<Person>();

      using (var fs = File.Open(teachersFileName, FileMode.Open)) {
        using (var reader = new CsvReader(fs)) {

          var periodCodes = await reader.ReadDataRecordAsync();

          while (reader.HasMoreRecords) {

            var timetable = await reader.ReadDataRecordAsync();
            var rooms = await reader.ReadDataRecordAsync();

            var currentTeacher = new Person { Email = timetable[0], Lessons = new List<Lesson>() };

            for (var i = 1; i < timetable.Count; i++) {
              if (string.IsNullOrEmpty(timetable[i])) continue;
              currentTeacher.Lessons.Add(new Lesson {
                PeriodCode = periodCodes[i],
                Class = timetable[i],
                Room = rooms[i]
              });
            }

            teachers.Add(currentTeacher);
          }
        }
      }
      return teachers;
    }

    private static async Task WriteTimetableAsync(Person person, Settings settings)
    {
      var service = GetCalendarService(settings.ServiceAccountKey, person.Email);
      var calendarId = await PrepareCalendarAsync(service);

      Console.WriteLine("  - Adding events...");
      var batch = new UnlimitedBatch(service);

      var myStudyLeave = person.YearGroup == null ? new List<StudyLeave>() : settings.StudyLeave.Where(o => o.Year == person.YearGroup);
      var myLessons = person.Lessons.GroupBy(o => o.PeriodCode).ToDictionary(o => o.Key, o => o.First());

      foreach (var dayOfCalendar in settings.DayTypes.Where(o => o.Key >= DateTime.Today)) {

        var date = dayOfCalendar.Key;
        var dayCode = dayOfCalendar.Value;
        if (myStudyLeave.Any(o => o.StartDate <= date && o.EndDate >= date)) continue;

        for (var period = 1; period <= settings.DayTypes.Count; period++) {

          string title = $"P{period}. ";
          string room;

          if (settings.OverrideDictionary.TryGetValue((date, period), out var overrideTitle)) {
            if (string.IsNullOrEmpty(overrideTitle)) continue;
            title += overrideTitle;
            room = null;
          } else if (myLessons.TryGetValue($"{dayCode}:{period}", out var lesson)) {
            if (person.YearGroup == null) {
              var classYearGroup = lesson.YearGroup;
              if (classYearGroup != null && settings.StudyLeave.Any(o => o.Year == classYearGroup && o.StartDate <= date && o.EndDate >= date)) continue;
            }
            title += string.IsNullOrEmpty(lesson.Teacher) ? lesson.Class : $"{lesson.Class} ({lesson.Teacher})";
            room = lesson.Room;
          } else {
            continue;
          }

          var lessonTime = settings.LessonTimes[period - 1];
          var start = new DateTime(date.Year, date.Month, date.Day, lessonTime.StartHour, lessonTime.StartMinute, 0);
          var end = start.AddMinutes(lessonTime.Duration);

          var ev = new Event() {
            Summary = title,
            Location = room,
            Start = new EventDateTime() { DateTime = start },
            End = new EventDateTime() { DateTime = end }
          };
          batch.Queue(service.Events.Insert(ev, calendarId));

        }
      }

      await batch.ExecuteAsync();

      Console.WriteLine("  - Done.\n");
    }

    private static CalendarService GetCalendarService(string serviceAccountKey, string email)
    {
      var credential = GoogleCredential.FromJson(serviceAccountKey).CreateScoped(CalendarService.Scope.Calendar).CreateWithUser(email);

      return new CalendarService(new BaseClientService.Initializer() {
        HttpClientInitializer = credential,
        ApplicationName = appName,
      });
    }

    private static async Task<string> PrepareCalendarAsync(CalendarService service)
    {
      var calendars = await service.CalendarList.List().ExecuteAsync();
      var calendarId = calendars.Items.FirstOrDefault(o => o.Summary == calendarName)?.Id;

      if (calendarId == null) {
        Console.WriteLine("  - Creating new calendar...");
        var newCalendar = new Calendar { Summary = calendarName };
        newCalendar = await service.Calendars.Insert(newCalendar).ExecuteAsync();
        calendarId = newCalendar.Id;
        await service.CalendarList.SetColor(calendarId, calendarColour).ExecuteAsync();
      } else {
        Console.WriteLine("  - Clearing previous timetable from calendar...");
        var existingFutureEvents = await service.Events.List(calendarId).FetchAllAsync(after: DateTime.Today);
        var batch = new UnlimitedBatch(service);
        foreach (var existingEvent in existingFutureEvents) {
          batch.Queue(service.Events.Delete(calendarId, existingEvent.Id));
        }
        await batch.ExecuteAsync();
      }

      return calendarId;
    }

    private static void DisplayError(string message)
    {
      Console.WriteLine();
      var backgroundColor = Console.BackgroundColor;
      Console.BackgroundColor = ConsoleColor.Red;
      Console.WriteLine($"Error: {message}");
      Console.BackgroundColor = backgroundColor;
      Console.WriteLine();
    }
  }
}