using System.Collections.Generic;

namespace makecal
{
  public class Person
  {
    public string Email { get; set; }
    public int? YearGroup { get; set; }
    public List<Lesson> Lessons { get; set; }
  }
}
