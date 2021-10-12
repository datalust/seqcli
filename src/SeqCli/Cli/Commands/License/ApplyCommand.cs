using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Connection;
using SeqCli.Util;
using Serilog;

// ReSharper disable once UnusedType.Global

#nullable enable

namespace  SeqCli.Cli.Commands.License
{
    [Command("license", "apply", "Apply a license to the Seq server",
        Example = "seqcli license apply --certificate=\"license.txt\"")]
    class ApplyCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;
        readonly ConnectionFeature _connection;

        string? _certificateFilename;
        bool _certificateStdin;
        bool _automaticallyRefresh;
        
        public ApplyCommand(SeqConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            Options.Add("c=|certificate=",
                "Certificate file; the file must be UTF-8 text",
                v => _certificateFilename = ArgumentString.Normalize(v));

            Options.Add("certificate-stdin",
                "Read the license certificate from `STDIN`",
                _ => _certificateStdin = true);

            Options.Add("automatically-refresh",
                "If the license is for a subscription, periodically check `datalust.co` and automatically refresh " +
                "the certificate when the subscription is changed or renewed",
                _ => _automaticallyRefresh = true);
            
            _connection = Enable<ConnectionFeature>();
        }

        protected override async Task<int> Run()
        {
            string certificate;
            if (_certificateStdin && _certificateFilename == null)
            {
                certificate = await Console.In.ReadToEndAsync();
            }
            else if (_certificateFilename != null)
            {
                if (!File.Exists(_certificateFilename))
                {
                    Log.Error("The file `{CertificateFilename}` does not exist", _certificateFilename);
                    return 1;
                }

                certificate = await File.ReadAllTextAsync(_certificateFilename, Encoding.UTF8);
            }
            else
            {
                Log.Error("A certificate must be provided, using one of either `--certificate=` or `--certificate-stdin`");
                return 1;
            }

            if (string.IsNullOrWhiteSpace(certificate))
            {
                Log.Error("The certificate cannot be blank");
                return 1;
            }

            var connection = _connectionFactory.Connect(_connection);
            var license = await connection.Licenses.FindCurrentAsync();
            license.LicenseText = certificate;
            license.AutomaticallyRefresh = _automaticallyRefresh;
            await connection.Licenses.UpdateAsync(license);
            return 0;
        }
    }
}
