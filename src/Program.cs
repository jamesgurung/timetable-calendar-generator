using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
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

        var outputType = ArgumentParser.Parse(args);

        var settings = await InputReader.LoadSettingsAsync();
        var googleKey = outputType == OutputType.GoogleWorkspace ? await InputReader.LoadGoogleKeyAsync() : null;
        var microsoftKey = outputType == OutputType.Microsoft365 ? await InputReader.LoadMicrosoftKeyAsync() : null;

        var people = await InputReader.LoadPeopleAsync();

        var calendarGenerator = new CalendarGenerator(settings);
        var calendarWriterFactory = new CalendarWriterFactory(outputType, googleKey, microsoftKey);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
          Console.SetBufferSize(Math.Max(ConsoleHelper.MinConsoleWidth, Console.BufferWidth),
            Math.Max(ConsoleHelper.HeaderHeight + people.Count + ConsoleHelper.FooterHeight, Console.BufferHeight));
        }

        Console.WriteLine($"\n{calendarWriterFactory.DisplayText}:");

        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        var writeTasks = new List<Task>();
        using (var throttler = new SemaphoreSlim(calendarWriterFactory.SimultaneousRequests))
        {
          for (var i = 0; i < people.Count; i++)
          {
            await throttler.WaitAsync();
            var person = people[i];
            var line = i + ConsoleHelper.HeaderHeight;
            ConsoleHelper.WriteDescription(line, $"({i + 1}/{people.Count}) {person.Email}");
            ConsoleHelper.WriteStatus(line, "...");

            writeTasks.Add(Task.Run(async () =>
            {
              try
              {
                var calendarWriter = calendarWriterFactory.GetCalendarWriter(person.Email);
                var events = calendarGenerator.Generate(person);
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
        }

        Console.SetCursorPosition(0, ConsoleHelper.HeaderHeight + people.Count);
        Console.WriteLine("\nOperation complete.\n");
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
