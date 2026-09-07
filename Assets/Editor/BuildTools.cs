using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SkeletonDefender.Editor
{
    public static class BuildTools
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";
        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            if (!Application.isBatchMode) EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ScenePath)) Prepare();
            };
        }
        [MenuItem("Skeleton Defender/Prepare project")]
        public static void Prepare()
        {
            PlayerSettings.companyName = "Skeleton Defender";
            PlayerSettings.productName = "Skeleton Defender";
            PlayerSettings.bundleVersion = "0.8.2";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 800;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.template = "PROJECT:Skeleton";
            PlayerSettings.WebGL.initialMemorySize = 64;
            PlayerSettings.WebGL.maximumMemorySize = 512;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "game.skeletondefender.prototype");
            QualitySettings.vSyncCount = 0;
            EditorSettings.serializationMode = SerializationMode.ForceText;
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory("Assets/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = PixelArt.C("101b22");
                cameraObject.AddComponent<AudioListener>(); cameraObject.transform.position = new Vector3(0, 0, -10);
                new GameObject("Skeleton Defender").AddComponent<SkeletonGame>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Skeleton Defender/Build Windows")]
        public static void BuildWindows() { Prepare(); RunBuildChecks(); BuildAutomation.ExportBalance(); Build(BuildTarget.StandaloneWindows64, "Builds/Windows/Skeleton Defender.exe"); }
        [MenuItem("Skeleton Defender/Build Browser")]
        public static void BuildWeb() { Prepare(); RunBuildChecks(); BuildAutomation.ExportBalance(); Build(BuildTarget.WebGL, "Builds/Web"); }
        [MenuItem("Skeleton Defender/Build Windows and Browser")]
        public static void BuildAll()
        {
            Prepare(); RunBuildChecks();
            Build(BuildTarget.StandaloneWindows64, "Builds/Windows/Skeleton Defender.exe");
            Build(BuildTarget.WebGL, "Builds/Web");
        }
        private static void RunBuildChecks()
        {
            // Explicit option for builds where the user wants to playtest personally.
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-skipSmokeTests") >= 0)
                Debug.Log("SKELETON_TESTS_SKIPPED: requested build without gameplay checks.");
            else SmokeTests.Run();
        }
        private static void Build(BuildTarget target, string location)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(location));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = location, target = target, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + report.summary.result);
            string outputDirectory = target == BuildTarget.WebGL ? location : Path.GetDirectoryName(location);
            File.Copy("Assets/Resources/Fonts/Ubuntu-LICENSE.txt", Path.Combine(outputDirectory, "Ubuntu-LICENSE.txt"), true);
            Debug.Log("SKELETON_BUILD_SUCCESS " + target + " " + report.summary.totalSize + " bytes");
        }
    }
}
