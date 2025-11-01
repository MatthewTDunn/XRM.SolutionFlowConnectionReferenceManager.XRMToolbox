using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SolutionConnectionReferenceReassignment.Models;
using SolutionConnectionReferenceReassignment.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SolutionConnectionReferenceReassignment.Services
{
    internal class FlowActionService
    {
        private readonly IOrganizationService _service;

        public FlowActionService(IOrganizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Retrieves the clientData JSON from Dataverse workflow record and parses it into a list of FlowActionModel objects via the ParseFlowActions utility <see cref="FlowJSONParser"/>
        /// </summary>
        /// <param name="workflowId">The GUID of the Power Automate flow <c>workflowid</c> of table <c>workflow</c></param>
        /// <returns>A list of FlowActionModel objects representing the actions in the flow <see cref="FlowActionModel"/></returns>
        public (List<FlowActionModel> Actions, int ErrorCount) GetFlowActions(Guid workflowId)
        {
            var actions = new List<FlowActionModel>();
            int errorCount = 0;

            var workflowQuery = new QueryExpression("workflow")
            {
                ColumnSet = new ColumnSet("workflowid", "name", "clientdata"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("workflowid", ConditionOperator.Equal, workflowId)
                    }
                }
            };

            var workflows = _service.RetrieveMultiple(workflowQuery).Entities;
            if (!workflows.Any())
            {
                MessageBox.Show("Flow not found.");
                return (actions, errorCount);
            }

            var flow = workflows.First();
            var clientData = flow.GetAttributeValue<string>("clientdata");
            if (string.IsNullOrWhiteSpace(clientData))
                return (actions, errorCount);

            JObject clientDataJson = null;
            try
            {
                clientDataJson = JsonConvert.DeserializeObject<JObject>(clientData);
            }
            catch (JsonException)
            {
                // TODO: MATT LOG IF NEEDED AT SOME POINT
                return (actions, errorCount);
            }

            if (clientDataJson != null)
            {
                var (parsedActions, parseErrorCount) = FlowJSONParser.ParseFlowActions(clientDataJson);
                actions.AddRange(parsedActions);
                errorCount += parseErrorCount;
            }

            return (actions, errorCount);
        }

        /// <summary>
        /// Backward compatibility method that returns only the actions list (for existing code that doesn't need error counts)
        /// </summary>
        /// <param name="workflowId">The GUID of the Power Automate flow <c>workflowid</c> of table <c>workflow</c></param>
        /// <returns>A list of FlowActionModel objects representing the actions in the flow</returns>
        public List<FlowActionModel> GetFlowActionsOnly(Guid workflowId)
        {
            var (actions, _) = GetFlowActions(workflowId);
            return actions;
        }
    }
}
