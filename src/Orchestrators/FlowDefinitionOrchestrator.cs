using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SolutionConnectionReferenceReassignment.Models;
using SolutionConnectionReferenceReassignment.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using SolutionConnectionReferenceReassignment.Utilities;

namespace SolutionConnectionReferenceReassignment.Orchestrators
{
    // Thought it may be best to handle this via a helper as both the FlowAction & ConnectionReference service utilise the same workflow.clientdata JSON.
    // Load from here to handle once and feed into utility for parsing.
    internal class FlowDefinitionOrchestrator
    {
        private readonly IOrganizationService _service;

        public FlowDefinitionOrchestrator(IOrganizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Retrieves and deserializes the <c>clientdata</c> JSON.
        /// </summary>
        /// <param name="workflowId">The GUID of the workflow to retrieve, as per the record <c>workflowid</c> of Dataverse table <c>workflow</c>.</param>
        /// <returns>A JSON object representing the workflow's <c>clientdata</c>, or <c>null</c> if the workflow is not found</returns>
        public JObject GetClientData(Guid workflowId)
        {
            var query = new QueryExpression("workflow")
            {
                ColumnSet = new ColumnSet("clientdata"),
                Criteria = new FilterExpression
                {
                    Conditions =
                {
                    new ConditionExpression("workflowid", ConditionOperator.Equal, workflowId)
                }
                }
            };

            var workflows = _service.RetrieveMultiple(query).Entities;

            if (!workflows.Any())
            {
                MessageBox.Show("Flow not found.");
                return null;
            }

            var clientDataRaw = workflows.First().GetAttributeValue<string>("clientdata");
            if (string.IsNullOrWhiteSpace(clientDataRaw))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<JObject>(clientDataRaw);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Error parsing clientData: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parses a workflow's <c>clientdata</c> into <see cref="FlowActionModel"/> and <see cref="ConnectionReferenceModel"/> objects.
        /// </summary>
        /// <param name="workflowId">The GUID of the workflow to retrieve, as per the record <c>workflowid</c> of Dataverse table <c>workflow</c>.</param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        /// <item><see cref="List{FlowActionModel}"/> representing all actions in the workflow.</item>
        /// <item><see cref="List{ConnectionReferenceModel}"/> representing all connection references in the workflow.</item>
        /// </list>
        /// Returns empty lists if <c>clientdata</c> is null or invalid.
        /// </returns>
        public (List<FlowActionModel> Actions, List<ConnectionReferenceModel> ConnectionReferences, int ErrorCount) GetParsedFlowDefinition(Guid workflowId)
        {
            var clientData = GetClientData(workflowId);
            if (clientData == null)
                return (new List<FlowActionModel>(), new List<ConnectionReferenceModel>(), 0);

            var (actions, actionErrorCount) = FlowJSONParser.ParseFlowActions(clientData);
            var connectionReferences = FlowJSONParser.ParseFlowConnectionReferences(clientData);

            return (actions, connectionReferences, actionErrorCount);
        }
    }
}
