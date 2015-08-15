using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CommandTaskRunner
{
    class TaskParser
    {
        public static IEnumerable<CommandTask> LoadTasks(string configPath)
        {
            List<CommandTask> list = new List<CommandTask>();

            try
            {
                string document = File.ReadAllText(configPath);
                JObject root = JObject.Parse(document);

                var commandNode = root["commands"];

                foreach (var child in commandNode.Children<JProperty>())
                {
                    var command = JsonConvert.DeserializeObject<CommandTask>(child.Value.ToString());
                    command.Name = child.Name;
                    command.WorkingDirectory = MakeAbsolute(configPath, command.WorkingDirectory);
                    list.Add(command);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return null;
            }

            return list;
        }

        public static string MakeAbsolute(string baseFile, string file)
        {
            if (File.Exists(file))
                return file;

            file = file.TrimStart('.', '/', '\'');

            string folder = Path.GetDirectoryName(baseFile);

            return Path.Combine(folder, file);
        }
    }
}
