#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SemiconCity.Game;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SemiconCity.Editor
{
    public static class SemiconGameProjectBuilder
    {
        private const string SourceScenePath = "Assets/Semicon_World.unity";
        private const string GameFolder = "Assets/SemiconGame";
        private const string GameSceneFolder = GameFolder + "/Scenes";
        private const string GameScenePath = GameSceneFolder + "/SemiconCity_Playable.unity";
        private const string MaterialFolder = GameFolder + "/Materials";

        private static readonly Color32 Navy = new Color32(3, 20, 27, 255);
        private static readonly Color32 NavySoft = new Color32(7, 38, 44, 250);
        private static readonly Color32 NavyPanel = new Color32(5, 48, 53, 250);
        private static readonly Color32 Teal = new Color32(31, 190, 185, 255);
        private static readonly Color32 Cyan = new Color32(42, 216, 211, 255);
        private static readonly Color32 Amber = new Color32(247, 169, 30, 255);
        private static readonly Color32 Bone = new Color32(232, 229, 215, 255);
        private static readonly Color32 Muted = new Color32(134, 164, 168, 255);
        private static readonly Color32 PhotoInk = new Color32(12, 43, 71, 255);
        private static readonly Color32 PhotoInkMuted = new Color32(48, 78, 99, 255);
        private static readonly Color32 PhotoBlue = new Color32(16, 139, 194, 255);
        private static readonly Color32 PhotoMint = new Color32(18, 150, 103, 255);
        private static readonly Color32 PhotoBorder = new Color32(148, 188, 203, 210);
        private static readonly Color32 PhotoGlass = new Color32(248, 251, 252, 238);
        private static readonly Color32 PhotoGlassSoft = new Color32(236, 245, 248, 222);
        private static readonly Color32 PhotoTrack = new Color32(164, 188, 199, 255);

        private static Font editorFont;

        [MenuItem("Semicon City/Build First Playable")]
        public static void BuildFirstPlayable()
        {
            EnsureTextMeshProEssentials();
            if (!File.Exists(SourceScenePath))
            {
                throw new FileNotFoundException($"기준 맵 씬을 찾지 못했습니다: {SourceScenePath}");
            }

            EnsureFolder(GameSceneFolder);
            EnsureFolder(MaterialFolder);

            var sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(sourceScene, GameScenePath, true))
            {
                throw new InvalidOperationException("게임용 씬 복사에 실패했습니다.");
            }

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var existingRoot = GameObject.Find("SEMICON_GAME");
            if (existingRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRoot);
            }

            DisableExistingCameras();

            var root = new GameObject("SEMICON_GAME");
            var systemsRoot = NewChild(root.transform, "Systems");
            var state = systemsRoot.AddComponent<SemiconGameState>();
            systemsRoot.AddComponent<SemiconSceneArrival>();
            systemsRoot.AddComponent<SemiconRuntimeSmokeTest>();
            NewChild(root.transform, "Interior Flow Smoke Runner").AddComponent<SemiconInteriorFlowSmokeTest>();

            BuildWorldCollision(root.transform);
            var canvas = BuildCanvas(root.transform, out var hud, out var photoPanel, out var oxidationPanel,
                out var etchPanel, out var depositionPanel, out var metalPanel, out var edsPanel, out var marketPanel,
                out var packagePanel, out var productionPanel, out var loadoutPanel, out var contractPanel,
                out var archivePanel);
            var researchEntrance = BuildResearchDistrict(root.transform, hud);
            var marketEntrance = BuildMarketDistrict(root.transform, hud);
            var factoryEntrance = BuildFactoryDistrict(root.transform, hud);
            BuildWorkspaceEntrance(root.transform, hud);
            var player = BuildPlayer(root.transform, researchEntrance.transform.position, out var playerController, out var interactor);
            var cameraController = BuildCamera(root.transform, player.transform, out var gameCamera);
            playerController.ConfigureCamera(gameCamera.transform);
            interactor.Configure(hud, playerController, cameraController);
            BuildFirstTutorial(root.transform, hud, playerController, null, null);

            BuildEventSystem(root.transform);
            AddRuntimeFont(canvas.gameObject);
            AddSceneInstructions(root.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            SemiconInteriorSceneBuilder.BuildAllInteriorScenes(GameScenePath);
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;

            Selection.activeGameObject = GameObject.Find("SEMICON_GAME");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Semicon City] 첫 플레이 버전 생성 완료: {GameScenePath} / State=Systems");
        }

        public static void ImportTextMeshProEssentialsBatch()
        {
            const string settingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(settingsPath) != null)
            {
                EditorApplication.Exit(0);
                return;
            }

            var packagePath = FindTextMeshProEssentialsPackage();
            AssetDatabase.importPackageCompleted += OnTextMeshProEssentialsImported;
            AssetDatabase.importPackageFailed += OnTextMeshProEssentialsImportFailed;
            AssetDatabase.ImportPackage(packagePath, false);
        }

        private static void OnTextMeshProEssentialsImported(string packageName)
        {
            AssetDatabase.importPackageCompleted -= OnTextMeshProEssentialsImported;
            AssetDatabase.importPackageFailed -= OnTextMeshProEssentialsImportFailed;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SaveAssets();
            Debug.Log("[Semicon TMP] Essential Resources import complete / " + packageName);
            EditorApplication.Exit(0);
        }

        private static void OnTextMeshProEssentialsImportFailed(string packageName, string errorMessage)
        {
            AssetDatabase.importPackageCompleted -= OnTextMeshProEssentialsImported;
            AssetDatabase.importPackageFailed -= OnTextMeshProEssentialsImportFailed;
            Debug.LogError("[Semicon TMP] Essential Resources import failed / " + packageName + " / " + errorMessage);
            EditorApplication.Exit(1);
        }

        private static void BuildWorldCollision(Transform parent)
        {
            var collisionRoot = NewChild(parent, "World Collision");
            var added = 0;
            foreach (var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if (filter == null || filter.sharedMesh == null || !filter.gameObject.scene.IsValid() ||
                    filter.GetComponent<Collider>() != null)
                {
                    continue;
                }

                var path = GetHierarchyPath(filter.transform);
                var renderer = filter.GetComponent<Renderer>();
                var bounds = renderer != null ? renderer.bounds : default;
                if (!ShouldAddWalkableCollider(path, filter.name, bounds))
                {
                    continue;
                }

                var boxCollider = filter.gameObject.AddComponent<BoxCollider>();
                var meshBounds = filter.sharedMesh.bounds;
                var colliderCenter = meshBounds.center;
                var colliderSize = meshBounds.size;
                if (colliderSize.y < 0.12f)
                {
                    colliderCenter.y -= (0.12f - colliderSize.y) * 0.5f;
                    colliderSize.y = 0.12f;
                }
                boxCollider.center = colliderCenter;
                boxCollider.size = colliderSize;
                added++;
            }

            // Imported spline and FBX surfaces can leave sub-centimetre seams.
            // This invisible floor sits just beneath the authored meshes and only
            // catches the controller after it has missed one of those seams.
            var safetyGround = NewChild(collisionRoot.transform, "World Safety Ground");
            safetyGround.transform.position = new Vector3(3f, -0.04f, 2f);
            var safetyCollider = safetyGround.AddComponent<BoxCollider>();
            safetyCollider.size = new Vector3(222f, 0.30f, 188f);

            Debug.Log($"[Semicon Physics] Walkable colliders added={added} / safety floor={safetyCollider.size}");
        }

        private static bool ShouldAddWalkableCollider(string path, string objectName, Bounds bounds)
        {
            // The imported city meshes are extremely dense. Attaching new
            // colliders directly to them forces an expensive first-frame
            // geometry rebuild in the Windows player. Existing authored
            // colliders remain untouched; uncovered flat areas are handled by
            // the lightweight world safety floor created below.
            return false;
        }

        private static string GetHierarchyPath(Transform target)
        {
            var path = target.name;
            while (target.parent != null)
            {
                target = target.parent;
                path = target.name + "/" + path;
            }
            return path;
        }

        internal static Canvas BuildCanvas(Transform parent, out SemiconHud hud, out PhotoExperimentPanel photoPanel,
            out OxidationExperimentPanel oxidationPanel, out EtchExperimentPanel etchPanel,
            out DepositionExperimentPanel depositionPanel,
            out MetalExperimentPanel metalPanel,
            out EdsExperimentPanel edsPanel,
            out SemiconMarketPanel marketPanel,
            out PackageExperimentPanel packagePanel,
            out SemiconProductionPanel productionPanel,
            out SemiconFactoryLoadoutPanel loadoutPanel,
            out SemiconContractPanel contractPanel,
            out SemiconArchivePanel archivePanel)
        {
            var canvasObject = NewUiChild(parent, "Game UI");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            canvas.pixelPerfect = true;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var hudObject = NewUiChild(canvasObject.transform, "HUD");
            Stretch(hudObject.GetComponent<RectTransform>());

            var topBar = CreatePanel(hudObject.transform, "Top Bar", new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -80f), Vector2.zero, new Color32(3, 20, 27, 235), 0f);
            CreateText(topBar, "Company Title", "SEMICON CITY  /  PROCESS DEVELOPMENT", 25, Bone,
                TextAnchor.MiddleLeft, new Vector2(28f, 0f), new Vector2(680f, 80f), FontStyle.Bold);
            CreateText(topBar, "Revision", "FAB / R&D TERMINAL   REV.01", 15, Muted,
                TextAnchor.MiddleLeft, new Vector2(704f, 0f), new Vector2(420f, 80f), FontStyle.Normal);

            var creditsBadge = CreatePanel(topBar, "Credits Badge", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-472f, -25f), new Vector2(-252f, 25f), NavyPanel, 10f);
            CreateText(creditsBadge, "Credits Prefix", "₩", 22, Bone,
                TextAnchor.MiddleLeft, new Vector2(22f, 0f), new Vector2(38f, 50f), FontStyle.Bold);
            var creditsText = CreateText(creditsBadge, "Credits Value", "25,000", 22, Bone,
                TextAnchor.MiddleRight, new Vector2(52f, 0f), new Vector2(146f, 50f), FontStyle.Bold);
            var researchBadge = CreatePanel(topBar, "Research Badge", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-238f, -25f), new Vector2(-18f, 25f), new Color32(8, 92, 92, 255), 10f);
            CreateText(researchBadge, "Research Prefix", "연구 데이터", 18, Bone,
                TextAnchor.MiddleLeft, new Vector2(18f, 0f), new Vector2(126f, 50f), FontStyle.Bold);
            var researchText = CreateText(researchBadge, "Research Value", "120", 20, Bone,
                TextAnchor.MiddleRight, new Vector2(142f, 0f), new Vector2(58f, 50f), FontStyle.Bold);

            var objective = CreatePanel(hudObject.transform, "Objective", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(28f, -280f), new Vector2(438f, -98f), new Color32(3, 28, 35, 235), 14f);
            CreatePanel(objective, "Objective Accent", new Vector2(0f, 0f), new Vector2(0f, 1f),
                Vector2.zero, new Vector2(5f, 0f), Cyan, 0f);
            var objectiveIndex = CreateText(objective, "Objective Index", "STEP 01  /  04", 15, Cyan,
                TextAnchor.UpperLeft, new Vector2(22f, -16f), new Vector2(250f, 24f), FontStyle.Bold);
            var objectiveDistance = CreateText(objective, "Objective Distance", "-- m", 15, Amber,
                TextAnchor.UpperRight, new Vector2(278f, -16f), new Vector2(108f, 24f), FontStyle.Bold);
            var objectiveTitle = CreateText(objective, "Objective Title", "마켓에서 고순도 실리콘 확보", 21,
                Bone, TextAnchor.UpperLeft, new Vector2(22f, -48f), new Vector2(364f, 32f), FontStyle.Bold);
            var objectiveDetail = CreateText(objective, "Objective Detail",
                "자재 거래소에서 실리콘 묶음을 구매하세요.\nWASD 이동  ·  E 상호작용", 16, Muted,
                TextAnchor.UpperLeft, new Vector2(22f, -88f), new Vector2(364f, 68f), FontStyle.Normal);

            var interaction = CreatePanel(hudObject.transform, "Interaction Prompt", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-310f, 34f), new Vector2(310f, 122f), new Color32(3, 28, 35, 242), 14f);
            var interactionGroup = interaction.gameObject.AddComponent<CanvasGroup>();
            var interactionText = CreateText(interaction, "Interaction Text", string.Empty, 22, Bone,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(620f, 88f), FontStyle.Bold);

            var toast = CreatePanel(hudObject.transform, "Toast", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-330f, -158f), new Vector2(330f, -98f), new Color32(6, 72, 77, 250), 12f);
            var toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            var toastText = CreateText(toast, "Toast Text", string.Empty, 19, Bone,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(660f, 60f), FontStyle.Bold);

            hud = hudObject.AddComponent<SemiconHud>();
            SetPrivateField(hud, "creditsText", creditsText);
            SetPrivateField(hud, "researchText", researchText);
            SetPrivateField(hud, "objectiveIndexText", objectiveIndex);
            SetPrivateField(hud, "objectiveTitleText", objectiveTitle);
            SetPrivateField(hud, "objectiveDetailText", objectiveDetail);
            SetPrivateField(hud, "objectiveDistanceText", objectiveDistance);
            SetPrivateField(hud, "interactionText", interactionText);
            SetPrivateField(hud, "interactionGroup", interactionGroup);
            SetPrivateField(hud, "toastGroup", toastGroup);
            SetPrivateField(hud, "toastText", toastText);

            photoPanel = BuildPhotoPanel(canvasObject.transform, hud);
            oxidationPanel = BuildOxidationPanel(canvasObject.transform, hud);
            etchPanel = BuildEtchPanel(canvasObject.transform, hud);
            depositionPanel = BuildDepositionPanel(canvasObject.transform, hud);
            metalPanel = BuildMetalPanel(canvasObject.transform, hud);
            edsPanel = BuildEdsPanel(canvasObject.transform, hud);
            packagePanel = BuildPackagePanel(canvasObject.transform, hud);
            marketPanel = BuildMarketPanel(canvasObject.transform, hud);
            productionPanel = BuildProductionPanel(canvasObject.transform, hud);
            loadoutPanel = BuildFactoryLoadoutPanel(canvasObject.transform, productionPanel, hud);
            contractPanel = BuildContractPanel(canvasObject.transform, hud);
            archivePanel = BuildArchivePanel(canvasObject.transform);
            return canvas;
        }

        private static OxidationExperimentPanel BuildOxidationPanel(Transform canvas, SemiconHud hud)
        {
            var overlay = CreatePanel(canvas, "Oxidation Experiment Screen", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color32(1, 14, 20, 246), 0f);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();
            var frame = CreatePanel(overlay, "Oxidation Experiment Frame", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-880f, -470f), new Vector2(880f, 470f),
                new Color32(4, 45, 50, 252), 18f);

            var accent = CreatePanel(frame, "Oxidation Top Accent", new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -7f), Vector2.zero, Amber, 0f);
            accent.SetAsFirstSibling();
            CreateText(frame, "Oxidation Process Index", "02  /  SEMICONDUCTOR PROCESS", 17, Cyan,
                TextAnchor.UpperLeft, new Vector2(34f, -30f), new Vector2(500f, 28f), FontStyle.Bold);
            CreateText(frame, "Oxidation Title", "산화 공정 실험", 38, Bone,
                TextAnchor.UpperLeft, new Vector2(34f, -66f), new Vector2(600f, 52f), FontStyle.Bold);
            CreateText(frame, "Oxidation Subtitle", "THERMAL OXIDATION PROCESS WINDOW DEVELOPMENT", 16, Muted,
                TextAnchor.UpperLeft, new Vector2(36f, -121f), new Vector2(740f, 28f), FontStyle.Normal);
            var experimentCount = CreateText(frame, "Oxidation Experiment Count", "EXPERIMENT LOG  /  00", 16,
                Muted, TextAnchor.UpperRight, new Vector2(1110f, -36f), new Vector2(360f, 32f), FontStyle.Bold);
            var closeButton = CreateButton(frame, "Oxidation Close Button", "닫기  ×",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-188f, -78f), new Vector2(-34f, -28f),
                new Color32(10, 80, 84, 255), Bone, 20);

            var left = CreatePanel(frame, "Oxidation Parameter Panel", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(34f, 132f), new Vector2(572f, -172f), new Color32(3, 30, 37, 245), 14f);
            CreateText(left, "Oxidation Parameter Header", "PROCESS PARAMETERS", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(360f, 30f), FontStyle.Bold);
            CreateText(left, "Oxidation Parameter Help",
                "산화 온도와 시간을 조정해 목표 절연막을 만드세요.\n같은 조건은 항상 같은 결과를 만듭니다.", 18, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -60f), new Vector2(475f, 62f), FontStyle.Normal);

            CreateText(left, "Temperature Label", "01  산화 온도 / TEMPERATURE", 19, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -158f), new Vector2(360f, 30f), FontStyle.Bold);
            var temperatureValue = CreateText(left, "Temperature Value", "1000 °C", 20, Amber,
                TextAnchor.UpperRight, new Vector2(350f, -158f), new Vector2(160f, 30f), FontStyle.Bold);
            var temperatureSlider = CreateSlider(left, "Temperature Slider", new Vector2(24f, -212f),
                new Vector2(478f, 30f), 900f, 1150f, 1000f, true);
            CreateText(left, "Temperature Range", "900                                1150", 15, Muted,
                TextAnchor.UpperLeft, new Vector2(24f, -242f), new Vector2(478f, 24f), FontStyle.Normal);

            CreateText(left, "Oxidation Time Label", "02  산화 시간 / PROCESS TIME", 19, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -306f), new Vector2(370f, 30f), FontStyle.Bold);
            var processTimeValue = CreateText(left, "Oxidation Time Value", "60 min", 20, Amber,
                TextAnchor.UpperRight, new Vector2(350f, -306f), new Vector2(160f, 30f), FontStyle.Bold);
            var processTimeSlider = CreateSlider(left, "Oxidation Time Slider", new Vector2(24f, -360f),
                new Vector2(478f, 30f), 20f, 90f, 60f, true);
            CreateText(left, "Oxidation Time Range", "20                                    90", 15, Muted,
                TextAnchor.UpperLeft, new Vector2(24f, -390f), new Vector2(478f, 24f), FontStyle.Normal);
            var runButton = CreateButton(left, "Run Oxidation Experiment Button",
                "실험 실행     ▶     연구 데이터 8", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(24f, 26f), new Vector2(502f, 88f), Amber, Navy, 22);

            var center = CreatePanel(frame, "Oxidation Result Panel", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(592f, 132f), new Vector2(1270f, -172f), new Color32(4, 25, 32, 248), 14f);
            CreateText(center, "Oxidation Result Header", "FURNACE ANALYSIS  /  RESULT", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(420f, 30f), FontStyle.Bold);
            var furnace = CreatePanel(center, "Oxidation Furnace Display", new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(-210f, -258f), new Vector2(210f, -76f),
                new Color32(7, 51, 58, 255), 16f);
            CreatePanel(furnace, "Furnace Chamber", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-126f, -56f), new Vector2(126f, 56f), new Color32(8, 88, 91, 255), 56f);
            for (var index = 0; index < 5; index++)
            {
                CreatePanel(furnace, $"Oxidation Wafer {index}", new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(-82f + index * 40f, -38f),
                    new Vector2(-72f + index * 40f, 38f), Bone, 5f);
            }
            var heatLine = CreatePanel(furnace, "Oxidation Heat Scan", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-150f, -115f), new Vector2(150f, -109f), Amber, 3f);
            heatLine.gameObject.SetActive(false);
            CreateMetric(center, "Oxide Thickness", "산화막 두께 / THICKNESS", new Vector2(24f, -320f),
                out var thicknessValue);
            CreateMetric(center, "Oxide Uniformity", "막 균일도 / UNIFORMITY", new Vector2(24f, -414f),
                out var uniformityValue);
            CreateMetric(center, "Oxide Defect", "표면 결함률 / DEFECT", new Vector2(24f, -508f),
                out var defectValue);
            var status = CreateText(center, "Oxidation Result Status", "조건을 설정하고 첫 실험을 실행하세요.",
                19, Bone, TextAnchor.MiddleLeft, new Vector2(24f, -570f), new Vector2(622f, 56f), FontStyle.Bold);

            var right = CreatePanel(frame, "Oxidation Recipe Panel", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-470f, 132f), new Vector2(-34f, -172f), new Color32(3, 30, 37, 245), 14f);
            CreateText(right, "Oxidation Recipe Header", "RECIPE ARCHIVE", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(280f, 30f), FontStyle.Bold);
            CreateText(right, "Oxidation Recipe Subtitle", "현재 공정품: OXIDE-01", 18, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -62f), new Vector2(370f, 30f), FontStyle.Bold);
            var recipeText = CreateText(right, "Oxidation Recipe Text", "아직 저장된 산화 실험 데이터가 없습니다.",
                19, Bone, TextAnchor.UpperLeft, new Vector2(24f, -124f), new Vector2(380f, 340f), FontStyle.Normal);
            CreateText(right, "Oxidation Qualification Hint",
                "QUALIFICATION TARGET\n막 두께 92–108 nm  ·  균일도 90% 이상", 17, Amber,
                TextAnchor.UpperLeft, new Vector2(24f, -500f), new Vector2(390f, 54f), FontStyle.Bold);
            CreateText(right, "Oxidation Next Process", "레시피 확보 후 공장에서\nWAFER-01을 OXIDE-01로 가공할 수 있습니다.",
                17, Muted, TextAnchor.UpperLeft, new Vector2(24f, -564f), new Vector2(390f, 58f), FontStyle.Normal);

            var footer = CreatePanel(frame, "Oxidation Footer", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(34f, 22f), new Vector2(-34f, 112f), new Color32(3, 19, 25, 245), 10f);
            CreateText(footer, "Oxidation Footer Text",
                "막 두께는 온도와 시간에 따라 변하고, 공정창 중심에 가까울수록 균일도가 상승합니다.  |  ESC 닫기",
                17, Muted, TextAnchor.MiddleLeft, new Vector2(22f, 0f), new Vector2(1360f, 90f), FontStyle.Normal);

            var component = overlay.gameObject.AddComponent<OxidationExperimentPanel>();
            component.Configure(group, frame, temperatureSlider, processTimeSlider, temperatureValue,
                processTimeValue, thicknessValue, uniformityValue, defectValue, status, recipeText,
                experimentCount, runButton, closeButton, heatLine, hud);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return component;
        }

        private static EtchExperimentPanel BuildEtchPanel(Transform canvas, SemiconHud hud)
        {
            var overlay = CreatePanel(canvas, "Etch Experiment Screen", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color32(1, 14, 20, 246), 0f);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();
            var frame = CreatePanel(overlay, "Etch Experiment Frame", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-880f, -470f), new Vector2(880f, 470f),
                new Color32(4, 45, 50, 252), 18f);

            var accent = CreatePanel(frame, "Etch Top Accent", new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -7f), Vector2.zero, new Color32(116, 91, 220, 255), 0f);
            accent.SetAsFirstSibling();
            CreateText(frame, "Etch Process Index", "04  /  SEMICONDUCTOR PROCESS", 17, Cyan,
                TextAnchor.UpperLeft, new Vector2(34f, -30f), new Vector2(500f, 28f), FontStyle.Bold);
            CreateText(frame, "Etch Title", "식각 공정 실험", 38, Bone,
                TextAnchor.UpperLeft, new Vector2(34f, -66f), new Vector2(600f, 52f), FontStyle.Bold);
            CreateText(frame, "Etch Subtitle", "DRY ETCH PLASMA PROCESS WINDOW DEVELOPMENT", 16, Muted,
                TextAnchor.UpperLeft, new Vector2(36f, -121f), new Vector2(740f, 28f), FontStyle.Normal);
            var experimentCount = CreateText(frame, "Etch Experiment Count", "EXPERIMENT LOG  /  00", 16,
                Muted, TextAnchor.UpperRight, new Vector2(1110f, -36f), new Vector2(360f, 32f), FontStyle.Bold);
            var closeButton = CreateButton(frame, "Etch Close Button", "닫기  ×",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-188f, -78f), new Vector2(-34f, -28f),
                new Color32(10, 80, 84, 255), Bone, 20);

            var left = CreatePanel(frame, "Etch Parameter Panel", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(34f, 132f), new Vector2(572f, -172f), new Color32(3, 30, 37, 245), 14f);
            CreateText(left, "Etch Parameter Header", "PROCESS PARAMETERS", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(360f, 30f), FontStyle.Bold);
            CreateText(left, "Etch Parameter Help",
                "RF 파워와 식각 가스 유량을 조정해 목표 단면을 만드세요.\n같은 조건은 항상 같은 결과를 만듭니다.", 18, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -60f), new Vector2(475f, 62f), FontStyle.Normal);

            CreateText(left, "Etch Power Label", "01  RF 파워 / RF POWER", 19, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -158f), new Vector2(360f, 30f), FontStyle.Bold);
            var powerValue = CreateText(left, "Etch Power Value", "250 W", 20, Amber,
                TextAnchor.UpperRight, new Vector2(350f, -158f), new Vector2(160f, 30f), FontStyle.Bold);
            var powerSlider = CreateSlider(left, "Etch Power Slider", new Vector2(24f, -212f),
                new Vector2(478f, 30f), 150f, 350f, 250f, true);
            CreateText(left, "Etch Power Range", "150                                  350", 15, Muted,
                TextAnchor.UpperLeft, new Vector2(24f, -242f), new Vector2(478f, 24f), FontStyle.Normal);

            CreateText(left, "Etch Gas Label", "02  가스 유량 / GAS FLOW", 19, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -306f), new Vector2(370f, 30f), FontStyle.Bold);
            var gasValue = CreateText(left, "Etch Gas Value", "60 sccm", 20, Amber,
                TextAnchor.UpperRight, new Vector2(350f, -306f), new Vector2(160f, 30f), FontStyle.Bold);
            var gasSlider = CreateSlider(left, "Etch Gas Slider", new Vector2(24f, -360f),
                new Vector2(478f, 30f), 30f, 90f, 60f, true);
            CreateText(left, "Etch Gas Range", "30                                    90", 15, Muted,
                TextAnchor.UpperLeft, new Vector2(24f, -390f), new Vector2(478f, 24f), FontStyle.Normal);
            var runButton = CreateButton(left, "Run Etch Experiment Button",
                "실험 실행     ▶     연구 데이터 8", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(24f, 26f), new Vector2(502f, 88f), Amber, Navy, 22);

            var center = CreatePanel(frame, "Etch Result Panel", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(592f, 132f), new Vector2(1270f, -172f), new Color32(4, 25, 32, 248), 14f);
            CreateText(center, "Etch Result Header", "PROFILE ANALYSIS  /  RESULT", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(420f, 30f), FontStyle.Bold);
            var chamber = CreatePanel(center, "Etch Plasma Chamber", new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(-210f, -258f), new Vector2(210f, -76f),
                new Color32(7, 51, 58, 255), 16f);
            CreatePanel(chamber, "Etch Wafer Base", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-150f, -55f), new Vector2(150f, -25f), new Color32(232, 229, 215, 255), 5f);
            for (var index = 0; index < 6; index++)
            {
                var x = -128f + index * 51f;
                CreatePanel(chamber, $"Etch Pattern {index}", new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(x, -24f), new Vector2(x + 24f, 42f),
                    new Color32(42, 216, 211, 255), 3f);
            }
            var plasmaLine = CreatePanel(chamber, "Etch Plasma Scan", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-160f, 58f), new Vector2(160f, 68f),
                new Color32(116, 91, 220, 255), 5f);
            plasmaLine.gameObject.SetActive(false);
            CreateMetric(center, "Etch Depth", "식각 깊이 / ETCH DEPTH", new Vector2(24f, -320f),
                out var depthValue);
            CreateMetric(center, "Etch Profile", "측벽 정밀도 / PROFILE", new Vector2(24f, -414f),
                out var profileValue);
            CreateMetric(center, "Etch Selectivity", "선택비 / SELECTIVITY", new Vector2(24f, -508f),
                out var selectivityValue);
            var status = CreateText(center, "Etch Result Status", "조건을 설정하고 첫 실험을 실행하세요.",
                19, Bone, TextAnchor.MiddleLeft, new Vector2(24f, -570f), new Vector2(622f, 56f), FontStyle.Bold);

            var right = CreatePanel(frame, "Etch Recipe Panel", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-470f, 132f), new Vector2(-34f, -172f), new Color32(3, 30, 37, 245), 14f);
            CreateText(right, "Etch Recipe Header", "RECIPE ARCHIVE", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(280f, 30f), FontStyle.Bold);
            CreateText(right, "Etch Recipe Subtitle", "현재 공정품: ETCH-01", 18, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -62f), new Vector2(370f, 30f), FontStyle.Bold);
            var recipeText = CreateText(right, "Etch Recipe Text", "아직 저장된 식각 실험 데이터가 없습니다.",
                19, Bone, TextAnchor.UpperLeft, new Vector2(24f, -124f), new Vector2(380f, 340f), FontStyle.Normal);
            CreateText(right, "Etch Qualification Hint",
                "QUALIFICATION TARGET\n식각 깊이 112–128 nm  ·  측벽 정밀도 90% 이상", 17, Amber,
                TextAnchor.UpperLeft, new Vector2(24f, -500f), new Vector2(390f, 54f), FontStyle.Bold);
            CreateText(right, "Etch Next Process", "레시피 확보 후 공장에서\nPHOTO-01을 ETCH-01로 가공할 수 있습니다.",
                17, Muted, TextAnchor.UpperLeft, new Vector2(24f, -564f), new Vector2(390f, 58f), FontStyle.Normal);

            var footer = CreatePanel(frame, "Etch Footer", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(34f, 22f), new Vector2(-34f, 112f), new Color32(3, 19, 25, 245), 10f);
            CreateText(footer, "Etch Footer Text",
                "RF 파워는 식각 깊이에, 가스 유량은 깊이와 측벽 형상에 함께 영향을 줍니다.  |  ESC 닫기",
                17, Muted, TextAnchor.MiddleLeft, new Vector2(22f, 0f), new Vector2(1360f, 90f), FontStyle.Normal);

            var component = overlay.gameObject.AddComponent<EtchExperimentPanel>();
            component.Configure(group, frame, powerSlider, gasSlider, powerValue, gasValue, depthValue,
                profileValue, selectivityValue, status, recipeText, experimentCount, runButton, closeButton,
                plasmaLine, hud);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return component;
        }

        private static DepositionExperimentPanel BuildDepositionPanel(Transform canvas, SemiconHud hud)
        {
            var depositionAccent = new Color32(77, 201, 143, 255);
            var overlay = CreatePanel(canvas, "Deposition Experiment Screen", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color32(1, 14, 20, 246), 0f);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();
            var frame = CreatePanel(overlay, "Deposition Experiment Frame", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-880f, -470f), new Vector2(880f, 470f),
                new Color32(4, 45, 50, 252), 18f);

            var accent = CreatePanel(frame, "Deposition Top Accent", new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -7f), Vector2.zero, depositionAccent, 0f);
            accent.SetAsFirstSibling();
            CreateText(frame, "Deposition Process Index", "05  /  SEMICONDUCTOR PROCESS", 17, Cyan,
                TextAnchor.UpperLeft, new Vector2(34f, -30f), new Vector2(500f, 28f), FontStyle.Bold);
            CreateText(frame, "Deposition Title", "증착 공정 실험", 38, Bone,
                TextAnchor.UpperLeft, new Vector2(34f, -66f), new Vector2(600f, 52f), FontStyle.Bold);
            CreateText(frame, "Deposition Subtitle", "THIN FILM DEPOSITION PROCESS WINDOW DEVELOPMENT", 16, Muted,
                TextAnchor.UpperLeft, new Vector2(36f, -121f), new Vector2(760f, 28f), FontStyle.Normal);
            var experimentCount = CreateText(frame, "Deposition Experiment Count", "EXPERIMENT LOG  /  00", 16,
                Muted, TextAnchor.UpperRight, new Vector2(1110f, -36f), new Vector2(360f, 32f), FontStyle.Bold);
            var closeButton = CreateButton(frame, "Deposition Close Button", "닫기  ×",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-188f, -78f), new Vector2(-34f, -28f),
                new Color32(10, 80, 84, 255), Bone, 20);

            var left = CreatePanel(frame, "Deposition Parameter Panel", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(34f, 132f), new Vector2(572f, -172f), new Color32(3, 30, 37, 245), 14f);
            CreateText(left, "Deposition Parameter Header", "PROCESS PARAMETERS", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(360f, 30f), FontStyle.Bold);
            CreateText(left, "Deposition Parameter Help",
                "증착 온도와 챔버 압력을 조정해 균일한 박막을 만드세요.\n같은 조건은 항상 같은 결과를 만듭니다.", 18, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -60f), new Vector2(475f, 62f), FontStyle.Normal);

            CreateText(left, "Deposition Temperature Label", "01  증착 온도 / TEMPERATURE", 19, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -158f), new Vector2(390f, 30f), FontStyle.Bold);
            var temperatureValue = CreateText(left, "Deposition Temperature Value", "400 °C", 20, Amber,
                TextAnchor.UpperRight, new Vector2(350f, -158f), new Vector2(160f, 30f), FontStyle.Bold);
            var temperatureSlider = CreateSlider(left, "Deposition Temperature Slider", new Vector2(24f, -212f),
                new Vector2(478f, 30f), 300f, 500f, 400f, true);
            CreateText(left, "Deposition Temperature Range", "300                                  500", 15, Muted,
                TextAnchor.UpperLeft, new Vector2(24f, -242f), new Vector2(478f, 24f), FontStyle.Normal);

            CreateText(left, "Deposition Pressure Label", "02  챔버 압력 / PRESSURE", 19, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -306f), new Vector2(390f, 30f), FontStyle.Bold);
            var pressureValue = CreateText(left, "Deposition Pressure Value", "6 Torr", 20, Amber,
                TextAnchor.UpperRight, new Vector2(350f, -306f), new Vector2(160f, 30f), FontStyle.Bold);
            var pressureSlider = CreateSlider(left, "Deposition Pressure Slider", new Vector2(24f, -360f),
                new Vector2(478f, 30f), 2f, 10f, 6f, true);
            CreateText(left, "Deposition Pressure Range", "2                                      10", 15, Muted,
                TextAnchor.UpperLeft, new Vector2(24f, -390f), new Vector2(478f, 24f), FontStyle.Normal);
            var runButton = CreateButton(left, "Run Deposition Experiment Button",
                "실험 실행     ▶     연구 데이터 8", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(24f, 26f), new Vector2(502f, 88f), Amber, Navy, 22);

            var center = CreatePanel(frame, "Deposition Result Panel", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(592f, 132f), new Vector2(1270f, -172f), new Color32(4, 25, 32, 248), 14f);
            CreateText(center, "Deposition Result Header", "FILM ANALYSIS  /  RESULT", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(420f, 30f), FontStyle.Bold);
            var chamber = CreatePanel(center, "Deposition Chamber", new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(-210f, -258f), new Vector2(210f, -76f),
                new Color32(7, 51, 58, 255), 16f);
            CreatePanel(chamber, "Deposition Wafer Base", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-154f, -62f), new Vector2(154f, -34f), Bone, 5f);
            CreatePanel(chamber, "Deposition Film Layer", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-154f, -31f), new Vector2(154f, -13f), depositionAccent, 5f);
            for (var index = 0; index < 7; index++)
            {
                var x = -135f + index * 45f;
                CreatePanel(chamber, $"Deposition Gas Particle {index}", new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(x, 44f + (index % 2) * 22f),
                    new Vector2(x + 12f, 56f + (index % 2) * 22f), Cyan, 6f);
            }
            var depositionLine = CreatePanel(chamber, "Deposition Growth Scan", Vector2.zero, Vector2.one,
                new Vector2(42f, 36f), new Vector2(-42f, -32f), new Color32(77, 201, 143, 65), 4f);
            depositionLine.anchorMax = new Vector2(1f, 0.12f);
            depositionLine.gameObject.SetActive(false);
            CreateMetric(center, "Deposition Thickness", "박막 두께 / THICKNESS", new Vector2(24f, -320f),
                out var thicknessValue);
            CreateMetric(center, "Deposition Uniformity", "막 균일도 / UNIFORMITY", new Vector2(24f, -414f),
                out var uniformityValue);
            CreateMetric(center, "Deposition Coverage", "단차 피복성 / COVERAGE", new Vector2(24f, -508f),
                out var coverageValue);
            var status = CreateText(center, "Deposition Result Status", "조건을 설정하고 첫 실험을 실행하세요.",
                19, Bone, TextAnchor.MiddleLeft, new Vector2(24f, -570f), new Vector2(622f, 56f), FontStyle.Bold);

            var right = CreatePanel(frame, "Deposition Recipe Panel", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-470f, 132f), new Vector2(-34f, -172f), new Color32(3, 30, 37, 245), 14f);
            CreateText(right, "Deposition Recipe Header", "RECIPE ARCHIVE", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(280f, 30f), FontStyle.Bold);
            CreateText(right, "Deposition Recipe Subtitle", "현재 공정품: DEPO-01", 18, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -62f), new Vector2(370f, 30f), FontStyle.Bold);
            var recipeText = CreateText(right, "Deposition Recipe Text", "아직 저장된 증착 실험 데이터가 없습니다.",
                19, Bone, TextAnchor.UpperLeft, new Vector2(24f, -124f), new Vector2(380f, 340f), FontStyle.Normal);
            CreateText(right, "Deposition Qualification Hint",
                "QUALIFICATION TARGET\n박막 두께 74–86 nm  ·  균일도 90% 이상", 17, Amber,
                TextAnchor.UpperLeft, new Vector2(24f, -500f), new Vector2(390f, 54f), FontStyle.Bold);
            CreateText(right, "Deposition Next Process", "레시피 확보 후 공장에서\nETCH-01을 DEPO-01로 가공할 수 있습니다.",
                17, Muted, TextAnchor.UpperLeft, new Vector2(24f, -564f), new Vector2(390f, 58f), FontStyle.Normal);

            var footer = CreatePanel(frame, "Deposition Footer", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(34f, 22f), new Vector2(-34f, 112f), new Color32(3, 19, 25, 245), 10f);
            CreateText(footer, "Deposition Footer Text",
                "온도는 성장률에, 압력은 박막 균일도와 단차 피복성에 함께 영향을 줍니다.  |  ESC 닫기",
                17, Muted, TextAnchor.MiddleLeft, new Vector2(22f, 0f), new Vector2(1360f, 90f), FontStyle.Normal);

            var component = overlay.gameObject.AddComponent<DepositionExperimentPanel>();
            component.Configure(group, frame, temperatureSlider, pressureSlider, temperatureValue, pressureValue,
                thicknessValue, uniformityValue, coverageValue, status, recipeText, experimentCount, runButton,
                closeButton, depositionLine, hud);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return component;
        }

        private static MetalExperimentPanel BuildMetalPanel(Transform canvas, SemiconHud hud)
        {
            var metalAccent = new Color32(222, 177, 76, 255);
            var overlay = CreatePanel(canvas, "Metal Experiment Screen", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color32(1, 14, 20, 246), 0f);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();
            var frame = CreatePanel(overlay, "Metal Experiment Frame", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-880f, -470f), new Vector2(880f, 470f),
                new Color32(4, 45, 50, 252), 18f);

            var accent = CreatePanel(frame, "Metal Top Accent", new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -7f), Vector2.zero, metalAccent, 0f);
            accent.SetAsFirstSibling();
            CreateText(frame, "Metal Process Index", "06  /  SEMICONDUCTOR PROCESS", 17, Cyan,
                TextAnchor.UpperLeft, new Vector2(34f, -30f), new Vector2(500f, 28f), FontStyle.Bold);
            CreateText(frame, "Metal Title", "금속 배선 공정 실험", 38, Bone,
                TextAnchor.UpperLeft, new Vector2(34f, -66f), new Vector2(660f, 52f), FontStyle.Bold);
            CreateText(frame, "Metal Subtitle", "METALLIZATION AND INTERCONNECT PROCESS WINDOW DEVELOPMENT", 16,
                Muted, TextAnchor.UpperLeft, new Vector2(36f, -121f), new Vector2(820f, 28f), FontStyle.Normal);
            var experimentCount = CreateText(frame, "Metal Experiment Count", "EXPERIMENT LOG  /  00", 16,
                Muted, TextAnchor.UpperRight, new Vector2(1110f, -36f), new Vector2(360f, 32f), FontStyle.Bold);
            var closeButton = CreateButton(frame, "Metal Close Button", "닫기  ×",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-188f, -78f), new Vector2(-34f, -28f),
                new Color32(10, 80, 84, 255), Bone, 20);

            var left = CreatePanel(frame, "Metal Parameter Panel", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(34f, 132f), new Vector2(572f, -172f), new Color32(3, 30, 37, 245), 14f);
            CreateText(left, "Metal Parameter Header", "PROCESS PARAMETERS", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(360f, 30f), FontStyle.Bold);
            CreateText(left, "Metal Parameter Help",
                "스퍼터 파워와 공정 시간을 조정해 저저항 배선을 만드세요.\n같은 조건은 항상 같은 결과를 만듭니다.", 18, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -60f), new Vector2(475f, 62f), FontStyle.Normal);

            CreateText(left, "Metal Power Label", "01  스퍼터 파워 / SPUTTER POWER", 19, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -158f), new Vector2(420f, 30f), FontStyle.Bold);
            var powerValue = CreateText(left, "Metal Power Value", "250 W", 20, Amber,
                TextAnchor.UpperRight, new Vector2(350f, -158f), new Vector2(160f, 30f), FontStyle.Bold);
            var powerSlider = CreateSlider(left, "Metal Power Slider", new Vector2(24f, -212f),
                new Vector2(478f, 30f), 150f, 350f, 250f, true);
            CreateText(left, "Metal Power Range", "150                                  350", 15, Muted,
                TextAnchor.UpperLeft, new Vector2(24f, -242f), new Vector2(478f, 24f), FontStyle.Normal);

            CreateText(left, "Metal Time Label", "02  공정 시간 / PROCESS TIME", 19, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -306f), new Vector2(400f, 30f), FontStyle.Bold);
            var processTimeValue = CreateText(left, "Metal Time Value", "60 sec", 20, Amber,
                TextAnchor.UpperRight, new Vector2(350f, -306f), new Vector2(160f, 30f), FontStyle.Bold);
            var processTimeSlider = CreateSlider(left, "Metal Time Slider", new Vector2(24f, -360f),
                new Vector2(478f, 30f), 30f, 90f, 60f, true);
            CreateText(left, "Metal Time Range", "30                                    90", 15, Muted,
                TextAnchor.UpperLeft, new Vector2(24f, -390f), new Vector2(478f, 24f), FontStyle.Normal);
            var runButton = CreateButton(left, "Run Metal Experiment Button",
                "실험 실행     ▶     연구 데이터 8", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(24f, 26f), new Vector2(502f, 88f), Amber, Navy, 22);

            var center = CreatePanel(frame, "Metal Result Panel", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(592f, 132f), new Vector2(1270f, -172f), new Color32(4, 25, 32, 248), 14f);
            CreateText(center, "Metal Result Header", "INTERCONNECT ANALYSIS  /  RESULT", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(480f, 30f), FontStyle.Bold);
            var wafer = CreatePanel(center, "Metal Wafer Display", new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(-210f, -258f), new Vector2(210f, -76f),
                new Color32(7, 51, 58, 255), 16f);
            CreatePanel(wafer, "Metal Wafer Base", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-154f, -60f), new Vector2(154f, 60f), new Color32(21, 60, 67, 255), 12f);
            for (var index = 0; index < 5; index++)
            {
                var y = -42f + index * 21f;
                CreatePanel(wafer, $"Metal Trace Horizontal {index}", new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(-126f, y), new Vector2(126f, y + 8f),
                    metalAccent, 4f);
            }
            for (var index = 0; index < 4; index++)
            {
                var x = -96f + index * 64f;
                CreatePanel(wafer, $"Metal Via {index}", new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(x, -50f), new Vector2(x + 10f, 50f),
                    metalAccent, 5f);
            }
            var scanLine = CreatePanel(wafer, "Metal Conductivity Scan", Vector2.zero, Vector2.one,
                new Vector2(26f, 22f), new Vector2(-26f, -22f), new Color32(42, 216, 211, 62), 4f);
            scanLine.anchorMax = new Vector2(0.08f, 1f);
            scanLine.gameObject.SetActive(false);
            CreateMetric(center, "Metal Thickness", "배선 두께 / THICKNESS", new Vector2(24f, -320f),
                out var thicknessValue);
            CreateMetric(center, "Metal Resistance", "시트 저항 / SHEET RES.", new Vector2(24f, -414f),
                out var resistanceValue);
            CreateMetric(center, "Metal Adhesion", "접합 신뢰도 / ADHESION", new Vector2(24f, -508f),
                out var adhesionValue);
            var status = CreateText(center, "Metal Result Status", "조건을 설정하고 첫 실험을 실행하세요.",
                19, Bone, TextAnchor.MiddleLeft, new Vector2(24f, -570f), new Vector2(622f, 56f), FontStyle.Bold);

            var right = CreatePanel(frame, "Metal Recipe Panel", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-470f, 132f), new Vector2(-34f, -172f), new Color32(3, 30, 37, 245), 14f);
            CreateText(right, "Metal Recipe Header", "RECIPE ARCHIVE", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(280f, 30f), FontStyle.Bold);
            CreateText(right, "Metal Recipe Subtitle", "현재 공정품: METAL-01", 18, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -62f), new Vector2(380f, 30f), FontStyle.Bold);
            var recipeText = CreateText(right, "Metal Recipe Text", "아직 저장된 금속 배선 실험 데이터가 없습니다.",
                19, Bone, TextAnchor.UpperLeft, new Vector2(24f, -124f), new Vector2(380f, 340f), FontStyle.Normal);
            CreateText(right, "Metal Qualification Hint",
                "QUALIFICATION TARGET\n배선 두께 415–485 nm  ·  저항 0.130 Ω/□ 이하", 17, Amber,
                TextAnchor.UpperLeft, new Vector2(24f, -500f), new Vector2(400f, 54f), FontStyle.Bold);
            CreateText(right, "Metal Next Process", "레시피 확보 후 공장에서\nDEPO-01을 METAL-01로 가공할 수 있습니다.",
                17, Muted, TextAnchor.UpperLeft, new Vector2(24f, -564f), new Vector2(390f, 58f), FontStyle.Normal);

            var footer = CreatePanel(frame, "Metal Footer", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(34f, 22f), new Vector2(-34f, 112f), new Color32(3, 19, 25, 245), 10f);
            CreateText(footer, "Metal Footer Text",
                "파워는 금속 밀도와 접착력에, 시간은 배선 두께와 시트 저항에 함께 영향을 줍니다.  |  ESC 닫기",
                17, Muted, TextAnchor.MiddleLeft, new Vector2(22f, 0f), new Vector2(1360f, 90f), FontStyle.Normal);

            var component = overlay.gameObject.AddComponent<MetalExperimentPanel>();
            component.Configure(group, frame, powerSlider, processTimeSlider, powerValue, processTimeValue,
                thicknessValue, resistanceValue, adhesionValue, status, recipeText, experimentCount, runButton,
                closeButton, scanLine, hud);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return component;
        }

        private static PackageExperimentPanel BuildPackagePanel(Transform canvas, SemiconHud hud)
        {
            var packageAccent = new Color32(193, 104, 255, 255);
            var overlay = CreatePanel(canvas, "Package Experiment Screen", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color32(1, 14, 20, 246), 0f);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();
            var frame = CreatePanel(overlay, "Package Experiment Frame", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-880f, -470f), new Vector2(880f, 470f),
                new Color32(4, 45, 50, 252), 18f);
            CreatePanel(frame, "Package Top Accent", new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -7f), Vector2.zero, packageAccent, 0f);

            CreateText(frame, "Package Process Index", "08  /  SEMICONDUCTOR PROCESS", 17, Cyan,
                TextAnchor.UpperLeft, new Vector2(36f, -28f), new Vector2(520f, 30f), FontStyle.Bold);
            CreateText(frame, "Package Title", "반도체 패키징 신뢰성 실험", 38, Bone,
                TextAnchor.UpperLeft, new Vector2(36f, -68f), new Vector2(860f, 50f), FontStyle.Bold);
            CreateText(frame, "Package Subtitle", "WIRE BONDING · MOLDING · FINAL RELIABILITY TEST", 16, Muted,
                TextAnchor.UpperLeft, new Vector2(36f, -121f), new Vector2(820f, 28f), FontStyle.Normal);
            var experimentCount = CreateText(frame, "Package Experiment Count", "EXPERIMENT LOG  /  00", 16,
                Muted, TextAnchor.UpperRight, new Vector2(1110f, -36f), new Vector2(360f, 32f), FontStyle.Bold);
            var closeButton = CreateButton(frame, "Package Close Button", "닫기  ×",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-188f, -78f), new Vector2(-34f, -28f),
                new Color32(10, 80, 84, 255), Bone, 20);

            var left = CreatePanel(frame, "Package Parameter Panel", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(34f, 132f), new Vector2(572f, -172f), new Color32(3, 30, 37, 245), 14f);
            CreateText(left, "Package Parameter Header", "ASSEMBLY PARAMETERS", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(390f, 30f), FontStyle.Bold);
            CreateText(left, "Package Parameter Help",
                "본딩 압력과 몰딩 온도를 조정해 접합 불량과\n패키지 균열을 동시에 줄이세요.", 18, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -60f), new Vector2(475f, 62f), FontStyle.Normal);

            CreateText(left, "Package Bonding Force Label", "01  본딩 압력 / BOND FORCE", 19, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -158f), new Vector2(410f, 30f), FontStyle.Bold);
            var bondingForceValue = CreateText(left, "Package Bonding Force Value", "35 gf", 20, Amber,
                TextAnchor.UpperRight, new Vector2(350f, -158f), new Vector2(160f, 30f), FontStyle.Bold);
            var bondingForceSlider = CreateSlider(left, "Package Bonding Force Slider", new Vector2(24f, -212f),
                new Vector2(478f, 30f), 20f, 50f, 35f, true);
            CreateText(left, "Package Bonding Force Range", "20                                    50", 15, Muted,
                TextAnchor.UpperLeft, new Vector2(24f, -242f), new Vector2(478f, 24f), FontStyle.Normal);

            CreateText(left, "Package Molding Temperature Label", "02  몰딩 온도 / MOLD TEMPERATURE", 19, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -306f), new Vector2(430f, 30f), FontStyle.Bold);
            var moldingTemperatureValue = CreateText(left, "Package Molding Temperature Value", "175 °C", 20,
                Amber, TextAnchor.UpperRight, new Vector2(350f, -306f), new Vector2(160f, 30f), FontStyle.Bold);
            var moldingTemperatureSlider = CreateSlider(left, "Package Molding Temperature Slider",
                new Vector2(24f, -360f), new Vector2(478f, 30f), 160f, 190f, 175f, true);
            CreateText(left, "Package Molding Temperature Range", "160                                  190", 15,
                Muted, TextAnchor.UpperLeft, new Vector2(24f, -390f), new Vector2(478f, 24f), FontStyle.Normal);
            var runButton = CreateButton(left, "Run Package Experiment Button",
                "신뢰성 시험     ▶     연구 데이터 8", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(24f, 26f), new Vector2(502f, 88f), Amber, Navy, 22);

            var center = CreatePanel(frame, "Package Result Panel", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(592f, 132f), new Vector2(1270f, -172f), new Color32(4, 25, 32, 248), 14f);
            CreateText(center, "Package Result Header", "PACKAGE STACK  /  RELIABILITY RESULT", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(540f, 30f), FontStyle.Bold);
            var packageMap = CreatePanel(center, "Package Cross Section", new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(-220f, -258f), new Vector2(220f, -76f),
                new Color32(7, 51, 58, 255), 16f);
            var mold = CreatePanel(packageMap, "Mold Compound", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-170f, -52f), new Vector2(170f, 54f),
                new Color32(59, 39, 71, 255), 14f);
            CreatePanel(mold, "Package Die", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-58f, -23f), new Vector2(58f, 19f), packageAccent, 5f);
            CreatePanel(packageMap, "Package Substrate", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-190f, -66f), new Vector2(190f, -52f),
                new Color32(77, 201, 143, 255), 3f);
            for (var index = 0; index < 5; index++)
            {
                var y = -42f + index * 20f;
                CreatePanel(packageMap, $"Package Left Lead {index}", new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(-214f, y), new Vector2(-166f, y + 7f), Amber, 2f);
                CreatePanel(packageMap, $"Package Right Lead {index}", new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(166f, y), new Vector2(214f, y + 7f), Amber, 2f);
            }
            var leftBond = CreatePanel(mold, "Left Bond Wire", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-122f, 18f), new Vector2(-22f, 23f), Amber, 2f);
            leftBond.localEulerAngles = new Vector3(0f, 0f, 13f);
            var rightBond = CreatePanel(mold, "Right Bond Wire", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(22f, 18f), new Vector2(122f, 23f), Amber, 2f);
            rightBond.localEulerAngles = new Vector3(0f, 0f, -13f);
            var scanLine = CreatePanel(packageMap, "Package Seal Scan", new Vector2(0.5f, 0f),
                new Vector2(0.5f, 1f), new Vector2(-4f, 18f), new Vector2(4f, -18f), Cyan, 4f);
            scanLine.gameObject.SetActive(false);

            CreateMetric(center, "Package Bond Strength", "본딩 강도 / BOND STRENGTH", new Vector2(24f, -320f),
                out var bondStrengthValue);
            CreateMetric(center, "Package Integrity", "패키지 무결성 / INTEGRITY", new Vector2(24f, -414f),
                out var packageIntegrityValue);
            CreateMetric(center, "Package Final Pass", "최종 합격률 / FINAL PASS", new Vector2(24f, -508f),
                out var finalPassValue);
            var status = CreateText(center, "Package Result Status", "조건을 설정하고 첫 신뢰성 시험을 실행하세요.",
                19, Bone, TextAnchor.MiddleLeft, new Vector2(24f, -570f), new Vector2(622f, 56f), FontStyle.Bold);

            var right = CreatePanel(frame, "Package Recipe Panel", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-470f, 132f), new Vector2(-34f, -172f), new Color32(3, 30, 37, 245), 14f);
            CreateText(right, "Package Recipe Header", "ASSEMBLY RECIPE ARCHIVE", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(360f, 30f), FontStyle.Bold);
            CreateText(right, "Package Recipe Subtitle", "최종 공정품: SC-01", 18, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -62f), new Vector2(370f, 30f), FontStyle.Bold);
            var recipeText = CreateText(right, "Package Recipe Text", "아직 저장된 패키징 실험 데이터가 없습니다.",
                19, Bone, TextAnchor.UpperLeft, new Vector2(24f, -124f), new Vector2(380f, 340f), FontStyle.Normal);
            CreateText(right, "Package Qualification Hint",
                "QUALIFICATION TARGET\n본딩 강도 90% 이상  ·  최종 합격률 94% 이상", 17, Amber,
                TextAnchor.UpperLeft, new Vector2(24f, -500f), new Vector2(400f, 54f), FontStyle.Bold);
            CreateText(right, "Package Next Process", "레시피 확보 후 공장에서 EDS-01을\nSC-01 완제품으로 패키징할 수 있습니다.",
                17, Muted, TextAnchor.UpperLeft, new Vector2(24f, -564f), new Vector2(390f, 58f), FontStyle.Normal);

            var footer = CreatePanel(frame, "Package Footer", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(34f, 22f), new Vector2(-34f, 112f), new Color32(3, 19, 25, 245), 10f);
            CreateText(footer, "Package Footer Text",
                "본딩 압력이 낮으면 접합이 끊어지고, 몰딩 온도가 높으면 패키지 내부 응력이 커집니다.  |  ESC 닫기",
                17, Muted, TextAnchor.MiddleLeft, new Vector2(22f, 0f), new Vector2(1420f, 90f), FontStyle.Normal);

            var component = overlay.gameObject.AddComponent<PackageExperimentPanel>();
            component.Configure(group, frame, bondingForceSlider, moldingTemperatureSlider, bondingForceValue,
                moldingTemperatureValue, bondStrengthValue, packageIntegrityValue, finalPassValue, status,
                recipeText, experimentCount, runButton, closeButton, scanLine, hud);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return component;
        }

        private static EdsExperimentPanel BuildEdsPanel(Transform canvas, SemiconHud hud)
        {
            var edsAccent = new Color32(238, 103, 89, 255);
            var overlay = CreatePanel(canvas, "EDS Experiment Screen", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color32(1, 14, 20, 246), 0f);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();
            var frame = CreatePanel(overlay, "EDS Experiment Frame", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-880f, -470f), new Vector2(880f, 470f),
                new Color32(4, 45, 50, 252), 18f);

            var accent = CreatePanel(frame, "EDS Top Accent", new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -7f), Vector2.zero, edsAccent, 0f);
            accent.SetAsFirstSibling();
            CreateText(frame, "EDS Process Index", "07  /  SEMICONDUCTOR PROCESS", 17, Cyan,
                TextAnchor.UpperLeft, new Vector2(34f, -30f), new Vector2(500f, 28f), FontStyle.Bold);
            CreateText(frame, "EDS Title", "EDS 전기적 다이 선별 실험", 38, Bone,
                TextAnchor.UpperLeft, new Vector2(34f, -66f), new Vector2(760f, 52f), FontStyle.Bold);
            CreateText(frame, "EDS Subtitle", "ELECTRICAL DIE SORTING AND DEFECT SCREENING", 16, Muted,
                TextAnchor.UpperLeft, new Vector2(36f, -121f), new Vector2(780f, 28f), FontStyle.Normal);
            var experimentCount = CreateText(frame, "EDS Experiment Count", "EXPERIMENT LOG  /  00", 16,
                Muted, TextAnchor.UpperRight, new Vector2(1110f, -36f), new Vector2(360f, 32f), FontStyle.Bold);
            var closeButton = CreateButton(frame, "EDS Close Button", "닫기  ×",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-188f, -78f), new Vector2(-34f, -28f),
                new Color32(10, 80, 84, 255), Bone, 20);

            var left = CreatePanel(frame, "EDS Parameter Panel", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(34f, 132f), new Vector2(572f, -172f), new Color32(3, 30, 37, 245), 14f);
            CreateText(left, "EDS Parameter Header", "TEST PARAMETERS", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(360f, 30f), FontStyle.Bold);
            CreateText(left, "EDS Parameter Help",
                "테스트 전압과 누설전류 기준을 조정해 불량 다이를 찾으세요.\n같은 조건은 항상 같은 결과를 만듭니다.", 18, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -60f), new Vector2(475f, 62f), FontStyle.Normal);

            CreateText(left, "EDS Voltage Label", "01  테스트 전압 / TEST VOLTAGE", 19, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -158f), new Vector2(410f, 30f), FontStyle.Bold);
            var voltageValue = CreateText(left, "EDS Voltage Value", "3 V", 20, Amber,
                TextAnchor.UpperRight, new Vector2(350f, -158f), new Vector2(160f, 30f), FontStyle.Bold);
            var voltageSlider = CreateSlider(left, "EDS Voltage Slider", new Vector2(24f, -212f),
                new Vector2(478f, 30f), 1f, 5f, 3f, true);
            CreateText(left, "EDS Voltage Range", "1                                       5", 15, Muted,
                TextAnchor.UpperLeft, new Vector2(24f, -242f), new Vector2(478f, 24f), FontStyle.Normal);

            CreateText(left, "EDS Leakage Label", "02  누설전류 기준 / LEAK LIMIT", 19, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -306f), new Vector2(410f, 30f), FontStyle.Bold);
            var leakageValue = CreateText(left, "EDS Leakage Value", "30 μA", 20, Amber,
                TextAnchor.UpperRight, new Vector2(350f, -306f), new Vector2(160f, 30f), FontStyle.Bold);
            var leakageSlider = CreateSlider(left, "EDS Leakage Slider", new Vector2(24f, -360f),
                new Vector2(478f, 30f), 10f, 50f, 30f, true);
            CreateText(left, "EDS Leakage Range", "10                                    50", 15, Muted,
                TextAnchor.UpperLeft, new Vector2(24f, -390f), new Vector2(478f, 24f), FontStyle.Normal);
            var runButton = CreateButton(left, "Run EDS Experiment Button",
                "검사 실행     ▶     연구 데이터 8", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(24f, 26f), new Vector2(502f, 88f), Amber, Navy, 22);

            var center = CreatePanel(frame, "EDS Result Panel", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(592f, 132f), new Vector2(1270f, -172f), new Color32(4, 25, 32, 248), 14f);
            CreateText(center, "EDS Result Header", "DIE MAP ANALYSIS  /  RESULT", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(460f, 30f), FontStyle.Bold);
            var dieMap = CreatePanel(center, "EDS Die Map", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-210f, -258f), new Vector2(210f, -76f), new Color32(7, 51, 58, 255), 16f);
            for (var row = 0; row < 4; row++)
            {
                for (var column = 0; column < 7; column++)
                {
                    var x = -143f + column * 44f;
                    var y = 43f - row * 31f;
                    var failed = (row == 1 && column == 5) || (row == 3 && column == 2);
                    CreatePanel(dieMap, $"EDS Die {row}-{column}", new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(x + 30f, y + 20f),
                        failed ? edsAccent : new Color32(77, 201, 143, 255), 3f);
                }
            }
            var scanLine = CreatePanel(dieMap, "EDS Probe Scan", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(30f, -4f), new Vector2(-30f, 4f), Cyan, 4f);
            scanLine.gameObject.SetActive(false);
            CreateMetric(center, "EDS Yield", "양품 수율 / PASS YIELD", new Vector2(24f, -320f),
                out var yieldValue);
            CreateMetric(center, "EDS Detection", "결함 검출률 / DETECTION", new Vector2(24f, -414f),
                out var detectionValue);
            CreateMetric(center, "EDS False Reject", "오판정률 / FALSE REJECT", new Vector2(24f, -508f),
                out var falseRejectValue);
            var status = CreateText(center, "EDS Result Status", "조건을 설정하고 첫 검사를 실행하세요.",
                19, Bone, TextAnchor.MiddleLeft, new Vector2(24f, -570f), new Vector2(622f, 56f), FontStyle.Bold);

            var right = CreatePanel(frame, "EDS Recipe Panel", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-470f, 132f), new Vector2(-34f, -172f), new Color32(3, 30, 37, 245), 14f);
            CreateText(right, "EDS Recipe Header", "TEST RECIPE ARCHIVE", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(320f, 30f), FontStyle.Bold);
            CreateText(right, "EDS Recipe Subtitle", "현재 공정품: EDS-01", 18, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -62f), new Vector2(370f, 30f), FontStyle.Bold);
            var recipeText = CreateText(right, "EDS Recipe Text", "아직 저장된 EDS 실험 데이터가 없습니다.",
                19, Bone, TextAnchor.UpperLeft, new Vector2(24f, -124f), new Vector2(380f, 340f), FontStyle.Normal);
            CreateText(right, "EDS Qualification Hint",
                "QUALIFICATION TARGET\n양품 수율 92% 이상  ·  결함 검출률 94% 이상", 17, Amber,
                TextAnchor.UpperLeft, new Vector2(24f, -500f), new Vector2(400f, 54f), FontStyle.Bold);
            CreateText(right, "EDS Next Process", "레시피 확보 후 공장에서\nMETAL-01을 EDS-01로 선별할 수 있습니다.",
                17, Muted, TextAnchor.UpperLeft, new Vector2(24f, -564f), new Vector2(390f, 58f), FontStyle.Normal);

            var footer = CreatePanel(frame, "EDS Footer", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(34f, 22f), new Vector2(-34f, 112f), new Color32(3, 19, 25, 245), 10f);
            CreateText(footer, "EDS Footer Text",
                "전압이 너무 높으면 정상 다이를 손상시키고, 누설 기준이 느슨하면 결함 다이를 놓칠 수 있습니다.  |  ESC 닫기",
                17, Muted, TextAnchor.MiddleLeft, new Vector2(22f, 0f), new Vector2(1420f, 90f), FontStyle.Normal);

            var component = overlay.gameObject.AddComponent<EdsExperimentPanel>();
            component.Configure(group, frame, voltageSlider, leakageSlider, voltageValue, leakageValue, yieldValue,
                detectionValue, falseRejectValue, status, recipeText, experimentCount, runButton, closeButton,
                scanLine, hud);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return component;
        }

        private static PhotoExperimentPanel BuildPhotoPanel(Transform canvas, SemiconHud hud)
        {
            var overlay = CreatePanel(canvas, "Photo Experiment Screen", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color32(7, 23, 34, 158), 0f);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();

            CreatePanel(overlay, "A2 Floating Shadow", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-778f, -442f), new Vector2(778f, 430f), new Color32(0, 12, 20, 82), 18f);
            var frame = CreatePhotoGlassPanel(overlay, "Experiment Frame", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-770f, -430f), new Vector2(770f, 430f),
                new Color32(231, 241, 244, 226), 12f);
            CreatePanel(frame, "A2 Top Accent", new Vector2(0f, 1f), Vector2.one,
                new Vector2(16f, -4f), new Vector2(-16f, -1f), PhotoBlue, 0f);

            var header = CreatePhotoGlassPanel(frame, "Research Header", new Vector2(0f, 1f), Vector2.one,
                new Vector2(24f, -104f), new Vector2(-24f, -24f), new Color32(250, 252, 252, 238), 7f);
            CreatePhotoText(header, "Title", "포토 공정 연구", 30, PhotoInk, TextAnchor.UpperLeft,
                new Vector2(24f, -10f), new Vector2(330f, 40f), FontStyle.Bold);
            CreatePhotoText(header, "Subtitle", "PHOTO / 03   ·   PHOTOLITHOGRAPHY PROCESS WINDOW", 14, PhotoBlue,
                TextAnchor.UpperLeft, new Vector2(26f, -50f), new Vector2(560f, 22f), FontStyle.Bold);
            var experimentCount = CreatePhotoText(header, "Experiment Count", "RUN LOG  /  00", 14,
                PhotoInkMuted, TextAnchor.MiddleRight, new Vector2(765f, -47f), new Vector2(245f, 24f), FontStyle.Bold);

            var creditsBadge = CreatePhotoGlassPanel(header, "Photo Credits Badge", new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-474f, -64f), new Vector2(-318f, -16f),
                new Color32(245, 250, 251, 224), 6f);
            CreatePhotoText(creditsBadge, "Label", "₩ 25,000", 17, PhotoInk, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(156f, 48f), FontStyle.Bold);
            var researchBadge = CreatePhotoGlassPanel(header, "Photo Research Badge", new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-304f, -64f), new Vector2(-116f, -16f),
                new Color32(236, 247, 249, 226), 6f);
            var researchBalance = CreatePhotoText(researchBadge, "Label", "연구 데이터 120", 16, PhotoInk,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(188f, 48f), FontStyle.Bold);
            var closeButton = CreatePhotoButton(header, "Close Button", "닫기", new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-102f, -64f), new Vector2(-18f, -16f),
                new Color32(235, 244, 247, 242), PhotoInk, 16);

            var readyRoot = NewUiChild(frame, "Photo Ready Content");
            Stretch(readyRoot.GetComponent<RectTransform>());
            var readyGroup = readyRoot.AddComponent<CanvasGroup>();

            var parameterPanel = CreatePhotoGlassPanel(readyRoot.transform, "Parameter Panel",
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(24f, 86f), new Vector2(368f, -126f),
                new Color32(249, 252, 252, 224), 8f);
            CreatePhotoStepTitle(parameterPanel, "1", "조건 설정", new Vector2(20f, -18f));
            CreatePhotoText(parameterPanel, "Goal Label", "PROCESS TARGET", 13, PhotoBlue, TextAnchor.UpperLeft,
                new Vector2(20f, -66f), new Vector2(150f, 20f), FontStyle.Bold);
            CreatePhotoText(parameterPanel, "Goal Copy", "수율과 정밀도를 함께 확보하는\n안정 공정 윈도우를 찾으세요.", 15,
                PhotoInkMuted, TextAnchor.UpperLeft, new Vector2(20f, -88f), new Vector2(300f, 44f), FontStyle.Normal);

            CreatePhotoCompactParameterCard(parameterPanel, "Dose", "01", "노광량", "EXPOSURE DOSE",
                "90 mJ/cm²", -144f, 70f, 130f, 90f, true, 92f, 118f, "70", "130", "추천 105",
                out var doseValue, out var doseSlider, out var doseMinus, out var dosePlus);
            CreatePhotoCompactParameterCard(parameterPanel, "Focus", "02", "초점 보정", "FOCUS OFFSET",
                "-0.15 μm", -342f, -0.5f, 0.5f, -0.15f, false, -0.12f, 0.18f, "-0.50", "+0.50", "추천 +0.05",
                out var focusValue, out var focusSlider, out var focusMinus, out var focusPlus);
            CreatePhotoText(parameterPanel, "Adjustment Hint", "− / + 버튼 또는 슬라이더로 미세 조정", 13,
                PhotoInkMuted, TextAnchor.UpperCenter, new Vector2(22f, -548f), new Vector2(300f, 22f), FontStyle.Normal);

            var stagePanel = CreatePhotoGlassPanel(readyRoot.transform, "Wafer Stage Panel",
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(390f, 86f), new Vector2(998f, -126f),
                new Color32(242, 249, 250, 200), 8f);
            CreatePhotoText(stagePanel, "Stage Eyebrow", "LIVE PROCESS VIEW", 13, PhotoBlue, TextAnchor.UpperLeft,
                new Vector2(20f, -52f), new Vector2(210f, 20f), FontStyle.Bold);
            CreatePhotoText(stagePanel, "Stage Title", "웨이퍼 노광 미리보기", 22, PhotoInk, TextAnchor.UpperLeft,
                new Vector2(20f, -16f), new Vector2(310f, 32f), FontStyle.Bold);
            CreatePhotoText(stagePanel, "Stage Status", "SIMULATION READY", 13, PhotoMint, TextAnchor.UpperRight,
                new Vector2(370f, -21f), new Vector2(214f, 22f), FontStyle.Bold);
            CreatePanel(stagePanel, "Horizontal Axis", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-250f, -1f), new Vector2(250f, 1f), new Color32(66, 170, 200, 34), 0f);
            CreatePanel(stagePanel, "Vertical Axis", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-1f, -228f), new Vector2(1f, 228f), new Color32(66, 170, 200, 28), 0f);
            var stageFooter = CreatePhotoGlassPanel(stagePanel, "Stage Footer", new Vector2(0f, 0f), Vector2.right,
                new Vector2(20f, 20f), new Vector2(-20f, 62f), new Color32(235, 247, 246, 202), 5f);
            CreatePhotoText(stageFooter, "Label", "공정 창 상태", 13, PhotoInkMuted, TextAnchor.MiddleLeft,
                new Vector2(14f, 0f), new Vector2(120f, 42f), FontStyle.Bold);
            CreatePhotoText(stageFooter, "Value", "조건 입력 대기", 15, PhotoMint, TextAnchor.MiddleRight,
                new Vector2(280f, 0f), new Vector2(270f, 42f), FontStyle.Bold);

            var predictionPanel = CreatePhotoGlassPanel(readyRoot.transform, "Prediction Panel",
                new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-520f, 86f), new Vector2(-24f, -126f),
                new Color32(249, 252, 252, 224), 8f);
            CreatePhotoStepTitle(predictionPanel, "2", "결과 예측", new Vector2(20f, -18f));
            CreatePhotoText(predictionPanel, "Prediction Help", "조건 변경 시 예상 지표가 즉시 갱신됩니다.", 14,
                PhotoInkMuted, TextAnchor.UpperLeft, new Vector2(20f, -64f), new Vector2(430f, 22f), FontStyle.Normal);
            CreatePhotoForecastRow(predictionPanel, "Preview Yield", "수율", "YIELD", "목표 ≥ 88.0%", -100f,
                out var previewYield);
            CreatePhotoForecastRow(predictionPanel, "Preview Precision", "정밀도", "PRECISION", "목표 ≥ 90.0%", -198f,
                out var previewPrecision);
            CreatePhotoForecastRow(predictionPanel, "Preview Defect", "결함률", "DEFECT", "목표 ≤ 2.0%", -296f,
                out var previewDefect);
            var qualificationStrip = CreatePhotoGlassPanel(predictionPanel, "Qualification Strip",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -428f), new Vector2(-20f, -390f),
                new Color32(224, 245, 238, 218), 5f);
            CreatePhotoText(qualificationStrip, "Label", "목표 달성 시 PHOTO-01 레시피 획득", 14, PhotoMint,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(456f, 38f), FontStyle.Bold);
            CreatePhotoText(predictionPanel, "Execute Index", "03 / EXECUTE", 13, PhotoBlue, TextAnchor.UpperLeft,
                new Vector2(20f, -462f), new Vector2(160f, 20f), FontStyle.Bold);
            var runButton = CreatePhotoButton(predictionPanel, "Run Experiment Button", "실험 실행   ·   연구 데이터 8",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 22f), new Vector2(-20f, 80f),
                Amber, PhotoInk, 19);

            var historyStrip = CreatePhotoGlassPanel(readyRoot.transform, "Previous Best Strip",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 24f), new Vector2(-24f, 68f),
                new Color32(243, 249, 250, 210), 5f);
            var recipeText = CreatePhotoText(historyStrip, "Recipe Text", "최근 기록  ·  아직 저장된 실험이 없습니다.",
                14, PhotoInkMuted, TextAnchor.MiddleLeft, new Vector2(16f, 0f), new Vector2(1100f, 44f), FontStyle.Bold);

            var waferRootObject = NewUiChild(frame, "Photo Wafer Stage");
            var waferRoot = waferRootObject.GetComponent<RectTransform>();
            waferRoot.anchorMin = new Vector2(0.5f, 0.5f);
            waferRoot.anchorMax = new Vector2(0.5f, 0.5f);
            waferRoot.pivot = new Vector2(0.5f, 0.5f);
            waferRoot.anchoredPosition = new Vector2(-76f, -10f);
            waferRoot.sizeDelta = new Vector2(500f, 400f);
            var waferGraphic = waferRootObject.AddComponent<SemiconPhotoWaferGraphic>();
            waferGraphic.color = Color.white;
            waferGraphic.PatternReveal = 0.34f;

            var processingRoot = NewUiChild(frame, "Photo Processing Content");
            Stretch(processingRoot.GetComponent<RectTransform>());
            var processingGroup = processingRoot.AddComponent<CanvasGroup>();
            var processingStatus = CreatePhotoText(processingRoot.transform, "Processing Status",
                "PHOTO EXPOSURE\n웨이퍼 패턴 형성 중", 28, PhotoInk, TextAnchor.MiddleCenter,
                new Vector2(470f, -134f), new Vector2(600f, 76f), FontStyle.Bold);
            var progressShelf = CreatePhotoGlassPanel(processingRoot.transform, "Processing Progress Shelf",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-430f, 38f), new Vector2(430f, 108f),
                new Color32(245, 250, 251, 220), 7f);
            CreatePanel(progressShelf, "Progress Rail", new Vector2(0f, 0.25f), new Vector2(1f, 0.25f),
                new Vector2(74f, -1f), new Vector2(-74f, 1f), new Color32(128, 178, 197, 140), 0f);
            CreatePhotoText(progressShelf, "Mask Phase", "마스크 정렬 완료", 16, PhotoMint, TextAnchor.MiddleLeft,
                new Vector2(20f, 0f), new Vector2(220f, 70f), FontStyle.Bold);
            var processingProgress = CreatePhotoText(progressShelf, "Exposure Phase", "노광 진행  ·  58%", 18,
                PhotoBlue, TextAnchor.MiddleCenter, new Vector2(260f, 0f), new Vector2(340f, 70f), FontStyle.Bold);
            CreatePhotoText(progressShelf, "Develop Phase", "현상 대기", 16, PhotoInkMuted, TextAnchor.MiddleRight,
                new Vector2(620f, 0f), new Vector2(200f, 70f), FontStyle.Bold);
            var skipButton = CreatePhotoButton(processingRoot.transform, "Skip Photo Animation Button", "Space  건너뛰기",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-220f, 38f), new Vector2(-24f, 88f),
                new Color32(235, 245, 248, 236), PhotoInkMuted, 15);

            var resultRoot = NewUiChild(frame, "Photo Result Content");
            Stretch(resultRoot.GetComponent<RectTransform>());
            var resultGroup = resultRoot.AddComponent<CanvasGroup>();
            CreatePhotoText(resultRoot.transform, "Result Complete Status", "공정 실험 완료", 28, PhotoMint,
                TextAnchor.MiddleCenter, new Vector2(120f, -148f), new Vector2(600f, 54f), FontStyle.Bold);
            CreatePhotoText(resultRoot.transform, "Result Complete Subtitle", "QUALIFIED WAFER / PHOTO-01", 14, PhotoBlue,
                TextAnchor.MiddleCenter, new Vector2(160f, -198f), new Vector2(520f, 24f), FontStyle.Bold);
            var resultPanel = CreatePhotoGlassPanel(resultRoot.transform, "Photo Result Sheet",
                new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-650f, 86f), new Vector2(-24f, -126f),
                new Color32(249, 252, 252, 226), 8f);
            CreatePhotoText(resultPanel, "Title", "실험 결과", 24, PhotoInk, TextAnchor.UpperLeft,
                new Vector2(20f, -18f), new Vector2(220f, 34f), FontStyle.Bold);
            CreatePhotoText(resultPanel, "Subtitle", "PROCESS RESULT  /  PHOTO-01", 13, PhotoBlue,
                TextAnchor.UpperRight, new Vector2(300f, -23f), new Vector2(286f, 24f), FontStyle.Bold);
            CreatePhotoCompactResultRow(resultPanel, "Result Yield", "수율", "YIELD", "목표 ≥ 88.0%", -66f,
                out var resultYield, out var resultYieldDelta, out var resultYieldTarget);
            CreatePhotoCompactResultRow(resultPanel, "Result Precision", "정밀도", "PRECISION", "목표 ≥ 90.0%", -164f,
                out var resultPrecision, out var resultPrecisionDelta, out var resultPrecisionTarget);
            CreatePhotoCompactResultRow(resultPanel, "Result Defect", "결함률", "DEFECT", "목표 ≤ 2.0%", -262f,
                out var resultDefect, out var resultDefectDelta, out var resultDefectTarget);
            var recipeBanner = CreatePhotoGlassPanel(resultPanel, "Recipe Completion Banner",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -426f), new Vector2(-20f, -374f),
                new Color32(220, 246, 238, 226), 6f);
            var resultRecipe = CreatePhotoText(recipeBanner, "Recipe Title", "PHOTO-01 레시피 획득", 18, PhotoMint,
                TextAnchor.MiddleLeft, new Vector2(16f, 0f), new Vector2(260f, 52f), FontStyle.Bold);
            var resultRecipeDetail = CreatePhotoText(recipeBanner, "Recipe Detail", "생산 라인 사용 가능", 14,
                PhotoInkMuted, TextAnchor.MiddleRight, new Vector2(280f, 0f), new Vector2(286f, 52f), FontStyle.Normal);
            var confirmButton = CreatePhotoButton(resultPanel, "Photo Confirm Button", "확인",
                new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(20f, 104f), new Vector2(-8f, 158f),
                PhotoBlue, Color.white, 18);
            var repeatButton = CreatePhotoButton(resultPanel, "Photo Repeat Button", "다시 실험",
                new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(8f, 104f), new Vector2(-20f, 158f),
                new Color32(235, 245, 248, 240), PhotoInk, 18);
            CreatePhotoText(resultPanel, "Archive Note", "연구 기록이 FAB 도감에 저장되었습니다.", 13, PhotoInkMuted,
                TextAnchor.MiddleCenter, new Vector2(160f, -612f), new Vector2(306f, 22f), FontStyle.Normal);

            var component = overlay.gameObject.AddComponent<PhotoExperimentPanel>();
            component.Configure(group, frame, readyGroup, parameterPanel, predictionPanel, doseSlider, focusSlider,
                doseValue, focusValue, previewYield, previewPrecision, previewDefect, recipeText, experimentCount, researchBalance,
                doseMinus, dosePlus, focusMinus, focusPlus, runButton, closeButton, waferRoot, waferGraphic,
                processingGroup, processingStatus, processingProgress, skipButton, resultGroup, resultPanel,
                resultYield, resultYieldDelta, resultYieldTarget, resultPrecision, resultPrecisionDelta, resultPrecisionTarget,
                resultDefect, resultDefectDelta, resultDefectTarget, resultRecipe, resultRecipeDetail,
                confirmButton, repeatButton, hud);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            readyGroup.alpha = 1f;
            processingGroup.alpha = 0f;
            processingGroup.interactable = false;
            processingGroup.blocksRaycasts = false;
            resultGroup.alpha = 0f;
            resultGroup.interactable = false;
            resultGroup.blocksRaycasts = false;
            return component;
        }

        private static PhotoExperimentPanel BuildPhotoPanelPrevious(Transform canvas, SemiconHud hud)
        {
            var overlay = CreatePanel(canvas, "Photo Experiment Screen", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color32(199, 220, 226, 188), 0f);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();

            CreatePanel(overlay, "Experiment Frame Shadow", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-806f, -478f), new Vector2(806f, 466f),
                new Color32(25, 61, 78, 28), 16f);
            var frame = CreatePhotoGlassPanel(overlay, "Experiment Frame", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-800f, -470f), new Vector2(800f, 470f),
                new Color32(247, 251, 252, 249), 14f);
            CreatePanel(frame, "A2 Top Accent", new Vector2(0f, 1f), Vector2.one,
                new Vector2(18f, -4f), new Vector2(-18f, -1f), PhotoBlue, 0f);
            CreatePanel(frame, "Pearl Header Glow", new Vector2(0f, 1f), Vector2.one,
                new Vector2(2f, -132f), new Vector2(-2f, -2f), new Color32(255, 255, 255, 105), 15f);
            CreatePanel(frame, "Header Divider", new Vector2(0f, 1f), Vector2.one,
                new Vector2(34f, -139f), new Vector2(-34f, -136f), new Color32(125, 185, 207, 150), 0f);

            CreatePhotoText(frame, "Title", "포토 공정 실험", 40, PhotoInk,
                TextAnchor.UpperLeft, new Vector2(42f, -24f), new Vector2(470f, 58f), FontStyle.Bold);
            CreatePhotoText(frame, "Process Index", "PHOTO  /  03", 20, PhotoBlue,
                TextAnchor.UpperLeft, new Vector2(44f, -83f), new Vector2(250f, 30f), FontStyle.Bold);
            CreatePhotoText(frame, "Subtitle", "노광량과 초점 조건을 조절해 안정적인 포토 공정 윈도우를 찾으세요.", 19,
                PhotoInkMuted, TextAnchor.UpperLeft, new Vector2(190f, -82f), new Vector2(700f, 30f), FontStyle.Bold);
            var experimentCount = CreatePhotoText(frame, "Experiment Count", "EXPERIMENT LOG  /  00", 17,
                PhotoInkMuted, TextAnchor.UpperRight, new Vector2(920f, -101f), new Vector2(320f, 28f), FontStyle.Bold);

            var creditsBadge = CreatePhotoGlassPanel(frame, "Photo Credits Badge", new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-560f, -78f), new Vector2(-360f, -28f),
                new Color32(251, 253, 253, 226), 9f);
            CreatePhotoText(creditsBadge, "Label", "₩ 25,000", 20, PhotoInk, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(200f, 50f), FontStyle.Bold);
            var researchBadge = CreatePhotoGlassPanel(frame, "Photo Research Badge", new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-344f, -78f), new Vector2(-142f, -28f),
                new Color32(237, 248, 250, 232), 9f);
            var researchBalance = CreatePhotoText(researchBadge, "Label", "연구 데이터 120", 18, PhotoInk, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(202f, 50f), FontStyle.Bold);
            var closeButton = CreatePhotoButton(frame, "Close Button", "닫기  ×", new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-126f, -78f), new Vector2(-34f, -28f),
                new Color32(237, 246, 248, 248), PhotoInk, 18);

            var readyRoot = NewUiChild(frame, "Photo Ready Content");
            Stretch(readyRoot.GetComponent<RectTransform>());
            var readyGroup = readyRoot.AddComponent<CanvasGroup>();

            var parameterPanel = CreatePhotoGlassPanel(readyRoot.transform, "Parameter Panel",
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(36f, 126f), new Vector2(630f, -152f),
                PhotoGlass, 10f);
            CreatePhotoStepTitle(parameterPanel, "1", "조건 설정", new Vector2(24f, -20f));
            CreatePhotoText(parameterPanel, "Goal Label", "실험 목표", 17, PhotoInkMuted, TextAnchor.MiddleLeft,
                new Vector2(26f, -76f), new Vector2(105f, 34f), FontStyle.Bold);
            var goalPill = CreatePhotoGlassPanel(parameterPanel, "Balanced Goal", new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(142f, -112f), new Vector2(552f, -74f),
                new Color32(227, 246, 248, 235), 8f);
            CreatePhotoText(goalPill, "Label", "균형형 공정 윈도우  ·  수율과 정밀도 동시 확보", 17, PhotoInk,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(410f, 38f), FontStyle.Bold);

            CreatePhotoParameterCard(parameterPanel, "Dose", "01", "노광량", "EXPOSURE DOSE",
                "90 mJ/cm²", -132f, 70f, 130f, 90f, true, 92f, 118f, "70", "130", "추천 105",
                out var doseValue, out var doseSlider, out var doseMinus, out var dosePlus);
            CreatePhotoParameterCard(parameterPanel, "Focus", "02", "초점 보정", "FOCUS OFFSET",
                "-0.15 μm", -360f, -0.5f, 0.5f, -0.15f, false, -0.12f, 0.18f, "-0.50", "+0.50", "추천 +0.05",
                out var focusValue, out var focusSlider, out var focusMinus, out var focusPlus);

            var predictionPanel = CreatePhotoGlassPanel(readyRoot.transform, "Prediction Panel",
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(652f, 126f), new Vector2(-36f, -152f),
                PhotoGlass, 10f);
            CreatePhotoStepTitle(predictionPanel, "2", "결과 예측", new Vector2(24f, -20f));
            CreatePhotoText(predictionPanel, "Prediction Help", "조건을 변경하면 웨이퍼와 예상 지표가 즉시 갱신됩니다.",
                18, PhotoInkMuted, TextAnchor.UpperLeft, new Vector2(24f, -68f), new Vector2(650f, 28f),
                FontStyle.Bold);
            CreatePhotoText(predictionPanel, "Wafer Caption", "PREVIEW SIMULATION  /  PHOTO PATTERN",
                15, PhotoBlue, TextAnchor.UpperCenter, new Vector2(222f, -94f), new Vector2(460f, 24f),
                FontStyle.Bold);

            CreatePhotoPreviewMetric(predictionPanel, "Preview Yield", "수율 / YIELD", "목표 ≥ 88.0%", new Vector2(24f, -400f),
                out var previewYield);
            CreatePhotoPreviewMetric(predictionPanel, "Preview Precision", "정밀도 / PRECISION", "목표 ≥ 90.0%",
                new Vector2(320f, -400f), out var previewPrecision);
            CreatePhotoPreviewMetric(predictionPanel, "Preview Defect", "결함률 / DEFECT", "목표 ≤ 2.0%",
                new Vector2(616f, -400f), out var previewDefect);
            var qualificationStrip = CreatePhotoGlassPanel(predictionPanel, "Qualification Strip",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -566f), new Vector2(-24f, -530f),
                new Color32(226, 246, 240, 230), 8f);
            CreatePhotoText(qualificationStrip, "Label", "목표 범위에 들어오면 PHOTO-01 레시피를 획득할 수 있습니다.",
                17, PhotoMint, TextAnchor.MiddleLeft, new Vector2(16f, 0f), new Vector2(790f, 36f), FontStyle.Bold);
            CreatePhotoStepTitle(predictionPanel, "3", "실험 실행", new Vector2(24f, -590f));
            CreatePhotoText(predictionPanel, "Run Help", "설정한 조건으로 노광 시뮬레이션을 시작합니다.", 16,
                PhotoInkMuted, TextAnchor.UpperLeft, new Vector2(78f, -627f), new Vector2(410f, 24f), FontStyle.Bold);
            var runButton = CreatePhotoButton(predictionPanel, "Run Experiment Button", "▶   실험 실행     |     연구 데이터 8",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-424f, 28f), new Vector2(-24f, 86f),
                Amber, PhotoInk, 20);

            var historyStrip = CreatePhotoGlassPanel(readyRoot.transform, "Previous Best Strip",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(36f, 24f), new Vector2(-36f, 98f),
                new Color32(248, 251, 251, 220), 11f);
            var recipeText = CreatePhotoText(historyStrip, "Recipe Text", "이전 최고 기록   ·   아직 저장된 실험이 없습니다.",
                19, PhotoInkMuted, TextAnchor.MiddleLeft, new Vector2(24f, 0f), new Vector2(1500f, 74f),
                FontStyle.Bold);

            var waferRootObject = NewUiChild(frame, "Photo Wafer Stage");
            var waferRoot = waferRootObject.GetComponent<RectTransform>();
            waferRoot.anchorMin = new Vector2(0.5f, 0.5f);
            waferRoot.anchorMax = new Vector2(0.5f, 0.5f);
            waferRoot.pivot = new Vector2(0.5f, 0.5f);
            waferRoot.anchoredPosition = new Vector2(304f, 52f);
            waferRoot.sizeDelta = new Vector2(460f, 320f);
            var waferGraphic = waferRootObject.AddComponent<SemiconPhotoWaferGraphic>();
            waferGraphic.color = Color.white;
            waferGraphic.PatternReveal = 0.34f;

            var processingRoot = NewUiChild(frame, "Photo Processing Content");
            Stretch(processingRoot.GetComponent<RectTransform>());
            var processingGroup = processingRoot.AddComponent<CanvasGroup>();
            var processingStatus = CreatePhotoText(processingRoot.transform, "Processing Status",
                "PHOTO EXPOSURE\n노광 진행 중", 34, PhotoInk, TextAnchor.MiddleCenter,
                new Vector2(570f, -148f), new Vector2(620f, 82f), FontStyle.Bold);
            var progressShelf = CreatePhotoGlassPanel(processingRoot.transform, "Processing Progress Shelf",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-520f, 48f), new Vector2(520f, 146f),
                new Color32(247, 251, 252, 218), 12f);
            CreatePanel(progressShelf, "Progress Rail", new Vector2(0f, 0.28f), new Vector2(1f, 0.28f),
                new Vector2(90f, -2f), new Vector2(-90f, 2f), new Color32(161, 193, 207, 180), 0f);
            CreatePhotoText(progressShelf, "Mask Phase", "마스크 정렬 완료", 20, PhotoMint, TextAnchor.MiddleLeft,
                new Vector2(26f, 0f), new Vector2(230f, 98f), FontStyle.Bold);
            var processingProgress = CreatePhotoText(progressShelf, "Exposure Phase", "노광 진행  ·  58%", 22,
                PhotoBlue, TextAnchor.MiddleCenter, new Vector2(318f, 0f), new Vector2(400f, 98f), FontStyle.Bold);
            CreatePhotoText(progressShelf, "Develop Phase", "현상 대기", 20, PhotoInkMuted, TextAnchor.MiddleRight,
                new Vector2(785f, 0f), new Vector2(230f, 98f), FontStyle.Bold);
            var skipButton = CreatePhotoButton(processingRoot.transform, "Skip Photo Animation Button",
                "Space  건너뛰기", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-264f, 48f),
                new Vector2(-44f, 98f), new Color32(238, 247, 249, 245), PhotoInkMuted, 18);

            var resultRoot = NewUiChild(frame, "Photo Result Content");
            Stretch(resultRoot.GetComponent<RectTransform>());
            var resultGroup = resultRoot.AddComponent<CanvasGroup>();
            CreatePhotoText(resultRoot.transform, "Result Complete Status", "공정 실험 완료", 34, PhotoMint,
                TextAnchor.MiddleCenter, new Vector2(104f, -154f), new Vector2(650f, 62f), FontStyle.Bold);
            var resultPanel = CreatePhotoGlassPanel(resultRoot.transform, "Photo Result Sheet",
                new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-770f, 118f), new Vector2(-36f, -152f),
                new Color32(249, 252, 252, 228), 10f);
            CreatePhotoText(resultPanel, "Title", "실험 결과", 32, PhotoInk, TextAnchor.UpperLeft,
                new Vector2(24f, -20f), new Vector2(300f, 42f), FontStyle.Bold);
            CreatePhotoText(resultPanel, "Subtitle", "PROCESS RESULT  /  PHOTO-01", 16, PhotoBlue,
                TextAnchor.UpperRight, new Vector2(360f, -25f), new Vector2(340f, 28f), FontStyle.Bold);
            CreatePhotoResultMetric(resultPanel, "Result Yield", "수율 / YIELD", "목표 ≥ 88.0%", -72f,
                out var resultYield, out var resultYieldDelta, out var resultYieldTarget);
            CreatePhotoResultMetric(resultPanel, "Result Precision", "정밀도 / PRECISION", "목표 ≥ 90.0%", -198f,
                out var resultPrecision, out var resultPrecisionDelta, out var resultPrecisionTarget);
            CreatePhotoResultMetric(resultPanel, "Result Defect", "결함률 / DEFECT", "목표 ≤ 2.0%", -324f,
                out var resultDefect, out var resultDefectDelta, out var resultDefectTarget);
            var recipeBanner = CreatePhotoGlassPanel(resultPanel, "Recipe Completion Banner",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -546f), new Vector2(-24f, -462f),
                new Color32(219, 247, 239, 235), 10f);
            var resultRecipe = CreatePhotoText(recipeBanner, "Recipe Title", "PHOTO-01 레시피 획득", 24, PhotoMint,
                TextAnchor.UpperLeft, new Vector2(22f, -14f), new Vector2(430f, 30f), FontStyle.Bold);
            var resultRecipeDetail = CreatePhotoText(recipeBanner, "Recipe Detail",
                "포토 공정을 생산 라인에서 사용할 수 있습니다.", 17, PhotoInkMuted, TextAnchor.UpperLeft,
                new Vector2(22f, -48f), new Vector2(610f, 24f), FontStyle.Normal);
            var confirmButton = CreatePhotoButton(resultPanel, "Photo Confirm Button", "확인",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 46f), new Vector2(342f, 108f),
                PhotoBlue, Color.white, 22);
            var repeatButton = CreatePhotoButton(resultPanel, "Photo Repeat Button", "다시 실험",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-342f, 46f), new Vector2(-24f, 108f),
                new Color32(236, 247, 249, 250), PhotoInk, 22);
            CreatePhotoText(resultPanel, "Archive Note", "연구 기록이 FAB 도감에 저장되었습니다.", 16, PhotoInkMuted,
                TextAnchor.MiddleCenter, new Vector2(150f, -622f), new Vector2(430f, 30f), FontStyle.Bold);

            var component = overlay.gameObject.AddComponent<PhotoExperimentPanel>();
            component.Configure(group, frame, readyGroup, parameterPanel, predictionPanel, doseSlider, focusSlider,
                doseValue, focusValue, previewYield, previewPrecision, previewDefect, recipeText, experimentCount, researchBalance,
                doseMinus, dosePlus, focusMinus, focusPlus, runButton, closeButton, waferRoot, waferGraphic,
                processingGroup, processingStatus, processingProgress, skipButton, resultGroup, resultPanel,
                resultYield, resultYieldDelta, resultYieldTarget, resultPrecision, resultPrecisionDelta, resultPrecisionTarget,
                resultDefect, resultDefectDelta, resultDefectTarget,
                resultRecipe, resultRecipeDetail, confirmButton, repeatButton, hud);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            readyGroup.alpha = 1f;
            processingGroup.alpha = 0f;
            processingGroup.interactable = false;
            processingGroup.blocksRaycasts = false;
            resultGroup.alpha = 0f;
            resultGroup.interactable = false;
            resultGroup.blocksRaycasts = false;
            return component;
        }

        #if false
        private static PhotoExperimentPanel BuildPhotoPanelLegacy(Transform canvas, SemiconHud hud)
        {
            var overlay = CreatePanel(canvas, "Photo Experiment Screen", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color32(1, 14, 20, 246), 0f);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();

            var frame = CreatePanel(overlay, "Experiment Frame", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-880f, -470f), new Vector2(880f, 470f), new Color32(4, 45, 50, 252), 18f);

            var accent = CreatePanel(frame, "Top Accent", new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -7f), Vector2.zero, Teal, 0f);
            accent.SetAsFirstSibling();
            CreateText(frame, "Process Index", "03  /  SEMICONDUCTOR PROCESS", 17, Cyan,
                TextAnchor.UpperLeft, new Vector2(34f, -30f), new Vector2(500f, 28f), FontStyle.Bold);
            CreateText(frame, "Title", "포토 공정 실험", 38, Bone,
                TextAnchor.UpperLeft, new Vector2(34f, -66f), new Vector2(600f, 52f), FontStyle.Bold);
            CreateText(frame, "Subtitle", "PHOTOLITHOGRAPHY PROCESS WINDOW DEVELOPMENT", 16, Muted,
                TextAnchor.UpperLeft, new Vector2(36f, -121f), new Vector2(700f, 28f), FontStyle.Normal);
            var experimentCount = CreateText(frame, "Experiment Count", "EXPERIMENT LOG  /  00", 16, Muted,
                TextAnchor.UpperRight, new Vector2(1110f, -36f), new Vector2(360f, 32f), FontStyle.Bold);

            var closeButton = CreateButton(frame, "Close Button", "닫기  ×", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-188f, -78f), new Vector2(-34f, -28f), new Color32(10, 80, 84, 255), Bone, 20);

            var left = CreatePanel(frame, "Parameter Panel", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(34f, 132f), new Vector2(572f, -172f), new Color32(3, 30, 37, 245), 14f);
            CreateText(left, "Parameter Header", "PROCESS PARAMETERS", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(360f, 30f), FontStyle.Bold);
            CreateText(left, "Parameter Help", "중요 변수 두 개를 조정해 안정 공정창을 찾으세요.\n같은 조건은 항상 같은 결과를 만듭니다.", 18, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -60f), new Vector2(475f, 62f), FontStyle.Normal);

            CreateText(left, "Dose Label", "01  노광량 / EXPOSURE DOSE", 19, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -158f), new Vector2(330f, 30f), FontStyle.Bold);
            var doseValue = CreateText(left, "Dose Value", "90 mJ/cm²", 20, Amber,
                TextAnchor.UpperRight, new Vector2(350f, -158f), new Vector2(160f, 30f), FontStyle.Bold);
            var doseSlider = CreateSlider(left, "Dose Slider", new Vector2(24f, -212f), new Vector2(478f, 30f), 70f, 130f, 90f, true);
            CreateText(left, "Dose Range", "70                                  130", 15, Muted,
                TextAnchor.UpperLeft, new Vector2(24f, -242f), new Vector2(478f, 24f), FontStyle.Normal);

            CreateText(left, "Focus Label", "02  초점 보정 / FOCUS OFFSET", 19, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -306f), new Vector2(350f, 30f), FontStyle.Bold);
            var focusValue = CreateText(left, "Focus Value", "-0.15 μm", 20, Amber,
                TextAnchor.UpperRight, new Vector2(350f, -306f), new Vector2(160f, 30f), FontStyle.Bold);
            var focusSlider = CreateSlider(left, "Focus Slider", new Vector2(24f, -360f), new Vector2(478f, 30f), -0.5f, 0.5f, -0.15f, false);
            CreateText(left, "Focus Range", "-0.50                                +0.50", 15, Muted,
                TextAnchor.UpperLeft, new Vector2(24f, -390f), new Vector2(478f, 24f), FontStyle.Normal);

            var runButton = CreateButton(left, "Run Experiment Button", "실험 실행     ▶     연구 데이터 8", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(24f, 26f), new Vector2(502f, 88f), Amber, Navy, 22);

            var center = CreatePanel(frame, "Result Panel", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(592f, 132f), new Vector2(1270f, -172f), new Color32(4, 25, 32, 248), 14f);
            CreateText(center, "Result Header", "WAFER ANALYSIS  /  RESULT", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(420f, 30f), FontStyle.Bold);

            var wafer = CreatePanel(center, "Wafer Display", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-126f, -268f), new Vector2(126f, -70f), new Color32(8, 89, 91, 255), 99f);
            CreatePanel(wafer, "Wafer Core", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-82f, -82f), new Vector2(82f, 82f), new Color32(7, 43, 52, 255), 82f);
            for (var line = 0; line < 4; line++)
            {
                CreatePanel(wafer, $"Wafer Line {line}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-75f, -54f + line * 34f), new Vector2(75f, -51f + line * 34f), new Color32(45, 216, 211, 190), 0f);
            }

            var scanLine = CreatePanel(center, "Scan Line", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-380f, -170f), new Vector2(-376f, -48f), Amber, 0f);
            scanLine.gameObject.SetActive(false);

            CreateMetric(center, "Yield", "수율 / YIELD", new Vector2(24f, -320f), out var yieldValue);
            CreateMetric(center, "Precision", "패턴 정밀도 / PRECISION", new Vector2(24f, -414f), out var precisionValue);
            CreateMetric(center, "Defect", "대표 결함률 / DEFECT", new Vector2(24f, -508f), out var defectValue);

            var status = CreateText(center, "Result Status", "조건을 설정하고 첫 실험을 실행하세요.", 19, Bone,
                TextAnchor.MiddleLeft, new Vector2(24f, -570f), new Vector2(622f, 56f), FontStyle.Bold);

            var right = CreatePanel(frame, "Recipe Panel", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-470f, 132f), new Vector2(-34f, -172f), new Color32(3, 30, 37, 245), 14f);
            CreateText(right, "Recipe Header", "RECIPE ARCHIVE", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(280f, 30f), FontStyle.Bold);
            CreateText(right, "Recipe Subtitle", "현재 공정품: PHOTO-01", 18, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -62f), new Vector2(370f, 30f), FontStyle.Bold);
            var recipeText = CreateText(right, "Recipe Text", "아직 저장된 실험 데이터가 없습니다.", 19, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -124f), new Vector2(380f, 340f), FontStyle.Normal);
            CreateText(right, "Qualification Hint", "QUALIFICATION TARGET\n수율 88% 이상  ·  정밀도 90% 이상", 17, Amber,
                TextAnchor.UpperLeft, new Vector2(24f, -500f), new Vector2(380f, 54f), FontStyle.Bold);
            CreateText(right, "Next Process", "레시피 확보 후 공장에서\nOXIDE-01을 PHOTO-01로 가공할 수 있습니다.", 17, Muted,
                TextAnchor.UpperLeft, new Vector2(24f, -564f), new Vector2(380f, 54f), FontStyle.Normal);

            var footer = CreatePanel(frame, "Footer", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(34f, 22f), new Vector2(-34f, 112f), new Color32(3, 19, 25, 245), 10f);
            CreateText(footer, "Footer Text", "공정창에 가까울수록 수율과 정밀도가 동시에 상승합니다.  |  ESC 닫기", 17, Muted,
                TextAnchor.MiddleLeft, new Vector2(22f, 0f), new Vector2(1100f, 90f), FontStyle.Normal);

            var component = overlay.gameObject.AddComponent<PhotoExperimentPanel>();
            component.Configure(group, frame, doseSlider, focusSlider, doseValue, focusValue, yieldValue,
                precisionValue, defectValue, status, recipeText, experimentCount, runButton, closeButton, scanLine, hud);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return component;
        }

        #endif

        private static SemiconMarketPanel BuildMarketPanel(Transform canvas, SemiconHud hud)
        {
            var overlay = CreatePanel(canvas, "Materials Exchange Screen", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color32(1, 14, 20, 246), 0f);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();

            var frame = CreatePanel(overlay, "Market Frame", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-880f, -470f), new Vector2(880f, 470f), new Color32(4, 45, 50, 252), 18f);
            var accent = CreatePanel(frame, "Top Accent", new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -7f), Vector2.zero, Amber, 0f);
            accent.SetAsFirstSibling();

            CreateText(frame, "Market Index", "SUPPLY  /  FAB LOGISTICS NETWORK", 17, Cyan,
                TextAnchor.UpperLeft, new Vector2(34f, -30f), new Vector2(620f, 28f), FontStyle.Bold);
            CreateText(frame, "Market Title", "자재 거래소", 38, Bone,
                TextAnchor.UpperLeft, new Vector2(34f, -66f), new Vector2(600f, 52f), FontStyle.Bold);
            CreateText(frame, "Market Subtitle", "RAW MATERIAL PROCUREMENT  ·  FINISHED GOODS SHIPPING", 16, Muted,
                TextAnchor.UpperLeft, new Vector2(36f, -121f), new Vector2(760f, 28f), FontStyle.Normal);
            CreateText(frame, "Market Revision", "MARKET SESSION  /  LIVE", 16, Muted,
                TextAnchor.UpperRight, new Vector2(1110f, -36f), new Vector2(360f, 32f), FontStyle.Bold);

            var closeButton = CreateButton(frame, "Market Close Button", "닫기  ×", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-188f, -78f), new Vector2(-34f, -28f), new Color32(10, 80, 84, 255), Bone, 20);

            var catalog = CreatePanel(frame, "Materials Catalog", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(34f, 132f), new Vector2(1210f, -172f), new Color32(3, 30, 37, 245), 14f);
            CreateText(catalog, "Catalog Header", "PROCUREMENT CATALOG  /  10 EA BUNDLE", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -20f), new Vector2(620f, 30f), FontStyle.Bold);

            CreateMarketCard(catalog, "Silicon", "01", "고순도 실리콘 잉곳", "MAT-SI-01  /  웨이퍼 기판 원료",
                "₩ 180 / EA", "10개 구매    ▶    ₩ 1,800", -62f, -180f, out var siliconStock, out var buySilicon);
            CreateMarketCard(catalog, "Process Gas", "02", "특수가스 패키지", "MAT-GAS-02  /  산화·증착 공정용",
                "₩ 130 / EA", "10개 구매    ▶    ₩ 1,300", -190f, -308f, out var gasStock, out var buyGas);
            CreateMarketCard(catalog, "Chemicals", "03", "포토 공정 약품", "MAT-CHM-03  /  감광·세정 공정용",
                "₩ 95 / EA", "10개 구매    ▶    ₩ 950", -318f, -436f, out var chemicalStock, out var buyChemical);
            CreateMarketCard(catalog, "Metal Target", "04", "배선 금속 타깃", "MAT-MTL-04  /  금속 배선 공정용",
                "₩ 240 / EA", "10개 구매    ▶    ₩ 2,400", -446f, -564f, out var metalTargetStock,
                out var buyMetalTarget);

            var inventory = CreatePanel(frame, "Warehouse Inventory", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-530f, 132f), new Vector2(-34f, -172f), new Color32(3, 30, 37, 245), 14f);
            CreateText(inventory, "Inventory Header", "WAREHOUSE INVENTORY", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(380f, 30f), FontStyle.Bold);
            CreateText(inventory, "Credit Label", "AVAILABLE CREDIT", 15, Muted,
                TextAnchor.UpperLeft, new Vector2(24f, -72f), new Vector2(240f, 24f), FontStyle.Bold);
            var credits = CreateText(inventory, "Market Credits", "₩ 25,000", 32, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -102f), new Vector2(420f, 48f), FontStyle.Bold);

            CreateInventoryRow(inventory, "Silicon Inventory", "실리콘 잉곳", new Vector2(24f, -174f), out var siliconInventory);
            CreateInventoryRow(inventory, "Gas Inventory", "특수가스", new Vector2(24f, -232f), out var gasInventory);
            CreateInventoryRow(inventory, "Chemical Inventory", "공정 약품", new Vector2(24f, -290f), out var chemicalInventory);
            CreateInventoryRow(inventory, "Metal Target Inventory", "배선 금속 타깃", new Vector2(24f, -348f),
                out var metalTargetInventory);

            var shipping = CreatePanel(inventory, "Shipping Card", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(24f, -590f), new Vector2(-24f, -380f), new Color32(4, 48, 54, 255), 10f);
            CreateText(shipping, "Shipping Header", "FINISHED GOODS  /  출하 대기", 16, Amber,
                TextAnchor.UpperLeft, new Vector2(18f, -16f), new Vector2(330f, 26f), FontStyle.Bold);
            CreateText(shipping, "Shipping Product", "SC-01 제어 센서 패키지", 20, Bone,
                TextAnchor.UpperLeft, new Vector2(18f, -52f), new Vector2(270f, 32f), FontStyle.Bold);
            var finishedStock = CreateText(shipping, "Finished Stock", "0 UNIT", 20, Bone,
                TextAnchor.UpperRight, new Vector2(288f, -52f), new Vector2(130f, 32f), FontStyle.Bold);
            var firstOrderStatus = CreateText(shipping, "First Order Status", "CONTRACT 01  /  공정 개방 1 / 8", 15,
                Muted, TextAnchor.UpperLeft, new Vector2(18f, -91f), new Vector2(400f, 24f), FontStyle.Bold);
            var sellFinished = CreateButton(shipping, "Sell Finished Button", "1개 일반 출하  ▶",
                new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(18f, 12f), new Vector2(-6f, 64f), Teal, Bone, 17);
            var firstOrder = CreateButton(shipping, "First Order Button", "8대 공정 개방 필요",
                new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(6f, 12f), new Vector2(-18f, 64f), Amber, Navy, 17);

            var footer = CreatePanel(frame, "Market Footer", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(34f, 22f), new Vector2(-34f, 112f), new Color32(3, 19, 25, 245), 10f);
            var transaction = CreateText(footer, "Transaction Status", "EXCHANGE READY  /  거래할 품목을 선택하세요.", 18, Muted,
                TextAnchor.MiddleLeft, new Vector2(22f, 0f), new Vector2(1230f, 90f), FontStyle.Bold);
            CreateText(footer, "Market Footer Hint", "구매한 자재는 공장 창고로 즉시 입고됩니다.  |  ESC 닫기", 16, Muted,
                TextAnchor.MiddleRight, new Vector2(1240f, 0f), new Vector2(430f, 90f), FontStyle.Normal);

            var component = overlay.gameObject.AddComponent<SemiconMarketPanel>();
            component.Configure(group, frame, credits, siliconInventory, gasInventory, chemicalInventory,
                metalTargetInventory, finishedStock, firstOrderStatus, transaction, buySilicon, buyGas, buyChemical,
                buyMetalTarget, sellFinished, firstOrder, closeButton, hud);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return component;
        }

        private static SemiconContractPanel BuildContractPanel(Transform canvas, SemiconHud hud)
        {
            var overlay = CreatePanel(canvas, "Contract Board Screen", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color32(1, 14, 20, 246), 0f);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();
            var frame = CreatePanel(overlay, "Contract Board Frame", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-880f, -470f), new Vector2(880f, 470f),
                new Color32(4, 45, 50, 252), 18f);
            CreatePanel(frame, "Contract Top Accent", new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -7f), Vector2.zero, Amber, 0f).SetAsFirstSibling();
            CreateText(frame, "Contract Index", "LOGISTICS  /  CLIENT DELIVERY NETWORK", 17, Cyan,
                TextAnchor.UpperLeft, new Vector2(34f, -30f), new Vector2(650f, 28f), FontStyle.Bold);
            CreateText(frame, "Contract Title", "납품 계약 보드", 38, Bone,
                TextAnchor.UpperLeft, new Vector2(34f, -66f), new Vector2(620f, 52f), FontStyle.Bold);
            CreateText(frame, "Contract Subtitle", "PROCESS SAMPLE REQUESTS  ·  PRODUCT SUPPLY CONTRACTS", 16, Muted,
                TextAnchor.UpperLeft, new Vector2(36f, -121f), new Vector2(820f, 28f), FontStyle.Normal);
            var close = CreateButton(frame, "Contract Close Button", "닫기  ×", new Vector2(1f, 1f), Vector2.one,
                new Vector2(-188f, -78f), new Vector2(-34f, -28f), new Color32(10, 80, 84, 255), Bone, 20);

            var list = CreatePanel(frame, "Contract Catalog", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(34f, 122f), new Vector2(690f, -170f), new Color32(3, 30, 37, 245), 14f);
            CreateText(list, "Contract Catalog Header", "AVAILABLE CONTRACTS  /  09", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -20f), new Vector2(560f, 30f), FontStyle.Bold);
            var contractButtons = new Button[SemiconContractCatalog.Count];
            for (var index = 0; index < contractButtons.Length; index++)
            {
                var definition = SemiconContractCatalog.GetAt(index);
                var yTop = -62f - index * 57f;
                contractButtons[index] = CreateButton(list, $"Contract {index + 1:00} Button",
                    $"{definition.Code}  {definition.Name}", new Vector2(0f, 1f), Vector2.one,
                    new Vector2(24f, yTop - 48f), new Vector2(-24f, yTop),
                    index <= 5 ? new Color32(6, 72, 77, 255) : new Color32(92, 61, 12, 255), Bone, 17);
            }

            var detail = CreatePanel(frame, "Contract Detail", new Vector2(0f, 0f), Vector2.one,
                new Vector2(710f, 122f), new Vector2(-34f, -170f), new Color32(3, 30, 37, 245), 14f);
            var code = CreateText(detail, "Contract Code", "OXV-02  /  DELIVERY CONTRACT", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(28f, -26f), new Vector2(580f, 30f), FontStyle.Bold);
            var title = CreateText(detail, "Contract Name", "산화막 평가 웨이퍼", 32, Bone,
                TextAnchor.UpperLeft, new Vector2(28f, -70f), new Vector2(620f, 48f), FontStyle.Bold);
            var client = CreateText(detail, "Contract Client", "CLIENT  /  한빛 소재 연구소", 18, Amber,
                TextAnchor.UpperLeft, new Vector2(28f, -128f), new Vector2(620f, 30f), FontStyle.Bold);
            var description = CreateText(detail, "Contract Description", "절연막 두께와 균일도를 검증할 산화 웨이퍼 샘플", 18, Muted,
                TextAnchor.UpperLeft, new Vector2(28f, -178f), new Vector2(640f, 64f), FontStyle.Normal);
            var requirementBox = CreatePanel(detail, "Contract Requirement Box", new Vector2(0f, 1f), Vector2.one,
                new Vector2(28f, -414f), new Vector2(-28f, -258f), new Color32(4, 48, 54, 255), 10f);
            var requirement = CreateText(requirementBox, "Contract Requirement", "납품 품목\n필요 수량\n평균 품질", 20, Bone,
                TextAnchor.MiddleLeft, new Vector2(22f, 0f), new Vector2(430f, 156f), FontStyle.Bold);
            var reward = CreateText(requirementBox, "Contract Reward", "₩ 6,500\n연구 데이터  +6", 22, Amber,
                TextAnchor.MiddleRight, new Vector2(438f, 0f), new Vector2(250f, 156f), FontStyle.Bold);
            var status = CreateText(detail, "Contract Status", "LOCKED", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(28f, -444f), new Vector2(620f, 32f), FontStyle.Bold);
            var accept = CreateButton(detail, "Accept Contract Button", "계약 수락    ▶",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(28f, 24f), new Vector2(-28f, 94f), Amber, Navy, 21);
            var deliver = CreateButton(detail, "Deliver Contract Button", "계약 납품    ▶",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(28f, 24f), new Vector2(-28f, 94f), Teal, Bone, 21);

            var footer = CreatePanel(frame, "Contract Footer", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(34f, 22f), new Vector2(-34f, 104f), new Color32(3, 19, 25, 245), 10f);
            CreateText(footer, "Contract Footer Text", "계약은 한 번에 하나만 진행할 수 있습니다.  품질은 현재 재고의 평균값으로 판정됩니다.  |  ESC 닫기", 17, Muted,
                TextAnchor.MiddleLeft, new Vector2(22f, 0f), new Vector2(1520f, 82f), FontStyle.Normal);

            var component = overlay.gameObject.AddComponent<SemiconContractPanel>();
            component.Configure(group, frame, contractButtons, code, title, client, description, requirement,
                reward, status, accept, deliver, close, hud);
            group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false;
            return component;
        }

        private static SemiconArchivePanel BuildArchivePanel(Transform canvas)
        {
            var overlay = CreatePanel(canvas, "FAB Archive Screen", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color32(1, 14, 20, 246), 0f);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();
            var frame = CreatePanel(overlay, "FAB Archive Frame", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-880f, -470f), new Vector2(880f, 470f),
                new Color32(4, 45, 50, 252), 18f);
            CreatePanel(frame, "Archive Top Accent", new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -7f), Vector2.zero, Cyan, 0f).SetAsFirstSibling();
            CreateText(frame, "Archive Index", "FAB ARCHIVE  /  PRODUCTION TECHNOLOGY RECORDS", 17, Cyan,
                TextAnchor.UpperLeft, new Vector2(34f, -30f), new Vector2(760f, 28f), FontStyle.Bold);
            CreateText(frame, "Archive Title", "생산 기술 기록소", 38, Bone,
                TextAnchor.UpperLeft, new Vector2(34f, -66f), new Vector2(650f, 52f), FontStyle.Bold);
            var summary = CreateText(frame, "Archive Summary", "PROCESS 1 / 8     CONTRACT 0 / 9     SAMPLE 0 / 6", 16, Muted,
                TextAnchor.UpperRight, new Vector2(850f, -94f), new Vector2(690f, 34f), FontStyle.Bold);
            var close = CreateButton(frame, "Archive Close Button", "닫기  ×", new Vector2(1f, 1f), Vector2.one,
                new Vector2(-188f, -78f), new Vector2(-34f, -28f), new Color32(10, 80, 84, 255), Bone, 20);

            var tabNames = new[] { "공정 도감", "제품 도감", "자재 도감", "인력·로봇", "디스크", "고객·계약" };
            var tabs = new Button[tabNames.Length];
            for (var index = 0; index < tabs.Length; index++)
            {
                var left = 34f + index * 282f;
                tabs[index] = CreateButton(frame, $"Archive Tab {index + 1:00}", tabNames[index],
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(left, -180f),
                    new Vector2(left + 264f, -134f), index == 0 ? Teal : new Color32(6, 72, 77, 255), Bone, 18);
            }

            var contentPanel = CreatePanel(frame, "Archive Content Panel", new Vector2(0f, 0f), Vector2.one,
                new Vector2(34f, 116f), new Vector2(-34f, -204f), new Color32(3, 30, 37, 245), 14f);
            var sectionCode = CreateText(contentPanel, "Archive Section Code", "ARCHIVE SECTION  /  01", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(28f, -24f), new Vector2(520f, 30f), FontStyle.Bold);
            var sectionTitle = CreateText(contentPanel, "Archive Section Title", "공정 도감", 30, Bone,
                TextAnchor.UpperLeft, new Vector2(28f, -62f), new Vector2(600f, 46f), FontStyle.Bold);
            var content = CreateText(contentPanel, "Archive Content", "공정 기록을 불러오는 중입니다.", 18, Bone,
                TextAnchor.UpperLeft, new Vector2(28f, -124f), new Vector2(1620f, 560f), FontStyle.Normal);

            var footer = CreatePanel(frame, "Archive Footer", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(34f, 22f), new Vector2(-34f, 96f), new Color32(3, 19, 25, 245), 10f);
            CreateText(footer, "Archive Footer Text", "발견·실험·생산·납품 기록에 따라 도감 정보가 자동 갱신됩니다.  |  ESC 닫기", 17, Muted,
                TextAnchor.MiddleLeft, new Vector2(22f, 0f), new Vector2(1500f, 74f), FontStyle.Normal);

            var component = overlay.gameObject.AddComponent<SemiconArchivePanel>();
            component.Configure(group, frame, tabs, sectionCode, sectionTitle, summary, content, close);
            group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false;
            return component;
        }

        private static SemiconProductionPanel BuildProductionPanel(Transform canvas, SemiconHud hud)
        {
            var overlay = CreatePanel(canvas, "Production Control Screen", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color32(1, 14, 20, 246), 0f);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();
            var frame = CreatePanel(overlay, "Production Frame", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-830f, -440f), new Vector2(830f, 440f), new Color32(4, 45, 50, 252), 18f);
            var accent = CreatePanel(frame, "Production Top Accent", new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -7f), Vector2.zero, Teal, 0f);
            accent.SetAsFirstSibling();

            CreateText(frame, "Production Index", "FAB 01  /  PROCESS CONTROL", 17, Cyan,
                TextAnchor.UpperLeft, new Vector2(34f, -30f), new Vector2(600f, 28f), FontStyle.Bold);
            CreateText(frame, "Production Title", "생산 공정 제어", 38, Bone,
                TextAnchor.UpperLeft, new Vector2(34f, -66f), new Vector2(620f, 52f), FontStyle.Bold);
            CreateText(frame, "Production Subtitle", "RECIPE-DRIVEN MATERIAL CONVERSION  ·  WAREHOUSE LINK", 16, Muted,
                TextAnchor.UpperLeft, new Vector2(36f, -121f), new Vector2(820f, 28f), FontStyle.Normal);
            var selectedSlot = CreateText(frame, "Production Revision", "ACTIVE CELL  /  SLOT 01", 16, Muted,
                TextAnchor.UpperRight, new Vector2(1080f, -36f), new Vector2(360f, 32f), FontStyle.Bold);
            var closeButton = CreateButton(frame, "Production Close Button", "닫기  ×",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-188f, -78f), new Vector2(-34f, -28f),
                new Color32(10, 80, 84, 255), Bone, 20);

            var recipe = CreatePanel(frame, "Production Recipe", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(34f, 122f), new Vector2(540f, -170f), new Color32(3, 30, 37, 245), 14f);
            var recipeHeader = CreateText(recipe, "Recipe Header", "ACTIVE RECIPE  /  WAFER-01", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(420f, 30f), FontStyle.Bold);
            var waferRecipeButton = CreateButton(recipe, "Select Wafer Recipe Button", "WAFER  ◀",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -104f), new Vector2(130f, -62f),
                Teal, Bone, 16);
            var oxidationRecipeButton = CreateButton(recipe, "Select Oxidation Recipe Button", "OXIDE",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, -104f), new Vector2(246f, -62f),
                new Color32(116, 76, 8, 255), Bone, 16);
            var photoRecipeButton = CreateButton(recipe, "Select Photo Recipe Button", "PHOTO",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(256f, -104f), new Vector2(362f, -62f),
                new Color32(7, 91, 111, 255), Bone, 16);
            var etchRecipeButton = CreateButton(recipe, "Select Etch Recipe Button", "ETCH",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(372f, -104f), new Vector2(482f, -62f),
                new Color32(78, 38, 113, 255), Bone, 16);
            var depositionRecipeButton = CreateButton(recipe, "Select Deposition Recipe Button", "DEPO",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -154f), new Vector2(130f, -112f),
                new Color32(22, 105, 73, 255), Bone, 16);
            var metalRecipeButton = CreateButton(recipe, "Select Metal Recipe Button", "METAL",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, -154f), new Vector2(246f, -112f),
                new Color32(111, 78, 22, 255), Bone, 16);
            var edsRecipeButton = CreateButton(recipe, "Select EDS Recipe Button", "EDS",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(256f, -154f), new Vector2(362f, -112f),
                new Color32(105, 45, 43, 255), Bone, 16);
            var sc01RecipeButton = CreateButton(recipe, "Select Package Recipe Button", "PACKAGE",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(372f, -154f), new Vector2(482f, -112f),
                new Color32(8, 78, 82, 255), Bone, 16);
            var pm10RecipeButton = CreateButton(recipe, "Select PM-10 Recipe Button", "PM-10",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -204f), new Vector2(246f, -162f),
                new Color32(56, 90, 111, 255), Bone, 16);
            var dd20RecipeButton = CreateButton(recipe, "Select DD-20 Recipe Button", "DD-20",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(256f, -204f), new Vector2(482f, -162f),
                new Color32(92, 55, 105, 255), Bone, 16);
            var recipeStatus = CreateText(recipe, "Recipe Qualification", "STARTER RECIPE  /  AVAILABLE", 20,
                Teal, TextAnchor.UpperLeft, new Vector2(24f, -226f),
                new Vector2(440f, 34f), FontStyle.Bold);
            var recipeProduct = CreateText(recipe, "Recipe Product", "기초 웨이퍼  /  WAFER-01", 26, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -266f), new Vector2(440f, 42f), FontStyle.Bold);
            var recipeDescription = CreateText(recipe, "Recipe Description",
                "고순도 실리콘을 절단·연마하여 다음 공정에\n투입할 기초 웨이퍼를 제작합니다.", 17, Muted,
                TextAnchor.UpperLeft, new Vector2(24f, -312f), new Vector2(450f, 58f), FontStyle.Normal);
            var recipeCosts = CreateText(recipe, "Recipe Costs",
                "INPUT / 1 CYCLE\n\n고순도 실리콘      2 EA", 20, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -382f), new Vector2(440f, 160f), FontStyle.Bold);
            var inventory = CreatePanel(frame, "Production Inventory", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(560f, 122f), new Vector2(1095f, -170f), new Color32(3, 30, 37, 245), 14f);
            CreateText(inventory, "Production Inventory Header", "MATERIAL FEED  /  WAREHOUSE", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(460f, 30f), FontStyle.Bold);

            var siliconRow = CreatePanel(inventory, "Production Silicon Row", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(24f, -142f), new Vector2(-24f, -74f), new Color32(4, 48, 54, 255), 8f);
            var siliconLabel = CreateText(siliconRow, "Production Silicon Label", "01  고순도 실리콘", 20, Bone,
                TextAnchor.MiddleLeft, new Vector2(18f, 0f), new Vector2(300f, 68f), FontStyle.Bold);
            var siliconStock = CreateText(siliconRow, "Production Silicon Stock", "0  /  2", 22, Amber,
                TextAnchor.MiddleRight, new Vector2(322f, 0f), new Vector2(150f, 68f), FontStyle.Bold);

            var gasRow = CreatePanel(inventory, "Production Gas Row", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(24f, -226f), new Vector2(-24f, -158f), new Color32(4, 48, 54, 255), 8f);
            var gasLabel = CreateText(gasRow, "Production Gas Label", "02  특수가스", 20, Bone,
                TextAnchor.MiddleLeft, new Vector2(18f, 0f), new Vector2(300f, 68f), FontStyle.Bold);
            var gasStock = CreateText(gasRow, "Production Gas Stock", "0  /  1", 22, Amber,
                TextAnchor.MiddleRight, new Vector2(322f, 0f), new Vector2(150f, 68f), FontStyle.Bold);

            var chemicalRow = CreatePanel(inventory, "Production Chemical Row", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(24f, -310f), new Vector2(-24f, -242f), new Color32(4, 48, 54, 255), 8f);
            var chemicalLabel = CreateText(chemicalRow, "Production Chemical Label", "03  공정 약품", 20, Bone,
                TextAnchor.MiddleLeft, new Vector2(18f, 0f), new Vector2(300f, 68f), FontStyle.Bold);
            var chemicalStock = CreateText(chemicalRow, "Production Chemical Stock", "0  /  2", 22, Amber,
                TextAnchor.MiddleRight, new Vector2(322f, 0f), new Vector2(150f, 68f), FontStyle.Bold);
            CreateText(inventory, "Production Inventory Hint", "현재 재고  /  필요 수량", 16, Muted,
                TextAnchor.UpperRight, new Vector2(244f, -338f), new Vector2(240f, 26f), FontStyle.Normal);

            var output = CreatePanel(frame, "Production Output", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-530f, 122f), new Vector2(-34f, -170f), new Color32(3, 30, 37, 245), 14f);
            CreateText(output, "Production Output Header", "OUTPUT BUFFER", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(360f, 30f), FontStyle.Bold);
            var outputProduct = CreateText(output, "Production Output Product", "WAFER-01 기초 웨이퍼", 26, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -78f), new Vector2(390f, 40f), FontStyle.Bold);
            var outputLabel = CreateText(output, "Production Output Label", "중간 공정품 창고", 16, Muted,
                TextAnchor.UpperLeft, new Vector2(24f, -134f), new Vector2(200f, 28f), FontStyle.Bold);
            var finishedStock = CreateText(output, "Production Finished Stock", "0 UNIT", 34, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -168f), new Vector2(400f, 50f), FontStyle.Bold);
            var loadout = CreateText(output, "Production Loadout", "미배정\n미장착", 17, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -236f), new Vector2(420f, 62f), FontStyle.Bold);
            var performance = CreateText(output, "Production Performance", "CYCLE TIME    8.0s\nOUTPUT        1 UNIT\nQUALITY       80", 17, Muted,
                TextAnchor.UpperLeft, new Vector2(24f, -306f), new Vector2(420f, 92f), FontStyle.Bold);
            var queueStatus = CreateText(output, "Production Queue Status", "QUEUE EMPTY  /  START READY", 16, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -386f), new Vector2(420f, 28f), FontStyle.Bold);
            var progressTrack = CreatePanel(output, "Production Progress Track", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(24f, -430f), new Vector2(-24f, -410f), new Color32(8, 74, 79, 255), 4f);
            var progressFill = CreatePanel(progressTrack, "Production Progress Fill", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, Teal, 4f);
            var produceButton = CreateButton(output, "Produce SC-01 Button", "1 사이클 시작    ▶    WAFER-01",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 24f), new Vector2(-24f, 92f),
                Amber, Navy, 21);
            var collectButton = CreateButton(output, "Collect Production Button", "생산 완료품 회수    ▶",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 24f), new Vector2(-24f, 92f),
                Teal, Bone, 21);

            var footer = CreatePanel(frame, "Production Footer", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(34f, 22f), new Vector2(-34f, 104f), new Color32(3, 19, 25, 245), 10f);
            var productionStatus = CreateText(footer, "Production Status",
                "PROCESS CELL READY  /  레시피를 선택하고 생산을 시작하세요.", 18, Muted,
                TextAnchor.MiddleLeft, new Vector2(22f, 0f), new Vector2(1100f, 82f), FontStyle.Bold);
            CreateText(footer, "Production Footer Hint", "생산은 화면을 닫아도 계속 진행됩니다.  |  ESC 닫기", 16, Muted,
                TextAnchor.MiddleRight, new Vector2(1110f, 0f), new Vector2(470f, 82f), FontStyle.Normal);

            var component = overlay.gameObject.AddComponent<SemiconProductionPanel>();
            component.Configure(group, frame, recipeHeader, recipeStatus, recipeProduct, recipeDescription, recipeCosts,
                siliconStock, gasStock, chemicalStock, outputProduct, outputLabel, finishedStock, productionStatus,
                waferRecipeButton, oxidationRecipeButton, photoRecipeButton, etchRecipeButton,
                depositionRecipeButton, metalRecipeButton, edsRecipeButton, sc01RecipeButton, produceButton,
                collectButton, closeButton, pm10RecipeButton, dd20RecipeButton, hud,
                selectedSlot, loadout, performance, queueStatus, progressFill, siliconLabel, gasLabel, chemicalLabel);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return component;
        }

        private static SemiconFactoryLoadoutPanel BuildFactoryLoadoutPanel(Transform canvas,
            SemiconProductionPanel productionPanel, SemiconHud hud)
        {
            var overlay = CreatePanel(canvas, "Factory Loadout Screen", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color32(1, 14, 20, 246), 0f);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();
            var frame = CreatePanel(overlay, "Factory Loadout Frame", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-830f, -440f), new Vector2(830f, 440f), new Color32(4, 45, 50, 252), 18f);
            var accent = CreatePanel(frame, "Factory Loadout Accent", new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -7f), Vector2.zero, Teal, 0f);
            accent.SetAsFirstSibling();

            CreateText(frame, "Factory Loadout Index", "FAB 01  /  EQUIPMENT CONFIGURATION", 17, Cyan,
                TextAnchor.UpperLeft, new Vector2(34f, -30f), new Vector2(660f, 28f), FontStyle.Bold);
            CreateText(frame, "Factory Loadout Title", "생산 설비 배치 및 구성", 38, Bone,
                TextAnchor.UpperLeft, new Vector2(34f, -66f), new Vector2(760f, 52f), FontStyle.Bold);
            CreateText(frame, "Factory Loadout Subtitle", "MACHINE SLOT  ·  PERSONNEL ASSIGNMENT  ·  DISK MODULE", 16, Muted,
                TextAnchor.UpperLeft, new Vector2(36f, -121f), new Vector2(840f, 28f), FontStyle.Normal);
            CreateText(frame, "Factory Loadout Revision", "FAB CONFIG  /  REV.01", 16, Muted,
                TextAnchor.UpperRight, new Vector2(1090f, -36f), new Vector2(350f, 32f), FontStyle.Bold);
            var closeButton = CreateButton(frame, "Factory Loadout Close Button", "닫기  ×",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-188f, -78f), new Vector2(-34f, -28f),
                new Color32(10, 80, 84, 255), Bone, 20);

            var slotPanel = CreatePanel(frame, "Factory Slot List", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(34f, 122f), new Vector2(336f, -170f), new Color32(3, 30, 37, 245), 14f);
            CreateText(slotPanel, "Factory Slot Header", "MACHINE SLOTS", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(22f, -22f), new Vector2(250f, 30f), FontStyle.Bold);
            var machineStatus = CreateText(slotPanel, "Factory Machine Status", "EQUIPMENT ONLINE", 16, Teal,
                TextAnchor.UpperLeft, new Vector2(22f, -62f), new Vector2(258f, 52f), FontStyle.Bold);
            var slotButtons = new[]
            {
                CreateButton(slotPanel, "Factory Slot 01 Button", "01  ONLINE    ◀",
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -190f), new Vector2(-18f, -122f),
                    Teal, Bone, 19),
                CreateButton(slotPanel, "Factory Slot 02 Button", "02  EMPTY",
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -276f), new Vector2(-18f, -208f),
                    new Color32(8, 78, 82, 255), Bone, 19),
                CreateButton(slotPanel, "Factory Slot 03 Button", "03  EMPTY",
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -362f), new Vector2(-18f, -294f),
                    new Color32(8, 78, 82, 255), Bone, 19)
            };
            CreateText(slotPanel, "Factory Slot Hint", "빈 슬롯에는 SC-01 설비를 추가 배치할 수 있습니다.", 15, Muted,
                TextAnchor.UpperLeft, new Vector2(22f, -492f), new Vector2(258f, 60f), FontStyle.Normal);

            var workerPanel = CreatePanel(frame, "Factory Worker Assignment", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(356f, 122f), new Vector2(754f, -170f), new Color32(3, 30, 37, 245), 14f);
            CreateText(workerPanel, "Factory Worker Header", "PERSONNEL  /  ROBOT", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(22f, -22f), new Vector2(340f, 30f), FontStyle.Bold);
            var workerName = CreateText(workerPanel, "Factory Worker Name", "미배정", 24, Bone,
                TextAnchor.UpperLeft, new Vector2(22f, -58f), new Vector2(350f, 38f), FontStyle.Bold);
            var workerBonus = CreateText(workerPanel, "Factory Worker Bonus", "기본 설비 성능으로 가동", 15, Muted,
                TextAnchor.UpperLeft, new Vector2(22f, -98f), new Vector2(350f, 28f), FontStyle.Normal);
            var workerButtons = new[]
            {
                CreateButton(workerPanel, "Assign Mina Button", "미나    생산 +10 / 품질 +12",
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -196f), new Vector2(-18f, -142f),
                    new Color32(8, 93, 96, 255), Bone, 17),
                CreateButton(workerPanel, "Assign Rex Button", "렉스    속도 +16 / 생산 +5",
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -266f), new Vector2(-18f, -212f),
                    new Color32(8, 93, 96, 255), Bone, 17),
                CreateButton(workerPanel, "Assign BO7 Button", "BO-7    품질 +18 / 생산 +4",
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -336f), new Vector2(-18f, -282f),
                    new Color32(8, 93, 96, 255), Bone, 17),
                CreateButton(workerPanel, "Clear Worker Button", "인력 배정 해제",
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -406f), new Vector2(-18f, -352f),
                    new Color32(9, 55, 61, 255), Muted, 17)
            };

            var diskPanel = CreatePanel(frame, "Factory Disk Assignment", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(774f, 122f), new Vector2(1172f, -170f), new Color32(3, 30, 37, 245), 14f);
            CreateText(diskPanel, "Factory Disk Header", "TRAIT DISK  /  MODULE", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(22f, -22f), new Vector2(340f, 30f), FontStyle.Bold);
            var diskName = CreateText(diskPanel, "Factory Disk Name", "미장착", 24, Bone,
                TextAnchor.UpperLeft, new Vector2(22f, -58f), new Vector2(350f, 38f), FontStyle.Bold);
            var diskBonus = CreateText(diskPanel, "Factory Disk Bonus", "디스크 슬롯 비어 있음", 15, Muted,
                TextAnchor.UpperLeft, new Vector2(22f, -98f), new Vector2(350f, 28f), FontStyle.Normal);
            var diskButtons = new[]
            {
                CreateButton(diskPanel, "Assign Production Disk Button", "생산 증폭    생산 +12",
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -196f), new Vector2(-18f, -142f),
                    new Color32(116, 76, 8, 255), Bone, 17),
                CreateButton(diskPanel, "Assign Speed Disk Button", "오버클럭    속도 +15",
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -266f), new Vector2(-18f, -212f),
                    new Color32(7, 91, 111, 255), Bone, 17),
                CreateButton(diskPanel, "Assign Quality Disk Button", "계측 보정    품질 +15",
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -336f), new Vector2(-18f, -282f),
                    new Color32(78, 38, 113, 255), Bone, 17),
                CreateButton(diskPanel, "Clear Disk Button", "디스크 장착 해제",
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -406f), new Vector2(-18f, -352f),
                    new Color32(9, 55, 61, 255), Muted, 17)
            };

            var statsPanel = CreatePanel(frame, "Factory Performance Summary", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-468f, 122f), new Vector2(-34f, -170f), new Color32(3, 30, 37, 245), 14f);
            CreateText(statsPanel, "Factory Performance Header", "CELL PERFORMANCE", 18, Cyan,
                TextAnchor.UpperLeft, new Vector2(24f, -22f), new Vector2(360f, 30f), FontStyle.Bold);
            var slotTitle = CreateText(statsPanel, "Factory Selected Slot", "SLOT 01  /  SC-01 ASSEMBLY CELL", 18, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -62f), new Vector2(380f, 32f), FontStyle.Bold);
            var performance = CreateText(statsPanel, "Factory Performance Text",
                "생산 효율    100%\n작업 속도    100%\n품질 지수    80\n\n예상 사이클  8.0s\n사이클 산출  1 UNIT", 21, Bone,
                TextAnchor.UpperLeft, new Vector2(24f, -124f), new Vector2(380f, 250f), FontStyle.Bold);
            CreateText(statsPanel, "Factory Performance Rule", "생산 효율 120% 이상이면\n한 사이클에 완제품 2개를 생산합니다.", 16, Amber,
                TextAnchor.UpperLeft, new Vector2(24f, -388f), new Vector2(380f, 58f), FontStyle.Bold);
            var installButton = CreateButton(statsPanel, "Install Factory Machine Button", "설비 배치    ▶    ₩ 3,500",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(22f, 98f), new Vector2(-22f, 158f),
                Amber, Navy, 19);
            var productionButton = CreateButton(statsPanel, "Open Selected Production Button", "생산 제어 열기    ▶",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(22f, 24f), new Vector2(-22f, 84f),
                Teal, Bone, 19);

            var footer = CreatePanel(frame, "Factory Loadout Footer", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(34f, 22f), new Vector2(-34f, 104f), new Color32(3, 19, 25, 245), 10f);
            var status = CreateText(footer, "Factory Loadout Status",
                "CONFIGURATION READY  /  설비·인력·디스크를 선택하세요.", 18, Muted,
                TextAnchor.MiddleLeft, new Vector2(22f, 0f), new Vector2(1180f, 82f), FontStyle.Bold);
            CreateText(footer, "Factory Loadout Hint", "배정 정보는 즉시 저장됩니다.  |  ESC 닫기", 16, Muted,
                TextAnchor.MiddleRight, new Vector2(1190f, 0f), new Vector2(390f, 82f), FontStyle.Normal);

            var component = overlay.gameObject.AddComponent<SemiconFactoryLoadoutPanel>();
            component.Configure(group, frame, slotTitle, machineStatus, workerName, workerBonus, diskName, diskBonus,
                performance, status, slotButtons, workerButtons, diskButtons, installButton, productionButton,
                closeButton, productionPanel, hud);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return component;
        }

        private static SemiconScenePortal BuildResearchDistrict(Transform parent, SemiconHud hud)
        {
            var district = NewChild(parent, "Research District Gameplay");
            var labRoot = GameObject.Find("LABBUILDING");
            var parkRoot = GameObject.Find("Park");
            var labChildren = labRoot != null
                ? Enumerable.Range(0, labRoot.transform.childCount).Select(index => labRoot.transform.GetChild(index)).ToList()
                : new List<Transform>();

            var processNames = new[]
            {
                "01 WAFER", "02 OXIDATION", "03 PHOTO", "04 ETCH",
                "05 DEPOSITION", "06 METAL", "07 EDS", "08 PACKAGE"
            };

            var parkCenter = GetObjectBounds(parkRoot, out var parkBounds) ? parkBounds.center : Vector3.zero;
            var kinds = new[]
            {
                SemiconInteriorKind.Wafer, SemiconInteriorKind.Oxidation, SemiconInteriorKind.Photo,
                SemiconInteriorKind.Etch, SemiconInteriorKind.Deposition, SemiconInteriorKind.Metal,
                SemiconInteriorKind.Eds, SemiconInteriorKind.Package
            };
            var prompts = new[]
            {
                "[E]  웨이퍼 공정동 입장", "[E]  산화 공정 연구실 입장", "[E]  포토 공정 연구실 입장",
                "[E]  식각 공정 연구실 입장", "[E]  증착 공정 연구실 입장", "[E]  금속 배선 연구실 입장",
                "[E]  EDS 검사 연구실 입장", "[E]  패키징 연구실 입장"
            };
            var doors = new SemiconScenePortal[processNames.Length];
            for (var index = 0; index < labChildren.Count && index < processNames.Length; index++)
            {
                var building = labChildren[index];
                if (!GetObjectBounds(building.gameObject, out var bounds))
                {
                    continue;
                }

                var facing = parkCenter - bounds.center;
                facing.y = 0f;
                if (facing.sqrMagnitude < 0.01f) facing = Vector3.back;
                facing.Normalize();
                var radius = Mathf.Abs(facing.x) * bounds.extents.x + Mathf.Abs(facing.z) * bounds.extents.z + 2.4f;
                var signPosition = bounds.center + facing * radius;
                signPosition.y = FindGroundY(signPosition) + 2.25f;
                CreateWorldSign(district.transform, processNames[index], signPosition, facing,
                    index >= 1 && index <= 6 ? Amber : Teal);

                var doorPosition = signPosition;
                doorPosition.y = FindGroundY(doorPosition);
                doors[index] = SemiconInteriorSceneBuilder.CreateExteriorDoor(district.transform,
                    $"Process {index + 1:00} Building Entrance", kinds[index], doorPosition, facing, hud,
                    prompts[index], index + 1);
            }

            var fallbackStart = new Vector3(-34f, 0f, 25f);
            for (var index = 0; index < doors.Length; index++)
            {
                if (doors[index] != null) continue;
                var fallback = fallbackStart + new Vector3(index * 7f, 0f, 0f);
                fallback.y = FindGroundY(fallback);
                doors[index] = SemiconInteriorSceneBuilder.CreateExteriorDoor(district.transform,
                    $"Process {index + 1:00} Building Entrance", kinds[index], fallback, Vector3.back, hud,
                    prompts[index], index + 1);
            }
            return doors[2];
        }

        private static SemiconPackageTerminal CreatePackageTerminal(Transform parent, Vector3 position,
            Vector3 facing, PackageExperimentPanel panel)
        {
            var root = NewChild(parent, "Package Experiment Terminal");
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.1f, 0f);
            trigger.size = new Vector3(2.6f, 2.8f, 2.6f);

            var baseObject = CreatePrimitiveChild(root.transform, "Terminal Base", PrimitiveType.Cube,
                new Vector3(0f, 0.45f, 0f), new Vector3(1.55f, 0.9f, 0.7f),
                GetMaterial("TerminalDark", new Color32(4, 38, 44, 255), false));
            RemoveCollider(baseObject);
            var screen = CreatePrimitiveChild(root.transform, "Terminal Screen", PrimitiveType.Cube,
                new Vector3(0f, 1.38f, 0.02f), new Vector3(1.46f, 0.84f, 0.18f),
                GetMaterial("PackageTerminalScreen", new Color32(74, 42, 94, 255), true));
            RemoveCollider(screen);
            var glow = CreatePrimitiveChild(root.transform, "Hologram", PrimitiveType.Cylinder,
                new Vector3(0f, 2.05f, 0f), new Vector3(0.24f, 0.035f, 0.24f),
                GetMaterial("PackageTerminalGlow", new Color32(193, 104, 255, 255), true));
            RemoveCollider(glow);

            var terminal = root.AddComponent<SemiconPackageTerminal>();
            terminal.Configure(panel, glow.transform);
            return terminal;
        }

        private static SemiconEdsTerminal CreateEdsTerminal(Transform parent, Vector3 position,
            Vector3 facing, EdsExperimentPanel panel)
        {
            var root = NewChild(parent, "EDS Experiment Terminal");
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.1f, 0f);
            trigger.size = new Vector3(2.6f, 2.8f, 2.6f);

            var baseObject = CreatePrimitiveChild(root.transform, "Terminal Base", PrimitiveType.Cube,
                new Vector3(0f, 0.45f, 0f), new Vector3(1.55f, 0.9f, 0.7f),
                GetMaterial("TerminalDark", new Color32(4, 38, 44, 255), false));
            RemoveCollider(baseObject);
            var screen = CreatePrimitiveChild(root.transform, "Terminal Screen", PrimitiveType.Cube,
                new Vector3(0f, 1.38f, 0.02f), new Vector3(1.46f, 0.84f, 0.18f),
                GetMaterial("TerminalScreen", new Color32(8, 94, 98, 255), true));
            RemoveCollider(screen);
            var glow = CreatePrimitiveChild(root.transform, "Hologram", PrimitiveType.Cylinder,
                new Vector3(0f, 2.05f, 0f), new Vector3(0.24f, 0.035f, 0.24f),
                GetMaterial("EdsTerminalGlow", new Color32(238, 103, 89, 255), true));
            RemoveCollider(glow);

            var terminal = root.AddComponent<SemiconEdsTerminal>();
            terminal.Configure(panel, glow.transform);
            return terminal;
        }

        private static SemiconMetalTerminal CreateMetalTerminal(Transform parent, Vector3 position,
            Vector3 facing, MetalExperimentPanel panel)
        {
            var root = NewChild(parent, "Metal Experiment Terminal");
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.1f, 0f);
            trigger.size = new Vector3(2.6f, 2.8f, 2.6f);

            var baseObject = CreatePrimitiveChild(root.transform, "Terminal Base", PrimitiveType.Cube,
                new Vector3(0f, 0.45f, 0f), new Vector3(1.55f, 0.9f, 0.7f),
                GetMaterial("TerminalDark", new Color32(4, 38, 44, 255), false));
            RemoveCollider(baseObject);
            var screen = CreatePrimitiveChild(root.transform, "Terminal Screen", PrimitiveType.Cube,
                new Vector3(0f, 1.38f, 0.02f), new Vector3(1.46f, 0.84f, 0.18f),
                GetMaterial("TerminalScreen", new Color32(8, 94, 98, 255), true));
            RemoveCollider(screen);
            var glow = CreatePrimitiveChild(root.transform, "Hologram", PrimitiveType.Cylinder,
                new Vector3(0f, 2.05f, 0f), new Vector3(0.24f, 0.035f, 0.24f),
                GetMaterial("MetalTerminalGlow", new Color32(222, 177, 76, 255), true));
            RemoveCollider(glow);

            var terminal = root.AddComponent<SemiconMetalTerminal>();
            terminal.Configure(panel, glow.transform);
            return terminal;
        }

        private static SemiconDepositionTerminal CreateDepositionTerminal(Transform parent, Vector3 position,
            Vector3 facing, DepositionExperimentPanel panel)
        {
            var root = NewChild(parent, "Deposition Experiment Terminal");
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.1f, 0f);
            trigger.size = new Vector3(2.6f, 2.8f, 2.6f);

            var baseObject = CreatePrimitiveChild(root.transform, "Terminal Base", PrimitiveType.Cube,
                new Vector3(0f, 0.45f, 0f), new Vector3(1.55f, 0.9f, 0.7f),
                GetMaterial("TerminalDark", new Color32(4, 38, 44, 255), false));
            RemoveCollider(baseObject);
            var screen = CreatePrimitiveChild(root.transform, "Terminal Screen", PrimitiveType.Cube,
                new Vector3(0f, 1.38f, 0.02f), new Vector3(1.46f, 0.84f, 0.18f),
                GetMaterial("TerminalScreen", new Color32(8, 94, 98, 255), true));
            RemoveCollider(screen);
            var glow = CreatePrimitiveChild(root.transform, "Hologram", PrimitiveType.Cylinder,
                new Vector3(0f, 2.05f, 0f), new Vector3(0.24f, 0.035f, 0.24f),
                GetMaterial("DepositionTerminalGlow", new Color32(77, 201, 143, 255), true));
            RemoveCollider(glow);

            var terminal = root.AddComponent<SemiconDepositionTerminal>();
            terminal.Configure(panel, glow.transform);
            return terminal;
        }

        private static SemiconEtchTerminal CreateEtchTerminal(Transform parent, Vector3 position,
            Vector3 facing, EtchExperimentPanel panel)
        {
            var root = NewChild(parent, "Etch Experiment Terminal");
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.1f, 0f);
            trigger.size = new Vector3(2.6f, 2.8f, 2.6f);

            var baseObject = CreatePrimitiveChild(root.transform, "Terminal Base", PrimitiveType.Cube,
                new Vector3(0f, 0.45f, 0f), new Vector3(1.55f, 0.9f, 0.7f),
                GetMaterial("TerminalDark", new Color32(4, 38, 44, 255), false));
            RemoveCollider(baseObject);
            var screen = CreatePrimitiveChild(root.transform, "Terminal Screen", PrimitiveType.Cube,
                new Vector3(0f, 1.38f, 0.02f), new Vector3(1.46f, 0.84f, 0.18f),
                GetMaterial("TerminalScreen", new Color32(8, 94, 98, 255), true));
            RemoveCollider(screen);
            var glow = CreatePrimitiveChild(root.transform, "Hologram", PrimitiveType.Cylinder,
                new Vector3(0f, 2.05f, 0f), new Vector3(0.24f, 0.035f, 0.24f),
                GetMaterial("TerminalGlow", new Color32(116, 91, 220, 255), true));
            RemoveCollider(glow);

            var terminal = root.AddComponent<SemiconEtchTerminal>();
            terminal.Configure(panel, glow.transform);
            return terminal;
        }

        private static SemiconOxidationTerminal CreateOxidationTerminal(Transform parent, Vector3 position,
            Vector3 facing, OxidationExperimentPanel panel)
        {
            var root = NewChild(parent, "Oxidation Experiment Terminal");
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.1f, 0f);
            trigger.size = new Vector3(2.6f, 2.8f, 2.6f);

            var baseObject = CreatePrimitiveChild(root.transform, "Terminal Base", PrimitiveType.Cube,
                new Vector3(0f, 0.45f, 0f), new Vector3(1.55f, 0.9f, 0.7f),
                GetMaterial("TerminalDark", new Color32(4, 38, 44, 255), false));
            RemoveCollider(baseObject);
            var screen = CreatePrimitiveChild(root.transform, "Terminal Screen", PrimitiveType.Cube,
                new Vector3(0f, 1.38f, 0.02f), new Vector3(1.46f, 0.84f, 0.18f),
                GetMaterial("TerminalScreen", new Color32(8, 94, 98, 255), true));
            RemoveCollider(screen);
            var glow = CreatePrimitiveChild(root.transform, "Hologram", PrimitiveType.Cylinder,
                new Vector3(0f, 2.05f, 0f), new Vector3(0.24f, 0.035f, 0.24f),
                GetMaterial("TerminalGlow", Amber, true));
            RemoveCollider(glow);

            var terminal = root.AddComponent<SemiconOxidationTerminal>();
            terminal.Configure(panel, glow.transform);
            return terminal;
        }

        private static SemiconInteractionTerminal CreatePhotoTerminal(Transform parent, Vector3 position, Vector3 facing, PhotoExperimentPanel panel)
        {
            var root = NewChild(parent, "Photo Experiment Terminal");
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.1f, 0f);
            trigger.size = new Vector3(2.6f, 2.8f, 2.6f);

            var baseObject = CreatePrimitiveChild(root.transform, "Terminal Base", PrimitiveType.Cube,
                new Vector3(0f, 0.45f, 0f), new Vector3(1.55f, 0.9f, 0.7f), GetMaterial("TerminalDark", new Color32(4, 38, 44, 255), false));
            RemoveCollider(baseObject);
            var screen = CreatePrimitiveChild(root.transform, "Terminal Screen", PrimitiveType.Cube,
                new Vector3(0f, 1.38f, 0.02f), new Vector3(1.46f, 0.84f, 0.18f), GetMaterial("TerminalScreen", new Color32(8, 94, 98, 255), true));
            RemoveCollider(screen);
            var glow = CreatePrimitiveChild(root.transform, "Hologram", PrimitiveType.Cylinder,
                new Vector3(0f, 2.05f, 0f), new Vector3(0.24f, 0.035f, 0.24f), GetMaterial("TerminalGlow", Cyan, true));
            RemoveCollider(glow);

            var terminal = root.AddComponent<SemiconInteractionTerminal>();
            terminal.Configure(panel, glow.transform);
            return terminal;
        }

        private static SemiconScenePortal BuildMarketDistrict(Transform parent, SemiconHud hud)
        {
            var district = NewChild(parent, "Market District Gameplay");
            var entrance = GameObject.Find("Materials_Main_Entrance_Level_Threshold");
            var doorway = entrance != null ? entrance.transform.position : new Vector3(48.4f, 0.1f, -17.9f);
            var facing = Vector3.forward;

            var signPosition = doorway + facing * 0.35f;
            signPosition.y = FindGroundY(signPosition) + 2.65f;
            CreateWorldSign(district.transform, "SUPPLY / MARKET", signPosition, facing, Amber);

            var entrancePosition = doorway + facing * 1.45f;
            entrancePosition.y = FindGroundY(entrancePosition);
            return SemiconInteriorSceneBuilder.CreateExteriorDoor(district.transform, "Materials Hall Entrance",
                SemiconInteriorKind.Market, entrancePosition, facing, hud, "[E]  자재 거래소 입장");
        }

        private static SemiconContractTerminal CreateContractTerminal(Transform parent, Vector3 position,
            Vector3 facing, SemiconContractPanel panel)
        {
            var root = NewChild(parent, "FAB Contract Board Terminal");
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true; trigger.center = new Vector3(0f, 1.1f, 0f);
            trigger.size = new Vector3(2.8f, 2.8f, 2.8f);
            var baseObject = CreatePrimitiveChild(root.transform, "Contract Terminal Base", PrimitiveType.Cube,
                new Vector3(0f, 0.45f, 0f), new Vector3(1.55f, 0.9f, 0.7f),
                GetMaterial("ContractTerminalDark", new Color32(4, 38, 44, 255), false));
            RemoveCollider(baseObject);
            var screen = CreatePrimitiveChild(root.transform, "Contract Terminal Screen", PrimitiveType.Cube,
                new Vector3(0f, 1.38f, 0.02f), new Vector3(1.46f, 0.84f, 0.18f),
                GetMaterial("ContractTerminalScreen", new Color32(120, 74, 5, 255), true));
            RemoveCollider(screen);
            var glow = CreatePrimitiveChild(root.transform, "Hologram", PrimitiveType.Cylinder,
                new Vector3(0f, 2.05f, 0f), new Vector3(0.24f, 0.035f, 0.24f),
                GetMaterial("ContractTerminalGlow", Amber, true));
            RemoveCollider(glow);
            var terminal = root.AddComponent<SemiconContractTerminal>();
            terminal.Configure(panel, glow.transform);
            return terminal;
        }

        private static SemiconScenePortal BuildWorkspaceEntrance(Transform parent, SemiconHud hud)
        {
            var root = NewChild(parent, "FAB Workspace Gameplay");
            var position = new Vector3(0f, 0f, 8f);
            position.y = FindGroundY(position);
            CreateWorldSign(root.transform, "FAB WORKSPACE / ARCHIVE", position + new Vector3(0f, 2.9f, 0.2f),
                Vector3.back, Cyan);
            return SemiconInteriorSceneBuilder.CreateExteriorDoor(root.transform, "FAB Workspace Entrance",
                SemiconInteriorKind.Workspace, position, Vector3.back, hud, "[E]  워크스페이스 입장");
        }

        private static SemiconMarketTerminal CreateMarketTerminal(Transform parent, Vector3 position, Vector3 facing,
            SemiconMarketPanel panel)
        {
            var root = NewChild(parent, "Materials Exchange Terminal");
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.1f, 0f);
            trigger.size = new Vector3(2.8f, 2.8f, 2.8f);

            var baseObject = CreatePrimitiveChild(root.transform, "Terminal Base", PrimitiveType.Cube,
                new Vector3(0f, 0.45f, 0f), new Vector3(1.55f, 0.9f, 0.7f),
                GetMaterial("MarketTerminalDark", new Color32(4, 38, 44, 255), false));
            RemoveCollider(baseObject);
            var screen = CreatePrimitiveChild(root.transform, "Terminal Screen", PrimitiveType.Cube,
                new Vector3(0f, 1.38f, 0.02f), new Vector3(1.46f, 0.84f, 0.18f),
                GetMaterial("MarketTerminalScreen", new Color32(126, 76, 4, 255), true));
            RemoveCollider(screen);
            var glow = CreatePrimitiveChild(root.transform, "Hologram", PrimitiveType.Cylinder,
                new Vector3(0f, 2.05f, 0f), new Vector3(0.24f, 0.035f, 0.24f),
                GetMaterial("MarketTerminalGlow", Amber, true));
            RemoveCollider(glow);

            var terminal = root.AddComponent<SemiconMarketTerminal>();
            terminal.Configure(panel, glow.transform);
            return terminal;
        }

        private static SemiconScenePortal BuildFactoryDistrict(Transform parent, SemiconHud hud)
        {
            var district = NewChild(parent, "Factory District Gameplay");
            var approach = GameObject.Find("Site_Visitor_Dark_Approach_Path");
            var entrancePosition = approach != null ? approach.transform.position : new Vector3(-15.38f, 0.18f, -43.1f);
            entrancePosition.y = FindGroundY(entrancePosition);

            var signPosition = entrancePosition + Vector3.back * 0.25f;
            signPosition.y = FindGroundY(signPosition) + 2.7f;
            CreateWorldSign(district.transform, "FAB 01 / PRODUCTION", signPosition, Vector3.forward, Teal);

            var doorPosition = entrancePosition + Vector3.forward * 0.8f;
            doorPosition.y = FindGroundY(doorPosition);
            return SemiconInteriorSceneBuilder.CreateExteriorDoor(district.transform, "Factory Visitor Entrance",
                SemiconInteriorKind.Factory, doorPosition, Vector3.forward, hud, "[E]  FAB 01 공장 내부 입장");
        }

        private static void BuildFirstTutorial(Transform parent, SemiconHud hud,
            SemiconPlayerController player, SemiconMarketTerminal marketTerminal,
            SemiconProductionMachine productionMachine)
        {
            var tutorialRoot = NewChild(parent, "First Production Tutorial");
            var beacon = NewChild(tutorialRoot.transform, "Tutorial Objective Beacon");
            var beam = CreatePrimitiveChild(beacon.transform, "Guide Beam", PrimitiveType.Cylinder,
                Vector3.zero, new Vector3(0.16f, 2.6f, 0.16f),
                GetMaterial("TutorialGuideBeam", new Color32(41, 211, 207, 210), true));
            RemoveCollider(beam);
            var ring = CreatePrimitiveChild(beacon.transform, "Guide Ring", PrimitiveType.Cylinder,
                new Vector3(0f, -2.55f, 0f), new Vector3(1.15f, 0.035f, 1.15f),
                GetMaterial("TutorialGuideRing", Amber, true));
            RemoveCollider(ring);
            var pointer = CreatePrimitiveChild(beacon.transform, "Guide Pointer", PrimitiveType.Cube,
                new Vector3(0f, 2.75f, 0f), new Vector3(0.45f, 0.45f, 0.45f),
                GetMaterial("TutorialGuidePointer", Cyan, true));
            pointer.transform.localRotation = Quaternion.Euler(45f, 45f, 45f);
            RemoveCollider(pointer);

            var portals = UnityEngine.Object.FindObjectsByType<SemiconScenePortal>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var marketEntrance = portals.FirstOrDefault(portal => portal.name == "Materials Hall Entrance");
            var factoryEntrance = portals.FirstOrDefault(portal => portal.name == "Factory Visitor Entrance");
            var tutorial = tutorialRoot.AddComponent<SemiconFirstTutorial>();
            tutorial.Configure(hud, player, marketEntrance, marketTerminal, factoryEntrance, productionMachine,
                beacon.transform);
        }

        private static SemiconFactorySlotTerminal CreateFactorySlotTerminal(Transform parent, Vector3 position,
            Vector3 facing, int slotIndex, SemiconFactoryLoadoutPanel panel, GameObject machineVisual)
        {
            var root = NewChild(parent, $"Factory Slot {slotIndex + 1:00} Configuration Terminal");
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.95f, 0f);
            trigger.size = new Vector3(2.6f, 2.4f, 2.1f);

            var baseObject = CreatePrimitiveChild(root.transform, "Slot Terminal Base", PrimitiveType.Cube,
                new Vector3(0f, 0.38f, 0f), new Vector3(1.65f, 0.76f, 0.62f),
                GetMaterial("FactoryTerminalDark", new Color32(4, 38, 44, 255), false));
            RemoveCollider(baseObject);
            var screen = CreatePrimitiveChild(root.transform, "Slot Terminal Screen", PrimitiveType.Cube,
                new Vector3(0f, 1.08f, -0.08f), new Vector3(1.54f, 0.58f, 0.12f),
                GetMaterial("FactoryTerminalScreen", new Color32(7, 116, 117, 255), true));
            RemoveCollider(screen);
            var statusLight = CreatePrimitiveChild(root.transform, "Slot Terminal Status", PrimitiveType.Cylinder,
                new Vector3(0f, 1.52f, 0f), new Vector3(0.15f, 0.035f, 0.15f),
                GetMaterial("FactoryTerminalGlow", slotIndex == 0 ? Amber : Teal, true));
            RemoveCollider(statusLight);

            var terminal = root.AddComponent<SemiconFactorySlotTerminal>();
            terminal.Configure(slotIndex, panel, machineVisual, statusLight.transform);
            return terminal;
        }

        private static GameObject CreateInteriorShell(Transform parent, string name, Vector3 center, Color32 accent,
            string signText)
        {
            var room = NewChild(parent, name);
            room.transform.position = center;
            var floorMaterial = GetMaterial("InteriorFloor", new Color32(6, 32, 38, 255), false);
            var wallMaterial = GetMaterial("InteriorWall", new Color32(8, 54, 59, 255), false);
            var ceilingMaterial = GetMaterial("InteriorCeiling", new Color32(3, 21, 27, 255), false);

            CreatePrimitiveChild(room.transform, "Interior Floor", PrimitiveType.Cube, new Vector3(0f, 0f, 0f),
                new Vector3(18f, 0.28f, 12f), floorMaterial);
            CreatePrimitiveChild(room.transform, "Rear Wall", PrimitiveType.Cube, new Vector3(0f, 2f, 5.9f),
                new Vector3(18f, 4f, 0.28f), wallMaterial);
            CreatePrimitiveChild(room.transform, "Left Wall", PrimitiveType.Cube, new Vector3(-8.9f, 2f, 0f),
                new Vector3(0.28f, 4f, 12f), wallMaterial);
            CreatePrimitiveChild(room.transform, "Right Wall", PrimitiveType.Cube, new Vector3(8.9f, 2f, 0f),
                new Vector3(0.28f, 4f, 12f), wallMaterial);
            CreatePrimitiveChild(room.transform, "Front Wall Left", PrimitiveType.Cube, new Vector3(-5.6f, 2f, -5.9f),
                new Vector3(6.7f, 4f, 0.28f), wallMaterial);
            CreatePrimitiveChild(room.transform, "Front Wall Right", PrimitiveType.Cube, new Vector3(5.6f, 2f, -5.9f),
                new Vector3(6.7f, 4f, 0.28f), wallMaterial);
            CreatePrimitiveChild(room.transform, "Ceiling", PrimitiveType.Cube, new Vector3(0f, 4f, 0f),
                new Vector3(18f, 0.22f, 12f), ceilingMaterial);

            for (var index = -3; index <= 3; index++)
            {
                var strip = CreatePrimitiveChild(room.transform, $"Floor Guide {index + 4}", PrimitiveType.Cube,
                    new Vector3(index * 2.2f, 0.151f, 0f), new Vector3(0.035f, 0.012f, 10.8f),
                    GetMaterial("InteriorGuide", accent, true));
                RemoveCollider(strip);
            }

            for (var index = -2; index <= 2; index++)
            {
                var ceilingLight = CreatePrimitiveChild(room.transform, $"Ceiling Light {index + 3}", PrimitiveType.Cube,
                    new Vector3(index * 3f, 3.82f, 0f), new Vector3(1.7f, 0.04f, 0.32f),
                    GetMaterial("InteriorCeilingLight", Bone, true));
                RemoveCollider(ceilingLight);
            }

            CreateWorldSign(room.transform, signText, center + new Vector3(0f, 2.85f, 5.68f), Vector3.back, accent);
            return room;
        }

        private static Transform CreateMarker(Transform parent, string name, Vector3 localPosition, Vector3 facing)
        {
            var marker = NewChild(parent, name);
            marker.transform.localPosition = localPosition;
            marker.transform.localRotation = Quaternion.LookRotation(facing, Vector3.up);
            return marker.transform;
        }

        private static Transform CreateWorldMarker(Transform parent, string name, Vector3 position, Vector3 facing)
        {
            var marker = NewChild(parent, name);
            marker.transform.position = position;
            marker.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            return marker.transform;
        }

        private static SemiconScenePortal CreatePortal(Transform parent, string name, Vector3 position, Vector3 facing,
            Transform destination, string prompt, string arrivalMessage, SemiconHud hud, Color32 accent,
            GameObject activateZone = null, GameObject deactivateZone = null, bool interiorCamera = false)
        {
            var root = NewChild(parent, name);
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1f, 0f);
            trigger.size = new Vector3(2.6f, 2.5f, 2.2f);

            var pad = CreatePrimitiveChild(root.transform, "Portal Pad", PrimitiveType.Cube,
                new Vector3(0f, 0.05f, 0f), new Vector3(2.2f, 0.08f, 1.1f),
                GetMaterial("PortalPad", new Color32(5, 45, 50, 255), false));
            RemoveCollider(pad);
            var edge = CreatePrimitiveChild(root.transform, "Portal Accent", PrimitiveType.Cube,
                new Vector3(0f, 0.105f, 0f), new Vector3(2.05f, 0.025f, 0.96f),
                GetMaterial("PortalAccent", accent, true));
            RemoveCollider(edge);

            var portal = root.AddComponent<SemiconScenePortal>();
            portal.Configure(destination, prompt, arrivalMessage, hud, activateZone, deactivateZone, interiorCamera);
            return portal;
        }

        private static SemiconProductionMachine CreateProductionMachine(Transform parent, Vector3 position, Vector3 facing,
            SemiconProductionPanel panel, int slotIndex = 0)
        {
            var root = NewChild(parent, $"SC-01 Assembly Machine {slotIndex + 1:00}");
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.2f, 0f);
            trigger.size = new Vector3(3.4f, 2.8f, 3.2f);

            var baseObject = CreatePrimitiveChild(root.transform, "Machine Base", PrimitiveType.Cube,
                new Vector3(0f, 0.48f, 0f), new Vector3(3.2f, 0.96f, 1.8f),
                GetMaterial("FactoryMachineDark", new Color32(5, 40, 46, 255), false));
            RemoveCollider(baseObject);
            var chamber = CreatePrimitiveChild(root.transform, "Process Chamber", PrimitiveType.Cube,
                new Vector3(0f, 1.45f, 0.18f), new Vector3(2.45f, 1.0f, 1.35f),
                GetMaterial("FactoryMachineBody", new Color32(15, 90, 94, 255), false));
            RemoveCollider(chamber);
            var screen = CreatePrimitiveChild(root.transform, "Machine Screen", PrimitiveType.Cube,
                new Vector3(0f, 1.55f, -0.53f), new Vector3(1.45f, 0.58f, 0.08f),
                GetMaterial("FactoryMachineScreen", new Color32(7, 125, 126, 255), true));
            RemoveCollider(screen);
            var statusLight = CreatePrimitiveChild(root.transform, "Machine Status Light", PrimitiveType.Cylinder,
                new Vector3(0f, 2.15f, 0f), new Vector3(0.18f, 0.04f, 0.18f),
                GetMaterial("FactoryMachineGlow", Teal, true));
            RemoveCollider(statusLight);

            var machine = root.AddComponent<SemiconProductionMachine>();
            machine.Configure(panel, statusLight.transform, slotIndex);
            return machine;
        }

        internal static GameObject BuildPlayer(Transform parent, Vector3 terminalPosition, out SemiconPlayerController controller,
            out SemiconPlayerInteractor interactor)
        {
            var root = NewChild(parent, "Player");
            var groundY = FindGroundY(terminalPosition);
            var spawnDirection = Vector3.forward;
            var terminalForward = GameObject.Find("Photo Experiment Terminal")?.transform.forward ?? Vector3.back;
            spawnDirection = terminalForward;
            root.transform.position = new Vector3(terminalPosition.x, groundY + 0.06f, terminalPosition.z) + spawnDirection * 4.8f;
            root.transform.rotation = Quaternion.LookRotation(-spawnDirection, Vector3.up);

            var character = root.AddComponent<CharacterController>();
            character.height = 1.9f;
            character.radius = 0.42f;
            character.center = new Vector3(0f, 0.95f, 0f);
            character.stepOffset = 0.32f;
            character.skinWidth = 0.04f;

            var body = CreatePrimitiveChild(root.transform, "Player Visual", PrimitiveType.Capsule,
                new Vector3(0f, 0.95f, 0f), new Vector3(0.82f, 0.95f, 0.82f), GetMaterial("PlayerSuit", new Color32(22, 112, 117, 255), false));
            RemoveCollider(body);
            var stripe = CreatePrimitiveChild(root.transform, "Player Accent", PrimitiveType.Cube,
                new Vector3(0f, 1.15f, 0.37f), new Vector3(0.58f, 0.16f, 0.06f), GetMaterial("PlayerAccent", Amber, true));
            RemoveCollider(stripe);

            controller = root.AddComponent<SemiconPlayerController>();
            root.AddComponent<SemiconFallRecovery>();
            interactor = root.AddComponent<SemiconPlayerInteractor>();
            return root;
        }

        internal static SemiconThirdPersonCamera BuildCamera(Transform parent, Transform player, out Camera gameCamera)
        {
            var cameraObject = NewChild(parent, "Main Camera");
            cameraObject.tag = "MainCamera";
            gameCamera = cameraObject.AddComponent<Camera>();
            gameCamera.fieldOfView = 55f;
            gameCamera.nearClipPlane = 0.12f;
            gameCamera.farClipPlane = 1500f;
            cameraObject.AddComponent<AudioListener>();
            var controller = cameraObject.AddComponent<SemiconThirdPersonCamera>();
            controller.Configure(player);
            return controller;
        }

        internal static void BuildEventSystem(Transform parent)
        {
            foreach (var existing in UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
            var eventSystem = NewChild(parent, "EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        internal static void AddRuntimeFont(GameObject canvas)
        {
            canvas.AddComponent<SemiconRuntimeFont>();
        }

        private static void AddSceneInstructions(Transform parent)
        {
            // Keep this as a plain hierarchy marker: an Editor-only MonoBehaviour
            // serialized into the scene becomes a missing script in a player build.
            NewChild(parent, "README - WASD Move - E Interact - ESC Close");
            /*
            info.AddComponent<SemiconSceneReadme>().Configure(
                "WASD 이동 / Shift 달리기 / 마우스 카메라 / E 포토 단말기 / ESC 닫기\n" +
                "첫 목표: 수율 88%, 정밀도 90%를 만족하는 포토 레시피 확보");
            */
        }

        private static RectTransform CreatePhotoGlassPanel(Transform parent, string name, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color fillColor, float cornerCut)
        {
            var outer = CreatePanel(parent, name, anchorMin, anchorMax, offsetMin, offsetMax, PhotoBorder, cornerCut);
            var inner = CreatePanel(outer, "Glass Surface", Vector2.zero, Vector2.one,
                new Vector2(1f, 1f), new Vector2(-1f, -1f), fillColor, Mathf.Max(0f, cornerCut - 1f));
            inner.SetAsFirstSibling();
            var outerGraphic = outer.GetComponent<Graphic>();
            var innerGraphic = inner.GetComponent<Graphic>();
            if (outerGraphic != null) outerGraphic.raycastTarget = false;
            if (innerGraphic != null) innerGraphic.raycastTarget = false;
            return outer;
        }

        private static void CreatePhotoStepTitle(Transform parent, string number, string title, Vector2 position)
        {
            var badge = CreatePanel(parent, $"Step {number} Badge", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(position.x, position.y - 34f), new Vector2(position.x + 34f, position.y), PhotoBlue, 5f);
            badge.GetComponent<Graphic>().raycastTarget = false;
            CreatePhotoText(badge, "Number", number, 18, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(34f, 34f), FontStyle.Bold);
            CreatePhotoText(parent, $"Step {number} Title", title, 22, PhotoInk, TextAnchor.UpperLeft,
                new Vector2(position.x + 48f, position.y), new Vector2(360f, 36f), FontStyle.Bold);
        }

        private static void CreatePhotoCompactParameterCard(Transform parent, string name, string index,
            string koreanLabel, string englishLabel, string initialValue, float top, float min, float max,
            float value, bool wholeNumbers, float targetMin, float targetMax, string minimumLabel,
            string maximumLabel, string recommendedLabel, out TMP_Text valueText, out Slider slider,
            out Button minusButton, out Button plusButton)
        {
            var card = CreatePhotoGlassPanel(parent, name + " Compact Parameter", new Vector2(0f, 1f),
                new Vector2(1f, 1f), new Vector2(16f, top - 182f), new Vector2(-16f, top),
                new Color32(247, 251, 252, 218), 6f);
            CreatePhotoText(card, "Index", index, 14, PhotoBlue, TextAnchor.UpperLeft,
                new Vector2(14f, -12f), new Vector2(28f, 22f), FontStyle.Bold);
            CreatePhotoText(card, "Label", koreanLabel + "  /  " + englishLabel, 15, PhotoInk,
                TextAnchor.UpperLeft, new Vector2(46f, -11f), new Vector2(238f, 24f), FontStyle.Bold);
            valueText = CreatePhotoText(card, "Value", initialValue, 27, PhotoInk, TextAnchor.MiddleCenter,
                new Vector2(54f, -38f), new Vector2(204f, 42f), FontStyle.Bold);

            minusButton = CreatePhotoButton(card, name + " Minus Button", "−", new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(14f, -108f), new Vector2(50f, -72f),
                new Color32(235, 245, 248, 232), PhotoInk, 19);
            plusButton = CreatePhotoButton(card, name + " Plus Button", "+", new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-50f, -108f), new Vector2(-14f, -72f),
                new Color32(235, 245, 248, 232), PhotoInk, 19);
            slider = CreatePhotoSlider(card, name + " Slider", new Vector2(58f, -106f), new Vector2(196f, 26f),
                min, max, value, wholeNumbers, targetMin, targetMax);

            CreatePhotoText(card, "Minimum", minimumLabel, 12, PhotoInkMuted, TextAnchor.UpperLeft,
                new Vector2(58f, -130f), new Vector2(64f, 18f), FontStyle.Bold);
            CreatePhotoText(card, "Maximum", maximumLabel, 12, PhotoInkMuted, TextAnchor.UpperRight,
                new Vector2(196f, -130f), new Vector2(58f, 18f), FontStyle.Bold);
            var safePill = CreatePanel(card, "Safe Range", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(14f, -170f), new Vector2(100f, -148f), new Color32(220, 245, 235, 220), 5f);
            CreatePhotoText(safePill, "Label", "안전 범위", 12, PhotoMint, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(86f, 22f), FontStyle.Bold);
            var recommendedPill = CreatePanel(card, "Recommended", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-116f, -170f), new Vector2(-14f, -148f), new Color32(225, 242, 249, 222), 5f);
            CreatePhotoText(recommendedPill, "Label", recommendedLabel, 12, PhotoBlue, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(102f, 22f), FontStyle.Bold);
        }

        private static void CreatePhotoForecastRow(Transform parent, string name, string koreanLabel,
            string englishLabel, string targetLabel, float top, out TMP_Text value)
        {
            var row = CreatePhotoGlassPanel(parent, name, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(20f, top - 86f), new Vector2(-20f, top), new Color32(241, 248, 250, 214), 5f);
            CreatePhotoText(row, "Label", koreanLabel + "  /  " + englishLabel, 14, PhotoInk,
                TextAnchor.UpperLeft, new Vector2(14f, -10f), new Vector2(210f, 22f), FontStyle.Bold);
            value = CreatePhotoText(row, "Value", "--.-%", 27, PhotoInk, TextAnchor.MiddleLeft,
                new Vector2(14f, -35f), new Vector2(180f, 40f), FontStyle.Bold);
            CreatePhotoText(row, "Target", targetLabel, 13, PhotoMint, TextAnchor.MiddleRight,
                new Vector2(230f, -38f), new Vector2(190f, 36f), FontStyle.Bold);
        }

        private static void CreatePhotoCompactResultRow(Transform parent, string name, string koreanLabel,
            string englishLabel, string targetLabel, float top, out TMP_Text value, out TMP_Text delta,
            out TMP_Text target)
        {
            var row = CreatePhotoGlassPanel(parent, name, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(20f, top - 86f), new Vector2(-20f, top), new Color32(247, 251, 252, 226), 5f);
            CreatePhotoText(row, "Label", koreanLabel + "  /  " + englishLabel, 14, PhotoInk,
                TextAnchor.UpperLeft, new Vector2(14f, -10f), new Vector2(190f, 22f), FontStyle.Bold);
            value = CreatePhotoText(row, "Value", "0.0%", 29, PhotoInk, TextAnchor.MiddleLeft,
                new Vector2(14f, -35f), new Vector2(148f, 40f), FontStyle.Bold);
            delta = CreatePhotoText(row, "Delta", "이전 --.-%", 14, PhotoMint, TextAnchor.MiddleLeft,
                new Vector2(174f, -38f), new Vector2(174f, 36f), FontStyle.Bold);
            target = CreatePhotoText(row, "Target", targetLabel, 14, PhotoMint, TextAnchor.MiddleRight,
                new Vector2(350f, -32f), new Vector2(196f, 48f), FontStyle.Bold);
        }

        private static void CreatePhotoParameterCard(Transform parent, string name, string index, string koreanLabel,
            string englishLabel, string initialValue, float top, float min, float max, float value, bool wholeNumbers,
            float targetMin, float targetMax, string minimumLabel, string maximumLabel, string recommendedLabel,
            out TMP_Text valueText, out Slider slider, out Button minusButton, out Button plusButton)
        {
            var card = CreatePhotoGlassPanel(parent, name + " Parameter Card", new Vector2(0f, 1f),
                new Vector2(1f, 1f), new Vector2(24f, top - 210f), new Vector2(-24f, top),
                new Color32(250, 252, 252, 226), 7f);
            CreatePhotoText(card, "Index", index, 19, PhotoBlue, TextAnchor.UpperLeft,
                new Vector2(18f, -14f), new Vector2(36f, 28f), FontStyle.Bold);
            CreatePhotoText(card, "Label", koreanLabel + "  /  " + englishLabel, 19, PhotoInk, TextAnchor.UpperLeft,
                new Vector2(58f, -14f), new Vector2(380f, 28f), FontStyle.Bold);
            valueText = CreatePhotoText(card, "Value", initialValue, 35, PhotoInk, TextAnchor.MiddleCenter,
                new Vector2(110f, -48f), new Vector2(326f, 54f), FontStyle.Bold);

            minusButton = CreatePhotoButton(card, name + " Minus Button", "−", new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(18f, -122f), new Vector2(68f, -72f),
                new Color32(237, 246, 248, 246), PhotoInk, 24);
            plusButton = CreatePhotoButton(card, name + " Plus Button", "+", new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-68f, -122f), new Vector2(-18f, -72f),
                new Color32(237, 246, 248, 246), PhotoInk, 24);
            slider = CreatePhotoSlider(card, name + " Slider", new Vector2(84f, -121f), new Vector2(378f, 28f),
                min, max, value, wholeNumbers, targetMin, targetMax);

            CreatePhotoText(card, "Minimum", minimumLabel, 16, PhotoInkMuted, TextAnchor.UpperLeft,
                new Vector2(84f, -151f), new Vector2(70f, 22f), FontStyle.Bold);
            CreatePhotoText(card, "Maximum", maximumLabel, 16, PhotoInkMuted, TextAnchor.UpperRight,
                new Vector2(390f, -151f), new Vector2(72f, 22f), FontStyle.Bold);
            var safePill = CreatePanel(card, "Safe Range", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(18f, -198f), new Vector2(126f, -170f), new Color32(220, 245, 235, 238), 7f);
            CreatePhotoText(safePill, "Label", "안전 범위", 16, PhotoMint, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(108f, 28f), FontStyle.Bold);
            var recommendedPill = CreatePanel(card, "Recommended", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-142f, -198f), new Vector2(-18f, -170f), new Color32(226, 242, 250, 238), 7f);
            CreatePhotoText(recommendedPill, "Label", recommendedLabel, 16, PhotoBlue, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(124f, 28f), FontStyle.Bold);
        }

        private static Slider CreatePhotoSlider(Transform parent, string name, Vector2 position, Vector2 size,
            float min, float max, float value, bool wholeNumbers, float targetMin, float targetMax)
        {
            var root = NewUiChild(parent, name);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var background = CreatePanel(root.transform, "Background", Vector2.zero, Vector2.one,
                new Vector2(0f, 10f), new Vector2(0f, -10f), PhotoTrack, 4f);
            var targetStart = Mathf.InverseLerp(min, max, targetMin);
            var targetEnd = Mathf.InverseLerp(min, max, targetMax);
            var target = CreatePanel(root.transform, "Target Range", new Vector2(targetStart, 0.5f),
                new Vector2(targetEnd, 0.5f), new Vector2(0f, -6f), new Vector2(0f, 6f),
                new Color32(98, 211, 158, 220), 4f);
            var fillArea = NewUiChild(root.transform, "Fill Area");
            Stretch(fillArea.GetComponent<RectTransform>(), new Vector2(0f, 7f), new Vector2(-14f, -7f));
            var fill = CreatePanel(fillArea.transform, "Fill", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, PhotoBlue, 4f);
            var handleArea = NewUiChild(root.transform, "Handle Slide Area");
            Stretch(handleArea.GetComponent<RectTransform>(), new Vector2(7f, 0f), new Vector2(-7f, 0f));
            var handle = CreatePanel(handleArea.transform, "Handle", new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(-11f, -11f), new Vector2(11f, 11f),
                new Color32(252, 253, 250, 255), 11f);

            var slider = root.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.wholeNumbers = wholeNumbers;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Graphic>();
            slider.direction = Slider.Direction.LeftToRight;
            background.GetComponent<Graphic>().raycastTarget = false;
            target.GetComponent<Graphic>().raycastTarget = false;
            return slider;
        }

        private static void CreatePhotoPreviewMetric(Transform parent, string name, string label, string targetLabel, Vector2 position,
            out TMP_Text value)
        {
            var card = CreatePhotoGlassPanel(parent, name, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(position.x, position.y - 108f), new Vector2(position.x + 276f, position.y),
                PhotoGlassSoft, 6f);
            CreatePhotoText(card, "Label", label, 17, PhotoInkMuted, TextAnchor.UpperLeft,
                new Vector2(16f, -12f), new Vector2(244f, 26f), FontStyle.Bold);
            value = CreatePhotoText(card, "Value", "--.-%", 32, PhotoInk, TextAnchor.UpperLeft,
                new Vector2(16f, -42f), new Vector2(244f, 42f), FontStyle.Bold);
            CreatePhotoText(card, "Target", targetLabel, 16, PhotoMint, TextAnchor.UpperLeft,
                new Vector2(16f, -82f), new Vector2(244f, 24f), FontStyle.Bold);
        }

        private static void CreatePhotoResultMetric(Transform parent, string name, string label, string targetLabel,
            float top, out TMP_Text value, out TMP_Text delta, out TMP_Text target)
        {
            var row = CreatePhotoGlassPanel(parent, name, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(24f, top - 112f), new Vector2(-24f, top), new Color32(249, 252, 252, 244), 6f);
            CreatePhotoText(row, "Label", label, 17, PhotoInk, TextAnchor.UpperLeft,
                new Vector2(18f, -12f), new Vector2(250f, 25f), FontStyle.Bold);
            value = CreatePhotoText(row, "Value", "0.0%", 34, PhotoInk, TextAnchor.UpperLeft,
                new Vector2(18f, -42f), new Vector2(170f, 48f), FontStyle.Bold);
            delta = CreatePhotoText(row, "Delta", "이전 --.-%", 17, PhotoMint, TextAnchor.MiddleLeft,
                new Vector2(190f, -48f), new Vector2(230f, 40f), FontStyle.Bold);
            target = CreatePhotoText(row, "Target", targetLabel, 17, PhotoMint, TextAnchor.MiddleRight,
                new Vector2(438f, -32f), new Vector2(220f, 60f), FontStyle.Bold);
        }

        private static void CreateMetric(Transform parent, string name, string label, Vector2 position, out Text value)
        {
            var metric = CreatePanel(parent, name + " Metric", new Vector2(0f, 1f), new Vector2(0f, 1f),
                position, position + new Vector2(630f, 76f), new Color32(5, 48, 54, 255), 8f);
            CreateText(metric, name + " Label", label, 17, Muted, TextAnchor.MiddleLeft,
                new Vector2(18f, 0f), new Vector2(400f, 76f), FontStyle.Bold);
            value = CreateText(metric, name + " Value", "--.-%", 28, Bone, TextAnchor.MiddleRight,
                new Vector2(430f, 0f), new Vector2(180f, 76f), FontStyle.Bold);
        }

        private static void CreateMarketCard(Transform parent, string name, string index, string title,
            string description, string price, string buttonLabel, float top, float bottom,
            out Text bundleText, out Button purchaseButton)
        {
            var card = CreatePanel(parent, name + " Card", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(20f, bottom), new Vector2(-20f, top), new Color32(4, 48, 54, 255), 10f);
            CreatePanel(card, "Accent", new Vector2(0f, 0f), new Vector2(0f, 1f),
                Vector2.zero, new Vector2(6f, 0f), Teal, 0f);
            CreateText(card, "Index", index, 18, Cyan, TextAnchor.UpperLeft,
                new Vector2(20f, -18f), new Vector2(42f, 28f), FontStyle.Bold);
            CreateText(card, "Title", title, 22, Bone, TextAnchor.UpperLeft,
                new Vector2(72f, -16f), new Vector2(420f, 34f), FontStyle.Bold);
            CreateText(card, "Description", description, 16, Muted, TextAnchor.UpperLeft,
                new Vector2(72f, -54f), new Vector2(460f, 26f), FontStyle.Normal);
            CreateText(card, "Unit Price", price, 20, Amber, TextAnchor.UpperLeft,
                new Vector2(570f, -24f), new Vector2(180f, 32f), FontStyle.Bold);
            bundleText = CreateText(card, "Bundle", "BUNDLE  /  10 EA", 15, Muted, TextAnchor.UpperLeft,
                new Vector2(570f, -62f), new Vector2(190f, 26f), FontStyle.Bold);
            purchaseButton = CreateButton(card, "Buy " + name + " Button", buttonLabel,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-324f, -112f), new Vector2(-24f, -48f),
                Amber, Navy, 18);
        }

        private static void CreateInventoryRow(Transform parent, string name, string label, Vector2 position,
            out Text value)
        {
            var row = CreatePanel(parent, name, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(24f, position.y - 50f), new Vector2(-24f, position.y), new Color32(5, 48, 54, 255), 7f);
            CreateText(row, "Label", label, 17, Muted, TextAnchor.MiddleLeft,
                new Vector2(16f, 0f), new Vector2(250f, 50f), FontStyle.Bold);
            value = CreateText(row, "Value", "0 EA", 19, Bone, TextAnchor.MiddleRight,
                new Vector2(280f, 0f), new Vector2(150f, 50f), FontStyle.Bold);
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 position, Vector2 size,
            float min, float max, float value, bool wholeNumbers)
        {
            var root = NewUiChild(parent, name);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var background = CreatePanel(root.transform, "Background", Vector2.zero, Vector2.one,
                new Vector2(0f, 10f), new Vector2(0f, -10f), new Color32(27, 65, 70, 255), 5f);
            var fillArea = NewUiChild(root.transform, "Fill Area");
            Stretch(fillArea.GetComponent<RectTransform>(), new Vector2(0f, 7f), new Vector2(-14f, -7f));
            var fill = CreatePanel(fillArea.transform, "Fill", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, Teal, 5f);
            var handleArea = NewUiChild(root.transform, "Handle Slide Area");
            Stretch(handleArea.GetComponent<RectTransform>(), new Vector2(7f, 0f), new Vector2(-7f, 0f));
            var handle = CreatePanel(handleArea.transform, "Handle", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(-11f, -11f), new Vector2(11f, 11f), Bone, 11f);

            var slider = root.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.wholeNumbers = wholeNumbers;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Graphic>();
            slider.direction = Slider.Direction.LeftToRight;
            background.GetComponent<Graphic>().raycastTarget = false;
            return slider;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color, Color textColor, int fontSize)
        {
            var rect = CreatePanel(parent, name, anchorMin, anchorMax, offsetMin, offsetMax, color, 10f);
            var graphic = rect.GetComponent<SemiconCutCornerGraphic>();
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.78f, 0.82f, 0.82f, 1f);
            colors.disabledColor = new Color(0.35f, 0.4f, 0.4f, 0.75f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            CreateText(rect, "Label", label, fontSize, textColor, TextAnchor.MiddleCenter,
                Vector2.zero, RectSize(rect), FontStyle.Bold);
            return button;
        }

        private static Button CreatePhotoButton(Transform parent, string name, string label, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color, Color textColor, int fontSize)
        {
            var rect = CreatePanel(parent, name, anchorMin, anchorMax, offsetMin, offsetMax, color, 6f);
            var graphic = rect.GetComponent<SemiconCutCornerGraphic>();
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
            colors.pressedColor = new Color(0.82f, 0.87f, 0.89f, 1f);
            colors.disabledColor = new Color(0.55f, 0.6f, 0.62f, 0.7f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            CreatePhotoText(rect, "Label", label, fontSize, textColor, TextAnchor.MiddleCenter,
                Vector2.zero, RectSize(rect), FontStyle.Bold);
            return button;
        }

        private static TMP_Text CreatePhotoText(Transform parent, string name, string content, int fontSize,
            Color color, TextAnchor alignment, Vector2 position, Vector2 size, FontStyle style)
        {
            var gameObject = NewUiChild(parent, name);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = gameObject.AddComponent<SemiconSdfText>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style == FontStyle.Bold ? FontStyles.Bold : FontStyles.Normal;
            text.color = color;
            text.alignment = ConvertPhotoAlignment(alignment);
            text.overflowMode = TextOverflowModes.Truncate;
            text.enableWordWrapping = true;
            text.extraPadding = true;
            text.characterSpacing = 0f;
            text.wordSpacing = 0f;
            text.lineSpacing = 0f;
            text.raycastTarget = false;
            return text;
        }

        private static TextAlignmentOptions ConvertPhotoAlignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.TopLeft;
            }
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, Color color,
            TextAnchor alignment, Vector2 position, Vector2 size, FontStyle style)
        {
            var gameObject = NewUiChild(parent, name);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = gameObject.AddComponent<SemiconCrispText>();
            text.text = content;
            text.font = GetEditorFont();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color, float cornerCut)
        {
            var gameObject = NewUiChild(parent, name);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var graphic = gameObject.AddComponent<SemiconCutCornerGraphic>();
            graphic.color = color;
            graphic.CornerCut = cornerCut;
            return rect;
        }

        private static void CreateWorldSign(Transform parent, string text, Vector3 position, Vector3 facing, Color color)
        {
            var signRoot = NewChild(parent, text + " Sign");
            signRoot.transform.position = position;
            signRoot.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            var plate = CreatePrimitiveChild(signRoot.transform, "Plate", PrimitiveType.Cube,
                Vector3.zero, new Vector3(3.4f, 0.72f, 0.12f), GetMaterial("SignDark", NavySoft, false));
            RemoveCollider(plate);
            var accent = CreatePrimitiveChild(signRoot.transform, "Accent", PrimitiveType.Cube,
                new Vector3(-1.58f, 0f, 0.08f), new Vector3(0.08f, 0.58f, 0.08f), GetMaterial("SignAccent", color, true));
            RemoveCollider(accent);
            var textObject = NewChild(signRoot.transform, "Text");
            textObject.transform.localPosition = new Vector3(0.08f, 0f, 0.075f);
            textObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.font = GetEditorFont();
            textMesh.fontSize = 48;
            textMesh.characterSize = 0.08f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;
        }

        private static GameObject CreatePrimitiveChild(Transform parent, string name, PrimitiveType type,
            Vector3 localPosition, Vector3 localScale, Material material)
        {
            var child = GameObject.CreatePrimitive(type);
            child.name = name;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;
            var renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
            return child;
        }

        private static Material GetMaterial(string name, Color color, bool emission)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.color = color;
                ConfigureEmission(existing, color, emission);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = name, color = color };
            ConfigureEmission(material, color, emission);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void ConfigureEmission(Material material, Color color, bool enabled)
        {
            if (!material.HasProperty("_EmissionColor"))
            {
                return;
            }
            if (enabled)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.6f);
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }
        }

        private static bool GetObjectBounds(GameObject target, out Bounds bounds)
        {
            bounds = default;
            if (target == null)
            {
                return false;
            }
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return false;
            }
            bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }
            return true;
        }

        private static float FindGroundY(Vector3 point)
        {
            Physics.SyncTransforms();
            var origin = new Vector3(point.x, 800f, point.z);
            var hits = Physics.RaycastAll(origin, Vector3.down, 1600f, ~0, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
            {
                return point.y;
            }
            Array.Sort(hits, (left, right) => right.point.y.CompareTo(left.point.y));
            return hits[0].point.y;
        }

        private static void DisableExistingCameras()
        {
            foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                camera.enabled = false;
                var listener = camera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }
        }

        private static void RemoveCollider(GameObject target)
        {
            var collider = target != null ? target.GetComponent<Collider>() : null;
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static GameObject NewChild(Transform parent, string name)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static GameObject NewUiChild(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, Vector2.zero, Vector2.zero);
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Vector2 RectSize(RectTransform rect)
        {
            var width = rect.anchorMin.x == rect.anchorMax.x ? rect.offsetMax.x - rect.offsetMin.x : 300f;
            var height = rect.anchorMin.y == rect.anchorMax.y ? rect.offsetMax.y - rect.offsetMin.y : 60f;
            return new Vector2(Mathf.Abs(width), Mathf.Abs(height));
        }

        private static Font GetEditorFont()
        {
            if (editorFont == null)
            {
                editorFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            return editorFont;
        }

        private static void EnsureTextMeshProEssentials()
        {
            const string settingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(settingsPath) != null)
            {
                return;
            }

            throw new InvalidOperationException(
                "TMP Essential Resources가 없습니다. ImportTextMeshProEssentialsBatch를 먼저 실행하세요.");
        }

        private static string FindTextMeshProEssentialsPackage()
        {
            var packageCache = Path.GetFullPath("Library/PackageCache");
            var uguiFolders = Directory.Exists(packageCache)
                ? Directory.GetDirectories(packageCache, "com.unity.ugui@*")
                : Array.Empty<string>();
            var packagePath = uguiFolders
                .Select(folder => Path.Combine(folder, "Package Resources", "TMP Essential Resources.unitypackage"))
                .FirstOrDefault(File.Exists);
            if (string.IsNullOrEmpty(packagePath))
            {
                throw new FileNotFoundException("Unity 6 TMP Essential Resources 패키지를 찾지 못했습니다.");
            }
            return packagePath;
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

        private static void SetPrivateField(UnityEngine.Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().Name, fieldName);
            }
            field.SetValue(target, value);
            EditorUtility.SetDirty(target);
        }
    }

}
#endif
