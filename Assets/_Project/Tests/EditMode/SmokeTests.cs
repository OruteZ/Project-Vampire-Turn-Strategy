using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        // A Korean literal in player-facing code bypasses the string table (docs/areas/localization.md). DataSet.cs, logs and Inspector tooltips are editor-only text.
        [TestCase("Assets/_Project/Scripts/Core")]
        [TestCase("Assets/_Project/Scripts/Game/Screens")]
        public void PlayerTextComesFromTable(string folder)
        {
            var literal = new Regex("\"[^\"\n]*[가-힣][^\"\n]*\"");
            var hits = Directory.GetFiles(folder, "*.cs")
                .Where(f => Path.GetFileName(f) != "DataSet.cs")
                .SelectMany(f => File.ReadAllLines(f).Select((line, i) => (f, line, i)))
                .Where(x => !x.line.TrimStart().StartsWith("//") && !x.line.Contains("Debug.Log") && !x.line.Contains("[Tooltip(") && literal.IsMatch(x.line))
                .Select(x => Path.GetFileName(x.f) + ":" + (x.i + 1))
                .ToArray();
            Assert.That(hits, Is.Empty, "Korean text outside the string table: " + string.Join(", ", hits));
        }
    }
}
