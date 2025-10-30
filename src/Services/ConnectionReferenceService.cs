using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json.Linq;
using SolutionConnectionReferenceReassignment.Models;
using SolutionConnectionReferenceReassignment.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SolutionConnectionReferenceReassignment.Services
{
    internal class ConnectionReferenceService
    {
        private readonly IOrganizationService _service;

        public ConnectionReferenceService(IOrganizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Retrieves connection references from the Dataverse table [connectionreferences]
        /// Currently, only user-owned connection references are supported due to API restrictions.
        /// </summary>
        /// <param name="filterOption">
        /// **Currently removed** Used to determine what filter is applied when querying the Dataverse connectionreference table
        /// - "My Connection References" ➡️ Returns connection references owned by the current user.
        /// - "All Connection References" ➡️ Returns all connection references.
        /// </param>
        /// <returns>A list of <see cref="ConnectionReferenceModel"/> available to the user to reassign.</returns>

        public List<ConnectionReferenceModel>GetFilteredConnectionReferences()
        {
            Guid userId = GetCurrentUserId();

            var query = new QueryExpression("connectionreference")
            {
                ColumnSet = new ColumnSet("connectionreferencedisplayname", "connectionreferencelogicalname", "ownerid", "connectorid"),
                Criteria = new FilterExpression()
            };

            query.Criteria.AddCondition("ownerid", ConditionOperator.Equal, userId); // If below is possible, remove this line and uncomment switch statement

            // TODO: Determine if it is possible to update flow via other users connection reference. Currently limited and excluded from XRMToolbox MVP

            /* Original intent was to have the ability to choose between user owned, all connection references or service principal connection references 
             * However it looks as though there is logic preventing the programatic update of connection references that aren't owned by the user (makes sense as this is the case within the UI too)
             * 
             * Error example (identifying components removed):
             * System.ServiceModel.FaultException`1: 'Flow client error returned with status code "Forbidden" and details "{"error":{"code":"ConnectionAuthorizationFailed","message":"The caller object id is '{Caller GUID}'. Connection '{Connection Reference Logical Name}' to 'shared_commondataserviceforapps' cannot be used to activate this flow, either because this is not a valid connection or because it is not a connection you have access permission for. Either replace the connection with a valid connection you can access or have the connection owner activate the flow, so the connection is shared with you in the context of this flow."}}".'
             */

            /*

            switch (filterOption)
            {
                case "My Connection References":
                    query.Criteria.AddCondition("ownerid", ConditionOperator.Equal, userId);
                    break;
                case "All Connection References":
                    query.Criteria.AddCondition("ownerid", ConditionOperator.NotNull); 
                    break;
                default:
                    query.Criteria.AddCondition("ownerid", ConditionOperator.Equal, userId);
                    break;
            }
            */

            var results = _service.RetrieveMultiple(query).Entities;

            var connectionReferences = results.Select(e => new ConnectionReferenceModel
            {
                DisplayName = e.GetAttributeValue<string>("connectionreferencedisplayname"),
                Name = e.GetAttributeValue<string>("connectionreferencelogicalname"),
                ConnectorId = ExtractConnectorKey(e.GetAttributeValue<string>("connectorid"))
            }).ToList();

            connectionReferences.Insert(0, new ConnectionReferenceModel
            {
                DisplayName = string.Empty,
                Name = string.Empty,
                ConnectorId = string.Empty
            });

            return connectionReferences;
        }

        /// <summary>
        /// Method to obtain the UserId value of the user currently using this tool.
        /// </summary>
        /// <returns>The Dataverse systemuser GUID of the user currently using the tool</returns>
        /// <exception cref="InvalidOperationException"></exception>
        private Guid GetCurrentUserId()
        {
            try
            {
                var whoAmIRequest = new WhoAmIRequest();
                var whoAmIResponse = (WhoAmIResponse)_service.Execute(whoAmIRequest);

                
                return whoAmIResponse.UserId;

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve current user details.", ex);
            }
        }

        /// <summary>
        /// Method to parse <c>connectorid</c> column of <c>connectionreference</c> table to format consistent with tooling model <see cref="ConnectionReferenceModel"/>.
        /// </summary>
        /// <param name="connectorId">Connector id Power Platform path</param>
        /// <example>/providers/Microsoft.PowerApps/apis/shared_commondataserviceforapps ➡️ shared_commondataserviceforapps</example>
        /// <returns>The last path of the connectorid string</returns>
        private string ExtractConnectorKey(string connectorId)
        {
            if (string.IsNullOrEmpty(connectorId))
                return string.Empty;

            var connectionName = connectorId.Split('/');
            return connectionName.Last();
        }
    }
}
