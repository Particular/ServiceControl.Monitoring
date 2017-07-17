﻿namespace ServiceControl.Monitoring
{
    using System;
    using System.ComponentModel;
    using System.ServiceProcess;
    using System.Threading.Tasks;
    using NServiceBus;

    [DesignerCategory("Code")]
    class HostService : ServiceBase
    {
        public void Run(bool interactive)
        {
            ServiceName = Settings.EndpointName;

            if (interactive)
            {
                RunInteractive();
            }
            else
            {
                RunAsService();
            }
        }

        private void RunInteractive()
        {
            OnStart(null);
        }

        private void RunAsService()
        {
            Run(this);
        }

        protected override void OnStart(string[] args)
        {
            AsyncOnStart().GetAwaiter().GetResult();
        }

        async Task AsyncOnStart()
        {
            endpointInstance = await EndpointFactory.StartEndpoint(Settings);
        }

        protected override void OnStop()
        {
            AsyncOnStop().GetAwaiter().GetResult();

            OnStopping();
        }

        Task AsyncOnStop()
        {
            if (endpointInstance != null)
            {
                return endpointInstance.Stop();
            }
            return Task.FromResult(0);
        }

        protected override void Dispose(bool disposing)
        {
            OnStop();
        }

        public Settings Settings { get; set; }

        internal Action OnStopping = () => { };
        IEndpointInstance endpointInstance;
    }
}