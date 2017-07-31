# Timetable Calendar Generator :calendar:

This is a command line tool for bulk generating student and teacher timetables on Google Calendar.

![Student timetable](student-timetable.png)

### Usage

1. Ensure you have the [.NET Core 2.0 runtime](https://www.microsoft.com/net/download/core#/runtime) installed
1. Download [timetable-calendar-generator-2.1.zip](https://github.com/jamesgurung/timetable-calendar-generator/releases/download/v2.1/timetable-calendar-generator-2.1.zip) and extract the contents
1. In the same folder, add a directory called "inputs" and create the input files defined below
1. Open a command line and run `dotnet makecal.dll`

### Input files

#### settings.json

Configure lesson timings, study leave dates and periods to override for all users.

```
{
  "lessonTimes":
  [
    { "startTime": "08:50", "duration": 60 },
    { "startTime": "09:55", "duration": 60 },
    { "startTime": "11:15", "duration": 60 },
    { "startTime": "12:20", "duration": 60 },
    { "startTime": "14:00", "duration": 60 },
    { "startTime": "15:05", "duration": 60 }
  ],
  "studyLeave":
  [
    { "year": 11, "startDate": "04-Jun-18", endDate: "20-Jul-18" },
    { "year": 12, "startDate": "11-May-18", endDate: "10-Jun-18" },
    { "year": 13, "startDate": "25-May-18", endDate: "20-Jul-18" }
  ],
  "overrides":
  [
    { "date": "07-Sep-17", "period": 1, "title": "Tutorial" },
    { "date": "20-Dec-17", "period": 3, "title": "Whole school assembly" },
    { "date": "20-Dec-17", "period": 4, "title": "" },
    { "date": "20-Dec-17", "period": 5, "title": "" }
  ]
}

```

#### key.json

The private key file for your service account, which can be downloaded from the [Google Cloud Platform console](https://console.cloud.google.com/apis/credentials). Your service account needs domain-delegated authority to the scope `https://www.googleapis.com/auth/calendar`.

#### days.csv

Each teaching day in the school year, in `dd-MMM-yy` format, followed by a numerical week indicator (i.e. Week 1 or Week 2). Non-teaching days such as weekends and holidays should be omitted. This file can be created in a spreadsheet app.

```
06-Sep-17,1
07-Sep-17,1
08-Sep-17,1
11-Sep-17,2
...
```

#### students.csv

This can be run as a spreadsheet report from your MIS and then exported to CSV. Periods must be in the format `1Mon:2` (meaning Week 1 Monday Period 2). Whitespace not required.

```
Email               , Year , Subject  , Period , Room , Teacher
student1@school.org , 10   , Business , 1Mon:3 , D5   , JGO
                    ,      ,          , 1Tue:5 , D5   , JGO
                    ,      ,          , 1Thu:1 , D5   , JGO
                    ,      , English  , 1Thu:3 , E1   , CST
                    ,      ,          , 2Thu:3 , E1   , CST
student2@school.org , 11   , P.E.     , 1Tue:3 ,      , DBA
...
```

#### teachers.csv

This takes a different format. There is a column for each period in the timetable, and two rows for each teacher. The first row contains class codes, and the second contains room numbers.

```
Email               , 1Mon:1   , 1Mon:2   , 1Mon:3   , ...
teacher1@school.org , 10B/Ar1  , 13A/Ar1  , 9A/Ar1   , ...
                    , O3       , O6       , O3       , ...
teacher2@school.org ,          , 10ab/Ma4 , 8a/Ma3   , ...
                    ,          , M4       , M4       , ...
...
```

### Output

The tool creates a new Google Calendar for each user, called "My timetable", and fills this with their lessons for the year.

### Contributing

Pull requests are welcome.
