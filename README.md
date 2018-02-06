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

### `version`

Print the current executable version.
