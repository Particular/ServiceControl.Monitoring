namespace ServiceControl.Monitoring.Http
{
    using Nancy;
    public abstract class ApiModule : NancyModule
    {
        protected ApiModule()
        {
            After.AddItemToEndOfPipeline(ctx => ctx.Response
                .WithHeader("Access-Control-Allow-Origin", "*")
                .WithHeader("Access-Control-Allow-Methods", "POST,GET")
                .WithHeader("Accept", "application/json")
                .WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type"));
        }
    }
}