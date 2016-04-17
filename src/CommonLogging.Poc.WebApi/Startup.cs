using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using System.Web.Http;

[assembly: OwinStartup(typeof(CommonLogging.Poc.WebApi.Startup))]

namespace CommonLogging.Poc.WebApi
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            HttpConfiguration config = new HttpConfiguration();

            SwaggerConfig.Register(config);

            WebApiConfig.Register(config);

            LoggingConfig.Register(config);

            var container = AutofacConfig.Register(config);

            app.UseAutofacMiddleware(container);
            app.UseAutofacWebApi(config);
            app.UseWebApi(config);
        }
    }
}
