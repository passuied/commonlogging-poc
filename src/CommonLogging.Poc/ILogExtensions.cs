using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLogging.Poc
{
    public static class ILogExtensions
    {
        public static void Debug(this ILog log, Action<FormatMessageHandler> messageHandler, Guid correlationID, string corpName = null, Exception exception = null)
        {
            log.LogWithThreadContextVariables(() => log.Debug(messageHandler, exception), BuildContextDataDictionary(correlationID, corpName));
        }

        public static void Error(this ILog log, Action<FormatMessageHandler> messageHandler, Guid correlationID, string corpName = null, Exception exception = null)
        {
            log.LogWithThreadContextVariables(() => log.Error(messageHandler, exception), BuildContextDataDictionary(correlationID, corpName));
        }

        public static void Fatal(this ILog log, Action<FormatMessageHandler> messageHandler, Guid correlationID, string corpName = null, Exception exception = null)
        {
            log.LogWithThreadContextVariables(() => log.Fatal(messageHandler, exception), BuildContextDataDictionary(correlationID, corpName));
        }

        public static void Warn(this ILog log, Action<FormatMessageHandler> messageHandler, Guid correlationID, string corpName = null, Exception exception = null)
        {
            log.LogWithThreadContextVariables(() => log.Warn(messageHandler, exception), BuildContextDataDictionary(correlationID, corpName));
        }

        public static void Info(this ILog log, Action<FormatMessageHandler> messageHandler, Guid correlationID, string corpName = null, Exception exception = null)
        {
            log.LogWithThreadContextVariables(() => log.Info(messageHandler, exception), BuildContextDataDictionary(correlationID, corpName));
        }

        public static void Trace(this ILog log, Action<FormatMessageHandler> messageHandler, Guid correlationID, string corpName = null, Exception exception = null)
        {
            log.LogWithThreadContextVariables(() => log.Trace(messageHandler, exception), BuildContextDataDictionary(correlationID, corpName));
        }

        private static IDictionary<string, object> BuildContextDataDictionary(Guid correlationID, string corpName=null)
        {
            var dic = new Dictionary<string, object>();
            dic.Add("correlationId", correlationID);
            if (!string.IsNullOrEmpty(corpName))
                dic.Add("corp", corpName);

            return dic;
        }

        private static void LogWithThreadContextVariables(this ILog log, Action logAction, IDictionary<string, object> contextData)
        {
            // Set context data
            foreach(var kv in contextData)
            {
                log.ThreadVariablesContext.Set(kv.Key, kv.Value);

            }

            logAction();

            // Remove context data from thread variables
            foreach (var kv in contextData)
            {
                log.ThreadVariablesContext.Remove(kv.Key);

            }
        }
    }
}
