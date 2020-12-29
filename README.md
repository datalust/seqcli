# `seqcli` [![Build status](https://ci.appveyor.com/api/projects/status/sc3iacxwxqqfjgdh/branch/dev?svg=true)](https://ci.appveyor.com/project/datalust/seqcli/branch/dev) [![GitHub release](https://img.shields.io/github/release/datalust/seqcli.svg)](https://github.com/datalust/seqcli/releases)

The [Seq](https://datalust.co/seq) client command-line app. Supports logging (`seqcli log`), searching (`search`), tailing (`tail`), querying (`query`) and [JSON or plain-text log file](https://github.com/serilog/serilog-formatting-compact) ingestion (`ingest`).

![SeqCli Screenshot](https://raw.githubusercontent.com/datalust/seqcli/dev/asset/SeqCli.png)

## Getting started

Install or unzip the [release for your operating system](https://github.com/datalust/seqcli/releases). Or, if you have `dotnet` installed, `seqcli` can be installed as a global tool using:

```
dotnet tool install --global seqcli
```

To set a default server URL, run:

```
seqcli config -k connection.serverUrl -v https://your-seq-server
```

The API key will be stored in your `SeqCli.json` configuration file; on Windows, this is encrypted using DPAPI; on Mac/Linux the key is currently stored in plain text. As an alternative to storing the API key in configuration, it can be passed to each command via the `--apikey=` argument.

`seqcli` is also available as a Docker container under [`datalust/seqcli`](https://store.docker.com/community/images/datalust/seqcli):

```
docker run --rm datalust/seqcli:latest <command> [<args>]
```

Use Docker networks and volumes to make local files and other containers accessible to `seqcli` within its container.

## Commands

Usage:

```
seqcli <command> [<args>]
```

Available commands:

 - `apikey`
   - [`apikey create`](#apikey-create) &mdash; Create an API key for ingestion.
   - [`apikey list`](#apikey-list) &mdash; List available API keys.
   - [`apikey remove`](#apikey-remove) &mdash; Remove an API key from the server.
 - [`app-run`](#app-run) &mdash; Host a .NET `[SeqApp]` plug-in.
 - [`config`](#config) &mdash; View and set fields in the `SeqCli.json` file; run with no arguments to list all fields.
 - `dashboard`
   - [`dashboard list`](#dashboard-list) &mdash; List dashboards.
   - [`dashboard remove`](#dashboard-remove) &mdash; Remove a dashboard from the server.
   - [`dashboard render`](#dashboard-render) &mdash; Produce a CSV or JSON result set from a dashboard chart.
 - [`help`](#help) &mdash; Show information about available commands.
 - [`ingest`](#ingest) &mdash; Send log events from a file or `STDIN`.
 - [`log`](#log) &mdash; Send a structured log event to the server.
 - [`print`](#print) &mdash; Pretty-print events in CLEF/JSON format, from a file or `STDIN`.
 - `profile`
   - [`profile create`](#profile-create) &mdash; Create or replace a connection profile.
   - [`profile list`](#profile-list) &mdash; List connection profiles.
   - [`profile remove`](#profile-remove) &mdash; Remove a connection profile.
 - [`query`](#query) &mdash; Execute an SQL query and receive results in CSV format.
 - [`search`](#search) &mdash; Retrieve log events that match a given filter.
 - `signal`
   - [`signal create`](#signal-create) &mdash; Create a signal.
   - [`signal import`](#signal-import) &mdash; Import signals in newline-delimited JSON format.
   - [`signal list`](#signal-list) &mdash; List available signals.
   - [`signal remove`](#signal-remove) &mdash; Remove a signal from the server.
 - [`tail`](#tail) &mdash; Stream log events matching a filter.
 - `user`
   - [`user create`](#user-create) &mdash; Create a user.
   - [`user list`](#user-list) &mdash; List users.
   - [`user remove`](#user-remove) &mdash; Remove a user from the server.
 - [`version`](#version) &mdash; Print the current executable version.

### `apikey remove`

Remove an API key from the server.

Example:

```
seqcli apikey remove -t 'Test API Key'
```

| Option | Description |
| ------ | ----------- |
| `-t`, `--title=VALUE` | The title of the API key(s) to remove |
| `-i`, `--id=VALUE` | The id of a single API key to remove |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` config value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default the `connection.apiKey` config value will be used |
|       `--profile=VALUE` | A connection profile to use; by default the `connection.serverUrl` and `connection.apiKey` config values will be used |

### `apikey list`

List available API keys.

Example:

```
seqcli apikey list
```

| Option | Description |
| ------ | ----------- |
| `-t`, `--title=VALUE` | The title of the API key(s) to list |
| `-i`, `--id=VALUE` | The id of a single API key to list |
|       `--json` | Print output in newline-delimited JSON (the default is plain text) |
|       `--no-color` | Don't colorize text output |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` config value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default the `connection.apiKey` config value will be used |
|       `--profile=VALUE` | A connection profile to use; by default the `connection.serverUrl` and `connection.apiKey` config values will be used |

### `apikey create`

Create an API key for ingestion.

Example:

```
seqcli apikey create -t 'Test API Key' -p Environment=Test
```

| Option | Description |
| ------ | ----------- |
| `-t`, `--title=VALUE` | A title for the API key |
|       `--token=VALUE` | A pre-allocated API key token; by default, a new token will be generated and written to `STDOUT` |
| `-p`, `--property=NAME=VALUE` | Specify name/value properties, e.g. `-p Customer=C123 -p Environment=Production` |
|       `--filter=VALUE` | A filter to apply to incoming events |
|       `--minimum-level=VALUE` | The minimum event level/severity to accept; the default is to accept all events |
|       `--use-server-timestamps` | Discard client-supplied timestamps and use server clock values |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` config value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default the `connection.apiKey` config value will be used |
|       `--profile=VALUE` | A connection profile to use; by default the `connection.serverUrl` and `connection.apiKey` config values will be used |
|       `--json` | Print output in newline-delimited JSON (the default is plain text) |
|       `--no-color` | Don't colorize text output |

### `app run`

Host a .NET `[SeqApp]` plug-in.

Example:

```
seqcli tail --json | seqcli app run -d "./bin/Debug/netstandard2.2" -p ToAddress=example@example.com
```

| Option | Description |
| ------ | ----------- |
| `-d`, `--directory=VALUE` | The directory containing .NET Standard assemblies; defaults to the current directory |
| `-t`, `--type=VALUE` | The [SeqApp] plug-in type name; defaults to scanning assemblies for a single type marked with this attribute |
| `-p`, `--property=NAME=VALUE` | Specify name/value settings for the app, e.g. `-p ToAddress=example@example.com -p Subject="Alert!"` |
| `-s`, `--server=VALUE` | The URL of the Seq server, used only for app configuration (no connection is made to the server); by default the `connection.serverUrl` value will be used |
|       `--storage=VALUE` | A directory in which app-specific data can be stored; defaults to the current directory |

### `config`

View and set fields in the `SeqCli.json` file; run with no arguments to list all fields.

| Option | Description |
| ------ | ----------- |
| `-k`, `--key=VALUE` | The field, for example `connection.serverUrl` |
| `-v`, `--value=VALUE` | The field value; if not specified, the command will print the current value |
| `-c`, `--clear` | Clear the field |

### `dashboard render`

Produce a CSV or JSON result set from a dashboard chart.

Example:

```
seqcli dashboard render -i dashboard-159 -c 'Response Time (ms)' --last 7d --by 1h
```

| Option | Description |
| ------ | ----------- |
| `-i`, `--id=VALUE` | The id of a single dashboard to render |
| `-c`, `--chart=VALUE` | The title of a chart on the dashboard to render |
|       `--last=VALUE` | A duration over which the chart should be rendered, e.g. `7d`; this will be aligned to an interval boundary; either `--last` or `--start` and `--end` must be specified |
|       `--by=VALUE` | The time-slice interval for the chart data, as a duration, e.g. `1h` |
|       `--start=VALUE` | ISO 8601 date/time to query from |
|       `--end=VALUE` | Date/time to query to |
|       `--signal=VALUE` | A signal expression or list of intersected signal ids to apply, for example `signal-1,signal-2` |
|       `--timeout=VALUE` | The query execution timeout in milliseconds |
|       `--json` | Print output in newline-delimited JSON (the default is plain text) |
|       `--no-color` | Don't colorize text output |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` config value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default the `connection.apiKey` config value will be used |
|       `--profile=VALUE` | A connection profile to use; by default the `connection.serverUrl` and `connection.apiKey` config values will be used |

### `dashboard remove`

Remove a dashboard from the server.

Example:

```
seqcli dashboard remove -i dashboard-159
```

| Option | Description |
| ------ | ----------- |
| `-t`, `--title=VALUE` | The title of the dashboard(s) to remove |
| `-i`, `--id=VALUE` | The id of a single dashboard to remove |
| `-o`, `--owner=VALUE` | The id of the user to remove dashboards for; by default, shared dashboards are removd |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` config value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default the `connection.apiKey` config value will be used |
|       `--profile=VALUE` | A connection profile to use; by default the `connection.serverUrl` and `connection.apiKey` config values will be used |

### `dashboard list`

List dashboards.

Example:

```
seqcli dashboard list
```

| Option | Description |
| ------ | ----------- |
| `-t`, `--title=VALUE` | The title of the dashboard(s) to list |
| `-i`, `--id=VALUE` | The id of a single dashboard to list |
| `-o`, `--owner=VALUE` | The id of the user to list dashboards for; by default, shared dashboards are listd |
|       `--json` | Print output in newline-delimited JSON (the default is plain text) |
|       `--no-color` | Don't colorize text output |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` config value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default the `connection.apiKey` config value will be used |
|       `--profile=VALUE` | A connection profile to use; by default the `connection.serverUrl` and `connection.apiKey` config values will be used |

### `help`

Show information about available commands.

Example:

```
seqcli help search
```

| Option | Description |
| ------ | ----------- |
| `-m`, `--markdown` | Generate markdown for use in documentation |

### `ingest`

Send log events from a file or `STDIN`.

Example:

```
seqcli ingest -i log-*.txt --json --filter="@Level <> 'Debug'" -p Environment=Test
```

| Option | Description |
| ------ | ----------- |
| `-i`, `--input=VALUE` | File to ingest, including the `*` wildcard; if not specified, `STDIN` will be used |
|       `--invalid-data=VALUE` | Specify how invalid data is handled: `fail` (default) or `ignore` |
| `-p`, `--property=NAME=VALUE` | Specify name/value properties, e.g. `-p Customer=C123 -p Environment=Production` |
| `-x`, `--extract=VALUE` | An extraction pattern to apply to plain-text logs (ignored when `--json` is specified) |
|       `--json` | Read the events as JSON (the default assumes plain text) |
| `-f`, `--filter=VALUE` | Filter expression to select a subset of events |
| `-m`, `--message=VALUE` | A message to associate with the ingested events; https://messagetemplates.org syntax is supported |
| `-l`, `--level=VALUE` | The level or severity to associate with the ingested events; this will override any level information present in the events themselves |
|       `--send-failure=VALUE` | Specify how connection failures are handled: `fail` (default), `retry`, `continue`, or `ignore` |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` config value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default the `connection.apiKey` config value will be used |
|       `--profile=VALUE` | A connection profile to use; by default the `connection.serverUrl` and `connection.apiKey` config values will be used |

### `log`

Send a structured log event to the server.

Example:

```
seqcli log -m 'Hello, {Name}!' -p Name=World -p App=Test
```

| Option | Description |
| ------ | ----------- |
| `-m`, `--message=VALUE` | A message to associate with the event (the default is to send no message); https://messagetemplates.org syntax is supported |
| `-l`, `--level=VALUE` | The level or severity of the event (the default is `Information`) |
| `-t`, `--timestamp=VALUE` | The event timestamp as ISO-8601 (the current UTC timestamp will be used by default) |
| `-x`, `--exception=VALUE` | Additional exception or error information to send, if any |
| `-p`, `--property=NAME=VALUE` | Specify name/value properties, e.g. `-p Customer=C123 -p Environment=Production` |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` config value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default the `connection.apiKey` config value will be used |
|       `--profile=VALUE` | A connection profile to use; by default the `connection.serverUrl` and `connection.apiKey` config values will be used |

### `print`

Pretty-print events in CLEF/JSON format, from a file or `STDIN`.

Example:

```
seqcli print -i log-20201028.clef
```

| Option | Description |
| ------ | ----------- |
| `-i`, `--input=VALUE` | CLEF file to read, including the `*` wildcard; if not specified, `STDIN` will be used |
| `-f`, `--filter=VALUE` | Filter expression to select a subset of events |
|       `--template=VALUE` | Specify an output template to control plain text formatting |
|       `--invalid-data=VALUE` | Specify how invalid data is handled: `fail` (default) or `ignore` |
|       `--no-color` | Don't colorize text output |

### `profile remove`

Remove a connection profile.

Example:

```
seqcli profile remove -n Production
```

| Option | Description |
| ------ | ----------- |
| `-n`, `--name=VALUE` | The name of the connection profile to remove |

### `profile list`

List connection profiles.

Example:

```
seqcli profile list
```

### `profile create`

Create or replace a connection profile.

Example:

```
seqcli profile create -n Production -s https://seq.example.com -a th15ISanAPIk3y
```

| Option | Description |
| ------ | ----------- |
| `-n`, `--name=VALUE` | The name of the connection profile |
| `-s`, `--server=VALUE` | The URL of the Seq server |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server, if required |

### `query`

Execute an SQL query and receive results in CSV format.

Example:

```
seqcli query -q "select count(*) from stream group by @Level" --start="2018-02-28T13:00Z"
```

| Option | Description |
| ------ | ----------- |
| `-q`, `--query=VALUE` | The query to execute |
|       `--start=VALUE` | ISO 8601 date/time to query from |
|       `--end=VALUE` | Date/time to query to |
|       `--signal=VALUE` | A signal expression or list of intersected signal ids to apply, for example `signal-1,signal-2` |
|       `--timeout=VALUE` | The query execution timeout in milliseconds |
|       `--json` | Print output in newline-delimited JSON (the default is plain text) |
|       `--no-color` | Don't colorize text output |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` config value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default the `connection.apiKey` config value will be used |
|       `--profile=VALUE` | A connection profile to use; by default the `connection.serverUrl` and `connection.apiKey` config values will be used |

### `search`

Retrieve log events that match a given filter.

Example:

```
seqcli search -f "@Exception like '%TimeoutException%'" -c 30
```

| Option | Description |
| ------ | ----------- |
| `-f`, `--filter=VALUE` | A filter to apply to the search, for example `Host = 'xmpweb-01.example.com'` |
| `-c`, `--count=VALUE` | The maximum number of events to retrieve; the default is 1 |
|       `--start=VALUE` | ISO 8601 date/time to query from |
|       `--end=VALUE` | Date/time to query to |
|       `--json` | Print output in newline-delimited JSON (the default is plain text) |
|       `--no-color` | Don't colorize text output |
|       `--signal=VALUE` | A signal expression or list of intersected signal ids to apply, for example `signal-1,signal-2` |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` config value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default the `connection.apiKey` config value will be used |
|       `--profile=VALUE` | A connection profile to use; by default the `connection.serverUrl` and `connection.apiKey` config values will be used |

### `signal remove`

Remove a signal from the server.

Example:

```
seqcli signal remove -t 'Test Signal'
```

| Option | Description |
| ------ | ----------- |
| `-t`, `--title=VALUE` | The title of the signal(s) to remove |
| `-i`, `--id=VALUE` | The id of a single signal to remove |
| `-o`, `--owner=VALUE` | The id of the user to remove signals for; by default, shared signals are removd |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` config value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default the `connection.apiKey` config value will be used |
|       `--profile=VALUE` | A connection profile to use; by default the `connection.serverUrl` and `connection.apiKey` config values will be used |

### `signal list`

List available signals.

Example:

```
seqcli signal list
```

| Option | Description |
| ------ | ----------- |
| `-t`, `--title=VALUE` | The title of the signal(s) to list |
| `-i`, `--id=VALUE` | The id of a single signal to list |
| `-o`, `--owner=VALUE` | The id of the user to list signals for; by default, shared signals are listd |
|       `--json` | Print output in newline-delimited JSON (the default is plain text) |
|       `--no-color` | Don't colorize text output |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` config value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default the `connection.apiKey` config value will be used |
|       `--profile=VALUE` | A connection profile to use; by default the `connection.serverUrl` and `connection.apiKey` config values will be used |

### `signal import`

Import signals in newline-delimited JSON format.

Example:

```
seqcli signal import -i ./Exceptions.json
```

| Option | Description |
| ------ | ----------- |
| `-i`, `--input=VALUE` | File to import; if not specified, `STDIN` will be used |
| `-o`, `--owner=VALUE` | The id of the user to import signals for; by default, shared signals are importd |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` config value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default the `connection.apiKey` config value will be used |
|       `--profile=VALUE` | A connection profile to use; by default the `connection.serverUrl` and `connection.apiKey` config values will be used |
| `--merge` | Update signals that have ids matching those in the imported data; the default is to always create new signals |

### `signal create`

Create a signal.

Example:

```
seqcli signal create -t 'Exceptions' -f "@Exception is not null"
```

| Option | Description |
| ------ | ----------- |
| `-t`, `--title=VALUE` | A title for the signal |
|       `--description=VALUE` | A description for the signal |
| `-f`, `--filter=VALUE` | Filter to associate with the signal |
|       `--group=VALUE` | An explicit group name to associate with the signal; the default is to infer the group from the filter |
|       `--no-group` | Specify that no group should be inferred; the default is to infer the group from the filter |
|       `--protected` | Specify that the signal is editable only by administrators |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` config value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default the `connection.apiKey` config value will be used |
|       `--profile=VALUE` | A connection profile to use; by default the `connection.serverUrl` and `connection.apiKey` config values will be used |
|       `--json` | Print output in newline-delimited JSON (the default is plain text) |
|       `--no-color` | Don't colorize text output |

### `tail`

Stream log events matching a filter.

| Option | Description |
| ------ | ----------- |
| `-f`, `--filter=VALUE` | An optional server-side filter to apply to the stream, for example `@Level = 'Error'` |
|       `--json` | Print output in newline-delimited JSON (the default is plain text) |
|       `--no-color` | Don't colorize text output |
|       `--signal=VALUE` | A signal expression or list of intersected signal ids to apply, for example `signal-1,signal-2` |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` config value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default the `connection.apiKey` config value will be used |
|       `--profile=VALUE` | A connection profile to use; by default the `connection.serverUrl` and `connection.apiKey` config values will be used |

### `user remove`

Remove a user from the server.

Example:

```
seqcli user remove -n alice
```

| Option | Description |
| ------ | ----------- |
| `-n`, `--name=VALUE` | The username of the user(s) to remove |
| `-i`, `--id=VALUE` | The id of a single user to remove |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` config value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default the `connection.apiKey` config value will be used |
|       `--profile=VALUE` | A connection profile to use; by default the `connection.serverUrl` and `connection.apiKey` config values will be used |

### `user list`

List users.

Example:

```
seqcli user list
```

| Option | Description |
| ------ | ----------- |
| `-n`, `--name=VALUE` | The username of the user(s) to list |
| `-i`, `--id=VALUE` | The id of a single user to list |
|       `--json` | Print output in newline-delimited JSON (the default is plain text) |
|       `--no-color` | Don't colorize text output |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` config value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default the `connection.apiKey` config value will be used |
|       `--profile=VALUE` | A connection profile to use; by default the `connection.serverUrl` and `connection.apiKey` config values will be used |

### `user create`

Create a user.

Example:

```
seqcli user create -n alice -d 'Alice Example' -r 'User (read/write)' --password-stdin
```

| Option | Description |
| ------ | ----------- |
| `-n`, `--name=VALUE` | A unique username for the user |
| `-d`, `--display-name=VALUE` | A long-form name to aid in identifying the user |
| `-f`, `--filter=VALUE` | A view filter that limits the events visible to the user |
| `-r`, `--role=VALUE` | The title of a role that grants the user permissions on the server; if not specified, the default new user role will be assigned |
| `-e`, `--email=VALUE` | The user's email address (enables a Gravatar image for the user) |
| `-p`, `--password=VALUE` | An initial password for the user, if username/password authentication is in use; note that `--password-stdin` is more secure |
|       `--password-stdin` | Read the initial password for the user from `STDIN`, if username/password authentication is in use |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` config value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default the `connection.apiKey` config value will be used |
|       `--profile=VALUE` | A connection profile to use; by default the `connection.serverUrl` and `connection.apiKey` config values will be used |
|       `--json` | Print output in newline-delimited JSON (the default is plain text) |
|       `--no-color` | Don't colorize text output |

### `version`

Print the current executable version.

## Extraction patterns

The `seqcli ingest` command can be used for parsing plain text logs into structured log events.

```shell
seqcli ingest -x "{@t:timestamp} [{@l:level}] {@m:*}{:n}{@x:*}"
```

The `-x` argument above is an _extraction pattern_ that will parse events like:

```
2018-02-21 13:29:00.123 +10:00 [ERR] The operation failed
System.DivideByZeroException: Attempt to divide by zero
  at SomeClass.SomeMethod()
```

### Syntax

Extraction patterns have a simple high-level syntax:

 * Text that appears in the pattern is matched literally - so a pattern like `Hello, world!` will match logging statements that are made up of this greeting only,
 * Text between `{curly braces}` is a _match expression_ that identifies a part of the event to be extracted, and
 * Literal curly braces are escaped by doubling, so `{{` will match the literal text `{`, and `}}` matches `}`.
 
Match expressions have the form:

```
{name:matcher}
```

Both the name and matcher are optional, but either one or the other must be specified. Hence `{@t:timestamp}` specifies a name of `@t` and value `timestamp`, `{IPAddress}` specifies a name only, and `{:n}` a value only (in this case the built-in newline matcher).

The _name_ is the property name to be extracted; there are four built-in property names that get special handling:

 * `@t` - the event's timestamp
 * `@m` - the textual message associated with the event
 * `@l` - the event's level
 * `@x` - the exception or backtrace associated with the event
 
Other property names are attached to the event payload, so `{Elapsed:dec}` will extract a property called `Elapsed`, using the `dec` decimal matcher.

Match expressions with no name are consumed from the input, but are not added to the event payload.

### Matchers

Matchers identify chunks of the input event.

Different matchers are needed so that a piece of text like `200OK` can be separated into separate properties, i.e. `{StatusCode:nat}{Status:alpha}`. Here, the `nat` (natural number) matcher also coerces the result into a numeric value, so that it is attached to the event payload numerically as `200` instead of as the text `"200"`.

There are three kinds of matchers:

 * Matchers like `alpha` and `nat` are built-in _named_ matchers.
 * The special matchers `*`, `**` and so-on, are _non-greedy content_ matchers; these will match any text up until the next pattern element matches (`*`), the next two elements match, and so-on. We saw this in action with the `{@m:*}{:n}` elements in the example - the message is all of the text up until the next newline.
 * More complex _compound_ matchers are described using a sub-expression. These are prefixed with an equals sign `=`, like `{Phone:={:nat}-{:nat}-{:nat}}`. This will extract chunks of text like `123-456-7890` into the `Phone` property.

| Matcher | Description | Example |
|---|---|---|
| `*`, `**`, ... | Non-greedy content | |
| `alpha` | One or more letters | `Abc` |
| `alphanum` | One or more letters or numbers | `a1b2` |
| `dec` | A decimal number | `12.345` |
| `ident` | A C-style identifier  | `countOfMatches` |
| `int` | An integer | `-123` |
| `iso8601dt` | An ISO-8601 date-time | `2020-01-28T13:50:01.123` |
| `level` | A logging level name | `INF` |
| `line` | Any single-line content | `one line!` |
| `n` | A newline character or sequence | |
| `nat` | A nonnegative number | `123` |
| `s` | One or more space or tab characters | ` ` |
| `serilogdt` | A datetime in the default Serilog file logging format | `2020-01-28 13:50:01.123 +10:00` |
| `syslogdt` | A datetime in syslog format | `Dec  8 09:12:13` |
| `t` | A single tab character | `	` |
| `timestamp` | A datetime in any recognized format | |
| `token` | Any sequence of non-whitespace characters | `1+x$3` |
| `trailingident` | Multiline content with indented trailing lines | |
| `unixdt` | A datetime in Unix time format (seconds since Unix epoch) | `1608694199.999` |
| `w3cdt` | A W3C log format date/time pair | `2019-04-02 05:18:01` |

### Processing

Extraction patterns are processed from left to right. When the first non-matching pattern is encountered, extraction stops; any remaining text that couldn't be matched will be attached to the resulting event in an `@unmatched` property.

Multi-line events are handled by looking for lines that start with the first element of the extraction pattern to be used. This works well if the first line of each event begins with something unambiguous like an `iso8601dt` timestamp; if the lines begin with less specific syntax, the first few elements of the extraction pattern might be grouped to identify the start of events more accurately:

```
{:=[{@t} {@l}]} {@m:*}
```

Here the literal text `[`, a timestamp token, adjacent space ` `, level and closing `]` are all grouped so that they constitute a single logical pattern element to identify the start of events.

When logs are streamed into `seqcli ingest` in real time, a 10 ms deadline is applied, within which any trailing lines that make up the event must be received.

### Examples

#### Tail systemd logs

```shell
journalctl -f -n 0 |
  seqcli ingest -x "{@t:syslogdt} {host} {ident:*}: {@m:*}{:n}" --invalid-data=ignore
```

#### Tail `/var/log/syslog`

```shell
tail -c 0 -F /var/log/syslog |
  seqcli ingest -x "{@t:syslogdt} {host} {ident:*}: {@m:*}{:n}"
```

#### Ingest an IIS/W3C web server log

This example ingests log files in the format:

```shell
#Fields: date time s-ip cs-method cs-uri-stem cs-uri-query s-port cs-username c-ip cs(User-Agent) 
cs(Referer) sc-status sc-substatus sc-win32-status sc-bytes cs-bytes time-taken
```

The extraction pattern is wrapped in the example for display purposes, and must appear all in one string argument when invoked.

```shell
seqcli ingest -i http.log --invalid-data=ignore -x "{@t:w3cdt} {ServerIP} {@m:={Method} {RequestPath}} 
{Query} {Port:nat} {Username} {ClientIP} {UserAgent} {Referer} {StatusCode:nat} {Substatus:nat} 
{Win32Status:nat} {ResponseBytes:nat} {RequestBytes:nat} {Elapsed}{:n}"
```

A nested `{@m:=` pattern is used to collect a substring of the log line for display as the event's message.
