using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLogging.Poc
{
    public class MySampleManager : IMySampleManager
    {
        public MySampleManager(ILog log, MySampleAppContext context)
        {
            this.Log = log;
            this.Context = context;
        }

        public MySampleManager(ILoggerFactoryAdapter logAdapter, MySampleAppContext context)
        {
            this.Log = logAdapter.GetLogger(this.GetType());
            this.Context = context;
        }

        public void DoSomethingUsingCorpName()
        {
            Log.Debug(m => m("Starting DoSomethingUsingCorpName()"), this.Context.CorrelationID, corpName:this.Context.CorpName);

            // Do something

            Log.Debug(m => m("Completed DoSomethingUsingCorpName()"), this.Context.CorrelationID, this.Context.CorpName);
        }

        public void DoSomethingWithoutCorpName()
        {
            Log.Debug(m => m("Starting DoSomethingWithoutCorpName()"), this.Context.CorrelationID);

            // Do something

            Log.Debug(m => m("Completed DoSomethingWithoutCorpName()"), this.Context.CorrelationID);
        }

        public void ThrowSampleException()
        {
            try
            {
                throw new InvalidOperationException("Something is invalid");
            
            }
            catch(InvalidOperationException ioe)
            {
                Log.Error(m => m("Invalid operation"), this.Context.CorrelationID, this.Context.CorpName, ioe);
                throw;
            }
        }

        public ILog Log { get; private set; }
        public MySampleAppContext Context { get; private set; }
    }
}
