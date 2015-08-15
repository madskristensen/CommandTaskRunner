using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.JSON.Core.Schema;

namespace CommandTaskRunner
{
    [Export(typeof(IJSONSchemaSelector))]
    class CommandSchemaSelector : IJSONSchemaSelector
    {
        public event EventHandler AvailableSchemasChanged { add { } remove { } }

        public Task<IEnumerable<string>> GetAvailableSchemasAsync()
        {
            return Task.FromResult(Enumerable.Empty<string>());
        }

        public string GetSchemaFor(string fileLocation)
        {
            string fileName = Path.GetFileName(fileLocation);

            if (!fileName.Equals(Constants.FILENAME, StringComparison.OrdinalIgnoreCase))
                return null;

            string assembly = Assembly.GetExecutingAssembly().Location;
            string folder = Path.GetDirectoryName(assembly);
            return Path.Combine(folder, "schema\\commands-schema.json");
        }
    }
}
