using Newtonsoft.Json.Linq;
using SolutionConnectionReferenceReassignment.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SolutionConnectionReferenceReassignment.Utilities
{
    internal static class FlowJSONParser
    {
        /// <summary>
        /// Parses the <c>actions</c> section of a flow JSON <c>clientData</c> into a collection of <see cref="FlowActionModel"/> instances.
        /// </summary>
        /// <param name="clientData"> The <c>clientData</c> from the Dataverse <c>workflow</c> table.</param>
        /// <returns>
        /// A list of <see cref="FlowActionModel"/> objects, each representing a flow action 
        /// (with details such as action name, type, connection reference, and parameters).  
        /// </returns>
        /// <remarks>
        /// If an action does not conform to the expected JSON structure, it is skipped and a warning message is displayed.
        /// </remarks>
        public static (List<FlowActionModel> Actions, int ErrorCount) ParseFlowActions(JObject clientData)
        {
            var flowActionList = new List<FlowActionModel>();
            int errorCount = 0;

            var actions = clientData?["properties"]?["definition"]?["actions"] as JObject;
            if (actions == null) return (flowActionList, errorCount);

                foreach (var prop in actions.Properties())
                {
                    var actionObj = prop.Value as JObject;
                try
                {
                    flowActionList.Add(new FlowActionModel
                    {
                        ActionName = prop.Name,
                        ConnectionName = actionObj?["inputs"]?["host"]?["connectionName"]?.ToString() ?? "(none)",
                        OperationMetadataId = actionObj?["metadata"]?["operationMetadataId"]?.ToString() ?? "(none)",
                        // Wwe just want to grab specific path segment for resource instance
                        ApiIdExtract = (actionObj?["inputs"]?["host"]?["apiId"]?.ToString() ?? string.Empty)
                            .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                            .LastOrDefault() ?? "(none)",
                        // Use the connectionName associated with the action to grab the relevant connection reference logical name
                        ConnectionReferenceLogicalName = clientData?["properties"]?["connectionReferences"]?[actionObj?["inputs"]?["host"]?["connectionName"]?.ToString()]?["connection"]?["connectionReferenceLogicalName"]?.ToString() ?? "(none)",
                        OperationId = actionObj?["inputs"]?["host"]?["operationId"]?.ToString() ?? "(none)",
                        Parameters = actionObj?["inputs"]?["parameters"]?.ToString() ?? "(none)"
                    });
                }
                catch (Exception ex)
                {
                    errorCount++;
                    
                    /*
                    MessageBox.Show($"The action of {prop.Name} does not comply with the tooling format requirements. Action skipped \n\n" +
                        $"Error: {ex.Message}\n\n" +
                        $"StackTrace: {ex.StackTrace}",
                        "Flow Parsing Warning",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    */

                }

            }

            return (flowActionList, errorCount);
        }


        /// <summary>
        /// Parses the <c>connectionReferences</c> section of a clientData into <see cref="ConnectionReferenceModel"/> instances.
        /// </summary>
        /// <param name="clientData">JSON extracted from <c>clientData</c> attribute of <c>workflow</c> record in Dataverse.</param>
        /// <returns>
        /// A list of <see cref="ConnectionReferenceModel"/> objects, each representing a connection reference
        /// (with details such as logical name, API name, and runtime source).  
        /// Returns an empty list if no connection references are found.
        /// </returns>
        public static List<ConnectionReferenceModel> ParseFlowConnectionReferences(JObject clientData)
        {
            var flowConnectionReferenceList = new List<ConnectionReferenceModel>();
            var connectionReferences = clientData?["properties"]?["connectionReferences"] as JObject;
            if (connectionReferences == null) return flowConnectionReferenceList;

            foreach (var prop in connectionReferences.Properties())
            {
                var connectionReferenceItem = prop.Value as JObject;
                flowConnectionReferenceList.Add(new ConnectionReferenceModel
                {
                    Name = connectionReferenceItem?["api"]?["name"]?.ToString() ?? prop.Name,
                    LogicalName = connectionReferenceItem?["connection"]?["connectionReferenceLogicalName"]?.ToString() ?? "(none)",
                    RuntimeSource = connectionReferenceItem?["runtimeSource"]?.ToString() ?? "(none)"
                });
            }
            return flowConnectionReferenceList;
        }

        // <summary>
        /// Shows a summary warning if there were any parsing errors during flow processing.
        /// </summary>
        /// <param name="totalErrorCount">Total number of actions that failed to parse</param>
        /// <param name="totalFlowsProcessed">Total number of flows that were processed</param>
        public static void ShowErrorSummaryIfNeeded(int totalErrorCount, int totalFlowsProcessed)
        {
            if (totalErrorCount > 0)
            {
                MessageBox.Show(
                    $"Processing complete, however some Power Automate flow actions were skipped because they either don't use connection references or their format does not meet the expected:\n\n" +
                    $"• {totalErrorCount} flow action(s) skipped due to format incompatibility\n" +
                    $"• {totalFlowsProcessed} flow(s) were loaded\n\n" +
                    $"The skipped actions do not comply with the expected JSON structure for this tool (if statements, apply each, switch, etc)" +
                    $"All other actions have been loaded successfully.",
                    "Flow Parsing Summary",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
        }
    }
}
