# `seqcli` [![Build status](https://ci.appveyor.com/api/projects/status/sc3iacxwxqqfjgdh/branch/dev?svg=true)](https://ci.appveyor.com/project/datalust/seqcli/branch/dev) [![GitHub release](https://img.shields.io/github/release/datalust/seqcli.svg)](https://github.com/datalust/seqcli/releases)

The [Seq](https://getseq.net) client command-line app. Supports logging (`seqcli log`), searching (`search`), tailing (`tail`), querying (`query`) and [JSON or plain-text log file](https://github.com/serilog/serilog-formatting-compact) ingestion (`ingest`).

![SeqCli Screenshot](https://raw.githubusercontent.com/datalust/seqcli/dev/asset/SeqCli.png)

## Getting started

Install or unzip the [release for your operating system](https://github.com/datalust/seqcli/releases).

To set a default server URL, run:

```
seqcli config -k connection.serverUrl -v https://your-seq-server
```

The API key will be stored in your `SeqCli.json` configuration file; on Windows, this is encrypted using DPAPI; on Mac/Linux the key is currently stored in plain text. As an alternative to storing the API key in configuration, it can be passed to each command via the `--apikey=` argument.

## Commands

Usage:

```
seqcli <command> [<args>]
```

### `config`

View and set fields in the `SeqCli.json` file; run with no arguments to list all fields.

| Option | Description |
| ------ | ----------- |
| `-k`, `--key=VALUE` | The field, for example `connection.serverUrl` |
| `-v`, `--value=VALUE` | The field value; if not specified, the command will print the current value |
| `-c`, `--clear` | Clear the field |

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

Send JSON log events from a file or `STDIN`.

Example:

```
seqcli ingest -i events.clef --json --filter="@Level <> 'Debug'" -p Environment=Test
```

| Option | Description |
| ------ | ----------- |
| `-i`, `--input=VALUE` | CLEF file to ingest; if not specified, `STDIN` will be used |
|       `--invalid-data=VALUE` | Specify how invalid data is handled: fail (default) or ignore |
| `-p`, `--property=VALUE1=VALUE2` | Specify event properties, e.g. `-p Customer=C123 -p Environment=Production` |
| `-x`, `--extract=VALUE` | An extraction pattern to apply to plain-text logs (ignored when `--json` is specified) |
|       `--json` | Read the events as JSON (the default assumes plain text) |
| `-f`, `--filter=VALUE` | Filter expression to select a subset of events |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default `config.apiKey` value will be used |

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
| `-p`, `--property=VALUE1=VALUE2` | Specify event properties, e.g. `-p Customer=C123 -p Environment=Production` |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default `config.apiKey` value will be used |

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
|       `--json` | Print events in newline-delimited JSON (the default is plain text) |
|       `--no-color` | Don't colorize text output |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default `config.apiKey` value will be used |

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
|       `--json` | Print events in newline-delimited JSON (the default is plain text) |
|       `--no-color` | Don't colorize text output |
|       `--signal=VALUE` | A signal expression or list of intersected signal ids to apply, for example `signal-1,signal-2` |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default `config.apiKey` value will be used |

### `tail`

Stream log events matching a filter.

| Option | Description |
| ------ | ----------- |
| `-f`, `--filter=VALUE` | An optional server-side filter to apply to the stream, for example `@Level = 'Error'` |
|       `--json` | Print events in newline-delimited JSON (the default is plain text) |
|       `--no-color` | Don't colorize text output |
|       `--signal=VALUE` | A signal expression or list of intersected signal ids to apply, for example `signal-1,signal-2` |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the `connection.serverUrl` value will be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default `config.apiKey` value will be used |

### `version`

Print the current executable version.

## Extraction patterns

The `seqcli ingest` command can be used for parsing plain text logs into structured log events.

```shell
seqcli ingest -x "{@t:timestamp} [{@l:ident}] {@m:*}{:n}{@x:*}"
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

### Processing

Extraction patterns are processed from left to right. When the first non-matching pattern is encountered, extraction stops; any remaining text that couldn't be matched will be attached to the resulting event in an `@unmatched` property.

Multi-line events are handled by looking for lines that start with the first element of the extraction pattern to be used. This works well if the first line of each event begins with something unambiguous like an `iso8601dt` timestamp; if the lines begin with less specific syntax, the first few elements of the extraction pattern might be grouped to identify the start of events more accurately:

```
{:=[{@t} {@l}]} {@m:*}
```

Here the literal text `[`, a timestamp token, adjacent space ` `, level and closing `]` are all grouped so that they constitute a single logical pattern element to identify the start of events.

When logs are streamed into `seqcli ingest` in real time, a 10 ms deadline is applied, within which any trailing lines that make up the event must be received.

### Examples

**Tail systemd logs:**

```shell
journalctl -f -n 0 |
  seqcli ingest -x "{@t:syslogdt} {host} {ident:*}: {@m:*}{:n}" --invalid-data=ignore
```

**Tail `/var/log/syslog`**

```shell
tail -c 0 -F /var/log/syslog |
  seqcli ingest -x "{@t:syslogdt} {host} {ident:*}: {@m:*}{:n}"
```
