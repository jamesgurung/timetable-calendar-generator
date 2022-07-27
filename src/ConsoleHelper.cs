namespace TimetableCalendarGenerator;

public static class ConsoleHelper
{
  public static int HeaderHeight => 10;
  public static int FooterHeight => 3;

  private const int StatusCol = 50;
  private const int StatusWidth = 30;
  private static readonly ConsoleColor defaultBackground = Console.BackgroundColor;
  private static readonly object consoleLock = new();

  public static int MinConsoleWidth => StatusCol + StatusWidth;

  private static void Write(int line, int col, string text, ConsoleColor? colour = null)
  {
    lock (consoleLock)
    {
      colour ??= defaultBackground;
      Console.SetCursorPosition(col, line);
      Console.BackgroundColor = defaultBackground;
      Console.Write(new string(' ', StatusWidth));
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
    Write(line, StatusCol, text, colour);
  }

  public static void WriteError(string message)
  {
    lock (consoleLock)
    {
      Console.WriteLine();
      var backgroundColor = Console.BackgroundColor;
      Console.BackgroundColor = ConsoleColor.DarkRed;
      Console.WriteLine($"Error: {message}");
      Console.BackgroundColor = backgroundColor;
      Console.WriteLine();
    }
  }

}