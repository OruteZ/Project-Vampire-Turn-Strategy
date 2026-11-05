using System.Linq;
using NUnit.Framework;
using VampireSpawn.Core;

namespace VampireSpawn.Tests
{
    public class SmokeTests
    {
        [Test]
        public void CoreAssemblyHasNoUnityReference()
        {
            var unityRefs = typeof(Rules).Assembly.GetReferencedAssemblies()
                .Select(a => a.Name)
                .Where(n => n.StartsWith("UnityEngine") || n.StartsWith("UnityEditor"))
                .ToArray();
            Assert.That(unityRefs, Is.Empty, "VampireSpawn.Core must run without Unity: " + string.Join(", ", unityRefs));
        }
    }
}
