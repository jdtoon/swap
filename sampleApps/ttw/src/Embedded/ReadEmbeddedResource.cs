using System;
using System.IO;
using System.Reflection;

namespace ttw.Embedded
{
    public static class EmbeddedResourceReader
    {
        public static string ReadEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"Resource '{resourceName}' not found.");
                }
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
