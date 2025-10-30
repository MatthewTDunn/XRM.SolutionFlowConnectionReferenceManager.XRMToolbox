using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SolutionConnectionReferenceReassignment.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;

namespace SolutionConnectionReferenceReassignment.Services
{
    internal class SolutionService
    {
        private readonly IOrganizationService _service;

        public SolutionService(IOrganizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Retreives all unmanaged solutions.
        /// </summary>
        /// <returns>
        /// A list of <see cref="SolutionModel"/> objects representing unmanaged solutions, 
        /// including their unique identifier, friendly name, unique name, and version.
        /// </returns>
        /// <remarks>
        /// Only solutions where <c>ismanaged = false</c> are returned. 
        /// Results are ordered by the <c>friendlyname</c> attribute in ascending order.
        /// </remarks>
        public List<SolutionModel> GetUnmanagedSolutions()
        {
            var query = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("solutionid", "friendlyname", "version", "uniquename"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("ismanaged", ConditionOperator.Equal, false)
                    }
                },
                Orders =
                {
                    new OrderExpression("friendlyname", OrderType.Ascending)
                }
            };

            var results = _service.RetrieveMultiple(query);
            return results.Entities.Select(e => new SolutionModel
            {
                SolutionId = e.Id,
                FriendlyName = e.GetAttributeValue<string>("friendlyname"),
                UniqueName = e.GetAttributeValue<string>("uniquename"),
                Version = e.GetAttributeValue<string>("version")
            }).ToList();
        }
    }
}
