using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace SolutionConnectionReferenceReassignment
{
    // Do not forget to update version number and author (company attribute) in AssemblyInfo.cs class
    // To generate Base64 string for Images below, you can use https://www.base64-image.de/
    [Export(typeof(IXrmToolBoxPlugin)),
        ExportMetadata("Name", "Flow Connection Reference Mapper"),
        ExportMetadata("Description", "Audit, govern and remap Power Automate Flow connection references at scale."),
        // Please specify the base64 content of a 32x32 pixels image
        ExportMetadata("SmallImageBase64", "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"),
        // Please specify the base64 content of a 80x80 pixels image
        ExportMetadata("BigImageBase64", "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"),
        ExportMetadata("BackgroundColor", "Lavender"),
        ExportMetadata("PrimaryFontColor", "Black"),
        ExportMetadata("SecondaryFontColor", "Blue")]
    public class SolutionConnectionReferenceReassignment : PluginBase
    {
        
        public override IXrmToolBoxPluginControl GetControl()
        {
            return new SolutionConnectionReferenceReassignmentControl();
        }

        /// <summary>
        /// Constructor 
        /// </summary>
        public SolutionConnectionReferenceReassignment()
        {
            // If you have external assemblies that you need to load, uncomment the following to 
            // hook into the event that will fire when an Assembly fails to resolve
            // AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyResolveEventHandler);
        }

        /// <summary>
        /// Event fired by CLR when an assembly reference fails to load
        /// Assumes that related assemblies will be loaded from a subfolder named the same as the Plugin
        /// For example, a folder named Sample.XrmToolBox.MyPlugin 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Assembly AssemblyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            Assembly loadAssembly = null;
            Assembly currAssembly = Assembly.GetExecutingAssembly();

            // base name of the assembly that failed to resolve
            var argName = args.Name.Substring(0, args.Name.IndexOf(","));

            // check to see if the failing assembly is one that we reference.
            List<AssemblyName> refAssemblies = currAssembly.GetReferencedAssemblies().ToList();
            var refAssembly = refAssemblies.Where(a => a.Name == argName).FirstOrDefault();

            // if the current unresolved assembly is referenced by our plugin, attempt to load
            if (refAssembly != null)
            {
                // load from the path to this plugin assembly, not host executable
                string dir = Path.GetDirectoryName(currAssembly.Location).ToLower();
                string folder = Path.GetFileNameWithoutExtension(currAssembly.Location);
                dir = Path.Combine(dir, folder);

                var assmbPath = Path.Combine(dir, $"{argName}.dll");

                if (File.Exists(assmbPath))
                {
                    loadAssembly = Assembly.LoadFrom(assmbPath);
                }
                else
                {
                    throw new FileNotFoundException($"Unable to locate dependency: {assmbPath}");
                }
            }

            return loadAssembly;
        }
    }
}