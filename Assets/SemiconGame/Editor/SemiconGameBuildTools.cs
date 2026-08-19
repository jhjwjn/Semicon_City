#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SemiconCity.Editor
{
    public static class SemiconGameBuildTools
    {
        private const string ScenePath = "Assets/SemiconGame/Scenes/SemiconCity_Playable.unity";
        private const string OutputPath = "Builds/Smoke/SemiconCitySmoke.exe";

        [MenuItem("Semicon City/Build Windows Smoke Player")]
        public static void BuildWindowsSmokePlayer()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath) ?? "Builds/Smoke");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled)
                    .Select(scene => scene.path).ToArray(),
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Windows 스모크 빌드 실패: {report.summary.result} / errors={report.summary.totalErrors}");
            }
            Debug.Log($"[Semicon Build] PASS / {OutputPath} / {report.summary.totalSize:N0} bytes");
        }
    }
}
#endif
