using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Settings;
using EnvDTE80;
using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ShellConstants = Microsoft.VisualStudio.Shell.Interop.Constants;
using System.Runtime.InteropServices;

namespace TsWspCompilation
{
    public static class TsWspHelpers
    {
        ///<summary>Attempts to ensure that a file is writable.</summary>
        /// <returns>True if the file is not under source control or was checked out; false if the checkout failed or an error occurred.</returns>
        public static bool CheckOutFileFromSourceControl(string fileName)
        {
            try
            {
                var dte = TsWspPackage.DTE;
                if (dte == null)
                    return true;

                //if (dte == null || !File.Exists(fileName) || dte.Solution.FindProjectItem(fileName) == null)
                var item = dte.Solution.FindProjectItem(fileName);
                if (item == null)
                    return true;

                EnvDTE80.SourceControl2 sc2 = dte.SourceControl as EnvDTE80.SourceControl2;
                if (sc2 != null)
                {
                    if (sc2.IsItemUnderSCC(fileName) && !sc2.IsItemCheckedOut(fileName))
                        return sc2.CheckOutItem(fileName);
                    else
                    {
                        // some providers (like SourceGear Vault) dont appear to implement SourceControl or SourceControl2.
                        // use more aggresive steps if permitted
                        // we only do this is if the file is read only - if it is not the assumption is it was taken care of
                        FileInfo fi = new FileInfo(fileName);
                        if (fi.IsReadOnly)
                        {
                            if (TsWspSettings.Instance.AggressiveSourceControl)
                            {
                                var w = item.Open(EnvDTE.Constants.vsViewKindCode);
                                if (w != null)
                                {
                                    bool CachedVisibility = w.Visible;
                                    w.Activate();
                                    try
                                    {
                                        // unfortunately the command is language specific and can be provider specific.... 
                                        // placed in setting so it could be changed
                                        dte.ExecuteCommand(TsWspSettings.Instance.CheckOutCommand, string.Empty);
                                    }
                                    catch (Exception ex)
                                    {
                                        // item may not be in source control or some other case, allow to go forward
                                        // may still succeed if AlwaysOverwrite is specified for example.
                                    }
                                    if (w.Visible != CachedVisibility)
                                        w.Visible = CachedVisibility;
                                }
                            }
                        }
                    }

                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return false;
            }
        }

        public static bool IsFileInWebsiteProject(string fileName)
        {
            var dte = TsWspPackage.DTE;
            var item = dte.Solution.FindProjectItem(fileName);
            if (item == null)
                return false;
            if (item.ContainingProject == null)
                return false;
            return (item.ContainingProject.Object is VsWebSite.VSWebSite);
        }

        public static Project GetSolutionItemsProject()
        {
            Solution2 solution = TsWspPackage.DTE.Solution as Solution2;
            return solution.Projects
                           .OfType<Project>()
                           .FirstOrDefault(p => p.Name.Equals(TsWspConstants.SOLUTION_ITEMS_FOLDER, StringComparison.OrdinalIgnoreCase))
                      ?? solution.AddSolutionFolder(TsWspConstants.SOLUTION_ITEMS_FOLDER);
        }

        public static void AddFileToProject(this Project project, string file, string itemType = null)
        {
            if (project.Kind.Equals("{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}", StringComparison.OrdinalIgnoreCase)) // ASP.NET 5 projects
                return;

            try
            {
                var dte = TsWspPackage.DTE;
                if (dte.Solution.FindProjectItem(file) == null)
                {
                    ProjectItem item = project.ProjectItems.AddFromFile(file);

                    if (string.IsNullOrEmpty(itemType) ||
                        project.Kind.Equals("{E24C65DC-7377-472B-9ABA-BC803B73C61A}", StringComparison.OrdinalIgnoreCase) || // Website
                        project.Kind.Equals("{262852C6-CD72-467D-83FE-5EEB1973A190}", StringComparison.OrdinalIgnoreCase))   // Universal apps
                        return;

                    item.Properties.Item("ItemType").Value = "None";
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        /// <summary>
        /// add an output file to a source files project (updates solution explorer)
        /// </summary>
        public static void AddOutputFileToSourceFilesProject(string sourceFile, string outputFile)
        {
            var dte = TsWspPackage.DTE;
            var item = dte.Solution.FindProjectItem(sourceFile);
            if (item != null && item.ContainingProject != null)
            {
                item.ContainingProject.AddFileToProject(outputFile);
            }
        }

        /// <summary>
        /// takes a project item file contents and re-saves through IDE editor window, hopefully this triggers
        /// other extensions like bundlers, minifiers, linters etc to update themselves
        /// </summary>
        /// <param name="outputFile"></param>
        public static void SaveOutputFileThroughEditorWindow(string outputFile)
        {
            string jsSource = System.IO.File.ReadAllText(outputFile);
            var dte = TsWspPackage.DTE;
            var item = dte.Solution.FindProjectItem(outputFile);
            if (item != null)
            {
                // open it
                var w = item.Open(EnvDTE.Constants.vsViewKindCode);
                if (w != null)
                {
                    TextSelection ts = w.Document.Selection as TextSelection;
                    if (ts != null)
                    {
                        // replace all text with new source
                        ts.SelectAll();
                        ts.Insert(jsSource);
                        item.Save();
                        // move to top
                        ts.StartOfDocument();
                    }
                    else
                    {
                        Logger.Log("Could not update text in " + outputFile);
                    }
                }
                else
                {
                    Logger.Log("Could not open code window for " + outputFile);
                }
            }
            else
            {
                Logger.Log("Could not locate project item = " + outputFile);
            }

        }

        /// <summary>
        /// gets the filename associated with document cookie
        /// </summary>
        public static string GetDocumentFileName(this IVsRunningDocumentTable RDT, uint docCookie)
        {
            if (RDT == null)
                return null;

            uint flags, readlocks, editlocks;
            string fileName; IVsHierarchy hier;
            uint itemid; IntPtr docData;
            RDT.GetDocumentInfo(docCookie, out flags, out readlocks, out editlocks, out fileName, out hier, out itemid, out docData);

            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            return fileName;
        }

        public static bool EnsureNotReadonly(string fn)
        {
            try
            {
                System.IO.FileAttributes fa = System.IO.File.GetAttributes(fn);
                if ((fa & System.IO.FileAttributes.ReadOnly) != 0)
                    System.IO.File.SetAttributes(fn, fa ^ System.IO.FileAttributes.ReadOnly);
                return true;//success
            }
            catch (Exception ex)
            {
            }
            return false;
        }


        public static bool IsTypescriptFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;
            string ext = System.IO.Path.GetExtension(fileName);
            return (string.Compare(ext, ".ts", true) == 0 || string.Compare(ext, ".tsx", false) == 0);
        }

        public static bool HasParentTypescriptFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;
            string ext = System.IO.Path.GetExtension(fileName);
            if (string.Compare(ext, ".map", true) == 0 || string.Compare(ext, ".js", false) == 0)
            {
                if (string.Compare(ext, ".map", true) == 0 && !TsWspSettings.Instance.GenerateSourceMaps)
                    return false;

                string tsFileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(fileName), System.IO.Path.GetFileNameWithoutExtension(fileName));

                if (System.IO.File.Exists(tsFileName + ".ts"))
                    return true;

                if (System.IO.File.Exists(tsFileName + ".tsx"))
                    return true;

           }
            return false;

        }

        public static bool IsSingleProjectItemSelection(out IVsHierarchy hierarchy, out uint itemid)
        {
            hierarchy = null;
            itemid = VSConstants.VSITEMID_NIL;
            int hr = VSConstants.S_OK;

            var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            if (monitorSelection == null || solution == null)
            {
                return false;
            }

            IVsMultiItemSelect multiItemSelect = null;
            IntPtr hierarchyPtr = IntPtr.Zero;
            IntPtr selectionContainerPtr = IntPtr.Zero;

            try
            {
                hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out multiItemSelect, out selectionContainerPtr);

                if (ErrorHandler.Failed(hr) || hierarchyPtr == IntPtr.Zero || itemid == VSConstants.VSITEMID_NIL)
                {
                    // there is no selection
                    return false;
                }

                // multiple items are selected
                if (multiItemSelect != null) return false;

                // there is a hierarchy root node selected, thus it is not a single item inside a project

                if (itemid == VSConstants.VSITEMID_ROOT) return false;

                hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
                if (hierarchy == null) return false;

                Guid guidProjectID = Guid.Empty;

                if (ErrorHandler.Failed(solution.GetGuidOfProject(hierarchy, out guidProjectID)))
                {
                    return false; // hierarchy is not a project inside the Solution if it does not have a ProjectID Guid
                }

                // if we got this far then there is a single project item selected
                return true;
            }
            finally
            {
                if (selectionContainerPtr != IntPtr.Zero)
                {
                    Marshal.Release(selectionContainerPtr);
                }

                if (hierarchyPtr != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPtr);
                }
            }
        }

    }


}
