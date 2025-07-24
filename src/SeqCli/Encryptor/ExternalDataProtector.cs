using System;
using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace SeqCli.Encryptor;

class ExternalDataProtector : IDataProtector
{
    public ExternalDataProtector(string encryptor, string? encryptorArgs, string decryptor, string? decryptorArgs)
    {
        _encryptor = encryptor ?? throw new ArgumentNullException(nameof(encryptor));
        _encryptorArgs = encryptorArgs;
        _decryptor = decryptor ?? throw new ArgumentNullException(nameof(decryptor));
        _decryptorArgs = decryptorArgs;
    }

    readonly string _encryptor;
    readonly string? _encryptorArgs;
    readonly string _decryptor;
    readonly string? _decryptorArgs;
    
    public byte[] Encrypt(byte[] unencrypted)
    {
        var exit = Invoke(_encryptor, _encryptorArgs, unencrypted, out var encrypted, out var err);
        if (exit != 0)
        {
            throw new Exception($"Encryptor failed with exit code {exit} and produced: {err}.");
        }

        return encrypted;
    }

    public byte[] Decrypt(byte[] encrypted)
    {
        var exit = Invoke(_decryptor, _decryptorArgs, encrypted, out var decrypted, out var err);
        if (exit != 0)
        {
            throw new Exception($"Decryptor failed with exit code {exit} and produced: {err}.");
        }

        return decrypted;
    }

    static int Invoke(string fullExePath, string? args, byte[] stdin, out byte[] stdout, out string stderr)
    {
        var startInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            ErrorDialog = false,
            FileName = fullExePath,
            Arguments = args
        };

        using var process = Process.Start(startInfo);
        using var errorComplete = new ManualResetEvent(false);
        
        if (process == null)
            throw new InvalidOperationException("The process did not start.");

        var stderrBuf = new StringBuilder();
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null)
                // ReSharper disable once AccessToDisposedClosure
                errorComplete.Set();
            else
                stderrBuf.Append(e.Data);
        };
        process.BeginErrorReadLine();
        
        process.StandardInput.BaseStream.Write(stdin);
        process.StandardInput.BaseStream.Close();

        var stdoutBuf = ArrayPool<byte>.Shared.Rent(512);
        var stdoutBufLength = 0;
        while (true)
        {
            var remaining = stdoutBuf.Length - stdoutBufLength;
            if (remaining == 0)
            {
                var newBuffer = ArrayPool<byte>.Shared.Rent(stdoutBuf.Length * 2);
                stdoutBuf.CopyTo(newBuffer.AsSpan());
                
                ArrayPool<byte>.Shared.Return(stdoutBuf);
                stdoutBuf = newBuffer;
                
                remaining = stdoutBuf.Length - stdoutBufLength;
            }

            var read = process.StandardOutput.BaseStream.Read(stdoutBuf, stdoutBufLength, remaining);

            if (read == 0)
            {
                break;
            }

            stdoutBufLength += read;
        }
        
        errorComplete.WaitOne();
        stderr = stderrBuf.ToString();

        stdout = stdoutBuf.AsSpan()[..stdoutBufLength].ToArray();
        ArrayPool<byte>.Shared.Return(stdoutBuf);

        process.WaitForExit(TimeSpan.FromSeconds(30));
        
        return process.ExitCode;
    }
}