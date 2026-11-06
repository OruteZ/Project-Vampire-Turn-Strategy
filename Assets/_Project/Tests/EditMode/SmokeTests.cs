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

        // Rules emit Message keys; a Korean literal in Core would bypass the string table. DataSet.cs is editor-only validation text.
        [Test]
        public void CoreHasNoKoreanText()
        {
            var literal = new Regex("\"[^\"\n]*[가-힣][^\"\n]*\"");
            var hits = Directory.GetFiles("Assets/_Project/Scripts/Core", "*.cs")
                .Where(f => Path.GetFileName(f) != "DataSet.cs")
                .SelectMany(f => File.ReadAllLines(f).Select((line, i) => (f, line, i)))
                .Where(x => !x.line.TrimStart().StartsWith("//") && literal.IsMatch(x.line))
                .Select(x => Path.GetFileName(x.f) + ":" + (x.i + 1))
                .ToArray();
            Assert.That(hits, Is.Empty, "Korean text in Core: " + string.Join(", ", hits));
        }
    }
}
