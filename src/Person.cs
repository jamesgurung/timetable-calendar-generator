using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TimetableCalendarGenerator;

public class Person
{
  private static readonly EmailAddressAttribute Validator = new();

  private readonly string _email;
  public string Email
  {
    get => _email;
    init
    {
      if (string.IsNullOrEmpty(value) || !Validator.IsValid(value))
      {
        throw new InvalidOperationException("Invalid email address: " + value);
      }
      _email = value;
    }
  }
  public IList<Lesson> Lessons { get; init; }
  public int? YearGroup { get; init; }
}