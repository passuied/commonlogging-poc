using Autofac;
using Autofac.Integration.WebApi;
using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace CommonLogging.Poc.WebApi
{
    public static class AutofacCommonLoggingExtensions
    {
        public static void RegisterLoggerWithRequestContext(this ContainerBuilder builder, HttpConfiguration config)
        {
            builder.RegisterHttpRequestMessage(config);

            builder.Register(c =>
            {
                var context = c.Resolve<MySampleAppContext>();
                var log = Common.Logging.LogManager.Adapter.GetLogger("CommonLogging.Poc.WebApi");

                LoadContextVariables(context, log);

                return log;
            })
            .InstancePerRequest();


            builder.Register<MySampleAppContext>(c =>
            {
                var request = c.Resolve<HttpRequestMessage>();

                var correlationID = request.Headers.GetValues("correlationID").FirstOrDefault();
                var corpName = request.Headers.GetValues("corpName").FirstOrDefault();
                var userID = request.Headers.GetValues("userID").FirstOrDefault();

                int nUserID = 0;
                int.TryParse(userID, out nUserID);

                var context = new MySampleAppContext
                {
                    CorpName = corpName,
                    UserID = nUserID,
                    CorrelationID = !string.IsNullOrEmpty(correlationID) ? new Guid(correlationID) : Guid.NewGuid()
                };

                return context;

            }
            );

            builder.Register<Func<string, ILog>>(c =>
            {
                var cc = c.Resolve<IComponentContext>();

                return logName =>
                {
                    var context = cc.Resolve<MySampleAppContext>();
                    var log = Common.Logging.LogManager.Adapter.GetLogger(logName);

                    LoadContextVariables(context, log);

                    return log;
                };
            })
            .InstancePerRequest();

        }

        private static void LoadContextVariables(MySampleAppContext context, ILog log)
        {
            log.ThreadVariablesContext.Set("correlationId", context.CorrelationID);
            log.ThreadVariablesContext.Set("corp", context.CorpName);
            log.ThreadVariablesContext.Set("userId", context.UserID);
        }
    }
}