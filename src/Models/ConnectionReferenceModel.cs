using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionConnectionReferenceReassignment.Models
{
    internal class ConnectionReferenceModel
    {
        public string DisplayName { get; set; } // Populated by the Dataverse enrichment function within the orchestrator FlowDefinitionOrchestrator.cs
        public string Name { get; set; }

        public string LogicalName { get; set; }

        public string RuntimeSource { get; set; } //clientData exclusive property

        public string ConnectorId { get; set; } //correlate with "logicalname" for datagridview row. [make sure users can't pick inappropriate connection reference]

        public int IndividualFlowCount { get; set; }
        public int FlowActionCount {  get; set; }
        
    }
}
