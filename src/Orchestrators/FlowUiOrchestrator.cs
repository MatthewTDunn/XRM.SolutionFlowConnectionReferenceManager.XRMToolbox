using Microsoft.Xrm.Sdk;
using NuGet.Packaging;
using SolutionConnectionReferenceReassignment.Models;
using SolutionConnectionReferenceReassignment.Services;
using SolutionConnectionReferenceReassignment.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SolutionConnectionReferenceReassignment.Orchestrators
{
    internal class FlowUiOrchestrator
    {
        private readonly IOrganizationService _service;

        public FlowUiOrchestrator(IOrganizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Retrieves all relevant flow actions and unique connection references associated with the user specified <c>tree_SolutionFlowExplorer</c> node.
        /// </summary>
        /// <param name="node">The <c>tree_SolutionFlowExplorer</c> node selected by the user. The node's <c>Tag</c> property determines the scope of retrieval:
        /// <list type="bullet">
        /// <item>If the tag is a <see cref="SolutionModel"/>, retrieves all flows within that solution.</item>
        /// <item>If the tag is a <see cref="FlowModel"/>, retrieves all actions and connection references for that specific flow.</item>
        /// <item>If the tag is a <see cref="FlowActionModel"/>, retrieves that action and any associated connection references from its parent flow.</item>
        /// </list>
        /// </param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        /// <item><c>Actions</c>: a <see cref="List{FlowActionModel}"/> representing the flow actions associated with the node.</item>
        /// <item><c>ConnectionReferences</c>: a <see cref="List{ConnectionReferenceModel}"/> representing unique connection references associated with the retrieved actions.</item>
        /// </list>
        /// </returns>
        public (List<FlowActionModel> Actions, List<ConnectionReferenceModel> ConnectionReferences) GetDataForNode(TreeNode node)
        {
            var orchestrator = new FlowDefinitionOrchestrator(_service);
            var enricher = new DataEnricher(_service);
            List<FlowActionModel> actions = new List<FlowActionModel>();
            List<ConnectionReferenceModel> connectionReferences = new List<ConnectionReferenceModel>();

            int totalErrorCount = 0;
            int totalFlowsProcessed = 0;

            if (node.Parent == null && node.Text == "Current Environment")
            {
                var flowService = new FlowService(_service);
                var allFlows = flowService.GetAllFlowsInEnvironment();
                totalFlowsProcessed = allFlows.Count;

                foreach (var flow in allFlows)
                {
                    var (flowActions, flowConnectionReferences, errorCount) = orchestrator.GetParsedFlowDefinition(flow.FlowId);
                    totalErrorCount += errorCount;
                    enricher.EnrichFlowActionsWithFlowMetadata(flowActions, flow.Name, flow.FlowId);
                    actions.AddRange(flowActions);
                    connectionReferences.AddRange(flowConnectionReferences);
                }
            }
            else if (node.Tag is SolutionModel solution)
            {
                var flowService = new FlowService(_service);
                var flows = new List<FlowModel> { };
                if (node.Text == "Default Solution" || node.Text == "Common Data Services Default Solution")
                {
                    flows = flowService.GetAllFlowsInEnvironment();
                } else
                {
                    flows = flowService.GetFlowsInSolution(solution.SolutionId);
                }
                    
                totalFlowsProcessed = flows.Count;

                foreach (var flow in flows)
                {
                    var (flowActions, flowConnectionReferences, errorCount) = orchestrator.GetParsedFlowDefinition(flow.FlowId);
                    totalErrorCount += errorCount;
                    enricher.EnrichFlowActionsWithFlowMetadata(flowActions, flow.Name, flow.FlowId);
                    actions.AddRange(flowActions);
                    connectionReferences.AddRange(flowConnectionReferences);
                }
            }
            else if (node.Tag is FlowModel flowMetadata)
            {
                var (flowActions, flowConnectionReferences, errorCount) = orchestrator.GetParsedFlowDefinition(flowMetadata.FlowId);
                totalErrorCount += errorCount;
                totalFlowsProcessed = 1;
                enricher.EnrichFlowActionsWithFlowMetadata(flowActions, flowMetadata.Name, flowMetadata.FlowId);
                actions = flowActions;
                connectionReferences = flowConnectionReferences;
            }
            else if (node.Tag is FlowActionModel flowAction)
            {
                var parentNode = node.Parent;
                if (parentNode?.Tag is FlowModel parentFlowMetadata)
                {
                    flowAction.FlowName = parentFlowMetadata.Name;
                    flowAction.FlowId = parentFlowMetadata.FlowId;
                    var (flowActions, flowConnectionReferences, errorCount) = orchestrator.GetParsedFlowDefinition(parentFlowMetadata.FlowId);
                    totalErrorCount += errorCount;
                    totalFlowsProcessed = 1;
                    enricher.EnrichFlowActionsWithFlowMetadata(flowActions, parentFlowMetadata.Name, parentFlowMetadata.FlowId);
                    connectionReferences = flowConnectionReferences;
                }
                actions = new List<FlowActionModel> { flowAction };
            }

            // Show error summary if there were any errors
            FlowJSONParser.ShowErrorSummaryIfNeeded(totalErrorCount, totalFlowsProcessed);

            // Remove duplicates from the connection reference list
            var uniqueConnectionReferences = connectionReferences
                .GroupBy(cr => cr.LogicalName)
                .Select(g => g.First())
                .ToList();

            return (actions, uniqueConnectionReferences);
        }

    }
}
