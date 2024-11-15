﻿using System.Globalization;
using System.Text.Json;
using KBCsv;

namespace TimetableCalendarGenerator;

public static class InputReader
{
  private const string SettingsFileName = "inputs/settings.json";
  private const string GoogleKeyFileName = "inputs/google-key.json";
  private const string MicrosoftKeyFileName = "inputs/microsoft-key.json";
  private const string DaysFileName = "inputs/days.csv";
  private const string StudentsFileName = "inputs/students.csv";
  private const string TeachersFileName = "inputs/teachers.csv";
  private const string EventsFileName = "inputs/events.csv";

  private const char ReplacementCharacter = '\ufffd';

  private static readonly SourceGenerationContext JsonCtx = new(new() { PropertyNameCaseInsensitive = true });

  public static async Task<Settings> LoadSettingsAsync()
  {
    Settings settings;
    Console.WriteLine($"Reading {SettingsFileName}");
    await using (var fs = File.OpenRead(SettingsFileName))
    {
      settings = await JsonSerializer.DeserializeAsync(fs, JsonCtx.Settings);
    }

    if (settings?.Timings is null || settings.Timings.Count == 0) throw new InvalidOperationException("Invalid settings file.");

    Console.WriteLine($"Reading {DaysFileName}");
    settings.DayTypes = new Dictionary<DateOnly, string>();

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
        var date = DateOnly.ParseExact(record[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var weekType = record.Count > 1 ? record[1] : string.Empty;
        settings.DayTypes.Add(date, settings.WeekTypeAsSuffix ? $"{date:ddd}{weekType}" : $"{weekType}{date:ddd}");
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
    return await JsonSerializer.DeserializeAsync(fs, JsonCtx.MicrosoftClientKey);
  }

  public static async Task<IList<Person>> LoadPeopleAsync()
  {
    var people = new List<Person>();
    var sufficientInput = false;
    ILookup<string, CalendarEvent> oneOffEvents = null;

    if (File.Exists(EventsFileName))
    {
      Console.WriteLine($"Reading {EventsFileName}");
      oneOffEvents = await LoadOneOffEventsAsync();
    }

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
      people.AddRange(await LoadTeachersAsync(oneOffEvents));
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

    Console.WriteLine();
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
            YearGroup = int.Parse(yearString.StartsWith("Year ", StringComparison.Ordinal) ? yearString[5..] : yearString, CultureInfo.InvariantCulture),
            Lessons = []
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

  private static async Task<IEnumerable<Person>> LoadTeachersAsync(ILookup<string, CalendarEvent> oneOffEvents)
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

      var email = timetable[0].ToLowerInvariant();
      var teacherEvents = oneOffEvents?[email].ToList() ?? [];

      var currentTeacher = new Person
      {
        Email = email,
        Lessons = [],
        OneOffEvents = teacherEvents
      };

      for (var i = 1; i < timetable.Count; i++)
      {
        if (string.IsNullOrEmpty(timetable[i]))
        {
          continue;
        }
        var classCode = timetable[i].Trim().TrimEnd([ReplacementCharacter]);
        currentTeacher.Lessons.Add(new()
        {
          PeriodCode = periodCodes[i],
          Class = classCode,
          YearGroup = GetYearFromClassName(classCode),
          Room = rooms[i].Trim().TrimEnd([ReplacementCharacter])
        });
      }

      teachers.Add(currentTeacher);
    }

    return teachers;
  }

  private static async Task<ILookup<string, CalendarEvent>> LoadOneOffEventsAsync()
  {
    var events = new List<KeyValuePair<string, CalendarEvent>>();

    await using var fs = File.OpenRead(EventsFileName);
    using var reader = new CsvReader(fs);
    await reader.ReadHeaderRecordAsync();

    while (reader.HasMoreRecords)
    {
      var row = await reader.ReadDataRecordAsync();
      var date = DateOnly.ParseExact(row["Date"], "yyyy-MM-dd", CultureInfo.InvariantCulture);
      var startTime = TimeSpan.ParseExact(row["Time"], "hh\\:mm", CultureInfo.InvariantCulture);
      var endTime = startTime.Add(TimeSpan.FromMinutes(int.Parse(row["Duration"], CultureInfo.InvariantCulture)));
      events.Add(new(row["Email"].ToLowerInvariant(), new CalendarEvent
      {
        Title = row["Title"],
        Location = row["Location"],
        Start = new DateTime(date.Year, date.Month, date.Day, startTime.Hours, startTime.Minutes, 0),
        End = new DateTime(date.Year, date.Month, date.Day, endTime.Hours, endTime.Minutes, 0),
      }));
    }

    return events.ToLookup(o => o.Key, o => o.Value);
  }

  private static int? GetYearFromClassName(string className)
  {
    var yearDigits = className.TakeWhile(c => c is >= '0' and <= '9').ToArray();
    return yearDigits.Length == 0 ? null : int.Parse(new string(yearDigits), CultureInfo.InvariantCulture);
  }

}