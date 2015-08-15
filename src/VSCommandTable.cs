namespace CommandTaskRunner
{
    using System;
    
    /// <summary>
    /// Helper class that exposes all GUIDs used across VS Package.
    /// </summary>
    internal sealed partial class GuidList
    {
        public const string guidAddCommandPackageString = "bc2a198c-4598-4e8f-97a6-249573ca88a9";
        public const string guidCommandCmdSetString = "1b181a1d-76a0-4629-8e2a-8c24d86c29bd";
        public static Guid guidAddCommandPackage = new Guid(guidAddCommandPackageString);
        public static Guid guidCommandCmdSet = new Guid(guidCommandCmdSetString);
    }
    /// <summary>
    /// Helper class that encapsulates all CommandIDs uses across VS Package.
    /// </summary>
    internal sealed partial class PackageCommands
    {
        public const int AddCommandId = 0x0064;
    }
}
