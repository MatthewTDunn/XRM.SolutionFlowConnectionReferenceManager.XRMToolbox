using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SolutionConnectionReferenceReassignment.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionConnectionReferenceReassignment.Services
{
    internal class FlowService
    {
        private readonly IOrganizationService _service;

        public FlowService(IOrganizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Retrieves all Power Automate flows that are included in a given Dataverse solution.
        /// </summary>
        /// <param name="solutionId">The GUID of the Dataverse solution.</param>
        /// <returns>
        /// A list of <see cref="FlowModel"/> objects representing each flow in the solution, including its name,
        /// unique identifier, state code, and status code. Returns an empty list if no flows are found in the solution.
        /// </returns>
        /// <remarks>
        /// In the context of tool, This is the highlighted obj on the <c>tree_SolutionFlowExplorer</c> UI component via <see cref="SolutionConnectionReferenceReassignmentControl"/>.
        /// </remarks>
        public List<FlowModel> GetFlowsInSolution(Guid solutionId)
        {
            var componentQuery = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("objectid"),
                Criteria = new FilterExpression
                {
                    Conditions =
            {
                new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId),
                new ConditionExpression("componenttype", ConditionOperator.Equal, 29) // 29 is flow/workflow
            }
                }
            };
            var flowComponentIds = _service.RetrieveMultiple(componentQuery)
                .Entities
                .Select(e => e.GetAttributeValue<Guid>("objectid"))
                .ToList();
            var flows = new List<FlowModel>();
            foreach (var flowId in flowComponentIds)
            {
                var flow = _service.Retrieve("workflow", flowId, new ColumnSet("name", "statecode", "statuscode", "category", "type"));

                // Apply the same filtering as GetAllFlowsInEnvironment
                var category = flow.GetAttributeValue<OptionSetValue>("category")?.Value;
                var type = flow.GetAttributeValue<OptionSetValue>("type")?.Value;

                if (category == 5 && type == 1) // Only Power Automate flows with Definition type
                {
                    flows.Add(new FlowModel
                    {
                        Name = flow.GetAttributeValue<string>("name"),
                        FlowId = flow.Id,
                        StateCode = flow.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? -1,
                        StatusCode = flow.GetAttributeValue<OptionSetValue>("statuscode")?.Value ?? -1
                    });
                }
            }
            return flows;
        }


        public List<FlowModel> GetAllFlowsInEnvironment()
        {
            var query = new QueryExpression("workflow")
            {
                ColumnSet = new ColumnSet("workflowid", "name", "category", "statuscode", "statecode", "type"),
                Criteria = new FilterExpression
                {
                    Conditions =
            {
                new ConditionExpression("category", ConditionOperator.Equal, 5), // Power Automate flows
                new ConditionExpression("type", ConditionOperator.Equal, 1) // Definition type
            }
                }
            };

            var workflows = _service.RetrieveMultiple(query);

            return workflows.Entities.Select(w => new FlowModel
            {
                FlowId = w.Id,
                Name = w.GetAttributeValue<string>("name"),
                // Add other properties as needed based on your FlowModel structure
            }).ToList();
        }
    }
}
