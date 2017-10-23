using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KBCsv;

namespace makecal
{
  class CsvCalendarWriter : ICalendarWriter
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
        o.Start.ToString("M/d/yyyy"),
        o.Start.ToString("H:mm:ss"),
        o.End.ToString("M/d/yyyy"),
        o.End.ToString("H:mm:ss"),
        o.Location ?? string.Empty
      ));
      var records = new RecordBase[] { headerRecord }.Union(dataRecords).ToArray();

      using (var streamWriter = new StreamWriter(OutputFileName))
      using (var writer = new CsvWriter(streamWriter))
      {
        writer.ForceDelimit = true;
        await writer.WriteRecordsAsync(records, 0, records.Length);
      }
    }
  }
}