using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SolutionConnectionReferenceReassignment.Models;
using SolutionConnectionReferenceReassignment.Services;
using SolutionConnectionReferenceReassignment.Orchestrators;
using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using XrmToolBox.Extensibility;

namespace SolutionConnectionReferenceReassignment
{
    public partial class SolutionConnectionReferenceReassignmentControl : PluginControlBase
    {
        private Settings mySettings;
        private List<ConnectionReferenceModel> filteredConnectionReferences = new List<ConnectionReferenceModel>();

        public SolutionConnectionReferenceReassignmentControl()
        {
            InitializeComponent();
            
            // Event subscriber list
            tree_SolutionFlowExplorer.BeforeExpand += tree_SolutionFlowExplorer_BeforeExpand;
            tree_SolutionFlowExplorer.DrawMode = TreeViewDrawMode.OwnerDrawText;
            tree_SolutionFlowExplorer.DrawNode += Tree_SolutionFlowExplorer_DrawNode;
        }

        private void Tree_SolutionFlowExplorer_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            if (e.Node == null) return;

            TreeView tree = sender as TreeView;
            bool isSelected = (e.State & TreeNodeStates.Selected) != 0;

            // Determine background and text color
            Color backColor;
            Color foreColor;
            Font nodeFont = e.Node.NodeFont ?? tree.Font;

            if (isSelected)
            {
                // If the tree has focus, use normal highlight
                if (tree.Focused)
                {
                    backColor = SystemColors.Highlight;
                    foreColor = SystemColors.HighlightText;
                }
                else
                {
                    // Dim highlight when tree loses focus
                    backColor = SystemColors.Control;
                    foreColor = SystemColors.ControlText;
                }

                // Make the selected node bold regardless of focus
                nodeFont = new Font(nodeFont, FontStyle.Bold);
            }
            else
            {
                backColor = tree.BackColor;
                foreColor = tree.ForeColor;
            }

            // Draw background
            using (SolidBrush brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            // Draw text
            TextRenderer.DrawText(e.Graphics, e.Node.Text, nodeFont, e.Bounds, foreColor, TextFormatFlags.VerticalCenter);

            // If node is focused, draw focus rectangle
            if ((e.State & TreeNodeStates.Focused) != 0)
            {
                ControlPaint.DrawFocusRectangle(e.Graphics, e.Bounds);
            }

            // Dispose font if created dynamically
            if (nodeFont != e.Node.NodeFont && nodeFont != tree.Font)
            {
                nodeFont.Dispose();
            }
        }

        private void MyPluginControl_Load(object sender, EventArgs e)
        {
            // Loads or creates the settings for the plugin
            if (!SettingsManager.Instance.TryLoad(GetType(), out mySettings))
            {
                mySettings = new Settings();
                LogWarning("Settings not found => a new settings file has been created!");
            }
            else
            {
                LogInfo("Settings found and loaded");
            }



            // Disable dependent controls until connected
            ToggleMainControls(false);
            if (Service == null)
            {
                MessageBox.Show("Please connect to an environment first.", "No Connection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetupTool();
        }


        private void ToggleMainControls(bool state)
        {
            tree_SolutionFlowExplorer.Enabled = state;
            //cmb_ConnectionReferenceFilter.Enabled = state;
            dgv_ConnectionReferenceList.Enabled = state;
        }

        private void ToggleButtons(bool state, object nodeTag)
        {
            
        }       
        

        private void tsbClose_Click(object sender, EventArgs e)
        {
            CloseTool();
        }



        /// <summary>
        /// This event occurs when the plugin is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyPluginControl_OnCloseTool(object sender, EventArgs e)
        {
            // Before leaving, save the settings
            SettingsManager.Instance.Save(GetType(), mySettings);
        }

        /// <summary>
        /// This event occurs when the connection has been updated in XrmToolBox
        /// </summary>
        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);

            if (mySettings != null && detail != null)
            {
                mySettings.LastUsedOrganizationWebappUrl = detail.WebApplicationUrl;
                LogInfo("Connection has changed to: {0}", detail.WebApplicationUrl);
            }

            if (Service != null)
            {
                SetupTool();
            }

        }


