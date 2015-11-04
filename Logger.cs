using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TsWspCompilation
{
    public static class Logger
    {

        private static IVsOutputWindowPane pane;
        private static object _syncRoot = new object();

        private static bool EnsurePane()
        {
            if (pane == null)
            {
                lock (_syncRoot)
                {
                    if (pane == null)
                    {
                        pane = TsWspPackage.Instance.GetOutputPane(VSConstants.OutputWindowPaneGuid.BuildOutputPane_guid, TsWspConstants.PANE_CAPTION);
                    }
                }
            }

            return pane != null;
        }

        public static void ShowMessage(string message, string title = TsWspConstants.TITLE,
    MessageBoxButtons messageBoxButtons = MessageBoxButtons.OK,
    MessageBoxIcon messageBoxIcon = MessageBoxIcon.Warning,
    MessageBoxDefaultButton messageBoxDefaultButton = MessageBoxDefaultButton.Button1)
        {
            if (TsWspSettings.Instance.AllMessagesToOutputWindow)
            {
                Log(String.Format(CultureInfo.CurrentCulture, "{0}: {1}", title, message));
            }
            else
            {
                MessageBox.Show(message, title, messageBoxButtons, messageBoxIcon, messageBoxDefaultButton);
            }
        }

        public static void Log(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            try
            {
                if (EnsurePane())
                {
                    pane.OutputString(DateTime.Now.ToString() + ": " + message + Environment.NewLine);
                }
            }
            catch
            {
                // Do nothing
            }

        }

        public static void Log(Exception ex)
        {
            ShowMessage(ex.ToString(), ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error); 
        }

    }
}
