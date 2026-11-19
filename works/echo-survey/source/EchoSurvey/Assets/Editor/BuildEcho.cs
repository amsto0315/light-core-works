using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EchoSurvey.Editor
{
    public static class BuildEcho
    {
        [MenuItem("Echo Survey/Build Windows")]
        public static void BuildFromMenu()
        {
            BuildCI();
        }

        public static void BuildCI()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            const string scenePath = "Assets/Scenes/Main.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };

            PlayerSettings.companyName = "Echo Survey";
            PlayerSettings.productName = "Echo Survey";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);

            var outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/EchoSurvey"));
            Directory.CreateDirectory(outDir);
            var outPath = Path.Combine(outDir, "EchoSurvey.exe");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = outPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });

            Debug.Log($"[ECHO] build={report.summary.result} size={report.summary.totalSize} errors={report.summary.totalErrors}");
            if (Application.isBatchMode)
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
