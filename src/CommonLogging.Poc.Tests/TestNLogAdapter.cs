﻿using Common.Logging;
using Common.Logging.Configuration;
using Newtonsoft.Json.Linq;
using NLog.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using XunitShouldExtension;
using System.Reflection;

namespace CommonLogging.Poc.Tests
{
    public class TestNLogAdapter
    {
        public Assembly Assemby { get; private set; }

        [Fact]
        public void TestNLogAdapter_WhenUsingNLogAdapterWithTwoMemTargets_AndMinLevelDebug_ThenAllLogsGetLoggedIntoMemTargetInfo_And_AllExceptionsGetLoggedInMemTargetException()
        {
            // Configure NLog to write into MemTarget
            var config = new LoggingConfiguration();
            var memTargetInfo = new NLog.Targets.MemoryTarget();
            var memTargetException = new NLog.Targets.MemoryTarget();
            memTargetInfo.Layout =
@"{
    ""timestamp"" : '${longdate}',
    ""correlationId"" : '${mdc:item=correlationId}',
    ""level"" : '${level:upperCase=true}',
    ""message"" : '${message}',
    ""corpName"" : '${mdc:item=corp}'
}";

memTargetException.Layout =
@"{
    ""timestamp"" : '${longdate}',
    ""correlationId"" : '${mdc:item=correlationId}',
    ""level"" : '${level:upperCase=true}',
    ""message"" : '${message}',
    ""exception"" : { 
        ""type"" : '${exception:format=Type}',
        ""message"" : '${exception:format=Message}'
        }
    ,
    ""corpName"" : '${mdc:item=corp}'
}";
            config.AddTarget("memTargetInfo", memTargetInfo);
            config.AddTarget("memTargetException", memTargetInfo);
            config.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Debug, memTargetInfo));
            config.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Error, memTargetException));
            NLog.LogManager.Configuration = config;
            

            var properties = new NameValueCollection();
            var nLogAdapter = new Common.Logging.NLog.NLogLoggerFactoryAdapter(properties);
            Common.Logging.LogManager.Adapter = nLogAdapter;

            var context = new MySampleAppContext { CorpName = "corp1" };
            MySampleApp app = new MySampleApp(nLogAdapter.GetLogger("MySampleApp"), context);

            app.DoSomethingUsingCorpName();
            app.DoSomethingWithoutCorpName();
            Assert.Throws<InvalidOperationException>(() => app.ThrowSampleException());

            memTargetInfo.Logs.Count().ShouldBe(5);


            // Check all records have correlation ID and corp Name set
            memTargetInfo.Logs
                    .Select((l, i) => new { l, i })
                    .Where(x => new[] { 0, 1, 4 }.Contains(x.i))
                    .Select(x => JObject.Parse(x.l))
                    .All(l => l["corpName"].Value<string>() == context.CorpName && l["correlationId"].Value<string>() == context.CorrelationID.ToString())
                    .ShouldBeTrue();

            memTargetException.Logs.Count().ShouldBe(1);

            // Check all exception records have exception set
            memTargetException.Logs
                    .Select(l => JObject.Parse(l))
                    .All(l => l["exception"] != null && l["exception"].Children().Count() > 0)
                    .ShouldBeTrue();

        }

        [Fact]
        public void TestNLogAdapter_WhenCorpNamePassed_LogLevelVariesPerCorpName()
        {
            // Load extensions
            NLog.Config.ConfigurationItemFactory.Default.RegisterItemsFromAssembly(Assembly.Load("CommonLogging.NLogExtensions"));

            // Configure NLog to write into MemTarget
            var config = new LoggingConfiguration();
            var memTargetCorp = new NLog.Targets.MemoryTarget();
            memTargetCorp.Layout =
@"{
    ""timestamp"" : '${longdate}',
    ""correlationId"" : '${mdc:item=correlationId}',
    ""level"" : '${level:upperCase=true}',
    ""message"" : '${message}',
    ""corpName"" : '${mdc:item=corp}'
}";
            
            // Log as Error by default exception for Corp1:Debug
            config.AddTarget("memTarget", memTargetCorp);
            var rule = new LoggingRule("*", NLog.LogLevel.Debug, memTargetCorp);
            rule.Filters.Add(new NLog.Filters.ConditionBasedFilter { Condition=NLog.Conditions.ConditionParser.ParseExpression("ignoreLogCorp('${mdc:item=corp}', level)"), Action=NLog.Filters.FilterResult.IgnoreFinal });
            config.LoggingRules.Add(rule);
            NLog.LogManager.Configuration = config;


            var properties = new NameValueCollection();
            var nLogAdapter = new Common.Logging.NLog.NLogLoggerFactoryAdapter(properties);
            Common.Logging.LogManager.Adapter = nLogAdapter;

            var contextCorp1 = new MySampleAppContext { CorpName = "corp1" };
            MySampleApp appCorp1 = new MySampleApp(nLogAdapter.GetLogger("MySampleApp"), contextCorp1);

            appCorp1.DoSomethingUsingCorpName();
            appCorp1.DoSomethingWithoutCorpName();
            Assert.Throws<InvalidOperationException>(() => appCorp1.ThrowSampleException());


            var contextCorp2 = new MySampleAppContext { CorpName = "corp2" };
            MySampleApp appCorp2 = new MySampleApp(nLogAdapter.GetLogger("MySampleApp"), contextCorp2);

            appCorp2.DoSomethingUsingCorpName();
            appCorp2.DoSomethingWithoutCorpName();
            Assert.Throws<InvalidOperationException>(() => appCorp2.ThrowSampleException());

            memTargetCorp.Logs.Count().ShouldBe(4);
        }
    }
}