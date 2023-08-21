# StarLog

A simple log files reader.

## Overview

*To be done later.*

## Command line interface (CLI)

### `loadpath` command

Starlog supports opening folders or files with logs directly from command line. Here are the options:

| Parameter     | Description                                          |
|---------------|------------------------------------------------------|
| \-\-path      | Specifies the path to a folder or a file             |
| \-\-settings  | Inline settings of a profile                         |
| \-\-artifacts | Defines the number of lines of the file artifacts in each log file. The log record reading starts on the `artifacts + 1`'s line in each file. Can be from **0** to any positive number. |
| \-\-template  | Whether a template name or its identifier, listed in *ProfileSettingsTemplate.json*. When this parameter is being used, *\-\-settings* and *\-\-artifacts* are ignored. |

### Examples

1. Open a folder, providing the profile settings

    ```batch
    Starlog.exe loadpath --path "c:\Logs\Set1" --settings "LEVEL DATETIME [Thread] Logger - Message" --artifacts 3
    ```

    Where:

    * *"c:\Logs\Set 1"* is a path to the log files
    * *"LEVEL DATETIME [Thread] Logger - Message"* is a regular expression that represents a log record. It may contain the following recognizable named regex groups (optional): level, datetime, thread, logger, message.
    * *3* is a number of preserved lines in each log file, dedicated as "file artifacts"

2. Open a folder, providing the profile template name

    ```batch
    Starlog.exe loadpath --path "c:\Logs\Set1" --template "ProfileTemplate1"
    ```

    Where:

    * *"ProfileTemplate1"* is a name of the template. See [ProfileSettingsTemplate.json file format](#profilesettingstemplatejson-file-format) for details.

3. Open a folder, providing the profile template id

    ```batch
    Starlog.exe loadpath --path "c:\Logs\Set1" --template "dbfa3c35-3c4a-4a47-9775-b5d969f202aa"
    ```

    Where:

    * *"dbfa3c35-3c4a-4a47-9775-b5d969f202aa"* is an identifier of the template. See [ProfileSettingsTemplate.json file format](#profilesettingstemplatejson-file-format) for details.

## ProfileSettingsTemplate.json file format

File example:

```json
[
  {
    "Name": "ProfileTemplate1",
    "Settings": {
      "$type": "profile-settings",
      "$v": 1,
      "LogCodec": {
        "$type": "plaintext-profile-log-codec",
        "$v": 1,
        "LineRegex": "(?\u003Clevel\u003E\\w\u002B)\\s(?\u003Cdatetime\u003E[\\d\\-:\\.]\u002B\\s[\\d\\-:\\.]\u002B)\\s\\[(?\u003Cthread\u003E\\w\u002B)\\]\\s(?\u003Clogger\u003E[^\\s]\u002B)\\s-\\s(?\u003Cmessage\u003E.\u002B)",
        "LogCodec": "a38a40b6-c07f-49d5-a143-5c9f9f42149b"
      },
      "FileArtifactLinesCount": 3
    },
    "Id": "dbfa3c35-3c4a-4a47-9775-b5d969f202aa"
  }
]
```

Where:

* `Name` is a unique name of the profile template;
* `LineRegex` is a regular expression, which reflects the log record;
* `LogCodec` (*a38a40b6-c07f-49d5-a143-5c9f9f42149b*) points to `PlainTextProfileLogCodec`;
* `FileArtifactLinesCount` describes how many lines in each log file are dedicated to a «file artifact». The log record reading starts on the `FileArtifactLinesCount + 1`'s line in each file;
* `Id` is a unique identifier of the profile template. Can be random.
* The rest of properties are static and should remain in their places.

## Asset sources

* Logo by [DALL·E 2](https://openai.com/dall-e-2/)
* Some icons by [Icons8](https://icons8.com)
* Some other icons by [IconFinder](https://www.iconfinder.com/)
