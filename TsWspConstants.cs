using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsWspCompilation
{
    public static class TsWspConstants
    {
        //TODO: 32bit systems??
        public const string TSC_DEFAULT_PATH = @"C:\Program Files (x86)\Microsoft SDKs\TypeScript\1.6\tsc.exe";

        public const string SETTINGS_FILENAME = "TsWspCompilation.json";

        public const string SOLUTION_ITEMS_FOLDER = "Solution Items";

        public const string PANE_CAPTION = "TsWsp Compilation";

        public const string TITLE = "TsWsp Compilation";
    }

    static class GuidList
    {
        public const string guidTsWspPackage_string = "2e64a53b-9b88-49da-b7ce-1174266b093d";
        public const string guidTsWspPackageCmdSet_string = "7a93c612-833d-4dbf-809f-2af2ab73b49e";

        public static readonly Guid guidTsWspPackageCmdSet = new Guid(guidTsWspPackageCmdSet_string);

    }
    static class PkgCmdIDList
    {
        public const uint cmdCompile = 0x100;
    };
}
