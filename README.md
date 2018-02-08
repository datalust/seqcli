# `seqcli`

The [Seq](https://getseq.net) client command-line app.

## Getting started

Install or unzip the [release for your operating system](https://github.com/datalust/seqcli/releases).

To set a default server URL, run:

```
seqcli config -k connection.serverUrl -v https://your-seq-server
```

This will create `SeqConfig.json` in your home directory. A default API key can also be set by providing the `connection.apiKey` setting name.

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

| Option | Description |
| ------ | ----------- |
| `-m`, `--markdown` | Generate markdown for use in documentation |

### `ingest`

Send JSON log events from a file or `STDIN`.

Example:

```
seqcli ingest -i events.clef --filter="@Level <> 'Debug'" -p Environment=Test
```

| Option | Description |
| ------ | ----------- |
| `-i`, `--input=VALUE` | CLEF file to ingest; if not specified, `STDIN` will be used |
|       `--invalid-data=VALUE` | Specify how invalid data is handled: fail (default) or ignore |
| `-p`, `--property=VALUE1=VALUE2` | Specify event properties, e.g. `-p Customer=C123 -p Environment=Production` |
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
|       `--start=VALUE` | ISO 8601 date/time to query from (default: now - 24h) |
|       `--end=VALUE` | Date/time to query to (default: now) |
| `-n`, `--no-default-range` | If specified, missing `--start` and `--end` values will not be defaulted |
|       `--signal=VALUE` | A signal expression or list of intersected signal ids to apply, for example `signal-1,signal-2` |
|       `--timeout=VALUE` | The query execution timeout in milliseconds |
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
|       `--start=VALUE` | ISO 8601 date/time to query from (default: now - 24h) |
|       `--end=VALUE` | Date/time to query to (default: now) |
| `-n`, `--no-default-range` | If specified, missing `--start` and `--end` values will not be defaulted |
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
