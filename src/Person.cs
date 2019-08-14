using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace makecal
{
  public class Person
  {
    private static EmailAddressAttribute validator = new EmailAddressAttribute();

    private string _email;
    public string Email
    {
      get {
        return _email;
      }
      set {
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
