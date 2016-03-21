using System;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CommandTaskRunner
{
    class CommandHelpers
    {
        private static string[] _extensions = new[] { ".cmd", ".bat", ".ps1", ".psm1" };

        public static bool IsFileSupported(string file)
        {
            if (string.IsNullOrEmpty(file) || !file.Contains(":\\"))
                return false;

            string ext = Path.GetExtension(file).ToLowerInvariant();
            return _extensions.Contains(ext);
        }

        public static JObject GetJsonContent(string configPath, string file, string json = null)
        {
            json = json ?? (File.Exists(configPath) ? File.ReadAllText(configPath) : "{}");

            JObject root = GetJsonObject(configPath, json);
            AddCommandToRoot(file, configPath, root);

            return root;
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

        private static void AddCommandToRoot(string file, string configPath, JObject root)
        {
            JObject command = (JObject)root["commands"];

            var cmd = new
            {
                fileName = GetExecutableFileName(file),
                workingDirectory = ".",
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

            if (relative.Contains(' '))
                relative = $"\"{relative}\"";

            if (GetExecutableFileName(file) == "powershell.exe")
                return $"-ExecutionPolicy Bypass -NonInteractive -File {relative}";

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

        public static string MakeRelative(string baseFile, string file)
        {
            Uri baseUri = new Uri(baseFile, UriKind.RelativeOrAbsolute);
            Uri fileUri = new Uri(file, UriKind.RelativeOrAbsolute);

            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString());
        }
    }
}
