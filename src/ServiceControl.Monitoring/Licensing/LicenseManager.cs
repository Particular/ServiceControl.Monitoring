using System;

namespace ServiceControl.Monitoring.Licensing
{
    using System.IO;
    using NServiceBus.Logging;
    using Particular.Licensing;

    public class LicenseManager
    {
        internal License Details { get; set; }

        public void Refresh()
        {
            Logger.Debug("Checking License Status");
            var result = ActiveLicense.Find("ServiceControl",
                new LicenseSourceHKLMRegKey(),
                new LicenseSourceFilePath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "License", "License.xml")),
                new LicenseSourceFilePath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ParticularPlatformLicense.xml")));

            if (result.HasExpired)
            {
                foreach (var report in result.Report)
                {
                    Logger.Info(report);
                }
                Logger.Warn("License has expired");
            }
            
            Details = result.License;
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(ActiveLicense));
    }
    
}
