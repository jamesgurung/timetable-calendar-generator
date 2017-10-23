using System;

namespace makecal
{
  public static class ConsoleHelper
  {
    public static int HeaderHeight { get; } = 10;
    public static int FooterHeight { get; } = 3;

    private static readonly int statusCol = 50;
    private static readonly int statusWidth = 30;
    private static readonly ConsoleColor defaultBackground = Console.BackgroundColor;
    private static readonly object consoleLock = new object();

    private const char FULL_BLOCK = '\u2588';

    public static int MinConsoleWidth => statusCol + statusWidth;

    private static void Write(int line, int col, string text, ConsoleColor? colour = null)
    {
      lock (consoleLock)
      {
        if (colour == null)
        {
          colour = defaultBackground;
        }
        Console.SetCursorPosition(col, line);
        Console.BackgroundColor = defaultBackground;
        Console.Write(new string(' ', statusWidth));
        Console.SetCursorPosition(col, line);
        Console.BackgroundColor = colour.Value;
        Console.Write(text);
        Console.BackgroundColor = defaultBackground;
      }
    }

    public static void WriteDescription(int line, string text, ConsoleColor? colour = null)
    {
      Write(line, 0, text, colour);
    }

    public static void WriteStatus(int line, string text, ConsoleColor? colour = null)
    {
      Write(line, statusCol, text, colour);
    }

    public static void WriteProgress(int line, int progress) {
      if (progress < 0 || progress > 2)
      {
        throw new ArgumentOutOfRangeException(nameof(progress));
      }
      var status = new string(FULL_BLOCK, progress).PadRight(2);
      Write(line, statusCol, $"|{status}|");
    }

    public static void WriteError(string message)
    {
      lock (consoleLock)
      {
        Console.WriteLine();
        var backgroundColor = Console.BackgroundColor;
        Console.BackgroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {message}");
        Console.BackgroundColor = backgroundColor;
        Console.WriteLine();
      }
    }

  }
}