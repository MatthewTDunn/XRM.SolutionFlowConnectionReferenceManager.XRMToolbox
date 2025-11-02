using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionConnectionReferenceReassignment.Models
{
    internal class FlowUpdateModel
    {
        public class FlowUpdateResult
        {
            public int ProcessedFlows { get; set; }
            public int SuccessfulFlows { get; set; }
            public int TotalConnectionRefsChanged { get; set; }
            public int TotalActionsUpdated { get; set; }
            public List<string> PerFlowMessages { get; set; }

        }
        
        public class FlowUpdateItem
        {

        }

    }
}
