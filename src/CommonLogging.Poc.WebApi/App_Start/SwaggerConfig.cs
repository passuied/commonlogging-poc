using Swashbuckle.Application;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace CommonLogging.Poc.WebApi
{
    public class SwaggerConfig
    {
        internal static void Register(HttpConfiguration config)
        {
            var hdrCorrelationID = new SwaggerHeaderParameter { Key = "correlationId", Name = "correlationId", Description = "Correlation ID", DefaultValue=Guid.NewGuid().ToString() };
            var hdrCorp = new SwaggerHeaderParameter { Key = "corpName", Name = "corpName", Description = "client corp name" };
            var hdrUserId = new SwaggerHeaderParameter { Key = "userID", Name = "userID", Description = "User ID" };

            config
                .EnableSwagger(c => {

                    var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    // if current domain base directory a level above bin folder, move to bin folder
                    if (Directory.Exists(baseDirectory + "\\bin"))
                        baseDirectory = baseDirectory + "\\bin";

                  
                  
                    c.SingleApiVersion("v1", "Common Logging POC Web API");
                    c.RootUrl(req => req.RequestUri.GetLeftPart(UriPartial.Authority) +
                                    req.GetRequestContext().VirtualPathRoot.TrimEnd('/'));

                    c.DescribeAllEnumsAsStrings();

                    hdrCorp.Apply(c);
                    hdrCorrelationID.Apply(c);
                    hdrUserId.Apply(c);
                
                })
            .EnableSwaggerUi(c =>
            {
                c.DisableValidator();
            });
        }
    }
}