namespace ServiceControl.Monitoring
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Autofac;
    using Http;
    using Messaging;
    using Nancy;
    using NServiceBus;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;

    public class EndpointFactory
    {
        internal static Task<IEndpointInstance> StartEndpoint(Settings settings)
        {
            var endpointConfiguration = PrepareConfiguration(settings);
            return Endpoint.Start(endpointConfiguration);
        }

        public static EndpointConfiguration PrepareConfiguration(Settings settings)
        {
            var config = new EndpointConfiguration(settings.EndpointName);
            MakeMetricsReceiver(config, settings);
            return config;
        }

        public static void MakeMetricsReceiver(EndpointConfiguration config, Settings settings, string explicitConnectionStringValue = null)
        {
            config.UseContainer<AutofacBuilder>(
                c => c.ExistingLifetimeScope(CreateContainer(settings))
            );

            var selectedTransportType = DetermineTransportType(settings);
            var transport = config.UseTransport(selectedTransportType)
                .Transactions(TransportTransactionMode.ReceiveOnly);

            if (explicitConnectionStringValue != null)
            {
                transport.ConnectionString(explicitConnectionStringValue);
            }
            else
            {
                transport.ConnectionStringName("NServiceBus/Transport");
            }

            if (settings.EnableInstallers)
            {
                config.EnableInstallers(settings.Username);
            }

            config.DefineCriticalErrorAction(c =>
            {
                Environment.FailFast("NServiceBus Critical Error", c.Exception);
                return TaskEx.Completed;
            });

            config.GetSettings().Set<Settings>(settings);

            config.UseSerialization<NewtonsoftSerializer>();
            config.UsePersistence<InMemoryPersistence>();
            config.SendFailedMessagesTo(settings.ErrorQueue);
            config.DisableFeature<AutoSubscribe>();

            config.AddDeserializer<TaggedLongValueWriterOccurrenceSerializerDefinition>();
            config.Pipeline.Register(typeof(MessagePoolReleasingBehavior), "Releases pooled message.");
            config.EnableFeature<QueueLength.QueueLength>();

            config.EnableFeature<HttpEndpoint>();

            var recoverability = config.Recoverability();
            recoverability.DisableLegacyRetriesSatellite();
        }

        static IContainer CreateContainer(Settings settings)
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterModule<ApplicationModule>();
            containerBuilder.RegisterInstance(settings).As<Settings>().SingleInstance();

            var container = containerBuilder.Build();
            return container;
        }

        static Type DetermineTransportType(Settings settings)
        {
            var transportTypeName = transportCustomizations.ContainsKey(settings.TransportType) 
                ? transportCustomizations[settings.TransportType] 
                : settings.TransportType;
                
            var transportType = Type.GetType(transportTypeName);

            if (transportType != null)
            {
                return transportType;
            }

            var errorMsg = $"Configuration of transport failed. Could not resolve type `{settings.TransportType}`";
            Logger.Error(errorMsg);
            throw new Exception(errorMsg);
        }

        static Dictionary<string, string> transportCustomizations = new Dictionary<string, string>
        {
            {
              "NServiceBus.AzureServiceBusTransport, NServiceBus.Azure.Transports.WindowsAzureServiceBus",
              "ServiceControl.Transports.AzureServiceBus.ForwardingTopologyAzureServiceBusTransport, ServiceControl.Transports.AzureServiceBus"
            }
        };

        static ILog Logger = LogManager.GetLogger<EndpointFactory>();
    }

    class MessagePoolReleasingBehavior : Behavior<IIncomingLogicalMessageContext>
    {
        public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
        {
            try
            {
                await next().ConfigureAwait(false);
            }
            finally
            {
                var messageType = context.Message.MessageType;
                var instance = context.Message.Instance;

                if (messageType == typeof(TaggedLongValueOccurrence))
                {
                    ReleaseMessage<TaggedLongValueOccurrence>(instance);
                }
            }
        }

        static void ReleaseMessage<T>(object instance) where T : RawMessage, new()
        {
            RawMessage.Pool<T>.Default.Release((T) instance);
        }
    }

    class ApplicationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterAssemblyTypes(ThisAssembly)
                .Where(Include)
                .AsSelf()
                .AsImplementedInterfaces()
                .SingleInstance();
        }

        static bool Include(Type type)
        {
            if (IsMessageType(type))
                return false;

            if (IsNancyModule(type))
                return false;

            if (IsMessageHandler(type))
                return false;

            return true;
        }

        static bool IsMessageType(Type type)
        {
            return typeof(IMessage).IsAssignableFrom(type);
        }

        static bool IsNancyModule(Type type)
        {
            return typeof(INancyModule).IsAssignableFrom(type);
        }

        static bool IsMessageHandler(Type type)
        {
            return type.GetInterfaces()
                .Where(@interface => @interface.IsGenericType)
                .Select(@interface => @interface.GetGenericTypeDefinition())
                .Any(genericTypeDef => genericTypeDef == typeof(IHandleMessages<>));
        }
    }
}