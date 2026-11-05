using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace VampireSpawn.Tests
{
    // Paid Synty assets are not in git; tools/assets/required.tsv lists the ones this repo references (docs/guides/asset-setup.md).
    public class RequiredAssetsTests
    {
        static string[][] Rows() =>
            File.ReadAllLines(Path.Combine(Directory.GetCurrentDirectory(), "tools", "assets", "required.tsv"))
                .Skip(1).Where(l => l.Length > 0).Select(l => l.Split('\t')).ToArray();

        [Test]
        public void EveryRequiredAssetResolves()
        {
            var bad = Rows()
                .Where(r => AssetDatabase.GUIDToAssetPath(r[0]) != r[1] || AssetDatabase.LoadMainAssetAtPath(r[1]) == null)
                .Select(r => r[1] + " (" + r[2] + ")").ToList();
            Assert.IsEmpty(bad, "유료 에셋이 빠졌다. docs/guides/asset-setup.md 대로 tools/assets/setup.sh 를 돌린다:\n" + string.Join("\n", bad));
        }

        [Test]
        public void ListCoversEverySyntyReference()
        {
            var listed = Rows().Select(r => r[1]).ToHashSet();
            var unlisted = AssetDatabase.FindAssets("", new[] { "Assets/_Project" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.EndsWith(".cs") && !AssetDatabase.IsValidFolder(p))
                .SelectMany(p => AssetDatabase.GetDependencies(p, false).Select(d => (p, d)))
                .Where(x => x.d.StartsWith("Assets/Synty/") && !listed.Contains(x.d))
                .Select(x => x.d + " <- " + x.p).Distinct().ToList();
            Assert.IsEmpty(unlisted, "tools/assets/required.tsv 가 낡았다. node tools/assets/scan.mjs 로 다시 뽑는다:\n" + string.Join("\n", unlisted));
        }
    }
}
