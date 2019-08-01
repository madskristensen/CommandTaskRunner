namespace CommandTaskRunner
{
    class MSBuildProject
    {
        public static void SetVariables(string projectFile, ref string str)
        {
            MSBuildProject project = new MSBuildProject(projectFile);
            try
            {
                project.SetVariables(ref str);
            }
            finally
            {
                project.Unload();
            }
        }

        private MSBuildProject(string projectFile)
        {
            try
            {
                project = new Microsoft.Build.Evaluation.Project(projectFile);
            }
            catch (System.Exception)
            {
            }
        }

        private void SetVariables(ref string str)
        {
            if (project == null || project.Properties == null)
                return;

            foreach (Microsoft.Build.Evaluation.ProjectProperty p in project.Properties)
            {
                try
                {
                    ApplyVariable("$(" + p.Name + ")", p.EvaluatedValue, ref str);
                }
                catch (System.Exception)
                {
                }
            }
        }

        private void ApplyVariable(string key, string value, ref string str)
        {
            str = str.Replace(key, value);
        }

        private void Unload()
        {
            if (project != null)
                project.ProjectCollection.UnloadProject(project);
        }

        private Microsoft.Build.Evaluation.Project project;
    }
}
