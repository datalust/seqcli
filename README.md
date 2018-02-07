# `seqcli`

The [Seq](https://getseq.net) client command-line app.

## Getting started

Install or unzip the application. To set a default server URL, run:

```
seqcli config -k connection.serverUrl -v https://your-seq-server
```

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
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the URL in SeqCli.json will  be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default the API key in SeqCli.json will be used |

### `query`

Execute an SQL query and receive results in CSV format.

| Option | Description |
| ------ | ----------- |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the URL in SeqCli.json will  be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default the API key in SeqCli.json will be used |
| `-q`, `--query=VALUE` | The query to execute, for example `select count(*) from stream` |
|       `--start=VALUE` | ISO 8601 date/time to query from (default: now - 24h) |
|       `--end=VALUE` | Date/time to query to (default: now) |
|       `--no-default-range` | If specified, missing `--start` and `--end` values will not be defaulted |

### `tail`

Stream log events matching a filter.

| Option | Description |
| ------ | ----------- |
| `-s`, `--server=VALUE` | The URL of the Seq server; by default the URL in SeqCli.json will  be used |
| `-a`, `--apikey=VALUE` | The API key to use when connecting to the server; by default the API key in SeqCli.json will be used |
| `-f`, `--filter=VALUE` | An optional filter to apply to the stream, for example `@Level = 'Error'` |
|       `--json` | Print the streamed events in newline-delimited JSON (the default is plain text) |

### `version`

Print the current executable version.
