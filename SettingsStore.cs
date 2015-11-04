using ConfOxide;
using EnvDTE;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsWspCompilation
{
    internal static class SettingsStore
    {

        public static bool SolutionSettingsExist
        {
            get { return File.Exists(GetSolutionFilePath()); }
        }


        ///<summary>Loads the active settings file.</summary>
        public static void Load()
        {
            string jsonPath = GetFilePath();
            if (File.Exists(jsonPath))
            {
                TsWspSettings.Instance.ReadJsonFile(jsonPath);
                UpdateStatusBar("applied");
            }
        }
        ///<summary>Saves the current settings to the active settings file.</summary>
        public static void Save() { Save(GetFilePath()); }
        ///<summary>Saves the current settings to the specified settings file.</summary>
        private static void Save(string filename)
        {
            TsWspHelpers.CheckOutFileFromSourceControl(filename);
            TsWspSettings.Instance.WriteJsonFile(filename);
            UpdateStatusBar("updated");
        }

        ///<summary>Creates a settings file for the active solution if one does not exist already, initialized from the current settings.</summary>
        public static void CreateSolutionSettings()
        {
            string path = GetSolutionFilePath();
            if (File.Exists(path))
                return;

            Save(path);
            TsWspHelpers.GetSolutionItemsProject().ProjectItems.AddFromFile(path);
            UpdateStatusBar("created");
        }


        #region Modern Locator
        private static string GetFilePath()
        {
            string path = GetSolutionFilePath();

            if (!File.Exists(path))
                path = GetUserFilePath();

            return path;
        }
        public static string GetSolutionFilePath()
        {
            Solution solution = TsWspPackage.DTE.Solution;

            if (solution == null || string.IsNullOrEmpty(solution.FullName))
                return null;

            return Path.Combine(Path.GetDirectoryName(solution.FullName), TsWspConstants.SETTINGS_FILENAME);
        }
        private static string GetUserFilePath()
        {
            var ssm = new ShellSettingsManager(TsWspPackage.Instance);
            return Path.Combine(ssm.GetApplicationDataFolder(ApplicationDataFolder.RoamingSettings), TsWspConstants.SETTINGS_FILENAME);
        }
        #endregion

        public static void UpdateStatusBar(string action)
        {
            try
            {
                if (SolutionSettingsExist)
                    TsWspPackage.DTE.StatusBar.Text =  TsWspConstants.TITLE + ": Solution settings " + action;
                else
                    TsWspPackage.DTE.StatusBar.Text = TsWspConstants.TITLE +  ": Global settings " + action;
            }
            catch
            {
                Logger.Log("Error updating status bar");
            }
        }

    }
}
