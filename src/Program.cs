using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace makecal
{
  public static class Program
  {
    private static readonly int headerHeight = 10;
    private static readonly int footerHeight = 3;

    private static readonly int maxAttempts = 4;
    private static readonly int retryFirst = 5000;
    private static readonly int retryExponent = 4;

    private static async Task Main(string[] args)
    {
      try
      {
        Console.Clear();
        Console.CursorVisible = false;

        Console.WriteLine("TIMETABLE CALENDAR GENERATOR\n");

        var argumentParser = new ArgumentParser(args);
        var outputFormat = argumentParser.GetOutputFormat();

        var settings = await InputReader.LoadSettingsAsync();
        var serviceAccountKey = (outputFormat.Type == OutputType.GoogleCalendar) ? await InputReader.LoadKeyAsync() : null;

        var people = await InputReader.LoadPeopleAsync();

        var calendarGenerator = new CalendarGenerator(settings);
        var calendarWriterFactory = new CalendarWriterFactory(outputFormat.Type, serviceAccountKey);
        
        Console.SetBufferSize(Math.Max(ConsoleHelper.MinConsoleWidth, Console.BufferWidth), Math.Max(headerHeight + people.Count + footerHeight, Console.BufferHeight));
        Console.WriteLine($"\nGenerating {outputFormat.Name} calendars:");

        var writeTasks = new List<Task>();
        var throttler = new SemaphoreSlim(outputFormat.SimultaneousRequests);

        for (var i = 0; i < people.Count; i++)
        {
          var countLocal = i;
          await throttler.WaitAsync();
          var person = people[countLocal];
          var line = countLocal + headerHeight;
          ConsoleHelper.WriteDescription(line, $"({countLocal + 1}/{people.Count}) {person.Email}");
          ConsoleHelper.WriteProgress(line, 0);

          writeTasks.Add(Task.Run(async () =>
          {
            try
            {              
              for (var attempt = 1; attempt <= maxAttempts; attempt++)
              {
                try
                {
                  var calendarWriter = calendarWriterFactory.GetCalendarWriter(person.Email);
                  await calendarWriter.PrepareAsync();
                  ConsoleHelper.WriteProgress(line, 1);

                  var events = calendarGenerator.Generate(person);
                  await calendarWriter.WriteAsync(events);
                  ConsoleHelper.WriteProgress(line, 2);

                  break;
                }
                catch (Google.GoogleApiException) when (attempt < maxAttempts)
                {
                  var backoff = retryFirst * (int)Math.Pow(retryExponent, attempt - 1);
                  ConsoleHelper.WriteStatus(line, $"Error. Retrying ({attempt} of {maxAttempts - 1})...", ConsoleColor.DarkYellow);
                  await Task.Delay(backoff);
                  ConsoleHelper.WriteProgress(line, 0);
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
        await Task.WhenAll(writeTasks);
        Console.SetCursorPosition(0, headerHeight + people.Count);
        Console.WriteLine("\nCalendar generation complete.\n");
      }
      catch (Exception exc)
      {
        ConsoleHelper.WriteError(exc.Message);
      }
      finally
      {
        Console.CursorVisible = true;
#if DEBUG
        Console.ReadKey();
#endif
      }
    }

  }
}