        private void cmb_SolutionList_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void SetupConnectionReferenceGrid(List<ConnectionReferenceModel> data, List<string> replacementOptions)
        {
            
        }

        private void dgv_ConnectionReferenceList_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            // Only operate when the ReplacementConnectionReference column is editing
            if (dgv_ConnectionReferenceList.CurrentCell?.ColumnIndex != dgv_ConnectionReferenceList.Columns["ReplacementConnectionReference"].Index)
                return;

            if (e.Control is ComboBox combo)
            {
                var row = dgv_ConnectionReferenceList.CurrentRow;

                // avoid double-subscribe
                combo.SelectedIndexChanged -= ReplacementCombo_SelectedIndexChanged;
                combo.SelectedIndexChanged += ReplacementCombo_SelectedIndexChanged;
            }
        }
        private void ReplacementCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!(sender is ComboBox combo)) return;

            // current row being edited
            var row = dgv_ConnectionReferenceList.CurrentRow;
            if (row == null) return;

            // Try to get the selected ConnectionReferenceModel object
            ConnectionReferenceModel selectedRef = null;

            // Preferred: the SelectedItem is the object (your DataSource is a List<ConnectionReferenceModel>)
            if (combo.SelectedItem is ConnectionReferenceModel obj)
            {
                selectedRef = obj;
            }
            else
            {
                // Fallback: SelectedValue contains the ValueMember (you use "Name"), so look it up in the cached list
                var selVal = combo.SelectedValue?.ToString();
                if (!string.IsNullOrEmpty(selVal))
                {
                    selectedRef = filteredConnectionReferences
                        .FirstOrDefault(r => string.Equals(r.Name, selVal, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (selectedRef != null)
            {
                row.Cells["ReplacementLogicalName"].Value = selectedRef.Name;        // connectionreferencelogicalname
                //TODO: REPLACE THIS WITH STANDARD VS SERVICE PRINCIPAL LOGIC
            }
            else
            {
                row.Cells["ReplacementLogicalName"].Value = "";
            }
        }



        private void dgv_Flows_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void tsb_closetool_Click(object sender, EventArgs e)
        {
            CloseTool();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/MatthewTDunn/ConnectionReferenceReassignmentTool");
        }

        private void tsb_about_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Solution Connection Reference Reassignment Tool\n\n" +
                "Connection References in the Power Platform can be messy and often created automatically as a byproduct of the development process. " +
                "This tool is designed to help you map these connection references to governed, service principal or managed connection references to assist in platform governance activities. " +
                "In addition to processes reassigned via this tool, please check Copilot Studio agents & Canvas Apps for connection reference candidates for consolidation. \n\n" +
                "Once connection references have been consolidated for your organisation, auditing and cleanup activities are recommended.",
                "About",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }


        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
        
        }

        private void toolStripLabel1_Click(object sender, EventArgs e)
        {

        }

        private void SetupTool()
        {
            if (Service != null)
            {
                EstablishEnvironmentComponentTreeView();
                EstablishConnectionReferenceFilter();
                ToggleMainControls(true);
                dgv_FlowActionList.DataSource = null;
                dgv_ConnectionReferenceList.DataSource = null;
                dgv_ConnectionReferenceList.Columns.Clear(); // Because these are manually added, the configuration persists - so we need to manually remove
            } 
            else
            {
                ToggleMainControls(false);
                MessageBox.Show("Please connect to an environment first.",
                "No Connection",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
                return;
            }
        }

        private void EstablishConnectionReferenceFilter()
        {
            var comboColumn = dgv_ConnectionReferenceList.Columns["ReplacementConnectionReference"] as DataGridViewComboBoxColumn;
            if (comboColumn != null)
            {
                comboColumn.DataSource = filteredConnectionReferences;
            }
        }


        private void EstablishEnvironmentComponentTreeView()
        {
            if (Service == null)
            {
                MessageBox.Show("Please connect to an environment first.",
                                "No Connection",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                return;
            }

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading solutions...",
                Work = (worker, args) =>
                {
                    var solutionService = new SolutionService(Service);
                    var solutionList = solutionService.GetUnmanagedSolutions();
                    args.Result = solutionList;
                },
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show("Error: " + args.Error.Message);
                        return;
                    }

                    var solutions = (List<SolutionModel>)args.Result;

                    tree_SolutionFlowExplorer.BeginUpdate();
                    tree_SolutionFlowExplorer.Nodes.Clear();

                    var environmentNode = new TreeNode("Current Environment")
                    {
                        ImageKey = "treeicon_environment.png",
                        SelectedImageKey = "treeicon_environment.png"
                    };
                    foreach (var solution in solutions)
                    {
                        var solutionNode = new TreeNode(solution.FriendlyName)
                        {
                            Tag = solution,
                            ImageKey = "treeicon_solution.png",
                            SelectedImageKey = "treeicon_solution.png"
                        };

                        solutionNode.Nodes.Add(new TreeNode("Loading..."));
                        environmentNode.Nodes.Add(solutionNode);
                    }

                    tree_SolutionFlowExplorer.Nodes.Add(environmentNode);
                    environmentNode.Expand();
                    tree_SolutionFlowExplorer.EndUpdate();
                }
            });
        }

        private void tsb_RefreshSolutionList_Click(object sender, EventArgs e)
        {
            SetupTool();
        }


        //private void cmb_ConnectionReferenceFilter_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    string filterOption = cmb_ConnectionReferenceFilter.SelectedItem?.ToString() ?? "My Connection References";
        //    filteredConnectionReferences = new ConnectionReferenceService(Service).GetFilteredConnectionReferences(/*filterOption*/);

        //    // Update the DataGridView ComboBox column
        //    var comboColumn = dgv_ConnectionReferenceList.Columns["ReplacementConnectionReference"] as DataGridViewComboBoxColumn;
        //    if (comboColumn != null)
        //    {
        //        comboColumn.DataSource = filteredConnectionReferences;
        //    }
        //}


        //private void SetupConnectionReferenceFilterCombo()
        //{

        //    cmb_ConnectionReferenceFilter.DropDownStyle = ComboBoxStyle.DropDownList;

        //    cmb_ConnectionReferenceFilter.Items.Clear();
        //    cmb_ConnectionReferenceFilter.Items.AddRange(new string[]
        //    {
        //        "My Connection References",
        //        "All Connection References",
        //    });

        //    if (cmb_ConnectionReferenceFilter.Items.Count > 0)
        //        cmb_ConnectionReferenceFilter.SelectedIndex = 0;

        //}

        private void cmb_SolutionList_Click(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void tree_SolutionFlowExplorer_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var node = e.Node;
            if (node == null) return;

            if (e.Node?.Tag is FlowActionModel)
            {
                MessageBox.Show(
                    "Due to the way Flow clientData is structured, it is advised to avoid programmatically updating connection references on an action-by-action basis with this tool. Instead, please update connection references directly through the Power Automate UI.",
                    "Notice",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading Flow Actions and Associated Connection References",
                Work = (worker, args) =>
                {
                    var coordinator = new FlowUiOrchestrator(Service);
                    var (actions, uniqueReferences) = coordinator.GetDataForNode(node);

                    args.Result = (actions, uniqueReferences);
                },
                PostWorkCallBack = args =>
                {
                    if (args.Error != null) {
                        MessageBox.Show("Error: " + args.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var (actions, uniqueReferences) = ((List<FlowActionModel>, List<ConnectionReferenceModel>))args.Result;

                    dgv_FlowActionList.DataSource = null;
                    dgv_FlowActionList.DataSource = actions;

                    dgv_ConnectionReferenceList.DataSource = null;                    
                    SetConnectionReferenceControlColumnConfiguration(uniqueReferences);

                    // We don't want users using this tool to update flows on an action-by-action basis so only populate connection reference list if not selecting an action.
                    if (e.Node?.Tag is FlowActionModel)
                    {
                        dgv_ConnectionReferenceList.DataSource = null;
                        dgv_ConnectionReferenceList.Enabled = false;
                    } else
                    {
                        dgv_ConnectionReferenceList.DataSource = uniqueReferences;
                        dgv_ConnectionReferenceList.Enabled = true;
                    }
                        
                    ToggleButtons(true, e.Node?.Tag);
                    UpdateFlowActionCounts();
                    UpdateIndividualFlowCounts();


                    //MATT TODO: HANDLE MORE APPROPRIATELY.
                    filteredConnectionReferences = new ConnectionReferenceService(Service).GetFilteredConnectionReferences(/*filterOption*/);
                    var comboColumn = dgv_ConnectionReferenceList.Columns["ReplacementConnectionReference"] as DataGridViewComboBoxColumn;
                    if (comboColumn != null)
                    {
                        comboColumn.DataSource = filteredConnectionReferences;
                    }

                    //Here we need to iterate over each row entry to align it with the logicalname of the connectionreference to be replaced
                    foreach (DataGridViewRow row in dgv_ConnectionReferenceList.Rows)
                        {
                            if (row.DataBoundItem is ConnectionReferenceModel rowData)
                            {
                                string nameToMatch = rowData.Name;

                                var filtered = filteredConnectionReferences
                                    .Where(x => x.ConnectorId.Equals(nameToMatch, StringComparison.OrdinalIgnoreCase)
                                    || string.IsNullOrEmpty(x.ConnectorId)) // This ensures an empty option is always included (for no update on particular connection reference)
                                    .ToList();

                            var comboCell = new DataGridViewComboBoxCell
                                {
                                    DataSource = filtered,
                                    ValueMember = null,
                                    DisplayMember = "DisplayName",
                                    Value = null
                                };

                                row.Cells["ReplacementConnectionReference"] = comboCell;
                            }
                        }


                }
            });
        }

        private void UpdateIndividualFlowCounts()
        {
            if (dgv_ConnectionReferenceList.DataSource is List<ConnectionReferenceModel> connectionReferences)
            {
                // Dictionary to hold unique FlowIds per ConnectionReferenceLogicalName
                var flowIdSets = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

                foreach (DataGridViewRow flowRow in dgv_FlowActionList.Rows)
                {
                    if (flowRow.IsNewRow) continue;

                    var logicalName = flowRow.Cells["ConnectionReferenceLogicalName"]?.Value?.ToString();
                    var flowId = flowRow.Cells["FlowId"]?.Value?.ToString();

                    if (string.IsNullOrWhiteSpace(logicalName) || string.IsNullOrWhiteSpace(flowId))
                        continue;

                    if (!flowIdSets.ContainsKey(logicalName))
                        flowIdSets[logicalName] = new HashSet<string>();

                    flowIdSets[logicalName].Add(flowId); // ensures uniqueness
                }

                // Map counts back to the ConnectionReferenceModel
                foreach (var connRef in connectionReferences)
                {
                    if (flowIdSets.TryGetValue(connRef.LogicalName, out var flowIds))
                    {
                        connRef.IndividualFlowCount = flowIds.Count;
                    }
                    else
                    {
                        connRef.IndividualFlowCount = 0;
                    }
                }

                dgv_ConnectionReferenceList.Refresh();
            }
        }


        private void UpdateFlowActionCounts()
        {
            if (dgv_ConnectionReferenceList.DataSource is List<ConnectionReferenceModel> connectionReferences)
            {
                // Precompute counts using a dictionary for performance
                var flowCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (DataGridViewRow flowRow in dgv_FlowActionList.Rows)
                {
                    if (flowRow.IsNewRow) continue;

                    var logicalName = flowRow.Cells["ConnectionReferenceLogicalName"]?.Value?.ToString();
                    if (string.IsNullOrWhiteSpace(logicalName)) continue;

                    if (!flowCounts.ContainsKey(logicalName))
                        flowCounts[logicalName] = 0;

                    flowCounts[logicalName]++;
                }

                // Update each ConnectionReferenceModel
                foreach (var connRef in connectionReferences)
                {
                    flowCounts.TryGetValue(connRef.LogicalName, out int count);
                    connRef.FlowActionCount = count;
                }

                dgv_ConnectionReferenceList.Refresh();
            }
        }

        private void SetConnectionReferenceControlColumnConfiguration(List<ConnectionReferenceModel> connectionReferences)
        {
            dgv_ConnectionReferenceList.AutoGenerateColumns = false;
            dgv_ConnectionReferenceList.Columns.Clear();

            dgv_ConnectionReferenceList.Columns.Add(new DataGridViewTextBoxColumn()
            {
                ReadOnly = true,
                DataPropertyName = "Name",
                HeaderText = "Connection Type",
                Name = "Name",

                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 20
            });
            dgv_ConnectionReferenceList.Columns.Add(new DataGridViewTextBoxColumn()
            {
                ReadOnly = true,
                DataPropertyName = "LogicalName",
                HeaderText = "Logical Name",
                Name = "LogicalName",

                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 20
            });

            dgv_ConnectionReferenceList.Columns.Add(new DataGridViewTextBoxColumn()
            {
                ReadOnly = true,
                DataPropertyName = "IndividualFlowCount",
                HeaderText = "Impacted Flow Count",
                Name = "IndividualFlowCount",

                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 10
            });

            dgv_ConnectionReferenceList.Columns.Add(new DataGridViewTextBoxColumn()
            {
                ReadOnly = true,
                DataPropertyName = "FlowActionCount",
                HeaderText = "Flow Action Count",
                Name = "FlowActionCount",

                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 10
            });

            var filteredConnectionReferences = new ConnectionReferenceService(Service)
                .GetFilteredConnectionReferences();

            var comboColumn = new DataGridViewComboBoxColumn()
            {
                ReadOnly = false,
                DataPropertyName = "ReplacementConnectionReference",
                Name = "ReplacementConnectionReference",
                HeaderText = "Replacement Connection Reference",
                ValueMember = "Name",
                DisplayMember = "DisplayName",
                DataSource = filteredConnectionReferences, 
                DefaultCellStyle = new DataGridViewCellStyle()
                {
                    BackColor = Color.White
                },

                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 20
            };



            dgv_ConnectionReferenceList.Columns.Add(comboColumn);

            dgv_ConnectionReferenceList.Columns.Add(new DataGridViewTextBoxColumn()
            {
                ReadOnly = true,
                DataPropertyName = "ReplacementLogicalName",
                Name = "ReplacementLogicalName",
                HeaderText = "Replacement Logical Name",

                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 20
            });
        }


        private void tree_SolutionFlowExplorer_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            var node = e.Node;
            if (node == null) return;

            if (node.Tag is SolutionModel solution &&
                node.Nodes.Count == 1 && node.Nodes[0].Text == "Loading...")
            {
                node.Nodes.Clear();
                var flowService = new FlowService(Service);
                var flows = flowService.GetFlowsInSolution(solution.SolutionId);
                foreach (var flow in flows)
                {
                    var flowNode = new TreeNode(flow.Name)
                    {
                        Tag = flow,
                        ImageKey = "treeicon_powerautomate.png",
                        SelectedImageKey = "treeicon_powerautomate.png"
                    };
                    flowNode.Nodes.Add(new TreeNode("Loading..."));
                    node.Nodes.Add(flowNode);
                }
            }
            else if (node.Tag is FlowModel flowMetadata &&
                     node.Nodes.Count == 1 && node.Nodes[0].Text == "Loading...")
            {
                node.Nodes.Clear();
                var actionService = new FlowActionService(Service);
                // Use the backward compatibility method to avoid handling error counts here
                var actions = actionService.GetFlowActionsOnly(flowMetadata.FlowId);

                foreach (var action in actions)
                {
                    var actionNode = new TreeNode(action.ActionName)
                    {
                        Tag = action,
                        ImageKey = "treeicon_action.png",
                        SelectedImageKey = "treeicon_action.png"
                    };
                    node.Nodes.Add(actionNode);
                }
            }
        }

        private void dgv_ConnectionReferenceList_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var grid = sender as DataGridView;
            var row = grid.Rows[e.RowIndex];

            if (grid.Columns[e.ColumnIndex].Name == "ReplacementConnectionReference")
            {
                var comboCell = row.Cells["ReplacementConnectionReference"] as DataGridViewComboBoxCell;

                if (comboCell?.Value != null)
                {
                    string selectedValue = comboCell.Value.ToString();

                    // Search by DisplayName since that's what the combo box is actually returning
                    var selectedRef = filteredConnectionReferences.FirstOrDefault(r => r.DisplayName == selectedValue);

                    if (selectedRef != null)
                    {
                        row.Cells["ReplacementLogicalName"].Value = selectedRef.Name; // or selectedRef.LogicalName if you want the logical name
                    }
                    else
                    {
                        row.Cells["ReplacementLogicalName"].Value = "";
                    }
                }
                else
                {
                    row.Cells["ReplacementLogicalName"].Value = "";
                }
            }
        }




        // to ensure that the logical name of the replacement connection reference is immediately populated when the user clicks
        private void dgv_ConnectionReferenceList_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgv_ConnectionReferenceList.IsCurrentCellDirty)
            {
                dgv_ConnectionReferenceList.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void dgv_ConnectionReferenceList_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void btn_executeflowclientdataupdate_Click(object sender, EventArgs e)
        {
            // Step 1: Validate UI inputs
            if (!ValidateGridInputs())
                return;

            // Step 2: Build update parameters from UI
            var updateParams = BuildUpdateParameters();

            if (updateParams == null || updateParams.AffectedFlows.Count == 0)
            {
                MessageBox.Show("No flows to update based on selected criteria.",
                    "No Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Step 3: Show confirmation dialog
            if (!ShowUpdateConfirmation(updateParams))
                return;

            // Step 4: Execute the update asynchronously
            ExecuteFlowUpdates(updateParams);
        }

        /// <summary>
        /// Validates that all required fields are populated in the grid
        /// </summary>
        /// <summary>
        /// Validates that all required fields are populated in the grid
        /// </summary>
        private bool ValidateGridInputs()
        {
            // First, check for msdyn_ replacement warning
            foreach (DataGridViewRow row in dgv_ConnectionReferenceList.Rows)
            {
                if (row.IsNewRow) continue;

                var logicalName = row.Cells["LogicalName"]?.Value?.ToString();
                var replacementRef = row.Cells["ReplacementConnectionReference"]?.Value?.ToString();

                // Check if LogicalName starts with msdyn_ AND has a replacement value
                if (!string.IsNullOrWhiteSpace(logicalName) &&
                    logicalName.StartsWith("msdyn_", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(replacementRef))
                {
                    var result = MessageBox.Show(
                        "WARNING: You are attempting to replace one or more connection references that start with 'msdyn_'. " +
                        "These are typically system or managed connections.\n\n" +
                        "Are you sure you want to proceed?",
                        "System Connection Reference Warning",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.No)
                        return false;

                    // Only show warning once, then break
                    break;
                }
            }

            foreach (DataGridViewRow row in dgv_ConnectionReferenceList.Rows)
            {
                if (!row.IsNewRow)
                {
                    var replacementLogical = row.Cells["ReplacementLogicalName"].Value?.ToString();

                    // If ReplacementLogicalName has a value, validate that ReplacementConnectionReference is also populated
                    if (!string.IsNullOrWhiteSpace(replacementLogical))
                    {
                        var replacementRef = row.Cells["ReplacementConnectionReference"].Value?.ToString();

                        if (string.IsNullOrWhiteSpace(replacementRef))
                        {
                            MessageBox.Show(
                                "One or more connection references with replacement logical names are missing required replacement connection reference values.\n\n" +
                                "Please ensure that both Replacement Connection Reference and Replacement Logical Name are populated.\n\n" +
                                "Occasionally, the comboBox in the 'Replacement Connection Reference' column may not register correctly and may need to be reselected if this error persists.",
                                "Validation Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Builds the update parameters from the UI grids
        /// </summary>
        private FlowUpdateParameters BuildUpdateParameters()
        {
            var parameters = new FlowUpdateParameters();

            // Build connection mapping from ConnectionReferenceList grid
            BuildConnectionMapping(parameters);

            if (parameters.ConnectionMap.Count == 0)
                return null;

            // Gather affected flows and operation IDs from FlowActionList grid
            GatherAffectedFlowsAndOperations(parameters);

            return parameters;
        }

        /// <summary>
        /// Builds the connection mapping from the UI grid
        /// </summary>
        /// <summary>
        /// Builds the connection mapping from the UI grid
        /// </summary>
        private void BuildConnectionMapping(FlowUpdateParameters parameters)
        {
            foreach (DataGridViewRow row in dgv_ConnectionReferenceList.Rows)
            {
                if (row.IsNewRow) continue;

                var existingLogical = row.Cells["LogicalName"].Value?.ToString()?.Trim();
                var replacementLogical = row.Cells["ReplacementLogicalName"].Value?.ToString()?.Trim();

                // Only process rows that have a replacement logical name (non-empty means user wants to update)
                if (!string.IsNullOrWhiteSpace(existingLogical) && !string.IsNullOrWhiteSpace(replacementLogical))
                {
                    if (!parameters.ConnectionMap.ContainsKey(existingLogical))
                        parameters.ConnectionMap[existingLogical] = replacementLogical;
                }
            }
        }

        /// <summary>
        /// Gathers affected flows and operation IDs from the action grid
        /// </summary>
        private void GatherAffectedFlowsAndOperations(FlowUpdateParameters parameters)
        {
            foreach (DataGridViewRow row in dgv_FlowActionList.Rows)
            {
                if (row.IsNewRow) continue;

                var flowIdObj = row.Cells["FlowId"]?.Value;
                if (flowIdObj == null) continue;

                string flowId = flowIdObj.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(flowId)) continue;

                // Get operation metadata ID
                var operationId = row.Cells["OperationMetadataId"]?.Value?.ToString();
                if (!string.IsNullOrWhiteSpace(operationId))
                    parameters.OperationIdsForWork.Add(operationId);

                // Check if this flow is affected based on ConnectionReferenceLogicalName
                string actionLogical = row.Cells["ConnectionReferenceLogicalName"]?.Value?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(actionLogical)) continue;

                if (parameters.ConnectionMap.ContainsKey(actionLogical))
                {
                    parameters.AffectedFlows.Add(flowId);
                }
            }
        }

        /// <summary>
        /// Shows confirmation dialog with update statistics
        /// </summary>
        private bool ShowUpdateConfirmation(FlowUpdateParameters parameters)
        {
            int affectedActionCount = CountAffectedActions(parameters);

            string message = $"You are looking to update {parameters.ConnectionMap.Count} unique connection reference(s) " +
                             $"across {parameters.AffectedFlows.Count} unique flow(s) " +
                             $"(total potential impacted actions: {affectedActionCount}) - are you sure you wish to proceed?";

            return MessageBox.Show(message, "Confirm Update", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        /// <summary>
        /// Counts the number of affected actions for the confirmation message
        /// </summary>
        private int CountAffectedActions(FlowUpdateParameters parameters)
        {
            int count = 0;
            foreach (DataGridViewRow row in dgv_FlowActionList.Rows)
            {
                if (row.IsNewRow) continue;

                string actionLogical = row.Cells["ConnectionReferenceLogicalName"]?.Value?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(actionLogical)) continue;

                if (parameters.ConnectionMap.ContainsKey(actionLogical))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Executes the flow updates asynchronously using the service
        /// </summary>
        private void ExecuteFlowUpdates(FlowUpdateParameters parameters)
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Updating flow connection references...",
                Work = (worker, args) =>
                {
                    // Create the service and perform updates
                    var updateService = new FlowConnectionReferenceUpdateService(Service);
                    var result = updateService.UpdateFlowConnectionReferences(parameters);
                    args.Result = result;
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show($"Error: {args.Error.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var result = args.Result as FlowUpdateResult;
                    if (result != null)
                    {
                        ShowUpdateResults(result);
                    }
                    else
                    {
                        MessageBox.Show("No flows updated.", "Update Complete",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            });
        }

        /// <summary>
        /// Displays the update results to the user
        /// </summary>
        private void ShowUpdateResults(FlowUpdateResult result)
        {
            if (result.PerFlowMessages != null && result.PerFlowMessages.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Update Summary:");
                sb.AppendLine($"- Processed Flows: {result.ProcessedFlows}");
                sb.AppendLine($"- Successful Updates: {result.SuccessfulFlows}");
                sb.AppendLine($"- Total Connection References Changed: {result.TotalConnectionRefsChanged}");

                // MATT TODO: Might want to include additional detail in the future. Functionality not 100% working atm
                /*
                sb.AppendLine($"- Total Actions Updated: {result.TotalActionsUpdated}");
                sb.AppendLine();
                sb.AppendLine("Detailed Results:");

                foreach (var message in result.PerFlowMessages)
                {
                    sb.AppendLine(message);
                }

                */


                // MATT TODO: ADD REPORT FUNCTIONALITY POTENTIALLY

                /*

                var dialogResult = MessageBox.Show(
                    sb.ToString() + "\n\nWould you like to generate a PDF report of the changes?",
                    "Update Complete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (dialogResult == DialogResult.Yes)
                {
                    try
                    {
                        var reportService = new ReportService();
                        var flowActions = dgv_FlowActionList.DataSource as List<FlowActionModel>;

                        if (flowActions != null && flowActions.Any())
                        {
                            reportService.GenerateReport(flowActions);
                        }
                        else
                        {
                            MessageBox.Show("No flow action data available for report generation.",
                                "No Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error generating report: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                */

                // -------

                MessageBox.Show(sb.ToString(), "Update Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);  
            }
            else
            {
                MessageBox.Show("No flows updated.", "Update Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            tree_SolutionFlowExplorer_AfterSelect(null, new TreeViewEventArgs(tree_SolutionFlowExplorer.SelectedNode));
        }

        /*
        private void GenerateUpdateReport()
        {
            try
            {
                var reportService = new UpdateReportService();
                var flowActions = dgv_FlowActionList.DataSource as List<FlowActionModel>;

                if (flowActions != null && flowActions.Any())
                {
                    // Call the report generation method
                    reportService.GenerateReport(flowActions);
                }
                else
                {
                    MessageBox.Show("No flow action data available for report generation.",
                        "No Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        */

        
        

        private void tsb_Github_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/MatthewTDunn/XRM.FlowConnectionReferenceReassigner.XRMToolbox");
        }

        private void toolStripButton1_Click_1(object sender, EventArgs e)
        {

        }
    }
}