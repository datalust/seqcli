using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SeqCli.EndToEnd.Support;

public sealed class CaptiveProcess : ITestProcess, IDisposable
{
    readonly bool _captureOutput;
    readonly string _stopCommandFullExePath;
    readonly string _stopCommandArgs;
    readonly Process _process;
    readonly ManualResetEvent _outputComplete = new(false);
    readonly ManualResetEvent _errorComplete = new(false);

    readonly object _sync = new();
    readonly StringWriter _output = new();

    public CaptiveProcess(
        string fullExePath,
        string args = null,
        IDictionary<string, string> environment = null,
        bool captureOutput = true,
        bool supplyInput = false,
        string stopCommandFullExePath = null,
        string stopCommandArgs = null)
    {
        if (fullExePath == null) throw new ArgumentNullException(nameof(fullExePath));
        _captureOutput = captureOutput;
        _stopCommandFullExePath = stopCommandFullExePath;
        _stopCommandArgs = stopCommandArgs;

        var startInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardError = captureOutput,
            RedirectStandardOutput = captureOutput,
            RedirectStandardInput = supplyInput,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            ErrorDialog = false,
            FileName = fullExePath,
            Arguments = args ?? ""
        };
            
        if (environment != null)
        {
            foreach (var (name, value) in environment)
            {
                startInfo.Environment.Add(name, value);
            }
        }

        _process = Process.Start(startInfo);
        if (_process == null)
            throw new InvalidOperationException("The process did not start.");

        if (captureOutput)
        {
            _process.OutputDataReceived += (_, e) =>
            {
                if (e.Data == null)
                    _outputComplete.Set(); 
                else
                    WriteOutput(e.Data);
            };
            _process.BeginOutputReadLine();

            _process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data == null)
                    _errorComplete.Set();
                else
                    WriteOutput(e.Data);
            };
            _process.BeginErrorReadLine();
        }
    }

    public void WriteLineStdin(string s)
    {
        _process.StandardInput.WriteLine(s);
    }

    public void CompleteStdin()
    {
        _process.StandardInput.Close();
    }

    void WriteOutput(string o)
    {
        lock (_sync)
            _output.WriteLine(o);
    }

    public string Output
    {
        get
        {
            lock (_sync)
                return _output.ToString();
        }
    }

    public int WaitForExit(TimeSpan? timeout = null)
    {
        var processExitTimeout = timeout ?? Timeout.InfiniteTimeSpan;     
        _process.WaitForExit((int)processExitTimeout.TotalMilliseconds);

        if (_captureOutput)
        {
            if (!_outputComplete.WaitOne(TimeSpan.FromSeconds(1)))
                throw new IOException("STDOUT did not complete in the fixed 1 second window.");
                
            if (!_errorComplete.WaitOne(TimeSpan.FromSeconds(1)))
                throw new IOException("STDERR did not complete in the fixed 1 second window.");
        }

        return _process.ExitCode;
    }

    public void Dispose()
    {
        try
        {
            _process.Kill();
            WaitForExit();
            if (_stopCommandFullExePath != null)
            {
                var stopCommandStartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    ErrorDialog = false,
                    FileName = _stopCommandFullExePath,
                    Arguments = _stopCommandArgs ?? ""
                };

                using var stopCommandProcess = Process.Start(stopCommandStartInfo);
                stopCommandProcess?.WaitForExit();
            }
        }
        catch
        {
            // Ignored
        }
    }
}