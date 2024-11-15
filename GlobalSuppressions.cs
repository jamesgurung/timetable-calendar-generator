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
  Justification = "Object lifecycle and disposal is managed by the class", Scope = "type", Target = "~T:TimetableCalendarGenerator.MicrosoftUnlimitedBatch`1")]

[assembly: SuppressMessage("Naming", "CA1724:Type names should not match namespaces",
  Justification = "Clashes are not ambiguous.")]

[assembly: SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code",
  Justification = "Analyser error", Scope = "member", Target = "~M:TimetableCalendarGenerator.CalendarGenerator.Generate(TimetableCalendarGenerator.Person)")]

[assembly: SuppressMessage("Performance", "CA1852:Seal internal types",
  Justification = "Analyser error", Scope = "type", Target = "~T:TimetableCalendarGenerator.MicrosoftUnlimitedBatch`1")]

[assembly: SuppressMessage("Style", "IDE0130:Namespace does not match folder structure",
  Justification = "Intentional")]

[assembly: SuppressMessage("Style", "IDE0072:Add missing cases",
  Justification = "Intent is already clear", Scope = "member", Target = "~M:TimetableCalendarGenerator.CalendarWriterFactory.GetCalendarWriter(System.String)")]

[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression",
  Justification = "Readability", Scope = "member", Target = "~M:TimetableCalendarGenerator.EventComparer`1.Equals(`0,`0)")]

[assembly: SuppressMessage("Style", "IDE0058:Expression value is never used",
  Justification = "Use of discards is unnecessary")]

[assembly: SuppressMessage("Style", "IDE0010:Add missing cases",
  Justification = "Intent is already clear", Scope = "member", Target = "~M:TimetableCalendarGenerator.CalendarWriterFactory.GetCalendarWriter(System.String)")]

[assembly: SuppressMessage("Maintainability", "CA1515:Consider making public types internal",
  Justification = "Public types are preferred")]