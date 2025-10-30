using PdfSharp.Pdf;
using PdfSharp.Drawing;
using SolutionConnectionReferenceReassignment.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SolutionConnectionReferenceReassignment.Services
{
    internal class ReportService
    {
        public void GenerateReport(List<FlowActionModel> flowActions)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                Title = "Save Connection Reference Report",
                DefaultExt = "pdf",
                FileName = $"ConnectionReferenceReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    CreatePdfReport(flowActions, saveDialog.FileName);
                    MessageBox.Show($"Report saved to: {Path.GetFileName(saveDialog.FileName)}",
                        "Report Generated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error generating PDF: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void CreatePdfReport(List<FlowActionModel> flowActions, string filePath)
        {
            var document = new PdfDocument();
            document.Info.Title = "Flow Connection Reference Map Report";
            document.Info.Creator = "";
            document.Info.Author = $"";
            document.Info.Subject = "";

            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            // Define fonts
            var titleFont = new XFont("Arial", 16, XFontStyle.Bold);
            var sectionHeaderFont = new XFont("Arial", 12, XFontStyle.Bold);
            var headerFont = new XFont("Arial", 10, XFontStyle.Bold);
            var bodyFont = new XFont("Arial", 9, XFontStyle.Regular);

            double yPosition = 50;
            double margin = 50;
            double pageWidth = page.Width - (2 * margin);

            // Title
            gfx.DrawString("Flow Connection Reference Map Report", titleFont, XBrushes.Black,
                new XRect(margin, yPosition, pageWidth, 30), XStringFormats.TopLeft);
            yPosition += 40;

            // Generated date
            gfx.DrawString($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}", bodyFont, XBrushes.Black,
                new XRect(margin, yPosition, pageWidth, 20), XStringFormats.TopLeft);
            yPosition += 30;

            gfx.DrawString($"Updated Flows Table:", sectionHeaderFont, XBrushes.Black,
                new XRect(margin, yPosition, pageWidth, 20), XStringFormats.TopLeft);
            yPosition += 30;

            gfx.DrawString($"Connection Reference Map:", sectionHeaderFont, XBrushes.Black,
                new XRect(margin, yPosition, pageWidth, 20), XStringFormats.TopLeft);
            yPosition += 30;

            gfx.DrawString($"Flow Actions Table", sectionHeaderFont, XBrushes.Black,
                new XRect(margin, yPosition, pageWidth, 20), XStringFormats.TopLeft);
            yPosition += 30;

            // Check if we have data
            if (flowActions == null || flowActions.Count == 0)
            {
                gfx.DrawString("No flow actions to report.", bodyFont, XBrushes.Black,
                    new XRect(margin, yPosition, pageWidth, 20), XStringFormats.TopLeft);
                document.Save(filePath);
                document.Close();
                return;
            }

            // Table headers
            double col1Width = pageWidth * 0.25; // Flow Name
            double col2Width = pageWidth * 0.25; // Action Name  
            double col3Width = pageWidth * 0.25; // Connection Name
            double col4Width = pageWidth * 0.25; // Connection Reference

            gfx.DrawString("Flow Name", headerFont, XBrushes.Black,
                new XRect(margin, yPosition, col1Width, 20), XStringFormats.TopLeft);
            gfx.DrawString("Action Name", headerFont, XBrushes.Black,
                new XRect(margin + col1Width, yPosition, col2Width, 20), XStringFormats.TopLeft);
            gfx.DrawString("Connection", headerFont, XBrushes.Black,
                new XRect(margin + col1Width + col2Width, yPosition, col3Width, 20), XStringFormats.TopLeft);
            gfx.DrawString("Reference", headerFont, XBrushes.Black,
                new XRect(margin + col1Width + col2Width + col3Width, yPosition, col4Width, 20), XStringFormats.TopLeft);

            yPosition += 15;

            // Draw a line under headers
            gfx.DrawLine(XPens.Black, margin, yPosition, margin + pageWidth, yPosition);
            yPosition += 10;

            // Data rows
            foreach (var action in flowActions)
            {
                // Check if we need a new page
                if (yPosition > page.Height - 100)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    yPosition = 20;
                }

                // Truncate text if too long to prevent overflow
                string flowName = TruncateText(action.FlowName ?? "", 25);
                string actionName = TruncateText(action.ActionName ?? "", 25);
                string connectionName = TruncateText(action.ConnectionName ?? "", 20);
                string connectionRef = TruncateText(action.ConnectionReferenceLogicalName ?? "", 25);

                gfx.DrawString(flowName, bodyFont, XBrushes.Black,
                    new XRect(margin, yPosition, col1Width, 15), XStringFormats.TopLeft);
                gfx.DrawString(actionName, bodyFont, XBrushes.Black,
                    new XRect(margin + col1Width, yPosition, col2Width, 15), XStringFormats.TopLeft);
                gfx.DrawString(connectionName, bodyFont, XBrushes.Black,
                    new XRect(margin + col1Width + col2Width, yPosition, col3Width, 15), XStringFormats.TopLeft);
                gfx.DrawString(connectionRef, bodyFont, XBrushes.Black,
                    new XRect(margin + col1Width + col2Width + col3Width, yPosition, col4Width, 15), XStringFormats.TopLeft);

                yPosition += 20;
            }


            gfx.DrawString("Client Data Sections:", sectionHeaderFont, XBrushes.Black,
                new XRect(margin, yPosition, pageWidth, 20), XStringFormats.TopLeft);
            yPosition += 30;

            #region test comparison


            string jsonOriginal = @"
{
  ""properties"": {
    ""connectionReferences"": {
      ""shared_commondataserviceforapps"": {
        ""runtimeSource"": ""embedded"",
        ""connection"": { ""connectionReferenceLogicalName"": ""slc_sharedcommondataserviceforapps_81c1e"" },
        ""api"": { ""name"": ""shared_commondataserviceforapps"" }
      },
      ""shared_teams"": {
        ""runtimeSource"": ""embedded"",
        ""connection"": { ""connectionReferenceLogicalName"": ""slc_sharedteams_6d603"" },
        ""api"": { ""name"": ""shared_teams"" }
      },
      ""shared_sharepointonline"": {
        ""runtimeSource"": ""embedded"",
        ""connection"": { ""connectionReferenceLogicalName"": ""slc_sharedsharepointonline_f5aeb"" },
        ""api"": { ""name"": ""shared_sharepointonline"" }
      }
    },
    ""definition"": {
      ""$schema"": ""https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#"",
      ""contentVersion"": ""1.0.0.0"",
      ""parameters"": {
        ""$connections"": { ""defaultValue"": {}, ""type"": ""Object"" },
        ""$authentication"": { ""defaultValue"": {}, ""type"": ""SecureObject"" }
      },
      ""triggers"": {
        ""When_a_row_is_added,_modified_or_deleted"": {
          ""metadata"": { ""operationMetadataId"": ""526a4d06-5b00-4b9a-a976-a4cf73df4fcb"" },
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""SubscribeWebhookTrigger"",
              ""apiId"": ""/providers/Microsoft.PowerApps/apis/shared_commondataserviceforapps""
            },
            ""parameters"": {
              ""subscriptionRequest/message"": 1,
              ""subscriptionRequest/entityname"": ""account"",
              ""subscriptionRequest/scope"": 4,
              ""subscriptionRequest/filterexpression"": ""statecode eq null""
            },
            ""authentication"": ""@parameters('$authentication')""
          }
        }
      },
      ""actions"": {
        ""Add_a_new_row"": {
          ""runAfter"": {},
          ""metadata"": { ""operationMetadataId"": ""f2f21780-41d9-43bf-90ca-43ca094ef1be"" },
          ""type"": ""OpenApiConnection"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_commondataserviceforapps"",
              ""operationId"": ""CreateRecord"",
              ""apiId"": ""/providers/Microsoft.PowerApps/apis/shared_commondataserviceforapps""
            },
            ""parameters"": { ""entityName"": ""contacts"", ""item/lastname"": ""Matt Test"" },
            ""authentication"": ""@parameters('$authentication')""
          }
        },
        ""List_teams"": {
          ""runAfter"": { ""Add_a_new_row"": [""Succeeded""] },
          ""metadata"": { ""operationMetadataId"": ""d7c7ed3b-1e5d-432d-84db-444e58ceaaef"" },
          ""type"": ""OpenApiConnection"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_teams"",
              ""operationId"": ""GetAllTeams"",
              ""apiId"": ""/providers/Microsoft.PowerApps/apis/shared_teams""
            },
            ""parameters"": {},
            ""authentication"": ""@parameters('$authentication')""
          }
        },
        ""List_folder"": {
          ""runAfter"": { ""List_teams"": [""Succeeded""] },
          ""metadata"": {
            ""%252fLists"": ""/Lists"",
            ""operationMetadataId"": ""d0108004-defb-4543-a18e-aad9e2415e32""
          },
          ""type"": ""OpenApiConnection"",
          ""inputs"": {
            ""host"": {
              ""connectionName"": ""shared_sharepointonline"",
              ""operationId"": ""ListFolder"",
              ""apiId"": ""/providers/Microsoft.PowerApps/apis/shared_sharepointonline""
            },
            ""parameters"": {
              ""dataset"": ""https://websitereference.com.au"",
              ""id"": ""%252fLists""
            },
            ""authentication"": ""@parameters('$authentication')""
          }
        }
      }
    },
    ""templateName"": """"
  },
  ""schemaVersion"": ""1.0.0.0""
}";

            string jsonUpdated = @"
{
    ""FlowName"": ""ExampleFlow"",
    ""Action"": ""CreateRecord"",
    ""Connection"": ""UpdatedConnection"",
    ""Reference"": ""Ref456""
}";


            string formattedOriginal = JToken.Parse(jsonOriginal).ToString(Formatting.Indented);
            string formattedUpdated = JToken.Parse(jsonUpdated).ToString(Formatting.Indented);


            // Choose a monospace font for alignment
            var jsonFont = new XFont("Consolas", 8, XFontStyle.Regular);

            // Split page width
            double colWidth = (page.Width - 2 * margin - 20) / 2; // 20 = gap between columns
            double xLeft = margin;
            double xRight = margin + colWidth + 20;

            var originalLines = formattedOriginal.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var updatedLines = formattedUpdated.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Find max number of lines
            int maxLines = Math.Max(originalLines.Length, updatedLines.Length);

            // Line height
            double lineHeight = jsonFont.GetHeight();

            // Draw each line
            for (int i = 0; i < maxLines; i++)
            {
                double y = yPosition + i * lineHeight;

                if (i < originalLines.Length)
                    gfx.DrawString(originalLines[i], jsonFont, XBrushes.Black, new XRect(xLeft, y, colWidth, lineHeight), XStringFormats.TopLeft);

                if (i < updatedLines.Length)
                    gfx.DrawString(updatedLines[i], jsonFont, XBrushes.Black, new XRect(xRight, y, colWidth, lineHeight), XStringFormats.TopLeft);
            }

            // Move yPosition for next content
            yPosition += maxLines * lineHeight + 20;


            #endregion test comparison



            document.Save(filePath);
            document.Close();
        }

        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength - 3) + "...";
        }
    }
}