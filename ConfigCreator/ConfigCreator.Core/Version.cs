using System;
using System.Reflection;

namespace ConfigCreator.Core
{
    internal class Version
    {
        public string V { get; }
        public string Author { get; } = "Wes Stillwell - stillwellstudios.com";
        public string GenerationDate { get; } = DateTime.Now.ToString();

        public Version()
        {
            // Get the assembly version (which is set at compile time by our csproj target).
            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
            V = assemblyVersion != null ? assemblyVersion.ToString() : "1.0.0";
        }
    }
}
