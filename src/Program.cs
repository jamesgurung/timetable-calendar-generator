using Microsoft.Graph.Beta.Models.ODataErrors;

[assembly: CLSCompliant(true)]
namespace TimetableCalendarGenerator;

public static class Program
{
  private static async Task Main(string[] args)
  {
    try
    {
      Console.CursorVisible = false;

      var outputType = ArgumentParser.Parse(args);
      var settings = await InputReader.LoadSettingsAsync();
      var googleKey = outputType == OutputType.GoogleWorkspace ? await InputReader.LoadGoogleKeyAsync() : null;
      var microsoftKey = outputType == OutputType.Microsoft365 ? await InputReader.LoadMicrosoftKeyAsync() : null;
      var people = await InputReader.LoadPeopleAsync();

      var calendarGenerator = new CalendarGenerator(settings);
      var calendarWriterFactory = new CalendarWriterFactory(outputType, googleKey, microsoftKey, settings.StartDate, settings.EndDate);
      Console.WriteLine(calendarWriterFactory.DisplayText);

      var numberColumnWidth = ((int)Math.Log10(people.Count) * 2) + 5;
      var maxNameWidth = people.Max(o => o.Email.Length) + numberColumnWidth + 2;
      ConsoleHelper.ConfigureSize(people.Count, maxNameWidth);

      var writeTasks = new List<Task>(people.Count);
      using (var throttler = new SemaphoreSlim(calendarWriterFactory.SimultaneousRequests))
      {
        for (var i = 0; i < people.Count; i++)
        {
          await throttler.WaitAsync();
          ConsoleHelper.WriteName(i, $"({i + 1}/{people.Count})".PadRight(numberColumnWidth) + $" {people[i].Email}");
          ConsoleHelper.WriteStatus(i, "...");

          async Task WriteCalendarAsync(int index, Person person)
          {
            ICalendarWriter calendarWriter = null;
            try
            {
              calendarWriter = calendarWriterFactory.GetCalendarWriter(person.Email);
              var events = calendarGenerator.Generate(person);
              await calendarWriter.WriteAsync(events);
              ConsoleHelper.WriteStatus(index, "Done.");
            }
            catch (Exception exc)
            {
              var msg = (exc is ODataError odataError) ? odataError.Error.Message : exc.Message;
              ConsoleHelper.WriteStatus(index, msg, ConsoleColor.DarkRed);
            }
            finally
            {
              (calendarWriter as IDisposable)?.Dispose();
              throttler.Release();
            }
          }

          writeTasks.Add(WriteCalendarAsync(i, people[i]));
        }
        await Task.WhenAll(writeTasks);
      }

      ConsoleHelper.WriteFinishLine("Operation complete.");
    }
    catch (Exception exc)
    {
      ConsoleHelper.WriteFinishLine($"Error: {exc.Message}", ConsoleColor.DarkRed);
    }
    finally
    {
      Console.CursorVisible = true;
    }
  }
}