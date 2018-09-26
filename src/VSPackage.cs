using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using task = System.Threading.Tasks.Task;

namespace CommandTaskRunner
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.guidAddCommandPackageString)]
    [ProvideUIContextRule(PackageGuids.guidAutoloadString,
    name: "Supported Files",
    expression: "Extensions",
    termNames: new[] { "Extensions" },
    termValues: new[] { "HierSingleSelectionName:.(exe|cmd|bat|ps1|psm1)$" })]
    public sealed class CommandTaskRunnerPackage : AsyncPackage
    {
        protected override async task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await AddCommand.InitializeAsync(this);
        }
    }
}
