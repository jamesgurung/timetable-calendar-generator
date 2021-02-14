using System.Text.Json;
using System;
using System.Text.Json.Serialization;
using System.Globalization;

namespace makecal
{
  public class JsonDateConverter : JsonConverter<DateTime>
  {
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      return DateTime.ParseExact(reader.GetString() ?? string.Empty, "dd-MMM-yy", CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
      writer.WriteStringValue(value.ToString("dd-MMM-yy"));
    }
  }
}