using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionConnectionReferenceReassignment.Models
{
    internal class FlowActionModel
    {
        #region Enriched FlowMetadata Properties
        public string FlowName { get; set; } 
        public Guid FlowId { get; set; }
        #endregion

        public string ActionName { get; set; }
        public string OperationMetadataId { get; set; }
        public string ApiIdExtract { get; set; } // default format "/providers/Microsoft.PowerApps/apis/{apiIdExtract}"
        public string ConnectionName { get; set; }
        public string ConnectionReferenceLogicalName { get; set; }
        public string OperationId { get; set; }
        public string Parameters { get; set; }

    }
}
