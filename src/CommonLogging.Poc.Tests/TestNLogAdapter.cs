using Common.Logging;
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
            MySampleApp app = new MySampleApp(nLogAdapter, context);

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
            MySampleApp appCorp1 = new MySampleApp(nLogAdapter, contextCorp1);

            appCorp1.DoSomethingUsingCorpName();
            appCorp1.DoSomethingWithoutCorpName();
            Assert.Throws<InvalidOperationException>(() => appCorp1.ThrowSampleException());


            var contextCorp2 = new MySampleAppContext { CorpName = "corp2" };
            MySampleApp appCorp2 = new MySampleApp(nLogAdapter, contextCorp2);

            appCorp2.DoSomethingUsingCorpName();
            appCorp2.DoSomethingWithoutCorpName();
            Assert.Throws<InvalidOperationException>(() => appCorp2.ThrowSampleException());

            memTargetCorp.Logs.Count().ShouldBe(4);
        }

        [Fact]
        public void TestNLogAdapter_WhenAppsLogInParallel_AndLoggingByCorp_AndOnlyElevatingOneCorp_OnlyLogDebugMessagesOfOneCorp()
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
            rule.Filters.Add(new NLog.Filters.ConditionBasedFilter { Condition = NLog.Conditions.ConditionParser.ParseExpression("ignoreLogCorp('${mdc:item=corp}', level)"), Action = NLog.Filters.FilterResult.IgnoreFinal });
            config.LoggingRules.Add(rule);
            NLog.LogManager.Configuration = config;


            var properties = new NameValueCollection();
            var nLogAdapter = new Common.Logging.NLog.NLogLoggerFactoryAdapter(properties);
            Common.Logging.LogManager.Adapter = nLogAdapter;

            var concurrentApps = new List<MySampleApp>();
            for (int i = 1; i <= 10; i++)
            {
                var contextCorp = new MySampleAppContext { CorpName = "corp" +i };
                MySampleApp appCorp = new MySampleApp(nLogAdapter, contextCorp);
                concurrentApps.Add(appCorp);
            }

            Parallel.ForEach(concurrentApps,
                app =>
                {
                    app.DoSomethingUsingCorpName();
                    app.DoSomethingWithoutCorpName();
                    Assert.Throws<InvalidOperationException>(() => app.ThrowSampleException());
                });
                

            memTargetCorp.Logs.Count().ShouldBe(12);
        }

        [Fact]
        public void TestNLogAdapter_GivenMultipleClasses_WhenALoggerTargetsAClass_ThenOnlyEntriesFromThisClassDisplayInThisTarget()
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

            var memTargetAnotherApp = new NLog.Targets.MemoryTarget();
            memTargetAnotherApp.Layout =
@"{
    ""timestamp"" : '${longdate}',
    ""correlationId"" : '${mdc:item=correlationId}',
    ""level"" : '${level:upperCase=true}',
    ""message"" : '${message}',
    ""corpName"" : '${mdc:item=corp}'
}";

            // Log as Error by default exception for Corp1:Debug
            config.AddTarget("memTargetAnother", memTargetAnotherApp);


            // Elevation rule applies to specific class and will log from Debug and above regardless of corp
            var ruleAnotherApp = new LoggingRule("*.AnotherSampleApp", NLog.LogLevel.Debug, memTargetAnotherApp);
            ruleAnotherApp.Final = true;
            config.LoggingRules.Add(ruleAnotherApp);

            // Generic rule applies to every class with corp1 only elevated to Debug
            var rule = new LoggingRule("*", NLog.LogLevel.Debug, memTargetCorp);
            rule.Filters.Add(new NLog.Filters.ConditionBasedFilter { Condition = NLog.Conditions.ConditionParser.ParseExpression("ignoreLogCorp('${mdc:item=corp}', level)"), Action = NLog.Filters.FilterResult.IgnoreFinal });
            config.LoggingRules.Add(rule);
            
            


            NLog.LogManager.Configuration = config;


            var properties = new NameValueCollection();
            var nLogAdapter = new Common.Logging.NLog.NLogLoggerFactoryAdapter(properties);
            Common.Logging.LogManager.Adapter = nLogAdapter;

            var contextCorp1 = new MySampleAppContext { CorpName = "corp1" };
            MySampleApp appCorp1 = new MySampleApp(nLogAdapter, contextCorp1);

            appCorp1.DoSomethingUsingCorpName();
            appCorp1.DoSomethingWithoutCorpName();
            Assert.Throws<InvalidOperationException>(() => appCorp1.ThrowSampleException());


            var contextCorp2 = new MySampleAppContext { CorpName = "corp2" };
            AnotherSampleApp anotherApp = new AnotherSampleApp(nLogAdapter, contextCorp2);

            anotherApp.DoSomethingUsingCorpName();
            anotherApp.DoSomethingWithoutCorpName();
            Assert.Throws<InvalidOperationException>(() => anotherApp.ThrowSampleException());

            // AnotherApp logs 5(4debug+1error)
            memTargetAnotherApp.Logs.Count().ShouldBe(5);

            // MySampleApp logs 3 (corp1=2debug+1error)
            memTargetCorp.Logs.Count().ShouldBe(3);

            
        }

    }
}
