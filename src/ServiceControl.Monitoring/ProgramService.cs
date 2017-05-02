using System;
using System.ComponentModel;
using System.ServiceProcess;
using System.Threading.Tasks;
using NServiceBus;

namespace ServiceControl.Monitoring
{
    using System.IO;
    using System.Reflection;

    [DesignerCategory("Code")]
    class ProgramService : ServiceBase
    {
        IEndpointInstance endpointInstance;
        static void Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) => ResolveAssembly(e.Name);

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

        static Assembly ResolveAssembly(string name)
        {
            var assemblyLocation = Assembly.GetEntryAssembly().Location;
            var appDirectory = Path.GetDirectoryName(assemblyLocation);
            var requestingName = new AssemblyName(name).Name;

            // ReSharper disable once AssignNullToNotNullAttribute
            var combine = Path.Combine(appDirectory, requestingName + ".dll");
            return !File.Exists(combine) ? null : Assembly.LoadFrom(combine);
        }
    }
}