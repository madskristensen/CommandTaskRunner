using System.Windows.Media;
using CommandTaskRunner;
using Microsoft.VisualStudio.TaskRunnerExplorer;

namespace ProjectTaskRunner.Helpers
{
    internal class TaskRunnerConfig : TaskRunnerConfigBase
    {
        private ImageSource _rootNodeIcon;

        public TaskRunnerConfig(ITaskRunnerNode hierarchy, ImageSource rootNodeIcon)
            : base(hierarchy)
        {
            _rootNodeIcon = rootNodeIcon;
        }

        protected override ImageSource LoadRootNodeIcon()
        {
            return _rootNodeIcon;
        }
    }
}
