using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace TimetableCalendarGenerator;

public class JsonDateConverter : JsonConverter<DateTime>
{
  public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    return DateTime.ParseExact(reader.GetString() ?? string.Empty, "yyyy-MM-dd", CultureInfo.InvariantCulture);
  }

  public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
  {
    ArgumentNullException.ThrowIfNull(writer);
    writer.WriteStringValue(value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
  }
}