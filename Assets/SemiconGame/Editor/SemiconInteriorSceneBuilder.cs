#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using SemiconCity.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SemiconCity.Editor
{
    internal enum SemiconInteriorKind
    {
        Wafer = 1,
        Oxidation = 2,
        Photo = 3,
        Etch = 4,
        Deposition = 5,
        Metal = 6,
        Eds = 7,
        Package = 8,
        Factory = 9,
        Market = 10,
        Workspace = 11
    }

    internal static class SemiconInteriorSceneBuilder
    {
        internal const string WorldSceneName = "SemiconCity_Playable";
        private const string SceneFolder = "Assets/SemiconGame/Scenes/Interiors";
        private const string MaterialFolder = "Assets/SemiconGame/Materials/";

        private sealed class Definition
        {
            public SemiconInteriorKind Kind;
            public string SceneName;
            public string DisplayName;
            public Color32 Accent;
            public Vector3 ReturnPosition;
            public Quaternion ReturnRotation = Quaternion.identity;
        }

        private static readonly Dictionary<SemiconInteriorKind, Definition> Definitions =
            new Dictionary<SemiconInteriorKind, Definition>
            {
                { SemiconInteriorKind.Wafer, NewDefinition(SemiconInteriorKind.Wafer, "Semicon_Interior_01_Wafer", "01 WAFER", new Color32(31,190,185,255)) },
                { SemiconInteriorKind.Oxidation, NewDefinition(SemiconInteriorKind.Oxidation, "Semicon_Interior_02_Oxidation", "02 OXIDATION", new Color32(247,169,30,255)) },
                { SemiconInteriorKind.Photo, NewDefinition(SemiconInteriorKind.Photo, "Semicon_Interior_03_Photo", "03 PHOTO", new Color32(16,139,194,255)) },
                { SemiconInteriorKind.Etch, NewDefinition(SemiconInteriorKind.Etch, "Semicon_Interior_04_Etch", "04 ETCH", new Color32(116,91,220,255)) },
                { SemiconInteriorKind.Deposition, NewDefinition(SemiconInteriorKind.Deposition, "Semicon_Interior_05_Deposition", "05 DEPOSITION", new Color32(77,201,143,255)) },
                { SemiconInteriorKind.Metal, NewDefinition(SemiconInteriorKind.Metal, "Semicon_Interior_06_Metal", "06 METAL", new Color32(222,177,76,255)) },
                { SemiconInteriorKind.Eds, NewDefinition(SemiconInteriorKind.Eds, "Semicon_Interior_07_EDS", "07 EDS", new Color32(238,103,89,255)) },
                { SemiconInteriorKind.Package, NewDefinition(SemiconInteriorKind.Package, "Semicon_Interior_08_Package", "08 PACKAGE", new Color32(193,104,255,255)) },
                { SemiconInteriorKind.Factory, NewDefinition(SemiconInteriorKind.Factory, "Semicon_Interior_Factory", "FAB 01 FACTORY", new Color32(31,190,185,255)) },
                { SemiconInteriorKind.Market, NewDefinition(SemiconInteriorKind.Market, "Semicon_Interior_Market", "MATERIALS MARKET", new Color32(247,169,30,255)) },
                { SemiconInteriorKind.Workspace, NewDefinition(SemiconInteriorKind.Workspace, "Semicon_Interior_Workspace", "FAB WORKSPACE", new Color32(42,216,211,255)) }
            };

        internal static string GetSceneName(SemiconInteriorKind kind) => Definitions[kind].SceneName;

        internal static IReadOnlyList<string> GetAllScenePaths()
        {
            var paths = new List<string>();
            foreach (var definition in Definitions.Values)
            {
                paths.Add(GetScenePath(definition));
            }
            return paths;
        }

        internal static SemiconScenePortal CreateExteriorDoor(Transform parent, string objectName,
            SemiconInteriorKind kind, Vector3 groundPosition, Vector3 facing, SemiconHud hud,
            string prompt, int requiredProcess = 0)
        {
            if (facing.sqrMagnitude < 0.01f) facing = Vector3.forward;
            facing.y = 0f;
            facing.Normalize();
            var definition = Definitions[kind];
            definition.ReturnPosition = groundPosition + facing * 2.8f + Vector3.up * 0.08f;
            definition.ReturnRotation = Quaternion.LookRotation(-facing, Vector3.up);

            var root = CreateInteractionCube(parent, objectName, groundPosition + Vector3.up * 1.35f,
                new Vector3(2.5f, 2.7f, 0.55f), definition.Accent);
            root.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            var portal = root.AddComponent<SemiconScenePortal>();
            portal.ConfigureScene(definition.SceneName, new Vector3(0f, 0.08f, -3.8f), Quaternion.identity,
                prompt, $"{definition.DisplayName} 내부로 이동했습니다.", hud, true, requiredProcess);
            return portal;
        }

        internal static void BuildAllInteriorScenes(string worldScenePath)
        {
            EnsureFolder(SceneFolder);
            foreach (var definition in Definitions.Values)
            {
                BuildInteriorScene(definition);
            }

            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(worldScenePath, true) };
            foreach (var path in GetAllScenePaths()) scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(worldScenePath, OpenSceneMode.Single);
            Debug.Log($"[Semicon Interior] PASS / separate scenes={Definitions.Count} / world={worldScenePath}");
        }

        private static void BuildInteriorScene(Definition definition)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("SEMICON_INTERIOR");
            var systems = new GameObject("Systems");
            systems.transform.SetParent(root.transform, false);
            systems.AddComponent<SemiconGameState>();
            systems.AddComponent<SemiconSceneArrival>();

            var canvas = SemiconGameProjectBuilder.BuildCanvas(root.transform, out var hud, out var photoPanel,
                out var oxidationPanel, out var etchPanel, out var depositionPanel, out var metalPanel,
                out var edsPanel, out var marketPanel, out var packagePanel, out var productionPanel,
                out var loadoutPanel, out var contractPanel, out var archivePanel, out var gachaPanel);

            BuildPlaceholderRoom(root.transform, definition);
            BuildMachinePlaceholders(root.transform, definition.Kind, photoPanel, oxidationPanel, etchPanel,
                depositionPanel, metalPanel, edsPanel, packagePanel, marketPanel, productionPanel,
                loadoutPanel, contractPanel, archivePanel, gachaPanel);
            BuildExitDoor(root.transform, definition, hud);

            var player = SemiconGameProjectBuilder.BuildPlayer(root.transform, new Vector3(0f, 0f, 1.5f),
                out var playerController, out var interactor);
            player.transform.SetPositionAndRotation(new Vector3(0f, 0.08f, -3.8f), Quaternion.identity);
            var cameraController = SemiconGameProjectBuilder.BuildCamera(root.transform, player.transform,
                out var gameCamera);
            cameraController.SetInteriorMode(true);
            playerController.ConfigureCamera(gameCamera.transform);
            interactor.Configure(hud, playerController, cameraController);
            SemiconGameProjectBuilder.BuildEventSystem(root.transform);
            SemiconGameProjectBuilder.AddRuntimeFont(canvas.gameObject);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GetScenePath(definition));
        }

        private static void BuildPlaceholderRoom(Transform parent, Definition definition)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "PLACEHOLDER FLOOR - REPLACE WITH MODELED INTERIOR";
            floor.transform.SetParent(parent, false);
            floor.transform.localPosition = new Vector3(0f, -0.16f, 0f);
            floor.transform.localScale = new Vector3(18f, 0.3f, 14f);
            floor.GetComponent<Renderer>().sharedMaterial = LoadMaterial("InteriorFloor");

            var guide = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guide.name = "ROOM TYPE MARKER";
            guide.transform.SetParent(parent, false);
            guide.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            guide.transform.localScale = new Vector3(0.12f, 0.02f, 11f);
            guide.GetComponent<Renderer>().sharedMaterial = CreateAccentMaterial(definition.Accent);
            UnityEngine.Object.DestroyImmediate(guide.GetComponent<Collider>());

            var lightObject = new GameObject("Placeholder Directional Light");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(0.83f, 0.93f, 1f);
            RenderSettings.ambientLight = new Color(0.22f, 0.29f, 0.32f);
        }

        private static void BuildMachinePlaceholders(Transform parent, SemiconInteriorKind kind,
            PhotoExperimentPanel photoPanel, OxidationExperimentPanel oxidationPanel, EtchExperimentPanel etchPanel,
            DepositionExperimentPanel depositionPanel, MetalExperimentPanel metalPanel, EdsExperimentPanel edsPanel,
            PackageExperimentPanel packagePanel, SemiconMarketPanel marketPanel,
            SemiconProductionPanel productionPanel, SemiconFactoryLoadoutPanel loadoutPanel,
            SemiconContractPanel contractPanel, SemiconArchivePanel archivePanel, SemiconGachaPanel gachaPanel)
        {
            switch (kind)
            {
                case SemiconInteriorKind.Wafer:
                {
                    var machine = CreateInteractionCube(parent, "MACHINE CUBE - WAFER PRODUCTION",
                        new Vector3(0f, 1.2f, 2.4f), new Vector3(2.8f, 2.4f, 2.2f), new Color32(31,190,185,255));
                    var component = machine.AddComponent<SemiconProductionMachine>();
                    component.Configure(productionPanel, CreateGlow(machine.transform, new Color32(31,190,185,255)), 0);
                    break;
                }
                case SemiconInteriorKind.Oxidation:
                    CreateExperimentCube<SemiconOxidationTerminal>(parent, "MACHINE CUBE - OXIDATION", new Color32(247,169,30,255),
                        (terminal, glow) => terminal.Configure(oxidationPanel, glow));
                    break;
                case SemiconInteriorKind.Photo:
                    CreateExperimentCube<SemiconInteractionTerminal>(parent, "MACHINE CUBE - PHOTO", new Color32(16,139,194,255),
                        (terminal, glow) => terminal.Configure(photoPanel, glow));
                    break;
                case SemiconInteriorKind.Etch:
                    CreateExperimentCube<SemiconEtchTerminal>(parent, "MACHINE CUBE - ETCH", new Color32(116,91,220,255),
                        (terminal, glow) => terminal.Configure(etchPanel, glow));
                    break;
                case SemiconInteriorKind.Deposition:
                    CreateExperimentCube<SemiconDepositionTerminal>(parent, "MACHINE CUBE - DEPOSITION", new Color32(77,201,143,255),
                        (terminal, glow) => terminal.Configure(depositionPanel, glow));
                    break;
                case SemiconInteriorKind.Metal:
                    CreateExperimentCube<SemiconMetalTerminal>(parent, "MACHINE CUBE - METAL", new Color32(222,177,76,255),
                        (terminal, glow) => terminal.Configure(metalPanel, glow));
                    break;
                case SemiconInteriorKind.Eds:
                    CreateExperimentCube<SemiconEdsTerminal>(parent, "MACHINE CUBE - EDS", new Color32(238,103,89,255),
                        (terminal, glow) => terminal.Configure(edsPanel, glow));
                    break;
                case SemiconInteriorKind.Package:
                    CreateExperimentCube<SemiconPackageTerminal>(parent, "MACHINE CUBE - PACKAGE", new Color32(193,104,255,255),
                        (terminal, glow) => terminal.Configure(packagePanel, glow));
                    break;
                case SemiconInteriorKind.Factory:
                {
                    var machine = CreateInteractionCube(parent, "MACHINE CUBE - PRODUCTION SLOT 01",
                        new Vector3(-2.3f, 1.2f, 2.4f), new Vector3(3f, 2.4f, 2.2f), new Color32(31,190,185,255));
                    var production = machine.AddComponent<SemiconProductionMachine>();
                    production.Configure(productionPanel, CreateGlow(machine.transform, new Color32(31,190,185,255)), 0);
                    var config = CreateInteractionCube(parent, "MACHINE CUBE - WORKER AND DISK CONFIG",
                        new Vector3(2.3f, 1.2f, 2.4f), new Vector3(3f, 2.4f, 2.2f), new Color32(247,169,30,255));
                    var slot = config.AddComponent<SemiconFactorySlotTerminal>();
                    slot.Configure(0, loadoutPanel, machine.transform.GetChild(0).gameObject,
                        CreateGlow(config.transform, new Color32(247,169,30,255)));
                    break;
                }
                case SemiconInteriorKind.Market:
                {
                    var market = CreateInteractionCube(parent, "INTERACTION CUBE - MATERIAL MARKET",
                        new Vector3(-2.3f, 1.2f, 2.4f), new Vector3(3f, 2.4f, 2.2f), new Color32(247,169,30,255));
                    market.AddComponent<SemiconMarketTerminal>().Configure(marketPanel,
                        CreateGlow(market.transform, new Color32(247,169,30,255)));
                    var contract = CreateInteractionCube(parent, "INTERACTION CUBE - CONTRACT BOARD",
                        new Vector3(2.3f, 1.2f, 2.4f), new Vector3(3f, 2.4f, 2.2f), new Color32(31,190,185,255));
                    contract.AddComponent<SemiconContractTerminal>().Configure(contractPanel,
                        CreateGlow(contract.transform, new Color32(31,190,185,255)));
                    break;
                }
                case SemiconInteriorKind.Workspace:
                {
                    var archive = CreateInteractionCube(parent, "INTERACTION CUBE - FAB ARCHIVE",
                        new Vector3(-2.3f, 1.2f, 2.4f), new Vector3(3f, 2.4f, 2.2f), new Color32(42,216,211,255));
                    archive.AddComponent<SemiconArchiveTerminal>().Configure(archivePanel,
                        CreateGlow(archive.transform, new Color32(42,216,211,255)));
                    var supply = CreateInteractionCube(parent, "INTERACTION CUBE - ROBOT SUPPLY CENTER",
                        new Vector3(2.3f, 1.2f, 2.4f), new Vector3(3f, 2.4f, 2.2f), new Color32(247,169,30,255));
                    supply.AddComponent<SemiconGachaTerminal>().Configure(gachaPanel,
                        CreateGlow(supply.transform, new Color32(247,169,30,255)));
                    break;
                }
            }
        }

        private static void BuildExitDoor(Transform parent, Definition definition, SemiconHud hud)
        {
            var exit = CreateInteractionCube(parent, "EXIT DOOR CUBE - REPLACE WITH MODELED DOOR",
                new Vector3(-7.1f, 1.35f, -5.4f), new Vector3(2.5f, 2.7f, 0.55f), new Color32(247,169,30,255));
            var portal = exit.AddComponent<SemiconScenePortal>();
            portal.ConfigureScene(WorldSceneName, definition.ReturnPosition, definition.ReturnRotation,
                "[E]  건물 밖으로 나가기", "공장 단지로 돌아왔습니다.", hud, false);
        }

        private static void CreateExperimentCube<T>(Transform parent, string name, Color32 accent,
            Action<T, Transform> configure) where T : SemiconInteractable
        {
            var root = CreateInteractionCube(parent, name, new Vector3(0f, 1.2f, 2.4f),
                new Vector3(3f, 2.4f, 2.2f), accent);
            var terminal = root.AddComponent<T>();
            configure(terminal, CreateGlow(root.transform, accent));
        }

        private static GameObject CreateInteractionCube(Transform parent, string name, Vector3 position,
            Vector3 scale, Color32 accent)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = scale + new Vector3(0.8f, 0.5f, 0.8f);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "PLACEHOLDER VISUAL";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = scale;
            visual.GetComponent<Renderer>().sharedMaterial = CreateAccentMaterial(accent);
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
            return root;
        }

        private static Transform CreateGlow(Transform parent, Color32 color)
        {
            var glow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glow.name = "Interaction Glow";
            glow.transform.SetParent(parent, false);
            glow.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            glow.transform.localScale = new Vector3(0.55f, 0.08f, 0.55f);
            glow.GetComponent<Renderer>().sharedMaterial = CreateAccentMaterial(color);
            UnityEngine.Object.DestroyImmediate(glow.GetComponent<Collider>());
            return glow.transform;
        }

        private static Material LoadMaterial(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + name + ".mat") ??
                   CreateAccentMaterial(new Color32(10, 48, 55, 255));
        }

        private static Material CreateAccentMaterial(Color32 color)
        {
            var assetName = $"Placeholder_{color.r:X2}{color.g:X2}{color.b:X2}{color.a:X2}.mat";
            var assetPath = MaterialFolder + assetName;
            var existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", (Color)color * 0.3f);
            }
            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }

        private static string GetScenePath(Definition definition) => $"{SceneFolder}/{definition.SceneName}.unity";

        private static Definition NewDefinition(SemiconInteriorKind kind, string sceneName, string displayName,
            Color32 accent)
        {
            return new Definition
            {
                Kind = kind,
                SceneName = sceneName,
                DisplayName = displayName,
                Accent = accent,
                ReturnPosition = Vector3.zero,
                ReturnRotation = Quaternion.identity
            };
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent ?? "Assets", name);
        }
    }
}
#endif
