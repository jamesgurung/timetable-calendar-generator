using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KBCsv;

namespace makecal
{
  public class Program
  {
    const string timingsFileName = "timings.csv";
    const string daysFileName = "days.csv";
    const string studentsFileName = "students.csv";
    const string teachersFileName = "teachers.csv";
    const string studentsOutputFolder = @"\Student Timetables";
    const string teachersOutputFolder = @"\Teacher Timetables";

    private static List<LessonTime> timings;
    private static Dictionary<DateTime, string> dayTypes;

    private static void Main(string[] args)
    {
      try {

        Console.Clear();
        Console.WriteLine("TIMETABLE CALENDAR GENERATOR");
        Console.WriteLine();

        var generateStudentCalendars = args.Any(o => o == "-s");
        var generateTeacherCalendars = args.Any(o => o == "-t");

        if (!generateStudentCalendars && !generateTeacherCalendars) {
          Console.WriteLine("This tool must be used with at least one flag: dotnet makecal.dll -s -t");
          return;
        }

        Console.WriteLine($"Reading {timingsFileName}");
        timings = LoadTimings();
        Console.WriteLine();

        Console.WriteLine($"Reading {daysFileName}");
        dayTypes = LoadDayTypes();
        Console.WriteLine();
        
        if (generateStudentCalendars) {

          Console.WriteLine($"Reading {studentsFileName}");
          var students = LoadTimetables(studentsFileName);

          WriteCalendars(students, studentsOutputFolder);

        }

        if (generateTeacherCalendars) {

          Console.WriteLine($"Reading {teachersFileName}");
          var teachers = LoadTimetables(teachersFileName);

          WriteCalendars(teachers, teachersOutputFolder);

        }

        Console.WriteLine("Calendar generation complete.");
        Console.WriteLine();

      } catch (Exception exc) {

        Console.WriteLine();
        var backgroundColor = Console.BackgroundColor;
        Console.BackgroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {exc.Message}");
        Console.BackgroundColor = backgroundColor;
        Console.WriteLine();

      }

    }

    private static List<LessonTime> LoadTimings()
    {

      var lessonTimes = new List<LessonTime>();

      using (var fs = File.Open(timingsFileName, FileMode.Open))
      using (var reader = new CsvReader(fs)) {

        while (reader.HasMoreRecords) {
          var record = reader.ReadDataRecord();
          if (string.IsNullOrEmpty(record[0])) continue;
          var time = record[0].Split(':');
          lessonTimes.Add(new LessonTime { StartHour = int.Parse(time[0]), StartMinute = int.Parse(time[1]), DurationInMinutes = int.Parse(record[1]) });
        }

      }

      return lessonTimes;

    }

    private static Dictionary<DateTime, string> LoadDayTypes()
    {

      var dayTypes = new Dictionary<DateTime, string>();

      using (var fs = File.Open(daysFileName, FileMode.Open))
      using (var reader = new CsvReader(fs)) {
        
        while (reader.HasMoreRecords) {
          var record = reader.ReadDataRecord();
          if (string.IsNullOrEmpty(record[0])) continue;
          var date = DateTime.ParseExact(record[0], "dd-MMM-yy", null);
          dayTypes.Add(date, record[1] + date.DayOfWeek.ToString("G").Substring(0, 3));
        }

      }

      return dayTypes;

    }

    private static IEnumerable<Person> LoadTimetables(string fileName)
    {

      var people = new List<Person>();

      using (var fs = File.Open(fileName, FileMode.Open))
      using (var reader = new CsvReader(fs)) {
        {

          Person currentPerson = null;
          string currentSubject = null;

          while (reader.HasMoreRecords) {

            var record = reader.ReadDataRecord();
            if (string.IsNullOrEmpty(record[2]) || record[0] == "Email" || record[0] == "Work Email") continue;

            if (!string.IsNullOrEmpty(record[0])) {
              var newUsername = record[0].Split('@')[0].ToLower();
              if (currentPerson?.Username != newUsername) {
                currentPerson = new Person {Username = newUsername, Lessons = new List<Lesson>()};
                currentSubject = null;
                people.Add(currentPerson);
              }
            }

            if (!string.IsNullOrEmpty(record[1])) currentSubject = record[1];
            if (currentPerson == null || currentSubject == null) throw new Exception("Incorrectly formatted timetable.");

            currentPerson.Lessons.Add(new Lesson {
              PeriodCode = record[2],
              Class = currentSubject,
              Room = record[3],
              Teacher = (record.Count > 4) ? record[4] : null
            });

          }
        }
      }

      return people;

    }

    private static void WriteCalendars(IEnumerable<Person> people, string outputFolder) {
      
      var directory = Directory.GetCurrentDirectory() + outputFolder;
      Directory.CreateDirectory(directory);

      Console.WriteLine();

      foreach (var person in people) {
        Console.WriteLine($"Writing {person.Username}.csv");
        WriteCalendar(person, directory);
      }

      Console.WriteLine();

    }

    private static void WriteCalendar(Person person, string directory)
    {

      var path = $@"{directory}\{person.Username}.csv";

      using (var fs = File.Create(path))
      using (var writer = new CsvWriter(fs)) {

        writer.WriteRecord("Subject", "Start Date", "Start Time", "End Date", "End Time", "Location");

        foreach (var date in dayTypes) {

          var date1 = date;
          var periods = person.Lessons.Where(o => o.Day == date1.Value);

          foreach (var period in periods) {

            var lessonTime = timings[period.Period - 1];
            var startTime = new DateTime(date.Key.Year, date.Key.Month, date.Key.Day, lessonTime.StartHour, lessonTime.StartMinute, 0);
            var endTime = startTime.AddMinutes(lessonTime.DurationInMinutes);

            var title = $"P{period.Period}. ";
            title += string.IsNullOrEmpty(period.Teacher) ? period.Class : $"{period.Class} ({period.Teacher})";

            writer.WriteRecord(
              title,
              startTime.ToString("M/d/yyyy"),
              startTime.ToString("H:mm:ss"),
              endTime.ToString("M/d/yyyy"),
              endTime.ToString("H:mm:ss"),
              period.Room ?? string.Empty
            );

          }

        }

      }

    }
  }

  internal class Person
  {
    public string Username { get; set; }
    public List<Lesson> Lessons { get; set; }
  }

  internal class Lesson
  {
    public string PeriodCode
    {
      set
      {
        Period = int.Parse(value.Substring(5));
        Day = value.Substring(0, 4);
      }
    }

    public int Period { get; private set; }
    public string Day { get; private set; }

    public string Room { get; set; }
    public string Class { get; set; }
    public string Teacher { get; set; }
  }

  internal class DayType
  {
    public DateTime Date { get; set; }
    public int Type { get; set; }
  }

  internal class LessonTime
  {
    public int StartHour { get; set; }
    public int StartMinute { get; set; }
    public int DurationInMinutes { get; set; }
  }
}
