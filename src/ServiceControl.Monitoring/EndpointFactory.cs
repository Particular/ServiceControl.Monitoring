namespace ServiceControl.Monitoring
{
    using System;
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
    using Timings;

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

        public static void MakeMetricsReceiver(EndpointConfiguration config, Settings settings)
        {
            config.UseContainer<AutofacBuilder>(
                c => c.ExistingLifetimeScope(CreateContainer(settings))
            );

            var selectedTransportType = DetermineTransportType(settings);
            var transport = config.UseTransport(selectedTransportType);

            transport.ConnectionStringName("NServiceBus/Transport");

            if (settings.EnableInstallers)
            {
                config.EnableInstallers(settings.Username);
            }

            config.GetSettings().Set<Settings>(settings);

            config.UseSerialization<NewtonsoftSerializer>();
            config.UsePersistence<InMemoryPersistence>();
            config.SendFailedMessagesTo(settings.ErrorQueue);
            config.DisableFeature<AutoSubscribe>();

            var recoverability = config.Recoverability();
            recoverability.AddUnrecoverableException<UnknownLongValueOccurrenceMessageType>();
            recoverability.AddUnrecoverableException<UnknownOccurrenceMessageType>();
            config.AddDeserializer<LongValueOccurrenceSerializerDefinition>();
            config.AddDeserializer<OccurrenceSerializerDefinition>();
            config.AddDeserializer<TaggedLongValueWriterOccurrenceSerializerDefinition>();
            config.Pipeline.Register(typeof(MessagePoolReleasingBehavior), "Releases pooled message.");
            config.EnableFeature<QueueLength.QueueLength>();

            config.EnableFeature<HttpEndpoint>();
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
            var transportType = Type.GetType(settings.TransportType);
            if (transportType != null)
            {
                return transportType;
            }

            var errorMsg = $"Configuration of transport failed. Could not resolve type `{settings.TransportType}`";
            Logger.Error(errorMsg);
            throw new Exception(errorMsg);
        }

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
                else if (messageType == typeof(LongValueOccurrences))
                {
                    ReleaseMessage<LongValueOccurrences>(instance);
                }
                else if (messageType == typeof(Occurrences))
                {
                    ReleaseMessage<Occurrences>(instance);
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