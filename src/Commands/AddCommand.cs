using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using EnvDTE;
using EnvDTE80;
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

            OleMenuCommandService commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
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
            string configPath = GetConfigPath(item, out isSolution);
            bool configExist = File.Exists(configPath);
            string document = configExist ? File.ReadAllText(configPath) : "{}";

            JObject root = GetJsonObject(configPath, document);
            AddCommandToRoot(file, configPath, root);

            ProjectHelpers.CheckFileOutOfSourceControl(configPath);
            File.WriteAllText(configPath, root.ToString(), new UTF8Encoding(false));

            if (!configExist)
            {
                AddFileToProject(item, isSolution, configPath);
                VSPackage._dte.ItemOperations.OpenFile(configPath);
                VSPackage._dte.ExecuteCommand("SolutionExplorer.SyncWithActiveDocument");
                VSPackage._dte.ExecuteCommand("SolutionExplorer.SyncWithActiveDocument");
            }

            OpenTaskRunnerExplorer();
        }

        private static void AddFileToProject(ProjectItem item, bool isSolution, string configPath)
        {
            if (VSPackage._dte.Solution.FindProjectItem(configPath) != null)
                return;

            Project currentProject = null;

            if (isSolution && item.Kind != EnvDTE.Constants.vsProjectItemKindSolutionItems)
            {
                Solution2 solution = (Solution2)VSPackage._dte.Solution;

                foreach (Project project in solution.Projects)
                {
                    if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems && project.Name == "Solution Items")
                    {
                        currentProject = project;
                        break;
                    }
                }

                if (currentProject == null)
                    currentProject = solution.AddSolutionFolder("Solution Items");
            }

            currentProject = currentProject ?? item.ContainingProject;

            ProjectHelpers.AddFileToProject(currentProject, configPath, "None");
        }

        private static string GetConfigPath(ProjectItem item, out bool isSolution)
        {
            isSolution = false;

            // Solution items always goes into the solution
            if (item.Kind == EnvDTE.Constants.vsProjectItemKindSolutionItems)
            {
                isSolution = true;
                string solutionRoot = Path.GetDirectoryName(VSPackage._dte.Solution.FullName);
                return Path.Combine(solutionRoot, Constants.FILENAME);
            }

            string root = ProjectHelpers.GetRootFolder(item.ContainingProject);
            string configPath = Path.Combine(root, Constants.FILENAME);

            // Search all the way up to the drive root
            while (!File.Exists(configPath) && Path.GetDirectoryName(root) != null)
            {
                root = Path.GetDirectoryName(root);
                configPath = Path.Combine(root, Constants.FILENAME);
            }

            if (File.Exists(configPath))
                return configPath;

            string message = "Do you want to configure the task runner on the solution level?\r\rIf not, it will be configured on the project";
            var result = MessageBox.Show(message, Constants.VSIX_NAME, MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                isSolution = true;
                string solutionRoot = Path.GetDirectoryName(VSPackage._dte.Solution.FullName);
                return Path.Combine(solutionRoot, Constants.FILENAME);
            }
            else
            {
                string projectRoot = ProjectHelpers.GetRootFolder(item.ContainingProject);
                return Path.Combine(projectRoot, Constants.FILENAME);
            }
        }

        private static void AddCommandToRoot(string file, string configPath, JObject root)
        {
            JObject command = (JObject)root["commands"];

            var cmd = new
            {
                fileName = GetExecutableFileName(file),
                workingDirectory= ".",
                arguments = GetArguments(file, configPath),
            };

            string name = Path.GetFileNameWithoutExtension(file);

            if (command[name] != null)
                name += $" {DateTime.Now.GetHashCode()}";

            string taskString = JsonConvert.SerializeObject(cmd, Formatting.Indented);

            command.Add(name, JObject.Parse(taskString));
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
