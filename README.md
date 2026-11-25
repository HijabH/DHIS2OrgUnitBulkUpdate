# DHIS2 Organisation Unit Bulk Update

![build-and-test](https://github.com/HijabH/DHIS2OrgUnitBulkUpdate/actions/workflows/ci.yml/badge.svg)

A C# / .NET 8 console application that updates organisation units (health facilities) in [DHIS2](https://dhis2.org) in bulk from an Excel sheet: their name, short name and parent in the hierarchy.

In a national DHIS2 system, facilities are renamed, merged and moved between districts all the time. Doing that by hand in the maintenance app for hundreds of units takes days. I wrote the first version of this tool in my job, where a tool like it handled more than 14,000 organisation units and cut the manual effort by about 70%. This public version runs against DHIS2's public demo server and contains no real data.

## Features

- Reads facilities from the first worksheet of an `.xlsx` file
- Validates every row before sending anything: UID format, parent UID, required names, DHIS2 length limits, duplicate IDs
- Updates each unit with a JSON Patch, so only the name, short name and parent change and every other field (opening date, code, coordinates) stays as it is
- `--dry-run` checks the sheet without contacting DHIS2
- Prints a result per row and a summary, and returns a non-zero exit code if anything failed
- Credentials come from environment variables, never from code

## Quick start

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/HijabH/DHIS2OrgUnitBulkUpdate.git
cd DHIS2OrgUnitBulkUpdate

# check the sheet only
dotnet run -- samples/orgunits-sample.xlsx --dry-run

# update the units on the DHIS2 demo server (see .env.example)
export DHIS2_BASE_URL=https://play.im.dhis2.org/stable-2-41-x
export DHIS2_USERNAME=admin
export DHIS2_PASSWORD=district
dotnet run -- samples/orgunits-sample.xlsx
```

On Windows PowerShell, set the variables with `$env:DHIS2_BASE_URL="..."`. The demo server is reset regularly and the version in its URL changes; pick a current one from [play.dhis2.org](https://play.dhis2.org) if needed.

## Excel format

Row 1 is a header. Columns, in this order:

| Column | Required | Description |
| --- | --- | --- |
| A `ParentID` | no | UID of the new parent unit; leave empty to keep the current parent |
| B `ID` | yes | UID of the organisation unit to update |
| C `Name` | yes | New full name, up to 230 characters |
| D `ShortName` | yes | New short name, up to 50 characters |

[`samples/orgunits-sample.xlsx`](samples/orgunits-sample.xlsx) updates Ngelehun CHC in the Sierra Leone demo database, and includes a duplicate row and an invalid ID so you can see how rejected rows are reported:

```text
Valid rows: 1, rejected rows: 2
  Row 3: duplicate ID 'DiszpKrYNg8', skipped
  Row 4: ID 'not-a-uid' is not a valid DHIS2 UID (11 characters, starting with a letter)
Dry run: nothing was sent to DHIS2.
```

## Project structure

| Path | Responsibility |
| --- | --- |
| `Program.cs` | Arguments, configuration and the update loop |
| `Services/ExcelDataReader.cs` | Reads the sheet (EPPlus) and filters out invalid and duplicate rows |
| `Services/HealthFacilityValidator.cs` | Row validation rules |
| `Services/DHIS2ApiClient.cs` | Builds the JSON Patch and calls `PATCH /api/organisationUnits/{id}` |
| `tests/` | xUnit tests |

## Tests

```bash
dotnet test
```

24 xUnit tests cover UID and row validation, duplicate handling, the JSON Patch content, the HTTP request (method, URL, content type, authentication) and error handling, using a stub HTTP handler instead of a real server. GitHub Actions runs them on every push.

## Version history

**Version 1 (2024)** was my original console application: it read the sheet with EPPlus and sent each unit to DHIS2.

**Version 2 (2026)** is a cleanup of my original code. The changes:

- **Fixed a data-loss bug.** Version 1 sent a `PUT` containing only the name, short name, parent and a fixed opening date of 1970-01-01. A `PUT` replaces the whole object in DHIS2, so every update reset the real opening date and could clear other fields. Version 2 sends a JSON Patch that changes only the columns in the sheet.
- Moved the login out of the code into environment variables
- Added validation of UIDs, names and lengths, with row numbers in every message
- Added a dry-run mode, a summary and exit codes
- Changed the project from a web project on .NET 6 (out of support) to a console project on .NET 8, and removed three unused packages
- Removed a launch profile that contained a local file path
- Added tests, CI, a sample sheet and this README

## Limitations

- Requires DHIS2 2.37 or later, for JSON Patch support
- Updates units one at a time; for very large batches the `/api/metadata` endpoint would be faster
- EPPlus 7 is free for non-commercial use only

## License

[MIT](LICENSE)
