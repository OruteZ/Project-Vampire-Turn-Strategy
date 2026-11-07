using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VampireSpawn.Editor
{
    // /synty-reload without the Unity CLI (docs/guides/asset-setup.md): import missing Synty packages, then bake Sidekick recipes.
    // The baker references Synty assemblies, so it lives outside Assets and is copied under Assets/Synty (gitignored, present
    // only with Synty) before it runs; this assembly reaches it by name and still compiles on a clone without Synty.
    [InitializeOnLoad]
    public static class SyntyReload
    {
        const string Template = "tools/assets/SidekickBaker.cs", Installed = "Assets/Synty/_VampireSpawn/Editor/SidekickBaker.cs";
        const string Required = "tools/assets/required.tsv", Packages = "UPackage", BakerType = "VampireSpawn.SyntyLocal.SidekickBaker";
        const string Pending = "VampireSpawn.SyntyReload.Pending", PendingForce = "VampireSpawn.SyntyReload.Force", Last = "VampireSpawn.SyntyReload.Last";

        public static string LastResult => SessionState.GetString(Last, "");

        static SyntyReload()
        {
            if (SessionState.GetBool(Pending, false)) EditorApplication.update += BakeWhenReady;
        }

        static void BakeWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= BakeWhenReady;
            SessionState.SetBool(Pending, false);
            Report(Bake("", SessionState.GetBool(PendingForce, false)));
        }

        public static void Run(bool force)
        {
            if (EditorApplication.isPlaying) { Report("재생을 멈춘 뒤 다시 누르세요."); return; }
            var imported = ImportMissing();
            if (!Directory.Exists("Assets/Synty/SidekickCharacters"))
            {
                Report(imported + "\nSIDEKICK 패키지가 없어 캐릭터를 굽지 못했습니다. UPackage/ 에 패키지를 넣고 다시 누르세요 (docs/guides/asset-setup.md).");
                return;
            }
            if (InstallBaker())
            {
                SessionState.SetBool(Pending, true);
                SessionState.SetBool(PendingForce, force);
                Report(imported + "\n굽기 도구를 설치했습니다. 컴파일이 끝나면 굽습니다.");
                AssetDatabase.Refresh();
                return;
            }
            Report(imported + "\n" + Bake("", force));
        }

        // Packages holding an asset that required.tsv lists but this project cannot resolve.
        public static string ImportMissing()
        {
            if (!File.Exists(Required)) return "required.tsv 없음";
            var need = File.ReadAllLines(Required).Skip(1).Select(l => l.Split('\t')).Where(c => c.Length >= 3)
                .Where(c => string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(c[0])) || !File.Exists(AssetDatabase.GUIDToAssetPath(c[0])))
                .Select(c => c[2]).Distinct().ToList();
            if (need.Count == 0) return "유료 에셋 준비됨.";
            var missing = need.Where(p => !File.Exists(Path.Combine(Packages, p))).ToList();
            foreach (var p in need.Except(missing)) AssetDatabase.ImportPackage(Path.Combine(Packages, p), false);
            return "임포트: " + string.Join(", ", need.Except(missing)) + (missing.Count > 0 ? "\nUPackage/ 에 없는 패키지: " + string.Join(", ", missing) : "");
        }

        // True when the copy changed, which means a recompile has to happen before Bake can find the type.
        public static bool InstallBaker()
        {
            var src = File.ReadAllText(Template);
            if (File.Exists(Installed) && File.ReadAllText(Installed) == src) return false;
            Directory.CreateDirectory(Path.GetDirectoryName(Installed));
            File.WriteAllText(Installed, src);
            return true;
        }

        public static string Bake(string only, bool force)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(BakerType)).FirstOrDefault(t => t != null);
            if (type == null) return "FAIL 굽기 도구가 컴파일되지 않았다 (" + Installed + "). 콘솔 오류를 본다.";
            return (string)type.GetMethod("BakeAll").Invoke(null, new object[] { only ?? "", force });
        }

        // Not a modal dialog: the button also runs in a background Editor driven by the CLI, where a dialog would block.
        static void Report(string result)
        {
            result = result.Replace(" | ", "\n");
            SessionState.SetString(Last, result);
            if (result.Contains("FAIL") || result.Contains("MISSING")) Debug.LogWarning("Synty 다시 불러오기: " + result);
            else Debug.Log("Synty 다시 불러오기: " + result);
            foreach (var w in Resources.FindObjectsOfTypeAll<ToolsWindow>()) w.Repaint();
        }
    }
}
