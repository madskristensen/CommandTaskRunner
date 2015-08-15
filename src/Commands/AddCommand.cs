using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CommandTaskRunner
{
    internal sealed class AddCommand
    {
        private readonly Package package;
        private static string[] _extensions = new[] { ".cmd", ".bat", ".ps1", ".psm1" };

        private AddCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var cmdAddCommand = new CommandID(GuidList.guidCommandCmdSet, PackageCommands.AddCommandId);
                var addCommandItem = new OleMenuCommand(AddCommandToFile, cmdAddCommand);
                addCommandItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(addCommandItem);
            }
        }

        public static AddCommand Instance
        {
            get; private set;
        }

        private IServiceProvider ServiceProvider
        {
            get { return this.package; }
        }

        public static void Initialize(Package package)
        {
            Instance = new AddCommand(package);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand button = (OleMenuCommand)sender;
            button.Enabled = button.Visible = false;

            var item = ProjectHelpers.GetSelectedItems().FirstOrDefault();

            if (item == null || item.FileCount == 0)
                return;

            string file = item.FileNames[1];
            string ext = Path.GetExtension(file).ToLowerInvariant();

            button.Enabled = button.Visible = _extensions.Contains(ext);
        }

        private void AddCommandToFile(object sender, EventArgs e)
        {
            OleMenuCommand button = (OleMenuCommand)sender;
            var item = ProjectHelpers.GetSelectedItems().FirstOrDefault();

            if (item == null || item.FileCount == 0)
                return;

            string file = item.FileNames[1];
            bool isSolution;
            string folder = GetConfigFolder(item, out isSolution);
            string configPath = Path.Combine(folder, Constants.FILENAME);
            bool configExist = File.Exists(configPath);
            string document = configExist ? File.ReadAllText(configPath) : "{}";

            JObject root = GetJsonObject(configPath, document);
            JObject command = (JObject)root["commands"];

            var cmd = new
            {
                FileName = GetExecutableFileName(file),
                WorkingDirectory = ".",
                Arguments = GetArguments(file, configPath),
            };

            string taskString = JsonConvert.SerializeObject(cmd, Formatting.Indented);
            command.Add(Path.GetFileNameWithoutExtension(file), JObject.Parse(taskString));

            ProjectHelpers.CheckFileOutOfSourceControl(configPath);
            File.WriteAllText(configPath, root.ToString(), new UTF8Encoding(false));

            if (item.ContainingProject != null)
                ProjectHelpers.AddFileToProject(item.ContainingProject, configPath, "None");

            if (!configExist)
                VSPackage._dte.ItemOperations.OpenFile(configPath);

            OpenTaskRunnerExplorer();
        }

        private static string GetArguments(string file, string configPath)
        {
            string relative = MakeRelative(configPath, file).Replace("/", "\\");

            if (GetExecutableFileName(file) == "powershell.exe")
                return $"-ExecutionPolicy Bypass -File {relative}";

            return $"/c {relative}";
        }

        private static string GetExecutableFileName(string file)
        {
            string ext = Path.GetExtension(file).ToLowerInvariant();

            switch (ext)
            {
                case ".ps1":
                case ".psm1":
                    return "powershell.exe";
            }

            return "cmd.exe";
        }

        private void OpenTaskRunnerExplorer()
        {
            string vsCommandName = "View.TaskRunnerExplorer";
            var vsCommand = VSPackage._dte.Commands.Item(vsCommandName);

            if (vsCommand != null && vsCommand.IsAvailable)
                VSPackage._dte.ExecuteCommand(vsCommandName);
        }

        private static string GetConfigFolder(EnvDTE.ProjectItem item, out bool isSolution)
        {
            isSolution = false;

            if (item.ContainingProject == null || item.Properties == null)
            {
                isSolution = true;
                return Path.GetDirectoryName(VSPackage._dte.Solution.FullName);
            }

            return ProjectHelpers.GetRootFolder(item.ContainingProject);
        }

        private static JObject GetJsonObject(string configPath, string document)
        {
            JObject root;

            if (File.Exists(configPath))
                root = JObject.Parse(document);
            else
                root = JObject.Parse("{ \"commands\": {}  }");

            var commands = root["commands"];

            if (commands == null)
                root.Add("commands", JObject.Parse("{}"));

            return root;
        }

        public static string MakeRelative(string baseFile, string file)
        {
            Uri baseUri = new Uri(baseFile, UriKind.RelativeOrAbsolute);
            Uri fileUri = new Uri(file, UriKind.RelativeOrAbsolute);

            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString());
        }
    }
}
