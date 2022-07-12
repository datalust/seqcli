Contributing to seqcli
================

Testing
------

`seqcli` has two test projects: `SeqCli.EndToEnd` and `SeqCli.Tests`.

### SeqCli.EndToEnd 

`/test/SeqCli.EndToEnd` is a console app that implements integration tests. It uses a custom testing framework and xunit for assertions. 

Each test within the EndToEnd project (an implementation of the `ICliTestCase` interface) spans one child process for the Seq server and one child process for the seqcli command. 

The Seq server can be run via the `seq` executable (windows only) or as a `datalust/seq:latest` docker container. Which to use is controlled via the `--docker-server` argument. 

Some tests require a Seq license. These tests are run if a valid license is supplied via stdin and the `--license-certificate-stdin` argument is supplied. 

#### Adding Tests

The typical pattern is to execute a seqcli command, then make an assertion on the output of the command. E.g.

```c#
var exit = runner.Exec("apikey list", "-t Test --json --no-color");
Assert.Equal(0, exit);

var output = runner.LastRunProcess!.Output;
Assert.Contains("\"AssignedPermissions\": [\"Ingest\"]", output);
```

#### Running Tests

```shell
/SeqCli.EndToEnd$ dotnet run -- --docker-server
```

The `--docker-server` is optional if you have a `seq` executable in your path.

To run the multi user tests as well, save a valid license into a file and:

```shell
/SeqCli.EndToEnd$ cat license.txt | dotnet run -- --docker-server --license-certificate-stdin
```

To run a specific set of tests pass regular expressions matching the tests to run:

```shell
/SeqCli.EndToEnd$ dotnet run -- --docker-server *TestCase.cs Command*
```

### SeqCli.Tests

`/test/SeqCli.Tests` is a regular xunit testing project. To run: 

```shell
/SeqCli.Tests$ dotnet test
```