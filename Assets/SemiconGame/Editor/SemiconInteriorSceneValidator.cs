#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SemiconCity.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SemiconCity.Editor
{
    public static class SemiconInteriorSceneValidator
    {
        private const string WorldScenePath = "Assets/SemiconGame/Scenes/SemiconCity_Playable.unity";

        [MenuItem("Semicon City/Validate Separate Interior Scenes")]
        public static void ValidateInteriorScenes()
        {
            var errors = new List<string>();
            var enabledScenes = EditorBuildSettings.scenes.Where(scene => scene.enabled)
                .Select(scene => scene.path).ToHashSet();
            var interiorPaths = SemiconInteriorSceneBuilder.GetAllScenePaths();
            if (!enabledScenes.Contains(WorldScenePath)) errors.Add("외부 월드 씬이 Build Settings에 없습니다.");
            foreach (var path in interiorPaths)
            {
                if (!File.Exists(path)) errors.Add($"내부 씬 파일 없음: {path}");
                if (!enabledScenes.Contains(path)) errors.Add($"Build Settings 누락: {path}");
            }

            if (errors.Count == 0)
            {
                var world = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);
                ValidateCommon(world, errors, false);
                var entrances = UnityEngine.Object.FindObjectsByType<SemiconScenePortal>(FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
                if (entrances.Length < 11) errors.Add($"외부 입장 문 부족: {entrances.Length}/11");
                foreach (var expected in new[]
                         {
                             "Process 01 Building Entrance", "Process 02 Building Entrance",
                             "Process 03 Building Entrance", "Process 04 Building Entrance",
                             "Process 05 Building Entrance", "Process 06 Building Entrance",
                             "Process 07 Building Entrance", "Process 08 Building Entrance",
                             "Factory Visitor Entrance", "Materials Hall Entrance", "FAB Workspace Entrance"
                         })
                {
                    if (!entrances.Any(portal => portal.name == expected)) errors.Add($"외부 문 누락: {expected}");
                }

                foreach (var path in interiorPaths)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    ValidateCommon(scene, errors, true);
                    if (GameObject.Find("PLACEHOLDER FLOOR - REPLACE WITH MODELED INTERIOR") == null)
                        errors.Add($"{scene.name}: 플레이스홀더 바닥 없음");
                    if (GameObject.Find("EXIT DOOR CUBE - REPLACE WITH MODELED DOOR") == null)
                        errors.Add($"{scene.name}: 출구 큐브 없음");
                    ValidateExpectedMachine(scene, errors);
                }
            }

            EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);
            if (errors.Count > 0)
                throw new InvalidOperationException("분리형 내부 씬 검증 실패:\n- " + string.Join("\n- ", errors));
            Debug.Log($"[Semicon Interior Validate] PASS / world doors=11 / interiors={interiorPaths.Count} / " +
                      "floor+machine+exit+UI linkage complete");
        }

        private static void ValidateCommon(Scene scene, ICollection<string> errors, bool interior)
        {
            Require<SemiconGameState>(scene, errors);
            Require<SemiconSceneArrival>(scene, errors);
            Require<SemiconPlayerController>(scene, errors);
            Require<SemiconPlayerInteractor>(scene, errors);
            Require<SemiconThirdPersonCamera>(scene, errors);
            Require<SemiconHud>(scene, errors);
            if (interior) Require<SemiconScenePortal>(scene, errors);
            foreach (var root in scene.GetRootGameObjects())
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                var missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                if (missing > 0) errors.Add($"{scene.name}: {transform.name} missing script x{missing}");
            }
        }

        private static void ValidateExpectedMachine(Scene scene, ICollection<string> errors)
        {
            var name = scene.name;
            if (name.Contains("01_Wafer")) Require<SemiconProductionMachine>(scene, errors);
            else if (name.Contains("02_Oxidation")) Require<SemiconOxidationTerminal>(scene, errors);
            else if (name.Contains("03_Photo")) Require<SemiconInteractionTerminal>(scene, errors);
            else if (name.Contains("04_Etch")) Require<SemiconEtchTerminal>(scene, errors);
            else if (name.Contains("05_Deposition")) Require<SemiconDepositionTerminal>(scene, errors);
            else if (name.Contains("06_Metal")) Require<SemiconMetalTerminal>(scene, errors);
            else if (name.Contains("07_EDS")) Require<SemiconEdsTerminal>(scene, errors);
            else if (name.Contains("08_Package")) Require<SemiconPackageTerminal>(scene, errors);
            else if (name.Contains("Factory"))
            {
                Require<SemiconProductionMachine>(scene, errors);
                Require<SemiconFactorySlotTerminal>(scene, errors);
            }
            else if (name.Contains("Market"))
            {
                Require<SemiconMarketTerminal>(scene, errors);
                Require<SemiconContractTerminal>(scene, errors);
            }
            else if (name.Contains("Workspace")) Require<SemiconArchiveTerminal>(scene, errors);
        }

        private static void Require<T>(Scene scene, ICollection<string> errors) where T : Component
        {
            if (UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include) == null)
                errors.Add($"{scene.name}: {typeof(T).Name} 없음");
        }
    }
}
#endif
