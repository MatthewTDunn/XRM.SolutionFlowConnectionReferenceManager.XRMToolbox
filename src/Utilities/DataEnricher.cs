using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SolutionConnectionReferenceReassignment.Models;
using SolutionConnectionReferenceReassignment.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionConnectionReferenceReassignment.Utilities
{
    internal class DataEnricher
    {
        private readonly IOrganizationService _service;

        public DataEnricher(IOrganizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Enriches a collection of <see cref="FlowActionModel"/> objects by attaching flow-level metadata.
        /// </summary>
        /// <param name="actions">The list of <see cref="FlowActionModel"/> instances to enrich.
        /// <param name="flowName">The display name of the flow. If <c>null</c> or whitespace, the default value <c>"Unidentified Flow Name"</c> is assigned.</param>
        /// <param name="flowId">The GUID <c>workflowid</c> from the record on the<c>workflow</c> table</param>
        public void EnrichFlowActionsWithFlowMetadata(List<FlowActionModel> actions, string flowName, Guid flowId)
        {
            if (actions == null) 
                return;

            foreach (var action in actions)
            {
                action.FlowName = string.IsNullOrWhiteSpace(flowName) 
                    ? "Unidentified Flow Name"
                    : flowName;
                action.FlowId = flowId;
            }
        }
    }
}
