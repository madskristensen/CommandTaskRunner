using System;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CommandTaskRunner
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Version, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [Guid(GuidList.guidAddCommandPackageString)]
    public sealed class VSPackage : Package
    {
        public const string Version = "1.0";
        public static DTE2 _dte;

        protected override void Initialize()
        {
            _dte = GetService(typeof(DTE)) as DTE2;

            Logger.Initialize(this, Constants.VSIX_NAME);
            AddCommand.Initialize(this);

            base.Initialize();
        }
    }
}
