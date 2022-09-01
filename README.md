# Timetable Calendar Generator :calendar:

This is a cross-platform command line tool for bulk generating student and teacher timetables. It can create calendar files in comma-separated (.csv) or iCal (.ics) format, or **sync directly to Google Workspace or Microsoft 365 calendars**.

![Student timetable](resources/example.png)

### Usage on Windows

1. Download the latest `win-x64` ZIP package from our [Releases page](https://github.com/jamesgurung/timetable-calendar-generator/releases) and extract the contents.
1. In the "inputs" directory, add the input files defined below.
1. Open a command line and run one of the following commands:
    1. `makecal --csv` to generate comma-separated (.csv) calendar files
    1. `makecal --ical` to generate iCalendar (.ics) files
    1. `makecal --google` to directly sync timetables to Google Workspace calendars
    1. `makecal --microsoft` to directly sync timetables to Microsoft 365 calendars

### Usage on other platforms

1. Ensure you have the [.NET 6 runtime](https://dotnet.microsoft.com/download/dotnet/6.0) installed (on the download page, look for the latest ".NET Runtime 6.0.x" heading in the right-hand column).
1. Download and extract the `xplat` ZIP package from our [Releases page](https://github.com/jamesgurung/timetable-calendar-generator/releases).
1. In the "inputs" directory, add the input files defined below.
1. Run commands in the format: `dotnet makecal.dll --csv`

### Input files

#### settings.json

This file is required to configure:

* Daily **`timings`**, which can be customised for specific `days` and/or `yearGroups`
* Year group **`absences`** (e.g. for study leave or a staggered start of term)
* Period **`overrides`** (e.g. whole-school tutorials or early finishes)
* Lesson **`renames`**

```
{
  "timings":
  [
    { "period": "Tut", "startTime": "08:00", "duration": 45, "yearGroups": [11] },
    { "period": "Tut", "startTime": "08:30", "duration": 15 },
    { "period": "1"  , "startTime": "08:50", "duration": 60 },
    { "period": "2"  , "startTime": "09:55", "duration": 60 },
    { "period": "3"  , "startTime": "11:15", "duration": 60 },
    { "period": "4"  , "startTime": "12:20", "duration": 80, "days": ["1Fri", "2Fri"] },
    { "period": "4"  , "startTime": "12:20", "duration": 60 },
    { "period": "5"  , "startTime": "14:00", "duration": 60 }
  ],
  "absences":
  [
    { "yearGroups": [11, 13], "startDate": "2023-05-27", "endDate": "2023-08-01" }
  ],
  "overrides":
  [
    { "date": "2022-09-07", "period": "1", "yearGroups": [8, 9, 10, 12], "title": "" },
    { "date": "2022-09-08", "period": "1", "copyFromPeriod": "AM" },
    { "date": "2022-12-16", "period": "4", "title": "Whole school assembly" },
    { "date": "2022-12-16", "period": "5", "title": "" }
  ],
  "renames":
  [
    { "originalTitle": "PPA", "newTitle": "" }
  ]
}
```
If you specify multiple timings for the same `period`, then when creating each event the app will use the first entry matching any `days` and `yearGroups` filters. Make sure a fallback entry (with no filters) is always provided.

Overriding or renaming a lesson to a blank string (`""`) will prevent a calendar event from being created at that time. The `copyFromPeriod` option can be used to clone another lesson in the same day, for example to create an extended tutor period.

#### days.csv

List each teaching day in the school year, in `yyyy-MM-dd` format, followed by a week indicator (i.e. Week 1 or Week 2). Non-teaching days such as weekends and holidays should be excluded. This file can be created in a spreadsheet app.

```
2022-09-07,1
2022-09-08,1
2022-09-09,1
2022-09-12,2
...
```
For schools which use a one-week timetable, the second column should be omitted so the file only contains a list of working days.

#### students.csv

This can be run as a spreadsheet report from your MIS and then exported to CSV. Periods must be in the format `1Mon:2` (meaning Week 1 Monday Period 2). Whitespace is not required.

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
SIMS users can download the report [SIMS-StudentTimetables.RptDef](https://github.com/jamesgurung/timetable-calendar-generator/raw/master/resources/SIMS-StudentTimetables.RptDef).

#### teachers.csv

This takes a different format. There is a column for each period in the timetable, and two rows for each teacher: the first containing class codes, and the second containing room numbers. Whitespace is not required.

```
Email               , 1Mon:1   , 1Mon:2   , 1Mon:3   , ...
teacher1@school.org , 10B/Ar1  , 13A/Ar1  , 9A/Ar1   , ...
                    , O3       , O6       , O3       , ...
teacher2@school.org ,          , 10ab/Ma4 , 8a/Ma3   , ...
                    ,          , M4       , M4       , ...
...
```
To create this file in SIMS:

1. Click Reports -> Timetables -> All Staff Timetable.
1. Choose an Effective Date and click OK.
1. If needed, click the "Flip" button in the top-left corner. Teacher names should appear going down the page.
1. On the far right of the screen, click the button for "Show/Hide Cell Settings".
1. At the bottom of the Cell Settings pane, set "Number of Rows" to 2.
1. In the middle of the pane, there is a split box. Click and drag "Class#" to the top half, and "RM" to the lower half.
1. Back at the top-left of the screen, click "Export".
1. Change "HTML" to "Excel" and click OK.
1. When the spreadsheet opens, delete rows 1-4 which contain the header.
1. Replace staff names in the left-hand column with their email addresses. You may be able to do this with a `VLOOKUP` formula.
1. Save as `teachers.csv`

#### google-key.json

If you are using the `--google` flag to directly sync timetables to Google Calendar, your domain administrator will need to create a free service account key:

1. [Create a new project](https://console.cloud.google.com/projectcreate) on the Google Cloud Platform console.
1. [Enable the Google Calendar API.](https://console.cloud.google.com/apis/library/calendar-json.googleapis.com) Depending on the size of your school, you may also need to apply for a raised quota. The tool may use up to 1000 API requests per user when it is first run.
1. [Configure the OAuth consent screen.](https://console.cloud.google.com/apis/credentials/consent) Select "Internal" and set the app name to "Timetable Calendar Generator". Provide the email addresses as required. You do not need to add any scopes on the next screen.
1. [Create a new service account.](https://console.cloud.google.com/iam-admin/serviceaccounts) Give it any name, and skip both "Grant access" steps.
1. Once the service account is created, click Edit > Add key > Create new key > JSON. The service account's private key will be downloaded to your computer. Rename it to `google-key.json` and put it in the `inputs` folder.
1. Now delegate domain-wide authority to this service account:
    1. Still on the Edit page, tick "Enable G Suite domain-wide delegation", and save.
    1. On the Service Accounts overview page, click "View Client ID" and copy the long ID number.
    1. Open your Google Workspace [Admin console](https://admin.google.com/) and go to Main menu > Security > API controls.
    1. In the "Domain wide delegation" pane, select "Manage Domain Wide Delegation", and then "Add new".
    1. In the "Client ID" field enter the service account's Client ID which you copied earlier.
    1. In the "OAuth Scopes" field enter `https://www.googleapis.com/auth/calendar`
    1. Click "Authorize".

#### microsoft-key.json

This file is required if you are using the `--microsoft` flag to directly sync timetables to Microsoft 365.

```
{
  "clientId": "",
  "clientSecret": "",
  "tenantId": ""
}
```
To create these credentials, your domain administrator will need to set up a free App Registration:

1. Go to the [Azure Portal](https://portal.azure.com/) and sign in with your Microsoft 365 administrator account.
1. Use the search bar to go to "App registrations", and click "New registration". Name it "Timetable Calendar Generator", and select "Accounts in this organizational directory only".
1. Create a `microsoft-key.json` file with the format shown above, and set the `clientId` and `tenantId` as shown on your App Registration homepage.
1. Click "Certificates & secrets", then "New client secret", and create a secret with an appropriate expiry date. Copy the string from the "Value" column, and use this as your `clientSecret`.
1. Now click API permissions > Add a permission > Microsoft Graph > Application permissions. Select "Calendars > Calendars.ReadWrite" and "MailboxSettings > MailboxSettings.ReadWrite" (this is needed for adding a custom Timetable category to each user's calendar).
1. Once these permissions are added, click the "Grant admin consent" button.

### Output

The output depends on which flags are set:

#### `--csv` or `--ical`
Creates a "calendars" folder containing a CSV or ICS calendar file for each user. These files can be shared along with [instructions for importing to Google Calendar](import-tutorial.md) or any other calendar system. Note that the iCal format requires a timezone, and this is set to `Europe/London`.

#### `--google`
Synchronises each user's lessons directly to their Google Workspace calendar. The tool does not read or edit any events except for those which it creates itself (these are tagged with the extended property `makecal=true`).

#### `--microsoft`
Synchronises each user's lessons directly to their Microsoft 365 calendar. The tool does not read or edit any events except for those which it creates itself (these are tagged with the open extension `timetable-calendar-generator`). The Microsoft Graph API requires a timezone, and this is set to `Europe/London`.

### Automation

This app runs from the command line and supports automation. If you are running SIMS, you can set up a PowerShell script to generate `students.csv` using SIMS Command Reporter and then call `makecal` with your chosen parameters. This script can be run on a scheduled task; for example, weekly.

Note that `teachers.csv` cannot be generated by a script, due to its alternative format. This type of report is used because it includes meetings and other non-teaching periods, whereas regular SIMS reports do not. Teacher timetables do not tend to change very often, and on those occasions the report can be run manually.

### Contributing

If you have a question or feature request, please open an issue.

To contribute improvements to this project, or to adapt the code for the specific needs of your school, you are welcome to fork the repository.

Pull requests are welcome; please open an issue first to discuss.

### Credits

This project is maintained by [@jamesgurung](https://github.com/jamesgurung), who is a teacher at a UK secondary school.

Many thanks to [@jschneideruk](https://github.com/jschneideruk) for making Google Calendar updates much more efficient, and to [@timmy-mac](https://github.com/timmy-mac) for the update to use primary calendars. :+1:
