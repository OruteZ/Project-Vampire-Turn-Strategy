using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace VampireSpawn.Editor
{
    // Called by tools/build/build.sh: unity build --execute-method passes -buildOutput, the script passes BUILD_VERSION.
    public static class BuildScript
    {
        public static void Windows()
        {
            var args = Environment.GetCommandLineArgs();
            var output = args[Array.IndexOf(args, "-buildOutput") + 1];
            PlayerSettings.bundleVersion = Environment.GetEnvironmentVariable("BUILD_VERSION");
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var report = BuildPipeline.BuildPlayer(scenes, output, BuildTarget.StandaloneWindows64, BuildOptions.None);
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
