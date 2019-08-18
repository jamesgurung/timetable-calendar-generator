using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using KBCsv;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace makecal
{
  public static class InputReader
  {
    private static readonly string settingsFileName = @"inputs/settings.json";
    private static readonly string keyFileName = @"inputs/key.json";
    private static readonly string daysFileName = @"inputs/days.csv";
    private static readonly string studentsFileName = @"inputs/students.csv";
    private static readonly string teachersFileName = @"inputs/teachers.csv";

    private const char REPLACEMENT_CHARACTER = '\ufffd';
    

    public static async Task<Settings> LoadSettingsAsync()
    {
      Console.WriteLine($"Reading {settingsFileName}");
      var settingsText = await File.ReadAllTextAsync(settingsFileName);
      var settings = JsonConvert.DeserializeObject<Settings>(settingsText, new IsoDateTimeConverter { DateTimeFormat = "dd-MMM-yy" });

      Console.WriteLine($"Reading {daysFileName}");
      settings.DayTypes = new Dictionary<DateTime, string>();

      using (var fs = File.Open(daysFileName, FileMode.Open))
      using (var reader = new CsvReader(fs))
      {
        while (reader.HasMoreRecords)
        {
          var record = await reader.ReadDataRecordAsync();
          if (string.IsNullOrEmpty(record[0]))
          {
            continue;
          }
          var date = DateTime.ParseExact(record[0], "dd-MMM-yy", null);
          var weekType = record.Count > 1 ? record[1] : string.Empty;
          settings.DayTypes.Add(date, weekType + date.DayOfWeek.ToString("G").Substring(0, 3));
        }
      }
      return settings;
    }

    public static async Task<string> LoadKeyAsync()
    {
      Console.WriteLine($"Reading {keyFileName}");
      return await File.ReadAllTextAsync(keyFileName);
    }

    public static async Task<IList<Person>> LoadPeopleAsync()
    {
      var people = new List<Person>();
      var sufficientInput = false;

      if (File.Exists(studentsFileName))
      {
        Console.WriteLine($"Reading {studentsFileName}");
        people.AddRange(await LoadStudentsAsync());
        sufficientInput = true;
      }
      else
      {
        Console.WriteLine($"Not provided: {studentsFileName}");
      }

      if (File.Exists(teachersFileName))
      {
        Console.WriteLine($"Reading {teachersFileName}");
        people.AddRange(await LoadTeachersAsync());
        sufficientInput = true;
      }
      else
      {
        Console.WriteLine($"Not provided: {studentsFileName}");
      }

      if (!sufficientInput)
      {
        throw new InvalidOperationException($"You must include at least one of '{studentsFileName}' and '{teachersFileName}'.");
      }
      
      if (people.Count == 0)
      {
        throw new InvalidOperationException($"No students or teachers were found in the input files.");
      }

      return people;
    }

    private static async Task<IEnumerable<Person>> LoadStudentsAsync()
    {
      var students = new List<Person>();

      using (var fs = File.Open(studentsFileName, FileMode.Open))
      using (var reader = new CsvReader(fs))
      {
        Person currentStudent = null;
        string currentSubject = null;

        while (reader.HasMoreRecords)
        {
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
              currentStudent = new Person
              {
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
            throw new InvalidOperationException("Incorrectly formatted timetable (students).");
          }

          currentStudent.Lessons.Add(new Lesson
          {
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
      var teachers = new List<Person>();

      using (var fs = File.Open(teachersFileName, FileMode.Open))
      using (var reader = new CsvReader(fs))
      {
        var periodCodes = await reader.ReadDataRecordAsync();

        while (reader.HasMoreRecords)
        {
          var timetable = await reader.ReadDataRecordAsync();
          if (!reader.HasMoreRecords)
          {
            throw new InvalidOperationException("Incorrectly formatted timetable (teachers).");
          }
          var rooms = await reader.ReadDataRecordAsync();
          var currentTeacher = new Person { Email = timetable[0].ToLower(), Lessons = new List<Lesson>() };

          for (var i = 1; i < timetable.Count; i++)
          {
            if (string.IsNullOrEmpty(timetable[i]))
            {
              continue;
            }
            currentTeacher.Lessons.Add(new Lesson
            {
              PeriodCode = periodCodes[i],
              Class = timetable[i].Trim().TrimEnd(new[] { REPLACEMENT_CHARACTER }),
              Room = rooms[i].Trim().TrimEnd(new[] { REPLACEMENT_CHARACTER })
            });
          }

          teachers.Add(currentTeacher);
        }
      }
      return teachers;
    }

  }
}
