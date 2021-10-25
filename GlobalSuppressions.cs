using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task",
  Justification = "Not required for non-library Console app")]

[assembly: SuppressMessage("Usage", "CA2227:Collection properties should be read only",
  Justification = "Required for serialization", Scope = "type", Target = "~T:TimetableCalendarGenerator.Settings")]

[assembly: SuppressMessage("Usage", "CA2227:Collection properties should be read only",
  Justification = "Required for serialization", Scope = "type", Target = "~T:TimetableCalendarGenerator.Absence")]

[assembly: SuppressMessage("Usage", "CA2227:Collection properties should be read only",
  Justification = "Required for serialization", Scope = "type", Target = "~T:TimetableCalendarGenerator.Override")]

[assembly: SuppressMessage("Usage", "CA2227:Collection properties should be read only",
  Justification = "Required for serialization", Scope = "type", Target = "~T:TimetableCalendarGenerator.Timing")]

[assembly: SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters",
  Justification = "Strings will not be localized")]

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
  Justification = "Catch-all is intended")]

[assembly: SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase",
  Justification = "Emails are case insensitive", Scope = "type", Target = "~T:TimetableCalendarGenerator.InputReader")]

[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
  Justification = "Object lifecycle and disposal is managed by the class", Scope = "type", Target = "~T:TimetableCalendarGenerator.MicrosoftUnlimitedBatch")]
