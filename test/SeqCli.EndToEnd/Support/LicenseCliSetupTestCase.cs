using System;
using System.Threading.Tasks;
using Seq.Api;
using Serilog;

namespace SeqCli.EndToEnd.Support
{
    public class LicenseSetup
    {
        readonly bool _enabled;

        bool _attempted;
        string _certificate;

        public LicenseSetup(Args args)
        {
            _enabled = args.Multiuser();
        }
        
        public async Task SetupAsync(
            SeqConnection connection,
            ILogger logger)
        {
            if (!_enabled)
                return;

            if (!_attempted)
            {
                _attempted = true;
                logger.Information("Reading license certificate from STDIN....");
                _certificate = await Console.In.ReadToEndAsync();
            }

            var license = await connection.Licenses.FindCurrentAsync();
            license.LicenseText = _certificate;
            await connection.Licenses.UpdateAsync(license);
        }
    }
}