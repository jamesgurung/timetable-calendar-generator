using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KBCsv;

namespace makecal
{
  public class CsvCalendarWriter : ICalendarWriter
  {
    private string OutputFileName { get; }

    public CsvCalendarWriter(string outputFileName)
    {
      OutputFileName = outputFileName;
    }

    public async Task WriteAsync(IList<CalendarEvent> events)
    {
      var headerRecord = new HeaderRecord("Subject", "Start Date", "Start Time", "End Date", "End Time", "Location");
      var dataRecords = events.Select(o => new DataRecord(
        null,
        o.Title,
        o.Start.ToString("M/d/yyyy", CultureInfo.InvariantCulture),
        o.Start.ToString("H:mm:ss", CultureInfo.InvariantCulture),
        o.End.ToString("M/d/yyyy", CultureInfo.InvariantCulture),
        o.End.ToString("H:mm:ss", CultureInfo.InvariantCulture),
        o.Location ?? string.Empty
      ));
      var records = new RecordBase[] { headerRecord }.Union(dataRecords).ToArray();

      await using var streamWriter = new StreamWriter(OutputFileName);
      using var writer = new CsvWriter(streamWriter) { ForceDelimit = true };
      await writer.WriteRecordsAsync(records, 0, records.Length);
    }
  }
}
