using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TaskRunnerExplorer;
using ProjectTaskRunner.Helpers;
using Task = System.Threading.Tasks.Task;

namespace CommandTaskRunner
{
    internal class TrimmingStringComparer : IEqualityComparer<string>
    {
        private char _toTrim;
        private IEqualityComparer<string> _basisComparison;

        public TrimmingStringComparer(char toTrim)
            : this(toTrim, StringComparer.Ordinal)
        {
        }

        public TrimmingStringComparer(char toTrim, IEqualityComparer<string> basisComparer)
        {
            _toTrim = toTrim;
            _basisComparison = basisComparer;
        }

        public bool Equals(string x, string y)
        {
            string realX = x?.TrimEnd(_toTrim);
            string realY = y?.TrimEnd(_toTrim);
            return _basisComparison.Equals(realX, realY);
        }

        public int GetHashCode(string obj)
        {
            string realObj = obj?.TrimEnd(_toTrim);
            return realObj != null ? _basisComparison.GetHashCode(realObj) : 0;
        }
    }

    [TaskRunnerExport(Constants.FILENAME)]
    class TaskRunnerProvider : ITaskRunner
    {
        private ImageSource _icon;
        private HashSet<string> _dynamicNames = new HashSet<string>(new TrimmingStringComparer('\u200B'));

        [Import]
        internal SVsServiceProvider _serviceProvider = null;

        public void SetDynamicTaskName(string dynamicName)
        {
            _dynamicNames.Remove(dynamicName);
            _dynamicNames.Add(dynamicName);
        }

        public string GetDynamicName(string name)
        {
            IEqualityComparer<string> comparer = new TrimmingStringComparer('\u200B');
            return _dynamicNames.FirstOrDefault(x => comparer.Equals(name, x));
        }

        public TaskRunnerProvider()
        {
            _icon = new BitmapImage(new Uri(@"pack://application:,,,/CommandTaskRunner;component/Resources/project.png"));
        }

        public List<ITaskRunnerOption> Options
        {
            get { return null; }
        }

        public async Task<ITaskRunnerConfig> ParseConfig(ITaskRunnerCommandContext context, string configPath)
        {
            return await Task.Run(() =>
            {
                ITaskRunnerNode hierarchy = LoadHierarchy(configPath);

                if (!hierarchy.Children.Any() && !hierarchy.Children.First().Children.Any())
                    return null;

                return new TaskRunnerConfig(this, context, hierarchy, _icon);
            });
        }
        
        private void ApplyVariable(string key, string value, ref string str)
        {
            str = str.Replace(key, value);
        }

        private string SetVariables(string str, string cmdsDir)
        {
            if (str == null)
                return str;

            var dte = (DTE)_serviceProvider.GetService(typeof(DTE));

            var sln = dte.Solution;
            var projs = GetProjects(dte);
            var build = sln.SolutionBuild;
            var slnCfg = (SolutionConfiguration2)build.ActiveConfiguration;

            var proj = projs.Cast<Project>().FirstOrDefault(x => x.FileName.Contains(cmdsDir));

            ApplyVariable("$(ConfigurationName)", slnCfg.Name, ref str);
            ApplyVariable("$(DevEnvDir)", Path.GetDirectoryName(dte.FileName), ref str);
            ApplyVariable("$(PlatformName)", slnCfg.PlatformName, ref str);

            ApplyVariable("$(SolutionDir)", Path.GetDirectoryName(sln.FileName), ref str);
            ApplyVariable("$(SolutionExt)", Path.GetExtension(sln.FileName), ref str);
            ApplyVariable("$(SolutionFileName)", Path.GetFileName(sln.FileName), ref str);
            ApplyVariable("$(SolutionName)", Path.GetFileNameWithoutExtension(sln.FileName), ref str);
            ApplyVariable("$(SolutionPath)", sln.FileName, ref str);

            if (proj != null)
            {
                var projCfg = proj.ConfigurationManager.ActiveConfiguration;

                var outDir = (string)projCfg.Properties.Item("OutputPath").Value;

                var projectDir = Path.GetDirectoryName(proj.FileName);
                var targetFilename = (string)proj.Properties.Item("OutputFileName").Value;
                var targetPath = Path.Combine(projectDir, outDir, targetFilename);
                var targetDir = Path.Combine(projectDir, outDir);

                ApplyVariable("$(OutDir)", outDir, ref str);

                ApplyVariable("$(ProjectDir)", projectDir, ref str);
                ApplyVariable("$(ProjectExt)", Path.GetExtension(proj.FileName), ref str);
                ApplyVariable("$(ProjectFileName)", Path.GetFileName(proj.FileName), ref str);
                ApplyVariable("$(ProjectName)", proj.Name, ref str);
                ApplyVariable("$(ProjectPath)", proj.FileName, ref str);

                ApplyVariable("$(TargetDir)", targetDir, ref str);
                ApplyVariable("$(TargetExt)", Path.GetExtension(targetFilename), ref str);
                ApplyVariable("$(TargetFileName)", targetFilename, ref str);
                ApplyVariable("$(TargetName)", proj.Name, ref str);
                ApplyVariable("$(TargetPath)", targetPath, ref str);
            }

            return str;
        }

        private ITaskRunnerNode LoadHierarchy(string configPath)
        {
            ITaskRunnerNode root = new TaskRunnerNode(Constants.TASK_CATEGORY);
            string rootDir = Path.GetDirectoryName(configPath);
            var commands = TaskParser.LoadTasks(configPath);

            if (commands == null)
                return root;

            TaskRunnerNode tasks = new TaskRunnerNode("Commands");
            tasks.Description = "A list of command to execute";
            root.Children.Add(tasks);

            foreach (CommandTask command in commands.OrderBy(k => k.Name))
            {
                command.Arguments = SetVariables(command.Arguments, rootDir);
                command.FileName = SetVariables(command.FileName, rootDir);
                command.Name = SetVariables(command.Name, rootDir);
                command.WorkingDirectory = SetVariables(command.WorkingDirectory, rootDir);

                string cwd = command.WorkingDirectory ?? rootDir;

                // Add zero width space
                string commandName = command.Name += "\u200B";
                SetDynamicTaskName(commandName);

                TaskRunnerNode task = new TaskRunnerNode(commandName, true)
                {
                    Command = new TaskRunnerCommand(cwd, command.FileName, command.Arguments),
                    Description = $"Filename:\t {command.FileName}\r\nArguments:\t {command.Arguments}"
                };

                tasks.Children.Add(task);
            }

            return root;
        }

        private IList<Project> GetProjects(DTE dte)
        {
            Projects projects = dte.Solution.Projects;
            List<Project> list = new List<Project>();
            var item = projects.GetEnumerator();
            while (item.MoveNext())
            {
                var project = item.Current as Project;
                if (project == null)
                {
                    continue;
                }

                if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
                {
                    list.AddRange(GetSolutionFolderProjects(project));
                }
                else
                {
                    list.Add(project);
                }
            }

            return list;
        }

        private IEnumerable<Project> GetSolutionFolderProjects(Project solutionFolder)
        {
            List<Project> list = new List<Project>();
            for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
            {
                var subProject = solutionFolder.ProjectItems.Item(i).SubProject;
                if (subProject == null)
                {
                    continue;
                }

                if (subProject.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
                {
                    list.AddRange(GetSolutionFolderProjects(subProject));
                }
                else
                {
                    list.Add(subProject);
                }
            }
            return list;
        }
    }
}
