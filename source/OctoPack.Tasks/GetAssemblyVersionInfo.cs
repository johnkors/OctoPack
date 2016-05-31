using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OctoPack.Tasks
{
    public class GetAssemblyVersionInfo : AbstractTask
    {

        /// <summary>
        /// Specifies the files the retrieve info from.
        /// </summary>
        [Required]
        public ITaskItem[] AssemblyFiles { get; set; }

        /// <summary>
        /// Contains the retrieved version info
        /// </summary>
        [Output]
        public ITaskItem[] AssemblyVersionInfo { get; set; }

        public override bool Execute()
        {
            if (AssemblyFiles.Length <= 0)
            {
                return false;
            }
            
            var infos = new List<ITaskItem>();
            foreach (var assemblyFile in AssemblyFiles)
            {
                LogMessage(String.Format("Get version info from assembly: {0}", assemblyFile), MessageImportance.Normal);

                infos.Add(CreateTaskItemFromFileVersionInfo(assemblyFile.ItemSpec));
            }
            AssemblyVersionInfo = infos.ToArray();
            return true;
        }

        public bool UseFileVersion { get; set; }

        private TaskItem CreateTaskItemFromFileVersionInfo(string path)
        {
            var assembly = Assembly.LoadFrom(path);
            var info = FileVersionInfo.GetVersionInfo(path);
            var currentAssemblyName = assembly.GetName();
            var assemblyVersion = currentAssemblyName.Version;
            var assemblyFileVersion = info.FileVersion;
            var assemblyVersionInfo = info.ProductVersion;
            var nugetVersion = assembly.GetNugetVersion();

            if (nugetVersion != null)
            {
                // If we find a GitVersion information in the assembly, we can be pretty sure it's got the stuff we want, so let's use that.
                return new TaskItem(info.FileName, new Hashtable
                {
                    { "Version", nugetVersion },
                });
            }

            if (UseFileVersion)
            {
                return new TaskItem(info.FileName, new Hashtable
                {
                    {"Version", assemblyFileVersion },
                });
            }

            if (assemblyFileVersion == assemblyVersionInfo)
            {
                // Info version defaults to file version, so if they are the same, the customer probably doesn't want to use file version. Instead, use assembly version.
                return new TaskItem(info.FileName, new Hashtable
                {
                    {"Version", assemblyVersion.ToString()},
                });
            }
            
            // If the info version is different from file version, that must be what they want to use
            return new TaskItem(info.FileName, new Hashtable
            {
                {"Version", assemblyVersionInfo},
            });
        }
    }
}
