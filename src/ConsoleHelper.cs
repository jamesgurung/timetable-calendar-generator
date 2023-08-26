using System.Runtime.InteropServices;

namespace TimetableCalendarGenerator;

public static class ConsoleHelper
{
  private static int nameWidth;
  private static int statusWidth;
  private static int startLine;
  private static int lastLine;
  private static readonly ConsoleColor defaultBackground = Console.BackgroundColor;
  private static readonly object consoleLock = new();

  public static void ConfigureSize(int count, int maxNameWidth)
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      var bufferHeight = Console.CursorTop + count + 1;
      if (bufferHeight > short.MaxValue)
      {
        Console.Clear();
        bufferHeight = Console.CursorTop + count + 1;
      }
      if (Console.BufferHeight < bufferHeight)
      {
        Console.BufferHeight = bufferHeight;
      }
    }
    var width = Console.BufferWidth;
    nameWidth = Math.Min(maxNameWidth, width / 2);
    statusWidth = width - nameWidth - 1;
    startLine = Console.CursorTop;
  }

  public static void WriteName(int index, string text, ConsoleColor? backgroundColour = null)
  {
    ArgumentNullException.ThrowIfNull(text);
    Write(index, 0, nameWidth, text, backgroundColour);
  }

  public static void WriteStatus(int index, string text, ConsoleColor? backgroundColour = null)
  {
    ArgumentNullException.ThrowIfNull(text);
    Write(index, nameWidth + 1, statusWidth, text, backgroundColour);
  }

  public static void WriteFinishLine(string message, ConsoleColor? backgroundColour = null)
  {
    backgroundColour ??= defaultBackground;
    lock (consoleLock)
    {
      if (lastLine != default)
      {
        Console.SetCursorPosition(0, lastLine + 1);
      }
      Console.BackgroundColor = backgroundColour.Value;
      Console.WriteLine(message);
      Console.BackgroundColor = defaultBackground;
    }
  }

  private static void Write(int index, int col, int maxLength, string text, ConsoleColor? backgroundColour = null)
  {
    backgroundColour ??= defaultBackground;
    var line = index + startLine;
    if (text.Length > maxLength)
    {
      text = maxLength > 13 ? $"{text[..(maxLength - 3)]}..." : text[..maxLength];
    }
    lock (consoleLock)
    {
      Console.SetCursorPosition(col, line);
      Console.BackgroundColor = defaultBackground;
      Console.Write(new string(' ', maxLength));
      Console.SetCursorPosition(col, line);
      Console.BackgroundColor = backgroundColour.Value;
      Console.WriteLine(text);
      Console.BackgroundColor = defaultBackground;
      if (line > lastLine)
      {
        lastLine = line;
      }
    }
  }

}