using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace makecal
{
  public class Person
  {
    private static readonly EmailAddressAttribute validator = new();

    private string _email;
    public string Email
    {
      get => _email;
      set
      {
        if (string.IsNullOrEmpty(value) || !validator.IsValid(value))
        {
          throw new InvalidOperationException("Invalid email address: " + value);
        }
        _email = value;
      }
    }
    public int? YearGroup { get; set; }
    public List<Lesson> Lessons { get; set; }
  }
}
