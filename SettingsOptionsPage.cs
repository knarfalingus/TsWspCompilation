using System;
using ConfOxide;
using Microsoft.VisualStudio.Shell;

namespace TsWspCompilation
{

    // Visual Studio binds directly to the dialog page you pass it
    // so if you pass some data, the user changes the setting, clicks cancel,
    // the object is still updated
    // this generic class essentially makes copies 
    abstract class SettingsOptionPage<T> : DialogPage where T : SettingsBase<T>
    {
        ///<summary>Gets the actual settings instance read by the application.</summary>
        public T OriginalTarget { get; private set; }
        ///<summary>Gets a clone of the settings instance used to bind the options pages.</summary>
        public T DialogTarget { get; private set; }
        public override object AutomationObject { get { return DialogTarget; } }

        protected SettingsOptionPage(Func<TsWspSettings, T> targetSelector) : this(targetSelector(TsWspSettings.Instance)) { }
        private SettingsOptionPage(T target)
        {
            OriginalTarget = target;
            DialogTarget = target.CreateCopy();
            //TODO: Update copy when dialog is shown?
            //Settings.Updated += delegate { LoadSettingsFromStorage(); };
        }

        public override void ResetSettings()
        {
            DialogTarget.ResetValues();
        }

        public override void LoadSettingsFromStorage()
        {
            DialogTarget.AssignFrom(OriginalTarget);
        }
        public override void SaveSettingsToStorage()
        {
            OriginalTarget.AssignFrom(DialogTarget);
            SettingsStore.Save();
        }
    }

    class TsWspOptionPage : SettingsOptionPage<TsWspSettings>
    {
        public TsWspOptionPage() : base(s => s) { }
    }

}
