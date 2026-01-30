using Amazon.Lambda.AspNetCoreServer;

namespace ecommerce_back
{
   public class LambdaEntryPoint : APIGatewayHttpApiV2ProxyFunction
    {
        protected override void Init(IWebHostBuilder builder)
        {
            builder.UseStartup<Program>();
        }
    }
}