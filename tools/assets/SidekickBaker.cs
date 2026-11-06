// Copied by VampireSpawn.Editor.SyntyReload into Assets/Synty/_VampireSpawn/Editor/ (gitignored) and compiled there,
// because it references Synty assemblies and a clone without Synty must still compile. Edit this file, not the copy.
// Bakes every Sidekick recipe (Assets/_Project/Art/Characters/*.sk) into Assets/_Generated/Resources/Characters/<name>/.
// UnitLook finds the result by recipe name through Resources (docs/art/unit-visuals.md 3).
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VampireSpawn.SyntyLocal
{
    public static class SidekickBaker
    {
        const string RecipeDir = "Assets/_Project/Art/Characters", OutDir = "Assets/_Generated/Resources/Characters";
        static string Hash(string text) { using var sha = System.Security.Cryptography.SHA1.Create(); return string.Concat(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text)).Select(b => b.ToString("x2"))); }
        static Texture2D Copy(Texture src)
        {
            var rtex = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(src, rtex);
            var prev = RenderTexture.active; RenderTexture.active = rtex;
            var t = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point };
            t.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0); t.Apply();
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rtex);
            return t;
        }

        public static string BakeAll(string only, bool force)
        {
            if (!System.IO.Directory.Exists(RecipeDir)) return "no recipes";
            var db = new Synty.SidekickCharacters.Database.DatabaseManager();
            var baseModel = Resources.Load<GameObject>("Meshes/SK_BaseModel");
            var baseMat = Resources.Load<Material>("Materials/M_BaseMaterial");
            if (baseModel == null || baseMat == null) return "FAIL Sidekick package not imported (Resources/Meshes/SK_BaseModel)";
            var maps = new[] { "_ColorMap", "_MetallicMap", "_SmoothnessMap", "_ReflectionMap", "_EmissionMap", "_OpacityMap" };
            var log = new List<string>();
            foreach (var file in System.IO.Directory.GetFiles(RecipeDir, "*.sk").OrderBy(f => f))
            {
                var text = System.IO.File.ReadAllText(file);
                var name = System.IO.Path.GetFileNameWithoutExtension(file);
                if (!string.IsNullOrEmpty(only) && only != name) continue;
                var dir = OutDir + "/" + name;
                var prefabPath = dir + "/" + name + ".prefab";
                var hashPath = dir + "/recipe.sha1";
                if (!force && System.IO.File.Exists(prefabPath) && System.IO.File.Exists(hashPath) && System.IO.File.ReadAllText(hashPath).Trim() == Hash(text)) { log.Add(name + ": up to date"); continue; }
                var sc = new Unity.VisualScripting.YamlDotNet.Serialization.Deserializer().Deserialize<Synty.SidekickCharacters.Serialization.SerializedCharacter>(text);
                var mat = new Material(baseMat) { name = "M_" + name };
                var texs = new Dictionary<string, Texture2D>();
                foreach (var m in maps) if (baseMat.HasProperty(m) && baseMat.GetTexture(m) != null) { texs[m] = Copy(baseMat.GetTexture(m)); mat.SetTexture(m, texs[m]); }
                var rt = new Synty.SidekickCharacters.API.SidekickRuntime(baseModel, mat, null, db);
                var task = Synty.SidekickCharacters.API.SidekickRuntime.PopulateToolData(rt);
                while (!task.IsCompleted) System.Threading.Thread.Sleep(20);
                if (task.IsFaulted) return "FAIL " + name + ": tool data " + task.Exception.GetBaseException().Message;
                var set = sc.ColorSet.CreateSidekickColorSet(db);
                var missing = new List<string>();
                foreach (var row in sc.ColorRows ?? new List<Synty.SidekickCharacters.Serialization.SerializedColorRow>())
                {
                    var r = row.CreateSidekickColorRow(db, set);
                    if (r?.ColorProperty == null) continue;
                    if (!string.IsNullOrEmpty(row.MainColor)) rt.UpdateColor(Synty.SidekickCharacters.Enums.ColorType.MainColor, r);
                    if (!string.IsNullOrEmpty(row.Metallic)) rt.UpdateColor(Synty.SidekickCharacters.Enums.ColorType.Metallic, r);
                    if (!string.IsNullOrEmpty(row.Smoothness)) rt.UpdateColor(Synty.SidekickCharacters.Enums.ColorType.Smoothness, r);
                    if (!string.IsNullOrEmpty(row.Reflection)) rt.UpdateColor(Synty.SidekickCharacters.Enums.ColorType.Reflection, r);
                    if (!string.IsNullOrEmpty(row.Emission)) rt.UpdateColor(Synty.SidekickCharacters.Enums.ColorType.Emission, r);
                    if (!string.IsNullOrEmpty(row.Opacity)) rt.UpdateColor(Synty.SidekickCharacters.Enums.ColorType.Opacity, r);
                }
                var parts = new List<SkinnedMeshRenderer>();
                foreach (var p in sc.Parts)
                {
                    var sp = Synty.SidekickCharacters.Database.DTO.SidekickPart.SearchForByName(db, p.Name);
                    var smr = sp?.GetPartModel()?.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (smr == null) missing.Add(p.Name); else parts.Add(smr);
                }
                if (parts.Count == 0) return "FAIL " + name + ": no parts found";
                var bs = sc.BlendShapes ?? new Synty.SidekickCharacters.Serialization.SerializedBlendShapeValues();
                rt.BodyTypeBlendValue = bs.BodyTypeValue;
                rt.BodySizeHeavyBlendValue = bs.BodySizeValue > 0 ? bs.BodySizeValue : 0;
                rt.BodySizeSkinnyBlendValue = bs.BodySizeValue < 0 ? -bs.BodySizeValue : 0;
                rt.MusclesBlendValue = bs.MuscleValue;
                var go = rt.CreateCharacter(name, parts, true, true);
                System.IO.Directory.CreateDirectory(dir);
                foreach (var kv in texs)
                {
                    var png = dir + "/T_" + name + kv.Key + ".png";
                    System.IO.File.WriteAllBytes(png, kv.Value.EncodeToPNG());
                    UnityEditor.AssetDatabase.ImportAsset(png, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
                    var imp = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(png);
                    imp.filterMode = FilterMode.Point; imp.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed; imp.mipmapEnabled = false; imp.sRGBTexture = kv.Key == "_ColorMap";
                    imp.SaveAndReimport();
                    mat.SetTexture(kv.Key, UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(png));
                }
                var matPath = dir + "/M_" + name + ".mat";
                UnityEditor.AssetDatabase.DeleteAsset(matPath);
                UnityEditor.AssetDatabase.CreateAsset(mat, matPath);
                var meshPath = dir + "/SK_" + name + ".asset";
                UnityEditor.AssetDatabase.DeleteAsset(meshPath);
                foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mesh = UnityEngine.Object.Instantiate(smr.sharedMesh); mesh.name = "SK_" + name;
                    UnityEditor.AssetDatabase.CreateAsset(mesh, meshPath);
                    smr.sharedMesh = mesh;
                    smr.sharedMaterials = Enumerable.Repeat(mat, smr.sharedMaterials.Length).ToArray();
                }
                UnityEditor.PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                UnityEngine.Object.DestroyImmediate(go);
                System.IO.File.WriteAllText(hashPath, Hash(text));
                log.Add(name + ": baked parts=" + parts.Count + (missing.Count > 0 ? " MISSING " + string.Join(",", missing) : ""));
            }
            UnityEditor.AssetDatabase.SaveAssets();
            return string.Join(" | ", log);
        }
    }
}
