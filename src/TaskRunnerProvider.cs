using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.TaskRunnerExplorer;
using ProjectTaskRunner.Helpers;

namespace CommandTaskRunner
{
    [TaskRunnerExport(Constants.FILENAME)]
    class TaskRunnerProvider : ITaskRunner
    {
        private ImageSource _icon;
        private static Dictionary<string, int> _cache = new Dictionary<string, int>();

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

                return new TaskRunnerConfig(context, hierarchy, _icon);
            });
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
                string cwd = command.WorkingDirectory ?? rootDir;
                _cache[command.Name] = _cache.ContainsKey(command.Name) ? _cache[command.Name] + 1 : 0;

                TaskRunnerNode task = new TaskRunnerNode($"{command.Name} ({_cache[command.Name]})", true)
                {
                    Command = new TaskRunnerCommand(cwd, command.FileName, command.Arguments),
                    Description = command.Arguments
                };

                tasks.Children.Add(task);
            }

            return root;
        }
    }
}
