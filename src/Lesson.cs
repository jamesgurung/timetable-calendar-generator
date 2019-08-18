﻿using System.Linq;

namespace makecal
{
  public class Lesson
  {
    public string PeriodCode { get; set; }
    public string Room { get; set; }
    public string Class { get; set; }
    public string Teacher { get; set; }

    public int? YearGroup
    {
      get {
        var year = new string(Class.TakeWhile(char.IsDigit).ToArray());
        if (year == string.Empty)
        {
          return null;
        }
        return int.Parse(year);
      }
    }
  }
}
