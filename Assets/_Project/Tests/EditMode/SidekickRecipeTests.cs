using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using VampireSpawn.Game;

namespace VampireSpawn.Tests
{
    // Guards the recipe pipeline (docs/art/unit-visuals.md 3): every recipe is baked and up to date, every look resolves.
    public class SidekickRecipeTests
    {
        const string RecipeDir = "Assets/_Project/Art/Characters";

        static string Hex(byte[] b) => string.Concat(b.Select(x => x.ToString("x2")));

        static string[] Recipes() => Directory.Exists(RecipeDir) ? Directory.GetFiles(RecipeDir, "*.sk").Select(Path.GetFileNameWithoutExtension).ToArray() : new string[0];

        [TestCaseSource(nameof(Recipes))]
        public void RecipeIsBakedAndUpToDate(string name)
        {
            const string fix = " Run /synty-reload (bash tools/assets/sidekick-bake.sh).";
            Assert.IsTrue(File.Exists(UnitLook.BakedPath(name)), name + " is not baked." + fix);
            using (var sha = SHA1.Create())
                Assert.AreEqual(Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(File.ReadAllText(RecipeDir + "/" + name + ".sk")))),
                    File.ReadAllText(UnitLook.BakedFolder + "/" + name + "/recipe.sha1").Trim(), name + " changed after it was baked." + fix);
        }

        [Test]
        public void EveryUnitLookResolves()
        {
            var recipes = Recipes();
            foreach (var guid in AssetDatabase.FindAssets("t:UnitLook"))
            {
                var look = AssetDatabase.LoadAssetAtPath<UnitLook>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.IsNotNull(look.model, look.name + " has no model (with a recipe it is the fallback for unbaked clones)");
                if (!string.IsNullOrEmpty(look.recipe)) CollectionAssert.Contains(recipes, look.recipe, look.name + " names a recipe that is not in " + RecipeDir);
            }
        }
    }
}
