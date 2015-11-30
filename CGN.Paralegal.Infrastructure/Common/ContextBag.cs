using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace CGN.Paralegal.Infrastructure.Common
{
    public class ContextBag : IExtension<OperationContext>
    {
        public ContextBag()
        {
            State = new Dictionary<string, object>();
        }

        public IDictionary<string, object> State { get; private set; }

        // we don't really need implementations for these methods in this case

        public void Attach(OperationContext owner) { }

        public void Detach(OperationContext owner) { }
    }
}
