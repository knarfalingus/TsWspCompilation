using ConfOxide;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsWspCompilation
{

    // SettingsBase (ConfOxide) allows for easy persistence of settings type classes to JSON
    // https://github.com/SLaks/ConfOxide

    public sealed class TsWspSettings : SettingsBase<TsWspSettings>
    {
        public static readonly TsWspSettings Instance = new TsWspSettings();
        [Category("Setup")]
        [LocDisplayName("Tsc.exe path")]
        [Description("Full Path Of Typescript Compiler")]
        [DefaultValue(TsWspConstants.TSC_DEFAULT_PATH)]
        [System.ComponentModel.Editor(
        typeof(System.Windows.Forms.Design.FileNameEditor),
        typeof(System.Drawing.Design.UITypeEditor))]
        public string TypeScriptCompilerPath { get; set; }

        [Category("Behavior")]
        [LocDisplayName("Overwrite Readonly")]
        [Description("Overwrite readonly files (could be due to source control)")]
        [DefaultValue(false)]
        public bool OverwriteReadonly { get; set; }


        [Category("Behavior")]
        [LocDisplayName("Aggressive SCC")]
        [Description("Try extra hard to check out files from source control")]
        [DefaultValue(false)]
        public bool AggressiveSourceControl { get; set; }


        [Category("Behavior")]
        [LocDisplayName("Aggressive SCC Command")]
        [Description("What menu command to use when checking out files (language specific)")]
        [DefaultValue("File.CheckOut")]
        public string CheckOutCommand { get; set; }


        [Category("IDE")]
        [DisplayName("Redirect messages to Output Window")]
        [Description("Show user errors in the Output Window instead of showing message boxes.")]
        [DefaultValue(false)]
        public bool AllMessagesToOutputWindow { get; set; }

        public enum Version
        {
            Default = 0,
            ES5,
            ES3,
            ES6
        }


        [Category("Compilation")]
        [LocDisplayName("ECMAScript Version")]
        [Description("ECMAScript Version for generated Javascript files")]
        [DefaultValue(Version.Default)]
        public Version?  TargetVersion { get; set; }


        public enum ModuleKind
        {
            Default = 0,
            AMD,
            CommonJS,
            System,
            UMD
        }

        [Category("Compilation")]
        [LocDisplayName("Module Code Generation")]
        [Description("Kind of module code generated")]
        [DefaultValue(ModuleKind.Default)]
        public ModuleKind ModuleGeneration { get; set; }

        [Category("Compilation")]
        [LocDisplayName("Emit Comments")]
        [Description("Include comments in generated Javascript files")]
        [DefaultValue(true)]
        public bool EmitComments { get; set; }

        [Category("Compilation")]
        [LocDisplayName("Generate Declaration")]
        [Description("Generate corresponding d.ts files")]
        [DefaultValue(false)]
        public bool GenerateDeclaration { get; set; }

        [Category("Compilation")]
        [LocDisplayName("Generate Source Maps")]
        [Description("Generate corresponding '.map' file")]
        [DefaultValue(true)]
        public bool GenerateSourceMaps { get; set; }

        [Category("Compilation")]
        [LocDisplayName("Experimental Features")]
        [Description("Allow experimental feature usage")]
        [DefaultValue(false)]
        public bool AllowExperimental { get; set; }

        [Category("Compilation")]
        [LocDisplayName("No Implicit Any")]
        [Description("Raise error on expressions and declarations with an implied 'any' type")]
        [DefaultValue(false)]
        public bool NoImplicitAny { get; set; }

        [Category("Compilation")]
        [LocDisplayName("Preserve Constant Enums")]
        [Description("Do not erase const enum declarations in generated code")]
        [DefaultValue(false)]
        public bool PreserveConstEnums { get; set; }

        [Category("Compilation")]
        [LocDisplayName("Supress Implicit Any Index")]
        [Description("Surpress No Implicit Any Errors for indexing objects lacking index signatures.")]
        [DefaultValue(false)]
        public bool SuppressImplicitAnyIndex { get; set; }

    }
}
