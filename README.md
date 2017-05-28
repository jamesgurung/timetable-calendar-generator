# Timetable Calendar Generator :calendar:

This is a command line tool for bulk generating student and teacher calendars. It can also be used by an individual for generating their own calendar.

![Student timetable](tutorial/student-timetable.png)

### Usage

1. Ensure you have the [.NET Core runtime](https://www.microsoft.com/net/download/core#/runtime) installed
1. Download [timetable-calendar-generator-1.0.zip](https://github.com/jamesgurung/timetable-calendar-generator/releases/download/v1.0/timetable-calendar-generator-1.0.zip) and extract the contents
1. In the same folder, create the input files defined below
1. Open a command line and run `dotnet makecal.dll -s -t`

### Input files

#### timings.csv

Each lesson's start time and duration in minutes.

```
08:50,60
09:55,60
11:15,60
12:20,60
14:00,60
15:05,60
```

#### days.csv

Each teaching day in the school year, in `dd-MMM-yy` format, followed by a numerical week indicator (i.e. Week 1 or Week 2). Non-teaching days such as weekends and holidays should be omitted. This file can be created in a spreadsheet app.

```
05-Sep-16,1
06-Sep-16,1
07-Sep-16,1
08-Sep-16,1
09-Sep-16,1
12-Sep-16,2
...
```

#### students.csv

To generate student timetables, set the `-s` flag. If the flag is set then this file is required.

It can be run as a spreadsheet report from your MIS and then exported to CSV. Periods must be in the format `1Mon:2` (meaning Week 1 Monday Period 2). Whitespace not required.

```
Email               , Subject  , Period , Room , Initials
student1@school.org , Business , 1Mon:3 , D5   , JGO
                    ,          , 1Tue:5 , D5   , JGO
                    ,          , 1Thu:1 , D5   , JGO
                    , English  , 1Thu:3 , E1   , CST
                    ,          , 2Thu:3 , E1   , CST
student2@school.org , P.E.     , 1Tue:3 ,      , DBA
...
```

#### teachers.csv

To generate teacher timetables, set the `-t` flag. If the flag is set then this file is required.

```
Email               , Class    , Period , Room
teacher1@school.org , 11ab/Ma4 , 1Mon:3 , M4
                    ,          , 1Tue:5 , M4
                    ,          , 1Thu:1 , M4
                    , 8c/Ma3   , 1Thu:3 , M4
                    ,          , 2Thu:3 , M4
teacher2@school.org , 10ab/Pe1 , 1Tue:3 ,
...
```

### Output

The tool generates each person's calendar in CSV format. The files can then be shared with individuals along with [these instructions for importing to Google Calendar](tutorial/google-calendar.md).

### Contributing

Pull requests are welcome.
