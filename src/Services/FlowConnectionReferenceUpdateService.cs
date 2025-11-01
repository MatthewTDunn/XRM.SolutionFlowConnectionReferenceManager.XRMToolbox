using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SolutionConnectionReferenceReassignment.Services
{
    public class FlowConnectionReferenceUpdateService
    {
        private readonly IOrganizationService _service;

        public FlowConnectionReferenceUpdateService(IOrganizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Updates flow connection references based on the provided parameters
        /// </summary>
        public FlowUpdateResult UpdateFlowConnectionReferences(FlowUpdateParameters parameters)
        {
            var result = new FlowUpdateResult();
            var perFlowMessages = new List<string>();

            foreach (string flowId in parameters.AffectedFlows)
            {
                var flowResult = UpdateSingleFlow(flowId, parameters.ConnectionMap, parameters.OperationIdsForWork);
                perFlowMessages.Add(flowResult.Message);
                result.ProcessedFlows++;

                if (flowResult.Success)
                {
                    result.SuccessfulFlows++;
                    result.TotalConnectionRefsChanged += flowResult.ConnectionRefsChanged;
                    result.TotalActionsUpdated += flowResult.ActionsHostUpdated;
                }
            }

            result.PerFlowMessages = perFlowMessages;
            return result;
        }

        /// <summary>
        /// Updates a single flow's connection references
        /// </summary>
        private SingleFlowUpdateResult UpdateSingleFlow(
            string flowId,
            Dictionary<string, string> connectionMap,
            HashSet<string> operationIdsForWork)
        {
            var result = new SingleFlowUpdateResult { FlowId = flowId };

            try
            {
                // Retrieve flow entity
                var flowEntity = _service.Retrieve("workflow", new Guid(flowId), new ColumnSet("clientdata"));
                string clientDataJson = flowEntity.GetAttributeValue<string>("clientdata");

                if (string.IsNullOrWhiteSpace(clientDataJson))
                {
                    result.Message = $"{flowId}: skipped (no clientdata)";
                    return result;
                }

                JObject clientData = JObject.Parse(clientDataJson);

                // Determine targeted host keys if operation IDs are provided
                HashSet<string> targetedHostKeys = DetermineTargetedHostKeys(clientData, operationIdsForWork);

                // Update connection references
                var updatedPropNames = UpdateConnectionReferences(
                    clientData,
                    connectionMap,
                    targetedHostKeys,
                    out int connRefsChanged);
                result.ConnectionRefsChanged = connRefsChanged;

                if (connRefsChanged == 0 && (targetedHostKeys == null || targetedHostKeys.Count == 0))
                {
                    result.Message = $"{flowId}: no changes needed";
                    return result;
                }

                // Update triggers
                result.TriggersAuthRemoved = UpdateTriggers(clientData, updatedPropNames);

                // Update actions
                var actionUpdateResult = UpdateActions(
                    clientData,
                    updatedPropNames,
                    operationIdsForWork);
                result.ActionsAuthRemoved = actionUpdateResult.AuthRemoved;
                result.ActionsHostUpdated = actionUpdateResult.HostUpdated;

                // Save updated flow
                flowEntity["clientdata"] = clientData.ToString(Formatting.None);
                _service.Update(flowEntity);

                result.Success = true;
                result.Message = $"{flowId}: connRefsChanged={connRefsChanged}, actionsHostUpdated={result.ActionsHostUpdated}";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"{flowId}: ERROR - {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Determines which host keys are targeted based on operation metadata IDs
        /// </summary>
        private HashSet<string> DetermineTargetedHostKeys(JObject clientData, HashSet<string> operationIdsForWork)
        {
            if (operationIdsForWork == null || operationIdsForWork.Count == 0)
                return null;

            var targetedHostKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var actionsJ = clientData["properties"]?["definition"]?["actions"] as JObject;

            if (actionsJ != null)
            {
                foreach (var actionProp in actionsJ.Properties())
                {
                    var metadataId = actionProp.Value?["metadata"]?["operationMetadataId"]?.ToString();
                    if (string.IsNullOrWhiteSpace(metadataId)) continue;
                    if (!operationIdsForWork.Contains(metadataId)) continue;

                    var hostConnName = actionProp.Value?["inputs"]?["host"]?["connectionName"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(hostConnName))
                        targetedHostKeys.Add(hostConnName);
                }
            }

            return targetedHostKeys;
        }

        /// <summary>
        /// Updates connection references in the client data
        /// </summary>
        private HashSet<string> UpdateConnectionReferences(
            JObject clientData,
            Dictionary<string, string> connectionMap,
            HashSet<string> targetedHostKeys,
            out int connRefsChanged)
        {
            connRefsChanged = 0;
            var updatedPropNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var connectionRefs = clientData["properties"]?["connectionReferences"] as JObject;

            if (connectionRefs != null)
            {
                foreach (var prop in connectionRefs.Properties().ToList())
                {
                    // Skip if not in targeted host keys (when applicable)
                    if (targetedHostKeys != null && targetedHostKeys.Count > 0 && !targetedHostKeys.Contains(prop.Name))
                        continue;

                    var oldLogical = prop.Value?["connection"]?["connectionReferenceLogicalName"]?.ToString();
                    if (string.IsNullOrWhiteSpace(oldLogical)) continue;

                    if (connectionMap.TryGetValue(oldLogical, out var newLogical))
                    {
                        if (!string.Equals(oldLogical, newLogical, StringComparison.OrdinalIgnoreCase))
                        {
                            var connObj = prop.Value["connection"] as JObject;
                            if (connObj != null)
                            {
                                connObj["connectionReferenceLogicalName"] = newLogical;
                                updatedPropNames.Add(prop.Name);
                                connRefsChanged++;
                            }
                        }
                    }
                }
            }

            return updatedPropNames;
        }

        /// <summary>
        /// Updates triggers by removing authentication for related triggers
        /// </summary>
        private int UpdateTriggers(JObject clientData, HashSet<string> updatedPropNames)
        {
            int triggersAuthRemoved = 0;
            var triggers = clientData["properties"]?["definition"]?["triggers"] as JObject;
            var connectionRefs = clientData["properties"]?["connectionReferences"] as JObject;

            if (triggers != null)
            {
                foreach (var triggerProp in triggers.Properties())
                {
                    var triggerInputs = triggerProp.Value?["inputs"] as JObject;
                    if (triggerInputs == null) continue;

                    var host = triggerInputs["host"] as JObject;
                    if (host == null || host["connectionName"] == null) continue;

                    string hostConnName = host["connectionName"].ToString();
                    bool triggerIsRelated = false;

                    // Check if trigger is related to updated connection refs
                    if (updatedPropNames.Contains(hostConnName))
                    {
                        triggerIsRelated = true;
                    }
                    else if (connectionRefs != null)
                    {
                        var matching = connectionRefs.Properties()
                            .FirstOrDefault(p =>
                                string.Equals(p.Value?["connection"]?["connectionReferenceLogicalName"]?.ToString(),
                                    hostConnName, StringComparison.OrdinalIgnoreCase)
                                && updatedPropNames.Contains(p.Name));
                        if (matching != null) triggerIsRelated = true;
                    }

                    if (triggerIsRelated)
                    {
                        var authProp = triggerInputs.Property("authentication");
                        if (authProp != null)
                        {
                            authProp.Remove();
                            triggersAuthRemoved++;
                        }
                    }
                }
            }

            return triggersAuthRemoved;
        }

        /// <summary>
        /// Updates actions by removing authentication and setting connection reference names
        /// </summary>
        private ActionUpdateResult UpdateActions(
            JObject clientData,
            HashSet<string> updatedPropNames,
            HashSet<string> operationIdsForWork)
        {
            var result = new ActionUpdateResult();
            var actions = clientData["properties"]?["definition"]?["actions"] as JObject;
            var connectionRefs = clientData["properties"]?["connectionReferences"] as JObject;

            if (actions != null)
            {
                foreach (var actionProp in actions.Properties())
                {
                    var actionObj = actionProp.Value as JObject;
                    if (actionObj == null) continue;

                    // Check if action is targeted
                    var metadataId = actionObj?["metadata"]?["operationMetadataId"]?.ToString();
                    bool isTargetedAction = (operationIdsForWork != null && operationIdsForWork.Count > 0)
                        ? (!string.IsNullOrWhiteSpace(metadataId) && operationIdsForWork.Contains(metadataId))
                        : true;

                    if (!isTargetedAction) continue;

                    var inputs = actionObj["inputs"] as JObject;
                    if (inputs == null) continue;

                    var host = inputs["host"] as JObject;
                    if (host == null) continue;

                    string hostConnName = host["connectionName"]?.ToString();
                    if (string.IsNullOrEmpty(hostConnName)) continue;

                    bool actionIsRelated = false;
                    string resolvedHostKey = hostConnName;

                    if (updatedPropNames.Contains(hostConnName))
                    {
                        actionIsRelated = true;
                    }
                    else if (connectionRefs != null)
                    {
                        var matching = connectionRefs.Properties()
                            .FirstOrDefault(p =>
                                string.Equals(p.Value?["connection"]?["connectionReferenceLogicalName"]?.ToString(),
                                    hostConnName, StringComparison.OrdinalIgnoreCase)
                                && updatedPropNames.Contains(p.Name));
                        if (matching != null)
                        {
                            actionIsRelated = true;
                            resolvedHostKey = matching.Name;
                        }
                    }

                    if (actionIsRelated)
                    {
                        // Remove authentication
                        var authProp = inputs.Property("authentication");
                        if (authProp != null)
                        {
                            authProp.Remove();
                            result.AuthRemoved++;
                        }

                        // Update connection reference name
                        var existingConnRefNameProp = host.Property("connectionReferenceName");
                        if (existingConnRefNameProp == null || existingConnRefNameProp.Value.ToString() != resolvedHostKey)
                        {
                            host["connectionReferenceName"] = resolvedHostKey;
                            result.HostUpdated++;
                        }
                    }
                }
            }

            return result;
        }

        private class ActionUpdateResult
        {
            public int AuthRemoved { get; set; }
            public int HostUpdated { get; set; }
        }
    }

    #region Data Transfer Objects

    public class FlowUpdateParameters
    {
        public Dictionary<string, string> ConnectionMap { get; set; }
        public HashSet<string> AffectedFlows { get; set; }
        public HashSet<string> OperationIdsForWork { get; set; }

        public FlowUpdateParameters()
        {
            ConnectionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            AffectedFlows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            OperationIdsForWork = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public class FlowUpdateResult
    {
        public int ProcessedFlows { get; set; }
        public int SuccessfulFlows { get; set; }
        public int TotalConnectionRefsChanged { get; set; }
        public int TotalActionsUpdated { get; set; }
        public List<string> PerFlowMessages { get; set; }

        public FlowUpdateResult()
        {
            PerFlowMessages = new List<string>();
        }
    }

    public class SingleFlowUpdateResult
    {
        public string FlowId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public int ConnectionRefsChanged { get; set; }
        public int TriggersAuthRemoved { get; set; }
        public int ActionsAuthRemoved { get; set; }
        public int ActionsHostUpdated { get; set; }

    }

    #endregion
}