using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace makecal
{
  public static class Program
  {
    private static async Task Main(string[] args)
    {
      try
      {
        Console.Clear();
        Console.CursorVisible = false;
        Console.WriteLine("TIMETABLE CALENDAR GENERATOR\n");

        var argumentParser = new ArgumentParser(args);
        var outputFormat = argumentParser.OutputFormat;

        var settings = await InputReader.LoadSettingsAsync();
        var serviceAccountKey = (outputFormat.Type == OutputType.GoogleCalendar) ? await InputReader.LoadKeyAsync() : null;

        var people = await InputReader.LoadPeopleAsync();

        var calendarGenerator = new CalendarGenerator(settings);
        var calendarWriterFactory = new CalendarWriterFactory(outputFormat.Type, serviceAccountKey);
        
        Console.SetBufferSize(Math.Max(ConsoleHelper.MinConsoleWidth, Console.BufferWidth), Math.Max(ConsoleHelper.HeaderHeight + people.Count + ConsoleHelper.FooterHeight, Console.BufferHeight));
        Console.WriteLine($"\nGenerating {outputFormat.Name} calendars:");

        var writeTasks = new List<Task>();
        var throttler = new SemaphoreSlim(outputFormat.SimultaneousRequests);

        for (var i = 0; i < people.Count; i++)
        {
          var countLocal = i;
          await throttler.WaitAsync();
          var person = people[countLocal];
          var line = countLocal + ConsoleHelper.HeaderHeight;
          ConsoleHelper.WriteDescription(line, $"({countLocal + 1}/{people.Count}) {person.Email}");
          ConsoleHelper.WriteStatus(line, "...");

          writeTasks.Add(Task.Run(async () =>
          {
            try
            {
              var events = calendarGenerator.Generate(person);
              var calendarWriter = calendarWriterFactory.GetCalendarWriter(person.Email);
              await calendarWriter.WriteAsync(events);
              ConsoleHelper.WriteStatus(line, "Done.");
            }
            catch (Exception exc)
            {
              ConsoleHelper.WriteStatus(line, $"Failed. {exc.Message}", ConsoleColor.Red);
            }
            finally
            {
              throttler.Release();
            }
          }));
        }
        await Task.WhenAll(writeTasks);
        Console.SetCursorPosition(0, ConsoleHelper.HeaderHeight + people.Count);
        Console.WriteLine("\nCalendar generation complete.\n");
      }
      catch (Exception exc)
      {
        ConsoleHelper.WriteError(exc.Message);
      }
      finally
      {
        Console.CursorVisible = true;
      }
    }

  }
}
