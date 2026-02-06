using System;
using System.IO;
using System.Reflection;

namespace Liversen.DependencyCop
{
    static class EmbeddedResourceHelpers
    {
        public static string GetAnalyzerTestData(Type testClass, string baseName)
        {
            return GetFromCallingAssembly($"{testClass.Namespace}.TestData.Analyzer.{baseName}.cs");
        }

        public static string GetFixProviderTestData(Type testClass, string baseName)
        {
            return GetFromCallingAssembly($"{testClass.Namespace}.TestData.FixProvider.{baseName}.cs");
        }

        static string GetFromCallingAssembly(string resourceName)
        {
            return Get(Assembly.GetCallingAssembly(), resourceName);
        }

        static string Get(Assembly assembly, string resourceName)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new ArgumentException($"Unable to find resource {resourceName} in assembly {assembly.FullName}");
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
