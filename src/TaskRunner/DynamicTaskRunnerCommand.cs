namespace CommandTaskRunner
{
    class DynamicTaskRunnerCommand : Microsoft.VisualStudio.TaskRunnerExplorer.ITaskRunnerCommand
    {
        public DynamicTaskRunnerCommand(TaskRunnerProvider provider, string rootDir, string workingDirectory, string exe, string args, string options = null)
        {
            this.provider = provider;
            this.rootDir = rootDir;

            Executable = exe;
            Args = args;
            WorkingDirectory = workingDirectory;
            Options = options;
        }

        public string Executable
        {
            get {
                return SetVariables(exe);
            }
            set {
                exe = value;
            }
        }

        public string Args
        {
            get {
                return SetVariables(args);
            }
            set {
                args = value;
            }
        }

        public string WorkingDirectory { get; }
        public string Options { get; set; }

        private string SetVariables(string str)
        {
            return provider.SetVariables(str, rootDir);
        }

        private string args;
        private string exe;

        private TaskRunnerProvider provider;
        private string rootDir;
    }

}
