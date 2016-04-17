using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLogging.Poc
{
    public class MySampleAppContext
    {
        public MySampleAppContext()
        {
            CorrelationID = Guid.NewGuid();
        }
        public Guid CorrelationID { get; set; }

        public string CorpName { get; set; }

        public int UserID { get; set; }

    }
}
