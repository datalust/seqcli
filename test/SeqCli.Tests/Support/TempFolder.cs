#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace SeqCli.Tests.Support;

class TempFolder : IDisposable
{
    static readonly Guid Session = Guid.NewGuid();

    public TempFolder(string name)
    {
        Path = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Seq.Forwarder.Tests",
            Session.ToString("n"),
            name);

        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    public static TempFolder ForCaller([CallerMemberName] string? caller = null)
    {
        if (caller == null) throw new ArgumentNullException(nameof(caller));
        return new TempFolder(caller);
    }

    public string AllocateFilename(string? ext = null)
    {
        return System.IO.Path.Combine(Path, Guid.NewGuid().ToString("n") + "." + (ext ?? "tmp"));
    }
}