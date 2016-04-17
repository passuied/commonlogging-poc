using Common.Logging;
using Common.Logging.Configuration;
using NLog.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;

namespace CommonLogging.Poc.WebApi
{
    public class LoggingConfig
    {
        public static ILoggerFactoryAdapter Register(HttpConfiguration config)
        {
            // Load extensions
            NLog.Config.ConfigurationItemFactory.Default.RegisterItemsFromAssembly(Assembly.Load("CommonLogging.NLogExtensions"));

            // Configure NLog to write into MemTarget
            var logConfig = new LoggingConfiguration();

            // Configure target
            var fileTarget = new NLog.Targets.FileTarget();
            fileTarget.FileName = "${basedir}/logs/logfile.txt";
            fileTarget.KeepFileOpen = false;
            fileTarget.Layout =
@"{
    ""timestamp"" : '${longdate}',
    ""correlationId"" : '${mdc:item=correlationId}',
    ""level"" : '${level:upperCase=true}',
    ""message"" : '${message}',
    ""corpName"" : '${mdc:item=corp}',
    ""userID"" : ${mdc:item=userId}
}";

            // Log as Error by default exception for Corp1:Debug
            logConfig.AddTarget("fileTarget", fileTarget);
            var rule = new LoggingRule("*", NLog.LogLevel.Debug, fileTarget);
            rule.Filters.Add(new NLog.Filters.ConditionBasedFilter { Condition = NLog.Conditions.ConditionParser.ParseExpression("ignoreLogCorp('${mdc:item=corp}', level)"), Action = NLog.Filters.FilterResult.IgnoreFinal });
            logConfig.LoggingRules.Add(rule);
            NLog.LogManager.Configuration = logConfig;


            var properties = new NameValueCollection();
            var nLogAdapter = new Common.Logging.NLog.NLogLoggerFactoryAdapter(properties);
            Common.Logging.LogManager.Adapter = nLogAdapter;

            return nLogAdapter;
        }
    }
}