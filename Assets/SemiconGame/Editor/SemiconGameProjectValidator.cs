#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SemiconCity.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SemiconCity.Editor
{
    public static class SemiconGameProjectValidator
    {
        private const string ScenePath = "Assets/SemiconGame/Scenes/SemiconCity_Playable.unity";
        private const string ValidationFolder = "Assets/SemiconGame/Validation";

        [MenuItem("Semicon City/Validate And Capture First Playable")]
        public static void ValidateAndCapture()
        {
            if (!File.Exists(ScenePath))
            {
                throw new FileNotFoundException("플레이 씬이 없습니다.", ScenePath);
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var errors = new List<string>();
            Require<SemiconGameState>(scene, errors);
            Require<SemiconPlayerController>(scene, errors);
            Require<SemiconPlayerInteractor>(scene, errors);
            Require<SemiconThirdPersonCamera>(scene, errors);
            Require<SemiconHud>(scene, errors);
            Require<SemiconFirstTutorial>(scene, errors);
            Require<PhotoExperimentPanel>(scene, errors);
            Require<SemiconInteractionTerminal>(scene, errors);
            Require<OxidationExperimentPanel>(scene, errors);
            Require<SemiconOxidationTerminal>(scene, errors);
            Require<EtchExperimentPanel>(scene, errors);
            Require<SemiconEtchTerminal>(scene, errors);
            Require<DepositionExperimentPanel>(scene, errors);
            Require<SemiconDepositionTerminal>(scene, errors);
            Require<MetalExperimentPanel>(scene, errors);
            Require<SemiconMetalTerminal>(scene, errors);
            Require<EdsExperimentPanel>(scene, errors);
            Require<SemiconEdsTerminal>(scene, errors);
            Require<PackageExperimentPanel>(scene, errors);
            Require<SemiconPackageTerminal>(scene, errors);
            Require<SemiconMarketPanel>(scene, errors);
            Require<SemiconMarketTerminal>(scene, errors);
            Require<SemiconContractPanel>(scene, errors);
            Require<SemiconContractTerminal>(scene, errors);
            Require<SemiconArchivePanel>(scene, errors);
            Require<SemiconArchiveTerminal>(scene, errors);
            Require<SemiconProductionPanel>(scene, errors);
            Require<SemiconProductionMachine>(scene, errors);
            Require<SemiconFactoryLoadoutPanel>(scene, errors);
            Require<SemiconFactorySlotTerminal>(scene, errors);
            Require<SemiconFallRecovery>(scene, errors);
            Require<SemiconScenePortal>(scene, errors);
            Require<EventSystem>(scene, errors);

            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null || !camera.CompareTag("MainCamera"))
            {
                errors.Add("MainCamera 태그가 지정된 게임 카메라가 없습니다.");
            }
            var player = UnityEngine.Object.FindFirstObjectByType<SemiconPlayerController>();
            var terminal = UnityEngine.Object.FindFirstObjectByType<SemiconInteractionTerminal>();
            if (player != null && terminal != null)
            {
                var distance = Vector3.Distance(player.transform.position, terminal.transform.position);
                if (distance > 8f)
                {
                    errors.Add($"플레이어와 포토 단말기의 초기 거리가 너무 멉니다: {distance:0.00}m");
                }
                Debug.Log($"[Semicon Validate] Player={player.transform.position} / Terminal={terminal.transform.position} / Distance={distance:0.00}m");
            }

            ValidateSerializedReferences<UnityEngine.Object>(UnityEngine.Object.FindFirstObjectByType<PhotoExperimentPanel>(), errors);
            ValidateSerializedReferences<UnityEngine.Object>(UnityEngine.Object.FindFirstObjectByType<OxidationExperimentPanel>(), errors);
            ValidateSerializedReferences<UnityEngine.Object>(UnityEngine.Object.FindFirstObjectByType<EtchExperimentPanel>(), errors);
            ValidateSerializedReferences<UnityEngine.Object>(UnityEngine.Object.FindFirstObjectByType<DepositionExperimentPanel>(), errors);
            ValidateSerializedReferences<UnityEngine.Object>(UnityEngine.Object.FindFirstObjectByType<MetalExperimentPanel>(), errors);
            ValidateSerializedReferences<UnityEngine.Object>(UnityEngine.Object.FindFirstObjectByType<EdsExperimentPanel>(), errors);
            ValidateSerializedReferences<UnityEngine.Object>(UnityEngine.Object.FindFirstObjectByType<PackageExperimentPanel>(), errors);
            ValidateSerializedReferences<UnityEngine.Object>(UnityEngine.Object.FindFirstObjectByType<SemiconMarketPanel>(), errors);
            ValidateSerializedReferences<UnityEngine.Object>(UnityEngine.Object.FindFirstObjectByType<SemiconContractPanel>(FindObjectsInactive.Include), errors);
            ValidateSerializedReferences<UnityEngine.Object>(UnityEngine.Object.FindFirstObjectByType<SemiconArchivePanel>(FindObjectsInactive.Include), errors);
            ValidateSerializedReferences<UnityEngine.Object>(UnityEngine.Object.FindFirstObjectByType<SemiconProductionPanel>(FindObjectsInactive.Include), errors);
            ValidateSerializedReferences<UnityEngine.Object>(UnityEngine.Object.FindFirstObjectByType<SemiconFactoryLoadoutPanel>(FindObjectsInactive.Include), errors);
            ValidateSerializedReferences<UnityEngine.Object>(UnityEngine.Object.FindFirstObjectByType<SemiconHud>(), errors);
            ValidateSerializedReferences<UnityEngine.Object>(UnityEngine.Object.FindFirstObjectByType<SemiconFirstTutorial>(), errors);

            var marketTerminal = UnityEngine.Object.FindFirstObjectByType<SemiconMarketTerminal>(FindObjectsInactive.Include);
            var marketEntrance = FindTransform("Materials_Main_Entrance_Level_Threshold");
            var marketPortal = FindTransform("Materials Hall Entrance");
            if (marketPortal != null && marketEntrance != null)
            {
                var marketDistance = Vector3.Distance(marketPortal.position, marketEntrance.position);
                if (marketDistance > 4f)
                {
                    errors.Add($"마켓 입장 포털이 자재동 정문에서 너무 멉니다: {marketDistance:0.00}m");
                }
                Debug.Log($"[Semicon Validate] Market Portal={marketPortal.position} / Entrance={marketEntrance.position} / Distance={marketDistance:0.00}m");
            }
            if (marketTerminal != null && marketTerminal.transform.position.z < 150f)
            {
                errors.Add("마켓 거래 단말기가 전용 실내에 배치되지 않았습니다.");
            }
            if (FindTransform("First Order Button") == null || FindTransform("First Order Status") == null)
            {
                errors.Add("마켓 첫 주문 UI가 구성되지 않았습니다.");
            }
            if (FindTransform("Contract 01 Button") == null || FindTransform("Archive Tab 01") == null ||
                FindTransform("Select PM-10 Recipe Button") == null || FindTransform("Select DD-20 Recipe Button") == null)
            {
                errors.Add("후속 계약·도감·제품 변형 UI가 구성되지 않았습니다.");
            }

            var portals = UnityEngine.Object.FindObjectsByType<SemiconScenePortal>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (portals.Length < 4)
            {
                errors.Add($"실내 출입 포털 부족: {portals.Length}/4");
            }

            var factorySlots = UnityEngine.Object.FindObjectsByType<SemiconFactorySlotTerminal>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            if (factorySlots.Length != SemiconFactoryDefinitions.SlotCount)
            {
                errors.Add($"공장 설비 슬롯 수 불일치: {factorySlots.Length}/{SemiconFactoryDefinitions.SlotCount}");
            }

            ValidateWalkablePhysics(errors);

            if (errors.Count > 0)
            {
                throw new InvalidOperationException("첫 플레이 검증 실패:\n- " + string.Join("\n- ", errors));
            }

            EnsureFolder(ValidationFolder);
            Capture(camera, false, false, ValidationFolder + "/world-preview.png");
            Capture(camera, true, false, ValidationFolder + "/photo-experiment-preview.png");
            Capture(camera, false, true, ValidationFolder + "/materials-exchange-preview.png");
            AssetDatabase.Refresh();
            Debug.Log("[Semicon Validate] PASS / 8대 공정·후속 계약 9종·FAB ARCHIVE·생산·이동 표면 연결 완료");
        }

        private static void ValidateWalkablePhysics(ICollection<string> errors)
        {
            Physics.SyncTransforms();
            var safetyGround = FindTransform("World Safety Ground");
            if (safetyGround == null || safetyGround.GetComponent<BoxCollider>() == null)
            {
                errors.Add("월드 접합부 안전 지면이 없습니다.");
            }

            var samples = new[]
            {
                new Vector3(0f, 0.8f, 64f),
                new Vector3(0f, 0.8f, -58f),
                new Vector3(-44f, 0.8f, -6f),
                new Vector3(48f, 0.8f, -6f),
                new Vector3(40.8f, 0.8f, -13.9f),
                new Vector3(48.4f, 0.8f, -17.87f),
                new Vector3(-15.38f, 0.8f, -43.1f),
                new Vector3(1.56f, 0.8f, -68.4f)
            };
            foreach (var sample in samples)
            {
                if (!Physics.Raycast(sample, Vector3.down, out var hit, 1.5f, ~0, QueryTriggerInteraction.Ignore))
                {
                    errors.Add($"이동 표면 레이캐스트 실패: {sample}");
                    continue;
                }
                Debug.Log($"[Semicon Physics Validate] {sample.x:0.0},{sample.z:0.0} -> {hit.collider.name} y={hit.point.y:0.00}");
            }
        }

        private static void Capture(Camera camera, bool showExperiment, bool showMarket, string path)
        {
            if (camera == null)
            {
                throw new InvalidOperationException("렌더링할 카메라가 없습니다.");
            }

            const int width = 1920;
            const int height = 1080;
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            var experiment = FindTransform("Photo Experiment Screen");
            if (experiment != null)
            {
                experiment.gameObject.SetActive(showExperiment);
                var group = experiment.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    group.alpha = 1f;
                    group.interactable = showExperiment;
                    group.blocksRaycasts = showExperiment;
                }
            }
            var market = FindTransform("Materials Exchange Screen");
            if (market != null)
            {
                market.gameObject.SetActive(true);
                var group = market.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    group.alpha = showMarket ? 1f : 0f;
                    group.interactable = showMarket;
                    group.blocksRaycasts = showMarket;
                }
            }

            ApplyCaptureFont(canvas);
            var originalRenderMode = canvas != null ? canvas.renderMode : RenderMode.ScreenSpaceOverlay;
            var originalCamera = canvas != null ? canvas.worldCamera : null;
            var originalPlaneDistance = canvas != null ? canvas.planeDistance : 100f;
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 0.7f;
            }

            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1,
                name = "Semicon Validation Capture"
            };
            if (!target.Create())
            {
                UnityEngine.Object.DestroyImmediate(target);
                throw new InvalidOperationException("검증용 RenderTexture를 생성하지 못했습니다.");
            }
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousActive = RenderTexture.active;
            var previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Debug.Log($"[Semicon Validate] Capture={path}");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                if (canvas != null)
                {
                    canvas.renderMode = originalRenderMode;
                    canvas.worldCamera = originalCamera;
                    canvas.planeDistance = originalPlaneDistance;
                }
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void ApplyCaptureFont(Canvas canvas)
        {
            if (canvas == null)
            {
                return;
            }
            var font = Font.CreateDynamicFontFromOSFont(
                new[] { "Noto Sans KR", "Malgun Gothic", "맑은 고딕", "Arial" }, 28);
            if (font == null)
            {
                return;
            }
            foreach (var label in canvas.GetComponentsInChildren<Text>(true))
            {
                label.font = font;
            }
        }

        private static void Require<T>(Scene scene, ICollection<string> errors) where T : UnityEngine.Object
        {
            var found = UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (found == null)
            {
                errors.Add($"필수 구성요소 누락: {typeof(T).Name} ({scene.name})");
            }
        }

        private static void ValidateSerializedReferences<T>(UnityEngine.Object target, ICollection<string> errors)
        {
            if (target == null)
            {
                return;
            }
            var serializedObject = new SerializedObject(target);
            var iterator = serializedObject.GetIterator();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyType == SerializedPropertyType.ObjectReference &&
                    iterator.propertyPath != "m_Script" && iterator.objectReferenceValue == null)
                {
                    errors.Add($"{target.GetType().Name} 참조 누락: {iterator.propertyPath}");
                }
            }
        }

        private static Transform FindTransform(string name)
        {
            return Resources.FindObjectsOfTypeAll<Transform>()
                .FirstOrDefault(item => item.gameObject.scene.IsValid() && item.name == name);
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }
                current = next;
            }
        }
    }
}
#endif
