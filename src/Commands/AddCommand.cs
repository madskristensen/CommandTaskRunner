using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
using task = System.Threading.Tasks.Task;

namespace CommandTaskRunner
{
    internal sealed class AddCommand
    {
        private DTE2 _dte;

        private AddCommand(OleMenuCommandService commandService, DTE2 dte)
        {
            _dte = dte;

            var cmdAddCommand = new CommandID(PackageGuids.guidCommandCmdSet, PackageIds.AddCommandId);
            var addCommandItem = new OleMenuCommand(AddCommandToFile, cmdAddCommand)
            {
                Supported = false
            };

            commandService.AddCommand(addCommandItem);
        }

        public static AddCommand Instance
        {
            get; private set;
        }

        public static async task InitializeAsync(AsyncPackage package)
        {
            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            var dte = await package.GetServiceAsync(typeof(DTE)) as DTE2;

            Instance = new AddCommand(commandService, dte);
        }

        private void AddCommandToFile(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var button = (OleMenuCommand)sender;
            ProjectItem item = ProjectHelpers.GetSelectedItems().FirstOrDefault();

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
                _dte.ItemOperations.OpenFile(configPath);
                _dte.ExecuteCommand("SolutionExplorer.SyncWithActiveDocument");
                _dte.ExecuteCommand("SolutionExplorer.SyncWithActiveDocument");
            }

            OpenTaskRunnerExplorer();
            _dte.StatusBar.Text = $"File successfully added to {Constants.FILENAME}";
        }

        private void AddFileToProject(ProjectItem item, bool isSolution, string configPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_dte.Solution.FindProjectItem(configPath) != null)
                return;

            Project currentProject = null;

            if (isSolution && item.Kind != EnvDTE.Constants.vsProjectItemKindSolutionItems)
            {
                var solution = (Solution2)_dte.Solution;

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

        private string GetConfigPath(ProjectItem item, out bool isSolution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            isSolution = false;

            // Solution items always goes into the solution
            if (item.Kind == EnvDTE.Constants.vsProjectItemKindSolutionItems)
            {
                isSolution = true;
                string solutionRoot = Path.GetDirectoryName(_dte.Solution.FullName);
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
            MessageBoxResult result = MessageBox.Show(message, Vsix.Name, MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                isSolution = true;
                string solutionRoot = Path.GetDirectoryName(_dte.Solution.FullName);
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
            ThreadHelper.ThrowIfNotOnUIThread();

            string vsCommandName = "View.TaskRunnerExplorer";
            Command vsCommand = _dte.Commands.Item(vsCommandName);

            if (vsCommand != null && vsCommand.IsAvailable)
                _dte.ExecuteCommand(vsCommandName);
        }
    }
}
