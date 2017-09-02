using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using KBCsv;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace makecal
{
  public class Program
  {
    private static readonly string settingsFileName = @"inputs\settings.json";
    private static readonly string keyFileName = @"inputs\key.json";
    private static readonly string daysFileName = @"inputs\days.csv";
    private static readonly string studentsFileName = @"inputs\students.csv";
    private static readonly string teachersFileName = @"inputs\teachers.csv";

    private static readonly string appName = "makecal";
    private static readonly string calendarName = "My timetable";
    private static readonly string calendarColour = "#fbe983";

    private static readonly string blankingCode = "Blanking Code";

    private static readonly int headerHeight = 10;

    private static readonly int simultaneousRequests = 40;
    private static readonly int maxAttempts = 4;
    private static readonly int retryFirst = 5000;
    private static readonly int retryExponent = 4;

    private const char REPLACEMENT_CHARACTER = '\ufffd';

    static void Main()
    {
      MainAsync().GetAwaiter().GetResult();
    }

    private static async Task MainAsync()
    {
      try
      {
        Console.Clear();
        Console.CursorVisible = false;

        Console.WriteLine("TIMETABLE CALENDAR GENERATOR\n");

        var settings = await LoadSettingsAsync();
        var students = await LoadStudentsAsync();
        var teachers = await LoadTeachersAsync();

        Console.WriteLine("\nSetting up calendars:");

        var tasks = new List<Task>();
        var throttler = new SemaphoreSlim(initialCount: simultaneousRequests);
        
        var people = students.Concat(teachers).ToList();
       
        for (var i = 0; i < people.Count; i++)
        {
          var countLocal = i;
          await Task.Delay(10);
          await throttler.WaitAsync();
          var person = people[countLocal];
          tasks.Add(Task.Run(async () => {
            try
            {
              var line = countLocal + headerHeight;
              ConsoleHelper.WriteDescription(line, $"({countLocal + 1}/{people.Count}) {person.Email}");
              ConsoleHelper.WriteProgress(line, 0);
              for (var attempt = 1; attempt <= maxAttempts; attempt++)
              {
                try
                {
                  await WriteTimetableAsync(person, settings, line);
                  break;
                }
                catch (Google.GoogleApiException) when (attempt < maxAttempts)
                {
                  var backoff = retryFirst * (int)Math.Pow(retryExponent, attempt - 1);
                  ConsoleHelper.WriteStatus(line, $"Error. Retrying ({attempt} of {maxAttempts - 1})...", ConsoleColor.DarkYellow);
                  await Task.Delay(backoff);
                }
                catch (Exception exc)
                {
                  ConsoleHelper.WriteStatus(line, $"Failed. {exc.Message}", ConsoleColor.Red);
                  break;
                }
              }
            }
            finally
            {
              throttler.Release();
            }
          }));
        }
        await Task.WhenAll(tasks);
        Console.SetCursorPosition(0, headerHeight + people.Count);
        Console.WriteLine("\nCalendar generation complete.\n");
      }
      catch (Exception exc)
      {
        ConsoleHelper.WriteError(exc.Message);
      }
#if DEBUG
      finally {
        Console.ReadKey();
      }
#endif
    }

    private static async Task<Settings> LoadSettingsAsync()
    {
      Console.WriteLine($"Reading {settingsFileName}");
      var settingsText = await File.ReadAllTextAsync(settingsFileName);
      var settings = JsonConvert.DeserializeObject<Settings>(settingsText, new IsoDateTimeConverter { DateTimeFormat = "dd-MMM-yy" });

      Console.WriteLine($"Reading {keyFileName}");
      settings.ServiceAccountKey = await File.ReadAllTextAsync(keyFileName);

      Console.WriteLine($"Reading {daysFileName}");
      settings.DayTypes = new Dictionary<DateTime, string>();

      using (var fs = File.Open(daysFileName, FileMode.Open))
      using (var reader = new CsvReader(fs))
      {
        while (reader.HasMoreRecords)
        {
          var record = await reader.ReadDataRecordAsync();
          if (string.IsNullOrEmpty(record[0])) {
            continue;
          }
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

      using (var fs = File.Open(studentsFileName, FileMode.Open))
      using (var reader = new CsvReader(fs))
      {
        Person currentStudent = null;
        string currentSubject = null;

        while (reader.HasMoreRecords) {

          var record = await reader.ReadDataRecordAsync();
          if (record[StudentFields.Email] == nameof(StudentFields.Email))
          {
            continue;
          }

          if (!string.IsNullOrEmpty(record[StudentFields.Email]))
          {
            var newEmail = record[StudentFields.Email].ToLower();
            if (currentStudent?.Email != newEmail)
            {
              currentStudent = new Person {
                Email = newEmail,
                YearGroup = Int32.Parse(record[StudentFields.Year]),
                Lessons = new List<Lesson>()
              };
              currentSubject = null;
              students.Add(currentStudent);
            }
          }

          if (!string.IsNullOrEmpty(record[StudentFields.Subject]))
          {
            currentSubject = record[StudentFields.Subject];
          }

          if (currentStudent == null || currentSubject == null)
          {
            throw new Exception("Incorrectly formatted timetable.");
          }

          currentStudent.Lessons.Add(new Lesson {
            PeriodCode = record[StudentFields.Period],
            Class = currentSubject,
            Room = record[StudentFields.Room],
            Teacher = record[StudentFields.Teacher]
          });
        }
      }
      return students;
    }

    private static async Task<IEnumerable<Person>> LoadTeachersAsync()
    {
      Console.WriteLine($"Reading {teachersFileName}");

      var teachers = new List<Person>();

      using (var fs = File.Open(teachersFileName, FileMode.Open))
      using (var reader = new CsvReader(fs))
      {
        var periodCodes = await reader.ReadDataRecordAsync();

        while (reader.HasMoreRecords)
        {
          var timetable = await reader.ReadDataRecordAsync();
          var rooms = await reader.ReadDataRecordAsync();
          var currentTeacher = new Person { Email = timetable[0].ToLower(), Lessons = new List<Lesson>() };

          for (var i = 1; i < timetable.Count; i++)
          {
            if (string.IsNullOrEmpty(timetable[i]))
            {
              continue;
            }
            currentTeacher.Lessons.Add(new Lesson {
              PeriodCode = periodCodes[i],
              Class = timetable[i].Trim(new[] { REPLACEMENT_CHARACTER }),
              Room = rooms[i].Trim(new[] { REPLACEMENT_CHARACTER })
            });
          }

          teachers.Add(currentTeacher);
        }
      }
      return teachers;
    }

    private static async Task WriteTimetableAsync(Person person, Settings settings, int line)
    {
      var service = GetCalendarService(settings.ServiceAccountKey, person.Email);
      
      var calendarId = await PrepareCalendarAsync(service, line);
      ConsoleHelper.WriteProgress(line, 2);

      var batch = new UnlimitedBatch(service);

      var myStudyLeave = person.YearGroup == null ? new List<StudyLeave>() : settings.StudyLeave.Where(o => o.Year == person.YearGroup);
      var myLessons = person.Lessons.GroupBy(o => o.PeriodCode).ToDictionary(o => o.Key, o => o.First());

      foreach (var dayOfCalendar in settings.DayTypes.Where(o => o.Key >= DateTime.Today))
      {
        var date = dayOfCalendar.Key;
        var dayCode = dayOfCalendar.Value;
        if (myStudyLeave.Any(o => o.StartDate <= date && o.EndDate >= date))
        {
          continue;
        }

        for (var period = 1; period <= settings.DayTypes.Count; period++)
        {
          string title = $"P{period}. ";
          string room;

          if (myLessons.TryGetValue($"{dayCode}:{period}", out var lesson) && lesson.Class == blankingCode)
          {
            continue;
          }

          if (settings.OverrideDictionary.TryGetValue((date, period), out var overrideTitle))
          {
            if (string.IsNullOrEmpty(overrideTitle))
            {
              continue;
            }
            title += overrideTitle;
            room = null;
          }
          else if (lesson != null)
          {
            if (person.YearGroup == null)
            {
              var classYearGroup = lesson.YearGroup;
              if (classYearGroup != null && settings.StudyLeave.Any(o => o.Year == classYearGroup && o.StartDate <= date && o.EndDate >= date))
              {
                continue;
              }
            }
            var clsName = lesson.Class;
            if (settings.RenameDictionary.TryGetValue(clsName, out var newTitle))
            {
              if (string.IsNullOrEmpty(newTitle))
              {
                continue;
              }
              clsName = newTitle;
            }
            if (clsName == blankingCode)
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
      ConsoleHelper.WriteProgress(line, 3);
    }

    private static CalendarService GetCalendarService(string serviceAccountKey, string email)
    {
      var credential = GoogleCredential.FromJson(serviceAccountKey).CreateScoped(CalendarService.Scope.Calendar).CreateWithUser(email);

      return new CalendarService(new BaseClientService.Initializer() {
        HttpClientInitializer = credential,
        ApplicationName = appName
      });
    }

    private static async Task<string> PrepareCalendarAsync(CalendarService service, int line)
    {
      var calendars = await service.CalendarList.List().ExecuteAsync();
      var calendarId = calendars.Items.FirstOrDefault(o => o.Summary == calendarName)?.Id;

      ConsoleHelper.WriteProgress(line, 1);

      if (calendarId == null)
      {
        var newCalendar = new Calendar { Summary = calendarName };
        newCalendar = await service.Calendars.Insert(newCalendar).ExecuteAsync();
        calendarId = newCalendar.Id;
        await service.CalendarList.SetColor(calendarId, calendarColour).ExecuteAsync();
      }
      else
      {
        var existingFutureEvents = await service.Events.List(calendarId).FetchAllAsync(after: DateTime.Today);
        var batch = new UnlimitedBatch(service);
        foreach (var existingEvent in existingFutureEvents)
        {
          batch.Queue(service.Events.Delete(calendarId, existingEvent.Id));
        }
        await batch.ExecuteAsync();
      }

      return calendarId;
    }
  }
}