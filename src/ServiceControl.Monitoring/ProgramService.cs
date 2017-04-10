namespace ServiceControl.Monitoring
{
    using System;
    using System.ComponentModel;
    using System.ServiceProcess;
    using System.Threading.Tasks;
    using NServiceBus;

    [DesignerCategory("Code")]
    class ProgramService : ServiceBase
    {
        IEndpointInstance endpointInstance;

        static void Main()
        {
            using (var service = new ProgramService())
            {
                if (Environment.UserInteractive)
                {
                    service.OnStart(null);
                    Console.WriteLine("Bus started. Press any key to exit");
                    Console.ReadKey();
                    service.OnStop();
                    return;
                }
                Run(service);
            }
        }

        protected override void OnStart(string[] args)
        {
            AsyncOnStart().GetAwaiter().GetResult();
        }

        async Task AsyncOnStart()
        {
            endpointInstance = await EndpointFactory.StartEndpoint(true);
        }

        protected override void OnStop()
        {
            AsyncOnStop().GetAwaiter().GetResult();
        }

        Task AsyncOnStop()
        {
            if (endpointInstance != null)
            {
                return endpointInstance.Stop();
            }
            return Task.FromResult(0);
        }
    }
}