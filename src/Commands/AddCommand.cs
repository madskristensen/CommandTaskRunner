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

            button.Enabled = button.Visible = CommandHelpers.IsFileSupported(file);
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

            JObject json = CommandHelpers.GetJsonContent(configPath, file);

            ProjectHelpers.CheckFileOutOfSourceControl(configPath);
            File.WriteAllText(configPath, json.ToString(), new UTF8Encoding(false));

            if (!configExist)
            {
                AddFileToProject(item, isSolution, configPath);
                VSPackage._dte.ItemOperations.OpenFile(configPath);
                VSPackage._dte.ExecuteCommand("SolutionExplorer.SyncWithActiveDocument");
                VSPackage._dte.ExecuteCommand("SolutionExplorer.SyncWithActiveDocument");
            }

            OpenTaskRunnerExplorer();
            VSPackage._dte.StatusBar.Text = $"File successfully added to {Constants.FILENAME}";
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
            var result = MessageBox.Show(message, Vsix.Name, MessageBoxButton.YesNo, MessageBoxImage.Question);

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

        private void OpenTaskRunnerExplorer()
        {
            string vsCommandName = "View.TaskRunnerExplorer";
            var vsCommand = VSPackage._dte.Commands.Item(vsCommandName);

            if (vsCommand != null && vsCommand.IsAvailable)
                VSPackage._dte.ExecuteCommand(vsCommandName);
        }
    }
}
