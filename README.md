# KNOT Localization extensions

A simple extension for [KNOT-Localization](https://github.com/V0odo0/KNOT-Localization) that lets you to populate and
update a localization database from Google spreadsheets.

### Installation

1. Install [Knot-Localization](https://github.com/V0odo0/KNOT-Localization)
2. Then install package by adding the following to Package Manager:

```
https://github.com/IlyaKvant/KNOT-Localization-extensions.git#1.0.0
```

or by adding

```
"com.ilyakvant.knotlocalizationextensions": "https://github.com/IlyaKvant/KNOT-Localization-extensions.git#1.0.0"
```

to the `Packages/manifest.json` file.

### Usage

- Create a KNOT database as usual;
- Add the necessary languages, the empty `knot key collection` and the `knot text collection` for each one. <br>If you
  already have a database set up with languages, that's fine. The data will be overwritten from the Google Sheet. Keys
  and languages that aren't in the sheet **won't be deleted**;
- Create a Google Spreadsheet for localization in the following format:

|               | English [en]               | German (Germany) [de-DE]              | ... |
|---------------|----------------------------|---------------------------------------|-----|
| UI.Title      | Localization - Showcase    | KNOT Localization - Demo              | ... |
| Pages.0.title | Startup Language selection | Startsprache definieren               | ... |
| Pages.1.title | Fallback Language          | Zur'ckgreifen auf die Standardsprache | ... |
| ...           | ...                        | ...                                   | ... |

> **Important**: Languages are matched based on the `Culture Name` suffix. <br>This means it is important that the end
> of the language name includes a `Culture Name` that is exactly the same as the one specified in the Knot database -
`[zh-CN], [ko-KR], [en], [de]` etc.

- Grant read access to the table via the link;

> The link should look something like this:<br>
`https://docs.google.com/spreadsheets/d/[file_id]/edit?gid=[page_id]&usp=sharing`

- Create `KnotLocalizationImportExport` file by `Addons/Import Export` and add to Solvers `Import Google Sheet Solver`;
- Fill in the `Src Database`, `File Id` and `Page Id`(optional) fields;

> <img alt="Import-in-inspector" src="https://github.com/user-attachments/assets/3644c60d-0beb-41f3-9531-4efc34001dff" width="50%">

- Press the `Import All` button and wait for a message in the console indicating that the import is complete or that
  errors have occurred.

### Important Notes

- The extension does not delete or create anything. It only allows you to populate and update the database;
- It does not allow you to export data to a Google Sheet;
- It does not require any macros on Google Drive;
- It does not delete languages, keys, or metadata;
- It does not create or delete Text Collection files;
- You can use multiple key collections. The search will be performed across all of them. A new key will be added to the
  first suitable key collection;
- You can use any available Text collection providers. The extension will find the one where the desired key is stored.
  If the key is not in a collection, it will be added to the file of the first suitable Item Collection provider.
