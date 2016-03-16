using NLog.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLogging.NLogExtensions
{
    [ConditionMethods]
    public static class NLogExtensions
    {

        [ConditionMethod("ignoreLogCorp")]
        public static bool IgnoreLogCorp(string corpName, NLog.LogLevel level)
        {
            if (corpName == "corp1" && level >= NLog.LogLevel.Debug)
            {
                return false;
            }
            else if (level < NLog.LogLevel.Error)
            {
                return true;
            }
            else
                return false;

        }
    }
}
