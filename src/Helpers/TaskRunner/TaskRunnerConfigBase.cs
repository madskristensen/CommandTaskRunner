using System;
using System.Windows.Media;
using Microsoft.VisualStudio.TaskRunnerExplorer;

namespace ProjectTaskRunner.Helpers
{
    internal abstract class TaskRunnerConfigBase : ITaskRunnerConfig
    {
        private static ImageSource SharedIcon;
        private BindingsPersister _bindingsPersister;

        protected TaskRunnerConfigBase(ITaskRunnerNode hierarchy)
        {
            _bindingsPersister = new BindingsPersister();
            TaskHierarchy = hierarchy;
        }

        /// <summary>
        /// TaskRunner icon
        /// </summary>
        public virtual ImageSource Icon => SharedIcon ?? (SharedIcon = LoadRootNodeIcon());

        public ITaskRunnerNode TaskHierarchy { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public string LoadBindings(string configPath)
        {
            try
            {
                return _bindingsPersister.Load(configPath);
            }
            catch
            {
                return "<binding />";
            }
        }

        public bool SaveBindings(string configPath, string bindingsXml)
        {
            try
            {
                return _bindingsPersister.Save(configPath, bindingsXml);
            }
            catch
            {
                return false;
            }
        }

        protected virtual void Dispose(bool isDisposing)
        {
        }

        protected virtual ImageSource LoadRootNodeIcon()
        {
            return null;
        }
    }
}
