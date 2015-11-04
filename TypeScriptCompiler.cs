using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsWspCompilation
{
    public static class TypeScriptCompiler
    {

        private  static string TypeScriptOutputFileName(string tsFileName)
        {
            string outputFile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(tsFileName), System.IO.Path.GetFileNameWithoutExtension(tsFileName) + ".js");
            return outputFile;
        }


        private static string EmptyMapFileContents(string sourceFileName, string outFileName)
        {
            string sfn = System.IO.Path.GetFileName(sourceFileName);
            string ofn = System.IO.Path.GetFileName(outFileName);
            string src = $"{{\"version\":3,\"file\":\"{sfn}\",\"sourceRoot\":\"\",\"sources\":[\"{ofn}\"],\"names\":[],\"mappings\":\"AAAA,SAAS\"}}";
            return src;
        }


        private static void PrepareOutputFileForCompilation(string sourceFile, string outputFile, string emptyFileConents)
        {
            // create a temporary file if it doesnt already exist so we can add to project etc if needed
            if (!System.IO.File.Exists(outputFile))
                System.IO.File.WriteAllText(outputFile, emptyFileConents);

            // ensure output file is in our project
            TsWspHelpers.AddOutputFileToSourceFilesProject(sourceFile, outputFile);

            // check it ouf if needed
            // (this doesnt work for Sourcegear Vault....)
            if (!TsWspHelpers.CheckOutFileFromSourceControl(outputFile))
            {
                //abort on failed checkout
                throw new ApplicationException("Failed to check out file " + outputFile);
            }

            // make non read only if permitted
            System.IO.FileInfo fi = new System.IO.FileInfo(outputFile);
            if (fi.Exists == true && fi.IsReadOnly)
            {

                if (TsWspSettings.Instance.OverwriteReadonly == true)
                    TsWspHelpers.EnsureNotReadonly(outputFile);

                fi.Refresh();

                if (fi.IsReadOnly)
                {
                    string msg = $"TsWspCompilation\r\nWill not be able to write file {outputFile}\r\nfrom source {sourceFile}\r\n{outputFile} is read only";
                    throw new ApplicationException(msg);
                }
            }
        }



        public static void Compile(string sourceFile)
        {
            if (!TsWspHelpers.IsTypescriptFile(sourceFile))
                return;

            // this extension currrently only applies to web site projects
            bool b = TsWspHelpers.IsFileInWebsiteProject(sourceFile);
            if (!b)
                return;


            Window currentActiveWindow = null;
            Document doc = null;
            try
            {
                var dte = TsWspPackage.DTE;
                currentActiveWindow = dte.ActiveWindow;
                doc = dte.ActiveDocument;
                // if compiled from menu, may not be saved yet
                var item = dte.Solution.FindProjectItem(sourceFile);
                if (item != null)
                {
                    if (item.IsDirty)
                        item.Save();
                }

                    // check if a typescript file extension (even though specified ContentType attribute....)
                string outputFile = TypeScriptOutputFileName(sourceFile);
                string mapFile = outputFile + ".map";

                PrepareOutputFileForCompilation(sourceFile, outputFile, "/*empty*/");


                if (TsWspSettings.Instance.GenerateSourceMaps)
                {
                    string contents = EmptyMapFileContents(sourceFile, outputFile);
                    PrepareOutputFileForCompilation(sourceFile, mapFile, contents);
                }


                // compile the TypeScript file
                ExecuteTypeScript(sourceFile, outputFile, TsWspSettings.Instance);


                if (System.IO.File.Exists(outputFile))
                {
                    // now load the output into an editor and explicitly save
                    TsWspHelpers.SaveOutputFileThroughEditorWindow(outputFile);

                }
                else
                {
                    Logger.Log("Output file does not exist = " + outputFile);
                }

                if (TsWspSettings.Instance.GenerateSourceMaps)
                {
                    if (System.IO.File.Exists(mapFile))
                    {
                        // now load the output into an editor and explicitly save
                        TsWspHelpers.SaveOutputFileThroughEditorWindow(mapFile);

                    }
                    else
                    {
                        Logger.Log("Output file does not exist = " + mapFile);
                    }

                }


                return;
            }
            catch (Exception ex)
            {

                Logger.Log(ex);
                return;

            }
            finally
            {
                try
                {
                    if (currentActiveWindow != null)
                        currentActiveWindow.Activate();
                }
                catch
                {
                }
                if (doc != null)
                {
                    foreach (Window z in doc.Windows)
                    {
                        if (z != null)
                        {
                            if (z.Kind == "Document" || doc.Windows.Count == 1)
                            {
                                z.Activate();
                                break;
                            }
                        }
                    }
                }

            }

        }

        public static void ExecuteTypeScript(string sourceFile, string outputFile, TsWspSettings options)
        {
            var d = new Dictionary<string, string>();

            if (options.AllowExperimental == true)
                d.Add("--experimentalDecorators", null);

            if (options.EmitComments == false)
                d.Add("-removeComments", null);

            if (options.GenerateDeclaration == true)
                d.Add("-d", null);

            if (options.GenerateSourceMaps == true)
                d.Add("--sourcemap", null);

            if (options.ModuleGeneration != TsWspSettings.ModuleKind.Default)
                d.Add("--module", options.ModuleGeneration.ToString().ToLowerInvariant());

            if (options.NoImplicitAny == true)
                d.Add("--noImplicitAny", null);

            if (options.PreserveConstEnums == true)
                d.Add("--preserveConstEnums", null);

            if (options.SuppressImplicitAnyIndex == true)
                d.Add("--suppressImplicitAnyIndexErrors", null);
            
            //if (!String.IsNullOrEmpty(options.OutPath))
            //    d.Add("--out", options.OutPath);

            if (options.TargetVersion != TsWspSettings.Version.Default)
                d.Add("--target", options.TargetVersion.ToString());

            d.Add("--out", outputFile);


            if (!System.IO.File.Exists(TsWspSettings.Instance.TypeScriptCompilerPath))
            {
                Logger.ShowMessage("Cannot find tsc.exe at : " + TsWspSettings.Instance.TypeScriptCompilerPath, TsWspConstants.TITLE, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }

            // this will invoke `tsc` passing the TS path and other
            // parameters defined in Options parameter
            using (System.Diagnostics.Process p = new System.Diagnostics.Process())
            {

                string arguments = sourceFile + " " + String.Join(" ", d.Select(o => o.Key + " " + o.Value));
                ProcessStartInfo psi = new ProcessStartInfo(TsWspSettings.Instance.TypeScriptCompilerPath, arguments );

                Logger.Log("Compiling " + arguments);

                // run without showing console windows
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;

                // redirects the compiler error output, so we can read
                // and display errors if any
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;

                p.StartInfo = psi;

                p.Start();

                // reads the error output
                var msg = p.StandardError.ReadToEnd();

                // this actually will do nothing (TSC doesnt use stderr apparently)
                if (!string.IsNullOrWhiteSpace(msg))
                    Logger.Log("tsc.exe error: " + msg);

                // workaround....
                var output = p.StandardOutput.ReadToEnd();
                if (!string.IsNullOrEmpty(output))
                    output = output.Trim(new char[] { ' ', '\r', '\n', '\t', '\u00A0' });
                if (output != null && output.ToLowerInvariant().Contains("error"))
                    Logger.Log("tsc.exe error detected: " + output);

                

                // make sure it finished executing before proceeding 
                p.WaitForExit();

            }
        }

    }



}
