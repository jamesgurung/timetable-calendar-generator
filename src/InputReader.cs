using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KBCsv;

namespace makecal
{
  public static class InputReader
  {
    private const string SettingsFileName = @"inputs/settings.json";
    private const string GoogleKeyFileName = @"inputs/google-key.json";
    private const string MicrosoftKeyFileName = @"inputs/microsoft-key.json";
    private const string DaysFileName = @"inputs/days.csv";
    private const string StudentsFileName = @"inputs/students.csv";
    private const string TeachersFileName = @"inputs/teachers.csv";

    private const char ReplacementCharacter = '\ufffd';

    public static async Task<Settings> LoadSettingsAsync()
    {
      Settings settings;
      Console.WriteLine($"Reading {SettingsFileName}");
      await using (var fs = File.OpenRead(SettingsFileName))
      {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonDateConverter());
        settings = await JsonSerializer.DeserializeAsync<Settings>(fs, options);
      }

      if (settings?.Timings is null || settings.Timings.Count == 0) throw new Exception("Invalid settings file.");

      Console.WriteLine($"Reading {DaysFileName}");
      settings.DayTypes = new Dictionary<DateTime, string>();

      await using (var fs = File.OpenRead(DaysFileName))
      using (var reader = new CsvReader(fs))
      {
        while (reader.HasMoreRecords)
        {
          var record = await reader.ReadDataRecordAsync();
          if (string.IsNullOrEmpty(record[0]))
          {
            continue;
          }
          var date = DateTime.ParseExact(record[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);
          var weekType = record.Count > 1 ? record[1] : string.Empty;
          settings.DayTypes.Add(date, weekType + date.DayOfWeek.ToString("G")[..3]);
        }
      }
      return settings;
    }

    public static async Task<string> LoadGoogleKeyAsync()
    {
      Console.WriteLine($"Reading {GoogleKeyFileName}");
      return await File.ReadAllTextAsync(GoogleKeyFileName);
    }

    public static async Task<MicrosoftClientKey> LoadMicrosoftKeyAsync()
    {
      Console.WriteLine($"Reading {MicrosoftKeyFileName}");
      await using var fs = File.OpenRead(MicrosoftKeyFileName);
      var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
      return await JsonSerializer.DeserializeAsync<MicrosoftClientKey>(fs, options);
    }

    public static async Task<IList<Person>> LoadPeopleAsync()
    {
      var people = new List<Person>();
      var sufficientInput = false;

      if (File.Exists(StudentsFileName))
      {
        Console.WriteLine($"Reading {StudentsFileName}");
        people.AddRange(await LoadStudentsAsync());
        sufficientInput = true;
      }
      else
      {
        Console.WriteLine($"Not provided: {StudentsFileName}");
      }

      if (File.Exists(TeachersFileName))
      {
        Console.WriteLine($"Reading {TeachersFileName}");
        people.AddRange(await LoadTeachersAsync());
        sufficientInput = true;
      }
      else
      {
        Console.WriteLine($"Not provided: {StudentsFileName}");
      }

      if (!sufficientInput)
      {
        throw new InvalidOperationException($"You must include at least one of '{StudentsFileName}' and '{TeachersFileName}'.");
      }
      
      if (people.Count == 0)
      {
        throw new InvalidOperationException("No students or teachers were found in the input files.");
      }

      return people;
    }

    private static async Task<IEnumerable<Person>> LoadStudentsAsync()
    {
      var students = new List<Person>();

      await using var fs = File.OpenRead(StudentsFileName);
      using var reader = new CsvReader(fs);
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
          var newEmail = record[StudentFields.Email].ToLowerInvariant();
          if (currentStudent?.Email != newEmail)
          {
            var yearString = record[StudentFields.Year];
            currentStudent = new Person
            {
              Email = newEmail,
              YearGroup = int.Parse(yearString.StartsWith("Year ") ? yearString[5..] : yearString),
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

        if (currentStudent is null || currentSubject is null)
        {
          throw new InvalidOperationException("Incorrectly formatted timetable (students).");
        }

        currentStudent.Lessons.Add(new()
        {
          PeriodCode = record[StudentFields.Period],
          Class = currentSubject,
          Room = record[StudentFields.Room],
          Teacher = record[StudentFields.Teacher]
        });
      }

      return students;
    }

    private static async Task<IEnumerable<Person>> LoadTeachersAsync()
    {
      var teachers = new List<Person>();

      await using var fs = File.OpenRead(TeachersFileName);
      using var reader = new CsvReader(fs);
      var periodCodes = await reader.ReadDataRecordAsync();

      while (reader.HasMoreRecords)
      {
        var timetable = await reader.ReadDataRecordAsync();
        if (!reader.HasMoreRecords)
        {
          throw new InvalidOperationException("Incorrectly formatted timetable (teachers).");
        }
        var rooms = await reader.ReadDataRecordAsync();
        var currentTeacher = new Person { Email = timetable[0].ToLowerInvariant(), Lessons = new List<Lesson>() };

        for (var i = 1; i < timetable.Count; i++)
        {
          if (string.IsNullOrEmpty(timetable[i]))
          {
            continue;
          }
          var classCode = timetable[i].Trim().TrimEnd(new[] { ReplacementCharacter });
          currentTeacher.Lessons.Add(new()
          {
            PeriodCode = periodCodes[i],
            Class = classCode,
            YearGroup = GetYearFromClassName(classCode),
            Room = rooms[i].Trim().TrimEnd(new[] { ReplacementCharacter })
          });
        }

        teachers.Add(currentTeacher);
      }

      return teachers;
    }

    private static int? GetYearFromClassName(string className)
    {
      var yearDigits = className.TakeWhile(char.IsDigit).ToArray();
      return yearDigits.Length == 0 ? null : int.Parse(new string(yearDigits));
    }

  }
}
