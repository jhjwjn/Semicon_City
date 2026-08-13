using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    public sealed class SemiconRuntimeSmokeTest : MonoBehaviour
    {
        private IEnumerator Start()
        {
            var arguments = Environment.GetCommandLineArgs();
            if (arguments.Contains("--semicon-postgame-smoke-test"))
            {
                yield return RunPostgameContractsAndArchiveSmokeTest();
                yield break;
            }
            if (arguments.Contains("--semicon-campaign-smoke-test"))
            {
                yield return RunCampaignSmokeTest();
                yield break;
            }
            if (arguments.Contains("--semicon-tutorial-smoke-test"))
            {
                yield return RunFirstTutorialSmokeTest();
                yield break;
            }
            if (arguments.Contains("--semicon-market-smoke-test"))
            {
                yield return RunMarketSmokeTest();
                yield break;
            }
            if (arguments.Contains("--semicon-factory-smoke-test"))
            {
                yield return RunFactoryProgressionSmokeTest();
                yield break;
            }
            if (arguments.Contains("--semicon-loadout-smoke-test"))
            {
                yield return RunFactoryLoadoutSmokeTest();
                yield break;
            }
            if (arguments.Contains("--semicon-wafer-smoke-test"))
            {
                yield return RunWaferProductionSmokeTest();
                yield break;
            }
            if (arguments.Contains("--semicon-oxidation-smoke-test"))
            {
                yield return RunOxidationProductionSmokeTest();
                yield break;
            }
            if (arguments.Contains("--semicon-photo-smoke-test"))
            {
                yield return RunPhotoProductionSmokeTest();
                yield break;
            }
            if (arguments.Contains("--semicon-etch-smoke-test"))
            {
                yield return RunEtchProductionSmokeTest();
                yield break;
            }
            if (arguments.Contains("--semicon-deposition-smoke-test"))
            {
                yield return RunDepositionProductionSmokeTest();
                yield break;
            }
            if (arguments.Contains("--semicon-metal-smoke-test"))
            {
                yield return RunMetalProductionSmokeTest();
                yield break;
            }
            if (arguments.Contains("--semicon-eds-smoke-test"))
            {
                yield return RunEdsProductionSmokeTest();
                yield break;
            }
            if (arguments.Contains("--semicon-package-smoke-test"))
            {
                yield return RunPackageProductionSmokeTest();
                yield break;
            }

            if (!arguments.Contains("--semicon-smoke-test"))
            {
                yield break;
            }

            yield return null;
            var state = SemiconGameState.Instance;
            var player = FindFirstObjectByType<SemiconPlayerController>();
            var cameraController = FindFirstObjectByType<SemiconThirdPersonCamera>();
            var terminal = FindFirstObjectByType<SemiconInteractionTerminal>();
            var panel = FindFirstObjectByType<PhotoExperimentPanel>();
            if (state == null || player == null || cameraController == null || terminal == null || panel == null)
            {
                Debug.LogError("[Semicon Smoke] 필수 런타임 구성요소를 찾지 못했습니다.");
                Application.Quit(11);
                yield break;
            }

            state.ResetProgress();
            terminal.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.35f);

            var runButtonObject = GameObject.Find("Run Experiment Button");
            var runButton = runButtonObject != null ? runButtonObject.GetComponent<Button>() : null;
            if (runButton == null)
            {
                Debug.LogError("[Semicon Smoke] 실험 실행 버튼을 찾지 못했습니다.");
                Application.Quit(12);
                yield break;
            }

            runButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(3.35f);
            if (state.ExperimentCount != 1 || state.ResearchPoints != 112)
            {
                Debug.LogError($"[Semicon Smoke] 실험 결과 저장 실패: count={state.ExperimentCount}, research={state.ResearchPoints}");
                Application.Quit(13);
                yield break;
            }

            var outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-runtime-smoke-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(outputPath);
            Debug.Log($"[Semicon Smoke] PASS / 실험 1회 / 연구 데이터 112 / Capture={outputPath}");
            Application.Quit(0);
        }

        private IEnumerator RunFirstTutorialSmokeTest()
        {
            yield return null;
            var state = SemiconGameState.Instance;
            var hud = FindFirstObjectByType<SemiconHud>();
            var tutorial = FindFirstObjectByType<SemiconFirstTutorial>();
            if (state == null || hud == null || tutorial == null)
            {
                Debug.LogError("[Semicon Tutorial Smoke] Required tutorial component is missing.");
                Application.Quit(21);
                yield break;
            }

            state.ResetProgress();
            yield return new WaitForSecondsRealtime(0.25f);
            if (state.FirstTutorialCompleted || !hud.CurrentObjectiveTitle.Contains("실리콘"))
            {
                Debug.LogError($"[Semicon Tutorial Smoke] Initial objective mismatch: " +
                    $"complete={state.FirstTutorialCompleted}, title={hud.CurrentObjectiveTitle}");
                Application.Quit(22);
                yield break;
            }

            var initialPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-tutorial-start-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(initialPath);
            if (!state.TryBuyMaterial(SemiconMaterialKind.Silicon, 10))
            {
                Debug.LogError("[Semicon Tutorial Smoke] Silicon purchase failed.");
                Application.Quit(23);
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.2f);
            if (!hud.CurrentObjectiveTitle.Contains("WAFER-01"))
            {
                Debug.LogError($"[Semicon Tutorial Smoke] Factory objective mismatch: {hud.CurrentObjectiveTitle}");
                Application.Quit(24);
                yield break;
            }

            if (!state.TryStartProduction(0, SemiconRecipeKind.WaferSubstrate, 1,
                    out var job, out var reason))
            {
                Debug.LogError($"[Semicon Tutorial Smoke] Wafer start failed: {reason}");
                Application.Quit(25);
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.2f);
            if (!hud.CurrentObjectiveTitle.Contains("진행 중"))
            {
                Debug.LogError($"[Semicon Tutorial Smoke] Processing objective mismatch: {hud.CurrentObjectiveTitle}");
                Application.Quit(26);
                yield break;
            }

            yield return new WaitForSecondsRealtime(job.RemainingSeconds + 0.3f);
            if (!hud.CurrentObjectiveTitle.Contains("회수"))
            {
                Debug.LogError($"[Semicon Tutorial Smoke] Collect objective mismatch: {hud.CurrentObjectiveTitle}");
                Application.Quit(27);
                yield break;
            }
            if (!state.TryCollectProduction(0, out _, out _, out _, out reason))
            {
                Debug.LogError($"[Semicon Tutorial Smoke] Wafer collect failed: {reason}");
                Application.Quit(28);
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.25f);
            if (!state.FirstTutorialCompleted || state.UnlockedProcessCount != 2 ||
                state.WaferStock != 1 || state.SiliconStock != 8 ||
                state.Credits != 24700 || state.ResearchPoints != 132 ||
                tutorial.CurrentObjectiveKey != "EXPERIMENT_02")
            {
                Debug.LogError($"[Semicon Tutorial Smoke] Completion mismatch: " +
                    $"complete={state.FirstTutorialCompleted}, wafer={state.WaferStock}, " +
                    $"silicon={state.SiliconStock}, credits={state.Credits}, research={state.ResearchPoints}, " +
                    $"objective={tutorial.CurrentObjectiveKey}, title={hud.CurrentObjectiveTitle}");
                Application.Quit(29);
                yield break;
            }

            var completePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-tutorial-complete-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(completePath);
            Debug.Log($"[Semicon Tutorial Smoke] PASS / buy silicon→start wafer→wait→collect→reward / " +
                $"credits=24700 / research=132 / Start={initialPath} / Complete={completePath}");
            state.ResetProgress();
            Application.Quit(0);
        }

        private IEnumerator RunCampaignSmokeTest()
        {
            yield return null;
            var state = SemiconGameState.Instance;
            var tutorial = FindFirstObjectByType<SemiconFirstTutorial>();
            var marketPanel = FindFirstObjectByType<SemiconMarketPanel>();
            var player = FindFirstObjectByType<SemiconPlayerController>();
            var cameraController = FindFirstObjectByType<SemiconThirdPersonCamera>();
            if (state == null || tutorial == null || marketPanel == null || player == null || cameraController == null)
            {
                Debug.LogError("[Semicon Campaign Smoke] Campaign components are missing.");
                Application.Quit(31);
                yield break;
            }

            state.ResetProgress();
            if (!state.TryBuyMaterial(SemiconMaterialKind.Silicon, 10) ||
                !state.TryBuyMaterial(SemiconMaterialKind.ProcessGas, 10) ||
                !state.TryBuyMaterial(SemiconMaterialKind.Chemicals, 10) ||
                !state.TryBuyMaterial(SemiconMaterialKind.MetalTarget, 10))
            {
                Debug.LogError("[Semicon Campaign Smoke] Initial material purchase failed.");
                Application.Quit(32);
                yield break;
            }

            if (!StartAndCollectImmediately(state, SemiconRecipeKind.WaferSubstrate, out var reason))
            {
                Debug.LogError("[Semicon Campaign Smoke] WAFER-01 failed: " + reason);
                Application.Quit(33);
                yield break;
            }
            yield return null;
            if (!state.FirstTutorialCompleted || state.UnlockedProcessCount != 2 ||
                tutorial.CurrentObjectiveKey != "EXPERIMENT_02")
            {
                Debug.LogError($"[Semicon Campaign Smoke] Oxidation unlock mismatch: " +
                               $"tutorial={state.FirstTutorialCompleted}, unlocked={state.UnlockedProcessCount}, " +
                               $"objective={tutorial.CurrentObjectiveKey}");
                Application.Quit(34);
                yield break;
            }

            var steps = new[]
            {
                (process: 2, recipe: SemiconRecipeKind.OxidizedWafer),
                (process: 3, recipe: SemiconRecipeKind.PhotoPatternedWafer),
                (process: 4, recipe: SemiconRecipeKind.EtchedWafer),
                (process: 5, recipe: SemiconRecipeKind.DepositedWafer),
                (process: 6, recipe: SemiconRecipeKind.MetalizedWafer),
                (process: 7, recipe: SemiconRecipeKind.TestedWafer)
            };

            foreach (var step in steps)
            {
                if (!state.TrySpendResearch(8))
                {
                    Debug.LogError($"[Semicon Campaign Smoke] Research spend failed at process {step.process}.");
                    Application.Quit(35);
                    yield break;
                }
                RecordQualifiedExperiment(state, step.process);
                if (!StartAndCollectImmediately(state, step.recipe, out reason))
                {
                    Debug.LogError($"[Semicon Campaign Smoke] Process {step.process} failed: {reason}");
                    Application.Quit(36);
                    yield break;
                }
                yield return null;
                if (state.UnlockedProcessCount != step.process + 1)
                {
                    Debug.LogError($"[Semicon Campaign Smoke] Unlock mismatch after {step.process}: " +
                                   $"{state.UnlockedProcessCount}");
                    Application.Quit(37);
                    yield break;
                }
            }

            if (tutorial.CurrentObjectiveKey != "EXPERIMENT_08" || !state.TrySpendResearch(8))
            {
                Debug.LogError($"[Semicon Campaign Smoke] Package objective mismatch: {tutorial.CurrentObjectiveKey}");
                Application.Quit(38);
                yield break;
            }
            RecordQualifiedExperiment(state, 8);
            yield return null;
            if (tutorial.CurrentObjectiveKey != "ORDER_ACCEPT" || !InvokeButton("First Order Button") ||
                !state.FirstOrderAccepted)
            {
                Debug.LogError($"[Semicon Campaign Smoke] Order acceptance button failed / " +
                               $"accepted={state.FirstOrderAccepted} / {tutorial.CurrentObjectiveKey}");
                Application.Quit(39);
                yield break;
            }
            if (!StartAndCollectImmediately(state, SemiconRecipeKind.Sc01ControlSensor, out reason))
            {
                Debug.LogError("[Semicon Campaign Smoke] SC-01 production failed: " + reason);
                Application.Quit(40);
                yield break;
            }
            yield return null;
            if (tutorial.CurrentObjectiveKey != "ORDER_DELIVER" || !InvokeButton("First Order Button") ||
                !state.FirstOrderCompleted)
            {
                Debug.LogError($"[Semicon Campaign Smoke] Delivery button failed / " +
                               $"complete={state.FirstOrderCompleted} / {tutorial.CurrentObjectiveKey}");
                Application.Quit(41);
                yield break;
            }
            yield return null;

            if (!state.FirstOrderCompleted || state.FinishedProductStock != 0 || state.Credits != 29050 ||
                state.ResearchPoints != 106 || state.UnlockedProcessCount != 8 ||
                tutorial.CurrentObjectiveKey != "POSTGAME_CONTRACT")
            {
                Debug.LogError($"[Semicon Campaign Smoke] Final state mismatch: order={state.FirstOrderCompleted}, " +
                               $"finished={state.FinishedProductStock}, credits={state.Credits}, " +
                               $"research={state.ResearchPoints}, unlocked={state.UnlockedProcessCount}, " +
                               $"objective={tutorial.CurrentObjectiveKey}");
                Application.Quit(42);
                yield break;
            }

            var outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-campaign-complete-{Screen.width}x{Screen.height}.png"));
            marketPanel.Open(player, cameraController);
            yield return new WaitForSecondsRealtime(0.35f);
            yield return CaptureScreen(outputPath);
            Debug.Log($"[Semicon Campaign Smoke] PASS / 8 processes unlocked→order accepted→SC-01 delivered / " +
                      $"credits=29050 / research=106 / Capture={outputPath}");
            state.ResetProgress();
            Application.Quit(0);
        }

        private IEnumerator RunPostgameContractsAndArchiveSmokeTest()
        {
            yield return null;
            var state = SemiconGameState.Instance;
            var contractPanel = FindFirstObjectByType<SemiconContractPanel>(FindObjectsInactive.Include);
            var archivePanel = FindFirstObjectByType<SemiconArchivePanel>(FindObjectsInactive.Include);
            var player = FindFirstObjectByType<SemiconPlayerController>();
            var cameraController = FindFirstObjectByType<SemiconThirdPersonCamera>();
            if (state == null || contractPanel == null || archivePanel == null || player == null || cameraController == null)
            {
                Debug.LogError("[Semicon Postgame Smoke] Required contract/archive components are missing.");
                Application.Quit(51);
                yield break;
            }

            state.ResetProgress();
            var reason = string.Empty;
            if (!state.TryBuyMaterial(SemiconMaterialKind.Silicon, 30) ||
                !state.TryBuyMaterial(SemiconMaterialKind.ProcessGas, 30) ||
                !state.TryBuyMaterial(SemiconMaterialKind.Chemicals, 20) ||
                !state.TryBuyMaterial(SemiconMaterialKind.MetalTarget, 10) ||
                !state.TryAssignWorker(0, SemiconWorkerKind.Mina, out reason) ||
                !state.TryAssignDisk(0, SemiconDiskKind.Quality, out reason))
            {
                Debug.LogError("[Semicon Postgame Smoke] Initial setup failed: " + reason);
                Application.Quit(52);
                yield break;
            }

            for (var process = 2; process <= 8; process++) RecordQualifiedExperiment(state, process);
            if (!ProducePathImmediately(state, SemiconRecipeKind.Sc01ControlSensor, 1, out reason) ||
                !state.CompleteFirstTutorial() || !state.TryAcceptFirstOrder(out reason) ||
                !state.TryCompleteFirstOrder(out reason))
            {
                Debug.LogError("[Semicon Postgame Smoke] Campaign preparation failed: " + reason);
                Application.Quit(53);
                yield break;
            }

            contractPanel.Open(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            for (var index = 0; index < SemiconContractCatalog.Count; index++)
            {
                if (index == 6 && (!state.TryBuyMaterial(SemiconMaterialKind.Silicon, 30) ||
                                   !state.TryBuyMaterial(SemiconMaterialKind.ProcessGas, 30) ||
                                   !state.TryBuyMaterial(SemiconMaterialKind.Chemicals, 30) ||
                                   !state.TryBuyMaterial(SemiconMaterialKind.MetalTarget, 10)))
                {
                    Debug.LogError("[Semicon Postgame Smoke] Product contract material restock failed.");
                    Application.Quit(54);
                    yield break;
                }
                var definition = SemiconContractCatalog.GetAt(index);
                if (!ProducePathImmediately(state, definition.RequiredRecipe, definition.RequiredAmount, out reason))
                {
                    Debug.LogError($"[Semicon Postgame Smoke] Inventory preparation failed for {definition.Code}: {reason}");
                    Application.Quit(54);
                    yield break;
                }
                if (!InvokeButton($"Contract {index + 1:00} Button") ||
                    !InvokeButton("Accept Contract Button") || state.ActiveContract != definition.Kind ||
                    !InvokeButton("Deliver Contract Button") || state.GetContractCompletionCount(definition.Kind) != 1)
                {
                    Debug.LogError($"[Semicon Postgame Smoke] Contract UI flow failed for {definition.Code}: " +
                                   $"active={state.ActiveContract}, completions={state.GetContractCompletionCount(definition.Kind)}");
                    Application.Quit(55);
                    yield break;
                }
            }

            if (state.CompletedContractKinds != SemiconContractCatalog.Count ||
                state.GetLifetimeProduced(SemiconRecipeKind.Pm10PowerManagement) < 3 ||
                state.GetLifetimeProduced(SemiconRecipeKind.Dd20DisplayDriver) < 4 ||
                !state.IsContractUnlocked(SemiconContractKind.Dd20DisplayDriver))
            {
                Debug.LogError($"[Semicon Postgame Smoke] Final progression mismatch: contracts={state.CompletedContractKinds}, " +
                               $"pm10={state.GetLifetimeProduced(SemiconRecipeKind.Pm10PowerManagement)}, " +
                               $"dd20={state.GetLifetimeProduced(SemiconRecipeKind.Dd20DisplayDriver)}");
                Application.Quit(56);
                yield break;
            }

            var contractPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-contract-board-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(contractPath);
            InvokeButton("Contract Close Button");
            yield return new WaitForSecondsRealtime(0.25f);
            archivePanel.Open(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            if (!InvokeButton("Archive Tab 02"))
            {
                Debug.LogError("[Semicon Postgame Smoke] Product archive tab button is missing.");
                Application.Quit(57);
                yield break;
            }
            var archiveContentObject = GameObject.Find("Archive Content");
            var archiveContent = archiveContentObject != null ? archiveContentObject.GetComponent<Text>() : null;
            if (archiveContent == null || !archiveContent.text.Contains("PM-10") || !archiveContent.text.Contains("DD-20"))
            {
                Debug.LogError("[Semicon Postgame Smoke] Product archive content mismatch.");
                Application.Quit(58);
                yield break;
            }
            var archivePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-fab-archive-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(archivePath);
            Debug.Log($"[Semicon Postgame Smoke] PASS / contracts=9 / sample=6 / PM-10=3 / DD-20=4 / " +
                      $"Contract={contractPath} / Archive={archivePath}");
            state.ResetProgress();
            Application.Quit(0);
        }

        private static bool ProducePathImmediately(SemiconGameState state, SemiconRecipeKind target, int amount,
            out string reason)
        {
            var path = new[]
            {
                SemiconRecipeKind.WaferSubstrate,
                SemiconRecipeKind.OxidizedWafer,
                SemiconRecipeKind.PhotoPatternedWafer,
                SemiconRecipeKind.EtchedWafer,
                SemiconRecipeKind.DepositedWafer,
                SemiconRecipeKind.MetalizedWafer,
                SemiconRecipeKind.TestedWafer
            };
            foreach (var recipe in path)
            {
                if (!StartAndCollectImmediately(state, recipe, out reason, amount)) return false;
                if (recipe == target) return true;
            }
            return StartAndCollectImmediately(state, target, out reason, amount);
        }

        private static void RecordQualifiedExperiment(SemiconGameState state, int process)
        {
            switch (process)
            {
                case 2: state.RecordOxidationExperiment(1000, 60, 100f, 98f, true); break;
                case 3: state.RecordPhotoExperiment(105, 0.05f, 97f, 98f, true); break;
                case 4: state.RecordEtchExperiment(250, 60, 120f, 98f, true); break;
                case 5: state.RecordDepositionExperiment(400, 6, 80f, 98f, 95f, true); break;
                case 6: state.RecordMetalExperiment(250, 60, 450f, 0.119f, 98f, true); break;
                case 7: state.RecordEdsExperiment(3, 30, 96f, 98f, 2f, true); break;
                case 8: state.RecordPackageExperiment(35, 175, 96f, 97f, 97f, true); break;
            }
        }

        private static bool StartAndCollectImmediately(SemiconGameState state, SemiconRecipeKind recipe,
            out string reason, int batches = 1)
        {
            if (!state.TryStartProduction(0, recipe, batches, out _, out reason)) return false;
            var slot = state.GetFactorySlot(0);
            var now = DateTime.UtcNow;
            slot.activeJobStartUtcTicks = now.AddSeconds(-2).Ticks;
            slot.activeJobFinishUtcTicks = now.AddSeconds(-1).Ticks;
            return state.TryCollectProduction(0, out _, out _, out _, out reason);
        }

        private IEnumerator RunMarketSmokeTest()
        {
            yield return null;
            var state = SemiconGameState.Instance;
            var player = FindFirstObjectByType<SemiconPlayerController>();
            var cameraController = FindFirstObjectByType<SemiconThirdPersonCamera>();
            var terminal = FindFirstObjectByType<SemiconMarketTerminal>(FindObjectsInactive.Include);
            var panel = FindFirstObjectByType<SemiconMarketPanel>();
            if (state == null || player == null || cameraController == null || terminal == null || panel == null)
            {
                Debug.LogError("[Semicon Market Smoke] 필수 마켓 구성요소를 찾지 못했습니다.");
                Application.Quit(21);
                yield break;
            }

            state.ResetProgress();
            state.AddFinishedProducts(2);
            terminal.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.35f);

            var buttonNames = new[]
            {
                "Buy Silicon Button",
                "Buy Process Gas Button",
                "Buy Chemicals Button",
                "Buy Metal Target Button",
                "Sell Finished Button"
            };
            foreach (var buttonName in buttonNames)
            {
                var buttonObject = GameObject.Find(buttonName);
                var button = buttonObject != null ? buttonObject.GetComponent<Button>() : null;
                if (button == null)
                {
                    Debug.LogError($"[Semicon Market Smoke] 거래 버튼 누락: {buttonName}");
                    Application.Quit(22);
                    yield break;
                }
                button.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.08f);
            }

            if (state.Credits != 22750 || state.SiliconStock != 10 || state.ProcessGasStock != 10 ||
                state.ChemicalStock != 10 || state.MetalTargetStock != 10 || state.FinishedProductStock != 1)
            {
                Debug.LogError($"[Semicon Market Smoke] 거래 결과 불일치: credits={state.Credits}, " +
                    $"Si={state.SiliconStock}, Gas={state.ProcessGasStock}, Chemical={state.ChemicalStock}, " +
                    $"Metal={state.MetalTargetStock}, Product={state.FinishedProductStock}");
                Application.Quit(23);
                yield break;
            }

            var outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-market-smoke-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(outputPath);
            Debug.Log($"[Semicon Market Smoke] PASS / credits={state.Credits} / materials=10,10,10,10 / product=1 / Capture={outputPath}");
            Application.Quit(0);
        }

        private IEnumerator RunFactoryProgressionSmokeTest()
        {
            yield return null;
            var state = SemiconGameState.Instance;
            var player = FindFirstObjectByType<SemiconPlayerController>();
            var cameraController = FindFirstObjectByType<SemiconThirdPersonCamera>();
            var marketTerminal = FindFirstObjectByType<SemiconMarketTerminal>(FindObjectsInactive.Include);
            var productionMachine = FindObjectsByType<SemiconProductionMachine>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).FirstOrDefault(item => item.name == "SC-01 Assembly Machine 01");
            var marketEntrance = FindPortal("Materials Hall Entrance");
            var marketExit = FindPortal("Materials Hall Exit");
            var factoryEntrance = FindPortal("Factory Visitor Entrance");
            var factoryExit = FindPortal("Factory Interior Exit");
            if (state == null || player == null || cameraController == null || marketTerminal == null ||
                productionMachine == null || marketEntrance == null || marketExit == null ||
                factoryEntrance == null || factoryExit == null)
            {
                Debug.LogError("[Semicon Factory Smoke] Required progression component is missing.");
                Application.Quit(31);
                yield break;
            }

            if (!ValidateWorldPhysics())
            {
                Application.Quit(32);
                yield break;
            }

            state.ResetProgress();
            marketEntrance.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.12f);
            if (player.transform.position.z < 150f)
            {
                Debug.LogError($"[Semicon Factory Smoke] Market entrance transition failed: {player.transform.position}");
                Application.Quit(33);
                yield break;
            }

            marketTerminal.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            foreach (var buttonName in new[] { "Buy Silicon Button", "Buy Process Gas Button", "Buy Chemicals Button" })
            {
                if (!InvokeButton(buttonName))
                {
                    Application.Quit(34);
                    yield break;
                }
                yield return new WaitForSecondsRealtime(0.05f);
            }
            InvokeButton("Market Close Button");
            yield return new WaitForSecondsRealtime(0.22f);
            marketExit.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.12f);

            state.RecordPhotoExperiment(90, -0.15f, 90f, 92f, true);
            factoryEntrance.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.2f);
            if (player.transform.position.z < 150f)
            {
                Debug.LogError($"[Semicon Factory Smoke] Factory entrance transition failed: {player.transform.position}");
                Application.Quit(35);
                yield break;
            }

            var interiorPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-factory-interior-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(interiorPath);

            productionMachine.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            if (!InvokeButton("Select Wafer Recipe Button") || !InvokeButton("Produce SC-01 Button"))
            {
                Application.Quit(36);
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.12f);
            if (state.SiliconStock != 8 || state.ProcessGasStock != 10 || state.ChemicalStock != 10 ||
                state.WaferStock != 0 || !state.GetProductionJob(0).HasJob)
            {
                Debug.LogError($"[Semicon Factory Smoke] Production start mismatch: Si={state.SiliconStock}, " +
                    $"Gas={state.ProcessGasStock}, Chemical={state.ChemicalStock}, Wafer={state.WaferStock}, " +
                    $"Job={state.GetProductionJob(0).HasJob}");
                Application.Quit(37);
                yield break;
            }

            var activeJob = state.GetProductionJob(0);
            yield return new WaitForSecondsRealtime(activeJob.RemainingSeconds + 0.25f);
            if (!InvokeButton("Collect Production Button") || state.WaferStock != 1 ||
                state.GetProductionJob(0).HasJob)
            {
                Debug.LogError($"[Semicon Factory Smoke] Production collect mismatch: " +
                    $"Wafer={state.WaferStock}, Job={state.GetProductionJob(0).HasJob}");
                Application.Quit(37);
                yield break;
            }

            var productionPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-production-smoke-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(productionPath);
            state.AddFinishedProducts(1);
            InvokeButton("Production Close Button");
            yield return new WaitForSecondsRealtime(0.22f);
            factoryExit.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.12f);

            marketEntrance.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.12f);
            marketTerminal.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            if (!InvokeButton("Sell Finished Button"))
            {
                Application.Quit(38);
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.12f);

            if (state.Credits != 25150 || state.SiliconStock != 8 || state.ProcessGasStock != 10 ||
                state.ChemicalStock != 10 || state.WaferStock != 1 || state.FinishedProductStock != 0)
            {
                Debug.LogError($"[Semicon Factory Smoke] Full loop mismatch: credits={state.Credits}, " +
                    $"Si={state.SiliconStock}, Gas={state.ProcessGasStock}, Chemical={state.ChemicalStock}, " +
                    $"Wafer={state.WaferStock}, Product={state.FinishedProductStock}");
                Application.Quit(39);
                yield break;
            }

            var marketPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-full-loop-market-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(marketPath);
            Debug.Log($"[Semicon Factory Smoke] PASS / road+portals+buy+produce+sell / credits={state.Credits} / " +
                $"materials=8,10,10 / wafer=1 / product=0 / Interior={interiorPath} / Production={productionPath} / Market={marketPath}");
            Application.Quit(0);
        }

        private IEnumerator RunFactoryLoadoutSmokeTest()
        {
            yield return null;
            var state = SemiconGameState.Instance;
            var player = FindFirstObjectByType<SemiconPlayerController>();
            var cameraController = FindFirstObjectByType<SemiconThirdPersonCamera>();
            var loadoutPanel = FindFirstObjectByType<SemiconFactoryLoadoutPanel>(FindObjectsInactive.Include);
            var factoryEntrance = FindPortal("Factory Visitor Entrance");
            var slotTerminal = FindObjectsByType<SemiconFactorySlotTerminal>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).FirstOrDefault(item => item.name.Contains("01"));
            if (state == null || player == null || cameraController == null || loadoutPanel == null ||
                factoryEntrance == null || slotTerminal == null)
            {
                Debug.LogError("[Semicon Loadout Smoke] Required factory loadout component is missing.");
                Application.Quit(41);
                yield break;
            }

            state.ResetProgress();
            state.TryBuyMaterial(SemiconMaterialKind.Silicon, 10);
            state.TryBuyMaterial(SemiconMaterialKind.ProcessGas, 10);
            state.TryBuyMaterial(SemiconMaterialKind.Chemicals, 10);
            state.RecordPhotoExperiment(90, -0.15f, 90f, 92f, true);
            factoryEntrance.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.2f);

            slotTerminal.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            if (!InvokeButton("Assign Mina Button") || !InvokeButton("Assign Production Disk Button") ||
                !InvokeButton("Factory Slot 02 Button") || !InvokeButton("Install Factory Machine Button") ||
                !InvokeButton("Assign Rex Button") || !InvokeButton("Assign Speed Disk Button") ||
                !InvokeButton("Factory Slot 01 Button"))
            {
                Application.Quit(42);
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.2f);

            var slot0 = state.GetFactorySlot(0);
            var slot1 = state.GetFactorySlot(1);
            var stats = state.GetProductionStats(0);
            if (slot0.worker != SemiconWorkerKind.Mina || slot0.disk != SemiconDiskKind.Production ||
                slot1 == null || !slot1.machineInstalled || slot1.worker != SemiconWorkerKind.Rex ||
                slot1.disk != SemiconDiskKind.Speed || stats.Production != 122 || stats.OutputPerCycle != 2 ||
                state.Credits != 17450)
            {
                Debug.LogError($"[Semicon Loadout Smoke] Assignment mismatch: credits={state.Credits}, " +
                    $"slot0={slot0?.worker}/{slot0?.disk}, slot1={slot1?.machineInstalled}/{slot1?.worker}/{slot1?.disk}, " +
                    $"production={stats.Production}, output={stats.OutputPerCycle}");
                Application.Quit(43);
                yield break;
            }

            var loadoutPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-factory-loadout-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(loadoutPath);

            if (!InvokeButton("Open Selected Production Button"))
            {
                Application.Quit(44);
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.3f);
            if (!InvokeButton("Select Wafer Recipe Button") || !InvokeButton("Produce SC-01 Button"))
            {
                Application.Quit(45);
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.15f);
            if (state.SiliconStock != 8 || state.ProcessGasStock != 10 || state.ChemicalStock != 10 ||
                state.WaferStock != 0 || !state.GetProductionJob(0).HasJob)
            {
                Debug.LogError($"[Semicon Loadout Smoke] Enhanced production start mismatch: " +
                    $"materials={state.SiliconStock},{state.ProcessGasStock},{state.ChemicalStock}, " +
                    $"wafers={state.WaferStock}, job={state.GetProductionJob(0).HasJob}");
                Application.Quit(46);
                yield break;
            }

            var enhancedJob = state.GetProductionJob(0);
            yield return new WaitForSecondsRealtime(enhancedJob.RemainingSeconds + 0.25f);
            if (!InvokeButton("Collect Production Button") || state.WaferStock != 2 ||
                state.AverageWaferQuality != 92 ||
                state.GetProductionJob(0).HasJob)
            {
                Debug.LogError($"[Semicon Loadout Smoke] Enhanced production collect mismatch: " +
                    $"wafers={state.WaferStock}, quality={state.AverageWaferQuality}, " +
                    $"job={state.GetProductionJob(0).HasJob}");
                Application.Quit(46);
                yield break;
            }

            var productionPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-loadout-production-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(productionPath);
            Debug.Log($"[Semicon Loadout Smoke] PASS / 2 machines / unique assignments / wafer output=2 / " +
                $"quality=92 / Loadout={loadoutPath} / Production={productionPath}");
            Application.Quit(0);
        }

        private IEnumerator RunWaferProductionSmokeTest()
        {
            yield return null;
            var state = SemiconGameState.Instance;
            var player = FindFirstObjectByType<SemiconPlayerController>();
            var cameraController = FindFirstObjectByType<SemiconThirdPersonCamera>();
            var factoryEntrance = FindPortal("Factory Visitor Entrance");
            var productionMachine = FindObjectsByType<SemiconProductionMachine>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).FirstOrDefault(item => item.name == "SC-01 Assembly Machine 01");
            if (state == null || player == null || cameraController == null || factoryEntrance == null ||
                productionMachine == null)
            {
                Debug.LogError("[Semicon Wafer Smoke] Required production component is missing.");
                Application.Quit(51);
                yield break;
            }

            state.ResetProgress();
            state.TryBuyMaterial(SemiconMaterialKind.Silicon, 10);
            factoryEntrance.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.2f);
            productionMachine.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            if (!InvokeButton("Select Wafer Recipe Button") || !InvokeButton("Produce SC-01 Button"))
            {
                Application.Quit(52);
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.7f);

            var job = state.GetProductionJob(0);
            if (!job.HasJob || job.Recipe != SemiconRecipeKind.WaferSubstrate || job.IsComplete ||
                state.SiliconStock != 8 || state.WaferStock != 0)
            {
                Debug.LogError($"[Semicon Wafer Smoke] Start mismatch: job={job.HasJob}/{job.Recipe}/{job.IsComplete}, " +
                    $"silicon={state.SiliconStock}, wafer={state.WaferStock}");
                Application.Quit(53);
                yield break;
            }

            var processingPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-wafer-processing-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(processingPath);
            InvokeButton("Production Close Button");
            yield return new WaitForSecondsRealtime(0.22f);
            job = state.GetProductionJob(0);
            yield return new WaitForSecondsRealtime(job.RemainingSeconds + 0.25f);

            productionMachine.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            var readyJob = state.GetProductionJob(0);
            if (!readyJob.IsComplete)
            {
                Debug.LogError($"[Semicon Wafer Smoke] Completion timer mismatch: remaining={readyJob.RemainingSeconds:0.00}");
                Application.Quit(54);
                yield break;
            }
            var readyPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-wafer-ready-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(readyPath);
            if (!InvokeButton("Collect Production Button") || state.WaferStock != 1 ||
                state.AverageWaferQuality != 80 || state.GetProductionJob(0).HasJob)
            {
                Debug.LogError($"[Semicon Wafer Smoke] Collect mismatch: wafer={state.WaferStock}, " +
                    $"quality={state.AverageWaferQuality}, job={state.GetProductionJob(0).HasJob}");
                Application.Quit(55);
                yield break;
            }

            Debug.Log($"[Semicon Wafer Smoke] PASS / persistent timer / silicon=8 / wafer=1 / quality=80 / " +
                $"Processing={processingPath} / Ready={readyPath}");
            Application.Quit(0);
        }

        private IEnumerator RunOxidationProductionSmokeTest()
        {
            yield return null;
            var state = SemiconGameState.Instance;
            var player = FindFirstObjectByType<SemiconPlayerController>();
            var cameraController = FindFirstObjectByType<SemiconThirdPersonCamera>();
            var oxidationTerminal = FindFirstObjectByType<SemiconOxidationTerminal>(FindObjectsInactive.Include);
            var factoryEntrance = FindPortal("Factory Visitor Entrance");
            var productionMachine = FindObjectsByType<SemiconProductionMachine>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).FirstOrDefault(item => item.name == "SC-01 Assembly Machine 01");
            if (state == null || player == null || cameraController == null || oxidationTerminal == null ||
                factoryEntrance == null || productionMachine == null)
            {
                Debug.LogError("[Semicon Oxidation Smoke] Required oxidation component is missing.");
                Application.Quit(61);
                yield break;
            }

            state.ResetProgress();
            oxidationTerminal.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            if (!InvokeButton("Run Oxidation Experiment Button"))
            {
                Application.Quit(62);
                yield break;
            }
            yield return new WaitForSecondsRealtime(1.15f);
            if (!state.OxidationRecipeQualified || state.OxidationExperimentCount != 1 ||
                state.ResearchPoints != 112 || Mathf.Abs(state.BestOxideThickness - 100f) > 0.1f ||
                Mathf.Abs(state.BestOxideUniformity - 98f) > 0.1f)
            {
                Debug.LogError($"[Semicon Oxidation Smoke] Experiment mismatch: qualified={state.OxidationRecipeQualified}, " +
                    $"count={state.OxidationExperimentCount}, research={state.ResearchPoints}, " +
                    $"thickness={state.BestOxideThickness:0.0}, uniformity={state.BestOxideUniformity:0.0}");
                Application.Quit(63);
                yield break;
            }

            var experimentPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-oxidation-experiment-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(experimentPath);
            InvokeButton("Oxidation Close Button");
            yield return new WaitForSecondsRealtime(0.22f);

            state.TryBuyMaterial(SemiconMaterialKind.Silicon, 10);
            state.TryBuyMaterial(SemiconMaterialKind.ProcessGas, 10);
            factoryEntrance.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.2f);
            productionMachine.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            if (!InvokeButton("Select Wafer Recipe Button") || !InvokeButton("Produce SC-01 Button"))
            {
                Application.Quit(64);
                yield break;
            }

            var waferJob = state.GetProductionJob(0);
            yield return new WaitForSecondsRealtime(waferJob.RemainingSeconds + 0.25f);
            if (!InvokeButton("Collect Production Button") || state.WaferStock != 1 ||
                state.AverageWaferQuality != 80)
            {
                Debug.LogError($"[Semicon Oxidation Smoke] Wafer preparation mismatch: " +
                    $"wafer={state.WaferStock}, quality={state.AverageWaferQuality}");
                Application.Quit(65);
                yield break;
            }

            if (!InvokeButton("Select Oxidation Recipe Button") || !InvokeButton("Produce SC-01 Button"))
            {
                Application.Quit(66);
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.7f);
            var oxidationJob = state.GetProductionJob(0);
            if (!oxidationJob.HasJob || oxidationJob.Recipe != SemiconRecipeKind.OxidizedWafer ||
                oxidationJob.IsComplete || state.WaferStock != 0 || state.ProcessGasStock != 9 ||
                state.OxidizedWaferStock != 0)
            {
                Debug.LogError($"[Semicon Oxidation Smoke] Start mismatch: " +
                    $"job={oxidationJob.HasJob}/{oxidationJob.Recipe}/{oxidationJob.IsComplete}, " +
                    $"wafer={state.WaferStock}, gas={state.ProcessGasStock}, oxide={state.OxidizedWaferStock}");
                Application.Quit(67);
                yield break;
            }

            var processingPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-oxidation-processing-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(processingPath);
            InvokeButton("Production Close Button");
            yield return new WaitForSecondsRealtime(0.22f);
            oxidationJob = state.GetProductionJob(0);
            yield return new WaitForSecondsRealtime(oxidationJob.RemainingSeconds + 0.25f);

            productionMachine.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            var readyJob = state.GetProductionJob(0);
            if (!readyJob.IsComplete)
            {
                Debug.LogError($"[Semicon Oxidation Smoke] Completion timer mismatch: " +
                    $"remaining={readyJob.RemainingSeconds:0.00}");
                Application.Quit(68);
                yield break;
            }
            var readyPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-oxidation-ready-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(readyPath);
            if (!InvokeButton("Collect Production Button") || state.OxidizedWaferStock != 1 ||
                state.AverageOxidizedWaferQuality != 86 || state.GetProductionJob(0).HasJob)
            {
                Debug.LogError($"[Semicon Oxidation Smoke] Collect mismatch: oxide={state.OxidizedWaferStock}, " +
                    $"quality={state.AverageOxidizedWaferQuality}, job={state.GetProductionJob(0).HasJob}");
                Application.Quit(69);
                yield break;
            }

            Debug.Log($"[Semicon Oxidation Smoke] PASS / experiment→wafer→oxide / oxide=1 / quality=86 / " +
                $"Experiment={experimentPath} / Processing={processingPath} / Ready={readyPath}");
            state.ResetProgress();
            Application.Quit(0);
        }

        private IEnumerator RunPhotoProductionSmokeTest()
        {
            yield return null;
            var state = SemiconGameState.Instance;
            var player = FindFirstObjectByType<SemiconPlayerController>();
            var cameraController = FindFirstObjectByType<SemiconThirdPersonCamera>();
            var photoPanel = FindFirstObjectByType<PhotoExperimentPanel>(FindObjectsInactive.Include);
            var photoTerminal = FindFirstObjectByType<SemiconInteractionTerminal>(FindObjectsInactive.Include);
            var factoryEntrance = FindPortal("Factory Visitor Entrance");
            var productionMachine = FindObjectsByType<SemiconProductionMachine>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).FirstOrDefault(item => item.name == "SC-01 Assembly Machine 01");
            if (state == null || player == null || cameraController == null || photoPanel == null || photoTerminal == null ||
                factoryEntrance == null || productionMachine == null)
            {
                Debug.LogError("[Semicon Photo Smoke] Required photo component is missing.");
                Application.Quit(71);
                yield break;
            }

            state.ResetProgress();
            photoTerminal.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            var doseSliderObject = GameObject.Find("Dose Slider");
            var focusSliderObject = GameObject.Find("Focus Slider");
            var doseSlider = doseSliderObject != null ? doseSliderObject.GetComponent<Slider>() : null;
            var focusSlider = focusSliderObject != null ? focusSliderObject.GetComponent<Slider>() : null;
            if (doseSlider == null || focusSlider == null)
            {
                Debug.LogError("[Semicon Photo Smoke] Experiment sliders are missing.");
                Application.Quit(72);
                yield break;
            }
            doseSlider.value = 105f;
            focusSlider.value = 0.05f;
            var experimentReadyPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-photo-ui-ready-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(experimentReadyPath);
            if (!InvokeButton("Run Experiment Button"))
            {
                Application.Quit(72);
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.9f);
            if (!photoPanel.IsRunning ||
                photoPanel.CurrentPresentationState != PhotoExperimentPanel.PresentationState.Processing)
            {
                Debug.LogError($"[Semicon Photo Smoke] Processing presentation mismatch: " +
                    $"running={photoPanel.IsRunning}, state={photoPanel.CurrentPresentationState}");
                Application.Quit(73);
                yield break;
            }
            var experimentProcessingPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-photo-ui-processing-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(experimentProcessingPath);
            yield return new WaitForSecondsRealtime(2.45f);
            if (!state.PhotoRecipeQualified || state.ExperimentCount != 1 || state.ResearchPoints != 112 ||
                Mathf.Abs(state.BestYield - 97f) > 0.1f || Mathf.Abs(state.BestPrecision - 98f) > 0.1f)
            {
                Debug.LogError($"[Semicon Photo Smoke] Experiment mismatch: qualified={state.PhotoRecipeQualified}, " +
                    $"count={state.ExperimentCount}, research={state.ResearchPoints}, " +
                    $"yield={state.BestYield:0.0}, precision={state.BestPrecision:0.0}");
                Application.Quit(73);
                yield break;
            }
            if (photoPanel.IsRunning ||
                photoPanel.CurrentPresentationState != PhotoExperimentPanel.PresentationState.Result)
            {
                Debug.LogError($"[Semicon Photo Smoke] Result presentation mismatch: " +
                    $"running={photoPanel.IsRunning}, state={photoPanel.CurrentPresentationState}");
                Application.Quit(73);
                yield break;
            }

            var experimentPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-photo-experiment-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(experimentPath);
            InvokeButton("Close Button");
            yield return new WaitForSecondsRealtime(0.22f);

            state.RecordOxidationExperiment(1000, 60, 100f, 98f, true);
            state.TryBuyMaterial(SemiconMaterialKind.Silicon, 10);
            state.TryBuyMaterial(SemiconMaterialKind.ProcessGas, 10);
            state.TryBuyMaterial(SemiconMaterialKind.Chemicals, 10);
            factoryEntrance.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.2f);
            productionMachine.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);

            if (!InvokeButton("Select Wafer Recipe Button") || !InvokeButton("Produce SC-01 Button"))
            {
                Application.Quit(74);
                yield break;
            }
            var waferJob = state.GetProductionJob(0);
            yield return new WaitForSecondsRealtime(waferJob.RemainingSeconds + 0.25f);
            if (!InvokeButton("Collect Production Button") || state.WaferStock != 1)
            {
                Debug.LogError($"[Semicon Photo Smoke] Wafer preparation mismatch: wafer={state.WaferStock}");
                Application.Quit(75);
                yield break;
            }

            if (!InvokeButton("Select Oxidation Recipe Button") || !InvokeButton("Produce SC-01 Button"))
            {
                Application.Quit(75);
                yield break;
            }
            var oxideJob = state.GetProductionJob(0);
            yield return new WaitForSecondsRealtime(oxideJob.RemainingSeconds + 0.25f);
            if (!InvokeButton("Collect Production Button") || state.OxidizedWaferStock != 1 ||
                state.AverageOxidizedWaferQuality != 86)
            {
                Debug.LogError($"[Semicon Photo Smoke] Oxide preparation mismatch: " +
                    $"oxide={state.OxidizedWaferStock}, quality={state.AverageOxidizedWaferQuality}");
                Application.Quit(76);
                yield break;
            }

            if (!InvokeButton("Select Photo Recipe Button") || !InvokeButton("Produce SC-01 Button"))
            {
                Application.Quit(77);
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.7f);
            var photoJob = state.GetProductionJob(0);
            if (!photoJob.HasJob || photoJob.Recipe != SemiconRecipeKind.PhotoPatternedWafer ||
                photoJob.IsComplete || state.OxidizedWaferStock != 0 || state.ChemicalStock != 9 ||
                state.PatternedWaferStock != 0 || photoJob.Quality != 88)
            {
                Debug.LogError($"[Semicon Photo Smoke] Start mismatch: " +
                    $"job={photoJob.HasJob}/{photoJob.Recipe}/{photoJob.IsComplete}, " +
                    $"oxide={state.OxidizedWaferStock}, chemical={state.ChemicalStock}, " +
                    $"photo={state.PatternedWaferStock}, quality={photoJob.Quality}");
                Application.Quit(78);
                yield break;
            }

            var processingPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-photo-processing-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(processingPath);
            InvokeButton("Production Close Button");
            yield return new WaitForSecondsRealtime(0.22f);
            photoJob = state.GetProductionJob(0);
            yield return new WaitForSecondsRealtime(photoJob.RemainingSeconds + 0.25f);

            productionMachine.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            var readyJob = state.GetProductionJob(0);
            if (!readyJob.IsComplete)
            {
                Debug.LogError($"[Semicon Photo Smoke] Completion timer mismatch: {readyJob.RemainingSeconds:0.00}");
                Application.Quit(79);
                yield break;
            }
            var readyPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-photo-ready-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(readyPath);
            if (!InvokeButton("Collect Production Button") || state.PatternedWaferStock != 1 ||
                state.AveragePatternedWaferQuality != 88 || state.GetProductionJob(0).HasJob)
            {
                Debug.LogError($"[Semicon Photo Smoke] Collect mismatch: photo={state.PatternedWaferStock}, " +
                    $"quality={state.AveragePatternedWaferQuality}, job={state.GetProductionJob(0).HasJob}");
                Application.Quit(79);
                yield break;
            }

            Debug.Log($"[Semicon Photo Smoke] PASS / photo experiment→wafer→oxide→pattern / " +
                $"photo=1 / quality=88 / Experiment={experimentPath} / Processing={processingPath} / Ready={readyPath}");
            state.ResetProgress();
            Application.Quit(0);
        }

        private IEnumerator RunEtchProductionSmokeTest()
        {
            yield return null;
            var state = SemiconGameState.Instance;
            var player = FindFirstObjectByType<SemiconPlayerController>();
            var cameraController = FindFirstObjectByType<SemiconThirdPersonCamera>();
            var etchTerminal = FindFirstObjectByType<SemiconEtchTerminal>(FindObjectsInactive.Include);
            var factoryEntrance = FindPortal("Factory Visitor Entrance");
            var productionMachine = FindObjectsByType<SemiconProductionMachine>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).FirstOrDefault(item => item.name == "SC-01 Assembly Machine 01");
            if (state == null || player == null || cameraController == null || etchTerminal == null ||
                factoryEntrance == null || productionMachine == null)
            {
                Debug.LogError("[Semicon Etch Smoke] Required etch component is missing.");
                Application.Quit(81);
                yield break;
            }

            state.ResetProgress();
            etchTerminal.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            if (!InvokeButton("Run Etch Experiment Button"))
            {
                Application.Quit(82);
                yield break;
            }
            yield return new WaitForSecondsRealtime(1.15f);
            if (!state.EtchRecipeQualified || state.EtchExperimentCount != 1 || state.ResearchPoints != 112 ||
                Mathf.Abs(state.BestEtchDepth - 120f) > 0.1f || Mathf.Abs(state.BestEtchProfile - 98f) > 0.1f)
            {
                Debug.LogError($"[Semicon Etch Smoke] Experiment mismatch: qualified={state.EtchRecipeQualified}, " +
                    $"count={state.EtchExperimentCount}, research={state.ResearchPoints}, " +
                    $"depth={state.BestEtchDepth:0.0}, profile={state.BestEtchProfile:0.0}");
                Application.Quit(83);
                yield break;
            }

            var experimentPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-etch-experiment-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(experimentPath);
            InvokeButton("Etch Close Button");
            yield return new WaitForSecondsRealtime(0.22f);

            state.RecordOxidationExperiment(1000, 60, 100f, 98f, true);
            state.RecordPhotoExperiment(105, 0.05f, 97f, 98f, true);
            state.TryBuyMaterial(SemiconMaterialKind.Silicon, 10);
            state.TryBuyMaterial(SemiconMaterialKind.ProcessGas, 10);
            state.TryBuyMaterial(SemiconMaterialKind.Chemicals, 10);

            if (!state.TryStartProduction(0, SemiconRecipeKind.WaferSubstrate, 1, out var waferJob,
                    out var waferReason))
            {
                Debug.LogError("[Semicon Etch Smoke] Wafer start failed: " + waferReason);
                Application.Quit(84);
                yield break;
            }
            yield return new WaitForSecondsRealtime(waferJob.RemainingSeconds + 0.25f);
            if (!state.TryCollectProduction(0, out _, out _, out _, out waferReason))
            {
                Debug.LogError("[Semicon Etch Smoke] Wafer collect failed: " + waferReason);
                Application.Quit(84);
                yield break;
            }

            if (!state.TryStartProduction(0, SemiconRecipeKind.OxidizedWafer, 1, out var oxideJob,
                    out var oxideReason))
            {
                Debug.LogError("[Semicon Etch Smoke] Oxide start failed: " + oxideReason);
                Application.Quit(85);
                yield break;
            }
            yield return new WaitForSecondsRealtime(oxideJob.RemainingSeconds + 0.25f);
            if (!state.TryCollectProduction(0, out _, out _, out _, out oxideReason))
            {
                Debug.LogError("[Semicon Etch Smoke] Oxide collect failed: " + oxideReason);
                Application.Quit(85);
                yield break;
            }

            if (!state.TryStartProduction(0, SemiconRecipeKind.PhotoPatternedWafer, 1, out var photoJob,
                    out var photoReason))
            {
                Debug.LogError("[Semicon Etch Smoke] Photo start failed: " + photoReason);
                Application.Quit(86);
                yield break;
            }
            yield return new WaitForSecondsRealtime(photoJob.RemainingSeconds + 0.25f);
            if (!state.TryCollectProduction(0, out _, out _, out _, out photoReason) ||
                state.PatternedWaferStock != 1 || state.AveragePatternedWaferQuality != 88)
            {
                Debug.LogError($"[Semicon Etch Smoke] Photo preparation mismatch: reason={photoReason}, " +
                    $"stock={state.PatternedWaferStock}, quality={state.AveragePatternedWaferQuality}");
                Application.Quit(86);
                yield break;
            }

            factoryEntrance.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.2f);
            productionMachine.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            if (!InvokeButton("Select Etch Recipe Button") || !InvokeButton("Produce SC-01 Button"))
            {
                Application.Quit(87);
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.7f);
            var etchJob = state.GetProductionJob(0);
            if (!etchJob.HasJob || etchJob.Recipe != SemiconRecipeKind.EtchedWafer || etchJob.IsComplete ||
                state.PatternedWaferStock != 0 || state.ProcessGasStock != 8 || state.EtchedWaferStock != 0 ||
                etchJob.Quality != 89)
            {
                Debug.LogError($"[Semicon Etch Smoke] Start mismatch: " +
                    $"job={etchJob.HasJob}/{etchJob.Recipe}/{etchJob.IsComplete}, " +
                    $"pattern={state.PatternedWaferStock}, gas={state.ProcessGasStock}, " +
                    $"etch={state.EtchedWaferStock}, quality={etchJob.Quality}");
                Application.Quit(88);
                yield break;
            }

            var processingPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-etch-processing-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(processingPath);
            InvokeButton("Production Close Button");
            yield return new WaitForSecondsRealtime(0.22f);
            etchJob = state.GetProductionJob(0);
            yield return new WaitForSecondsRealtime(etchJob.RemainingSeconds + 0.25f);

            productionMachine.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            var readyJob = state.GetProductionJob(0);
            if (!readyJob.IsComplete)
            {
                Debug.LogError($"[Semicon Etch Smoke] Completion timer mismatch: {readyJob.RemainingSeconds:0.00}");
                Application.Quit(89);
                yield break;
            }
            var readyPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-etch-ready-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(readyPath);
            if (!InvokeButton("Collect Production Button") || state.EtchedWaferStock != 1 ||
                state.AverageEtchedWaferQuality != 89 || state.GetProductionJob(0).HasJob)
            {
                Debug.LogError($"[Semicon Etch Smoke] Collect mismatch: etch={state.EtchedWaferStock}, " +
                    $"quality={state.AverageEtchedWaferQuality}, job={state.GetProductionJob(0).HasJob}");
                Application.Quit(89);
                yield break;
            }

            Debug.Log($"[Semicon Etch Smoke] PASS / etch experiment→wafer→oxide→photo→etch / " +
                $"etch=1 / quality=89 / Experiment={experimentPath} / Processing={processingPath} / Ready={readyPath}");
            state.ResetProgress();
            Application.Quit(0);
        }

        private IEnumerator RunDepositionProductionSmokeTest()
        {
            yield return null;
            var state = SemiconGameState.Instance;
            var player = FindFirstObjectByType<SemiconPlayerController>();
            var cameraController = FindFirstObjectByType<SemiconThirdPersonCamera>();
            var depositionTerminal = FindFirstObjectByType<SemiconDepositionTerminal>(FindObjectsInactive.Include);
            var factoryEntrance = FindPortal("Factory Visitor Entrance");
            var productionMachine = FindObjectsByType<SemiconProductionMachine>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).FirstOrDefault(item => item.name == "SC-01 Assembly Machine 01");
            if (state == null || player == null || cameraController == null || depositionTerminal == null ||
                factoryEntrance == null || productionMachine == null)
            {
                Debug.LogError("[Semicon Deposition Smoke] Required deposition component is missing.");
                Application.Quit(91);
                yield break;
            }

            state.ResetProgress();
            depositionTerminal.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            if (!InvokeButton("Run Deposition Experiment Button"))
            {
                Application.Quit(92);
                yield break;
            }
            yield return new WaitForSecondsRealtime(1.15f);
            if (!state.DepositionRecipeQualified || state.DepositionExperimentCount != 1 ||
                state.ResearchPoints != 112 || Mathf.Abs(state.BestDepositionThickness - 80f) > 0.1f ||
                Mathf.Abs(state.BestDepositionUniformity - 98f) > 0.1f ||
                Mathf.Abs(state.BestDepositionCoverage - 95f) > 0.1f)
            {
                Debug.LogError($"[Semicon Deposition Smoke] Experiment mismatch: " +
                    $"qualified={state.DepositionRecipeQualified}, count={state.DepositionExperimentCount}, " +
                    $"research={state.ResearchPoints}, thickness={state.BestDepositionThickness:0.0}, " +
                    $"uniformity={state.BestDepositionUniformity:0.0}, coverage={state.BestDepositionCoverage:0.0}");
                Application.Quit(93);
                yield break;
            }

            var experimentPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-deposition-experiment-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(experimentPath);
            InvokeButton("Deposition Close Button");
            yield return new WaitForSecondsRealtime(0.22f);

            state.RecordOxidationExperiment(1000, 60, 100f, 98f, true);
            state.RecordPhotoExperiment(105, 0.05f, 97f, 98f, true);
            state.RecordEtchExperiment(250, 60, 120f, 98f, true);
            state.TryBuyMaterial(SemiconMaterialKind.Silicon, 10);
            state.TryBuyMaterial(SemiconMaterialKind.ProcessGas, 10);
            state.TryBuyMaterial(SemiconMaterialKind.Chemicals, 10);

            if (!state.TryStartProduction(0, SemiconRecipeKind.WaferSubstrate, 1, out var waferJob,
                    out var reason))
            {
                Debug.LogError("[Semicon Deposition Smoke] Wafer start failed: " + reason);
                Application.Quit(94);
                yield break;
            }
            yield return new WaitForSecondsRealtime(waferJob.RemainingSeconds + 0.25f);
            if (!state.TryCollectProduction(0, out _, out _, out _, out reason))
            {
                Debug.LogError("[Semicon Deposition Smoke] Wafer collect failed: " + reason);
                Application.Quit(94);
                yield break;
            }

            if (!state.TryStartProduction(0, SemiconRecipeKind.OxidizedWafer, 1, out var oxideJob, out reason))
            {
                Debug.LogError("[Semicon Deposition Smoke] Oxide start failed: " + reason);
                Application.Quit(95);
                yield break;
            }
            yield return new WaitForSecondsRealtime(oxideJob.RemainingSeconds + 0.25f);
            if (!state.TryCollectProduction(0, out _, out _, out _, out reason))
            {
                Debug.LogError("[Semicon Deposition Smoke] Oxide collect failed: " + reason);
                Application.Quit(95);
                yield break;
            }

            if (!state.TryStartProduction(0, SemiconRecipeKind.PhotoPatternedWafer, 1, out var photoJob, out reason))
            {
                Debug.LogError("[Semicon Deposition Smoke] Photo start failed: " + reason);
                Application.Quit(96);
                yield break;
            }
            yield return new WaitForSecondsRealtime(photoJob.RemainingSeconds + 0.25f);
            if (!state.TryCollectProduction(0, out _, out _, out _, out reason))
            {
                Debug.LogError("[Semicon Deposition Smoke] Photo collect failed: " + reason);
                Application.Quit(96);
                yield break;
            }

            if (!state.TryStartProduction(0, SemiconRecipeKind.EtchedWafer, 1, out var etchJob, out reason))
            {
                Debug.LogError("[Semicon Deposition Smoke] Etch start failed: " + reason);
                Application.Quit(97);
                yield break;
            }
            yield return new WaitForSecondsRealtime(etchJob.RemainingSeconds + 0.25f);
            if (!state.TryCollectProduction(0, out _, out _, out _, out reason) || state.EtchedWaferStock != 1 ||
                state.AverageEtchedWaferQuality != 89)
            {
                Debug.LogError($"[Semicon Deposition Smoke] Etch preparation mismatch: reason={reason}, " +
                    $"stock={state.EtchedWaferStock}, quality={state.AverageEtchedWaferQuality}");
                Application.Quit(97);
                yield break;
            }

            factoryEntrance.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.2f);
            productionMachine.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            if (!InvokeButton("Select Deposition Recipe Button") || !InvokeButton("Produce SC-01 Button"))
            {
                Application.Quit(98);
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.7f);
            var depositionJob = state.GetProductionJob(0);
            if (!depositionJob.HasJob || depositionJob.Recipe != SemiconRecipeKind.DepositedWafer ||
                depositionJob.IsComplete || state.EtchedWaferStock != 0 || state.ProcessGasStock != 7 ||
                state.DepositedWaferStock != 0 || depositionJob.Quality != 89)
            {
                Debug.LogError($"[Semicon Deposition Smoke] Start mismatch: " +
                    $"job={depositionJob.HasJob}/{depositionJob.Recipe}/{depositionJob.IsComplete}, " +
                    $"etch={state.EtchedWaferStock}, gas={state.ProcessGasStock}, " +
                    $"deposition={state.DepositedWaferStock}, quality={depositionJob.Quality}");
                Application.Quit(99);
                yield break;
            }

            var processingPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-deposition-processing-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(processingPath);
            InvokeButton("Production Close Button");
            yield return new WaitForSecondsRealtime(0.22f);
            depositionJob = state.GetProductionJob(0);
            yield return new WaitForSecondsRealtime(depositionJob.RemainingSeconds + 0.25f);

            productionMachine.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            var readyJob = state.GetProductionJob(0);
            if (!readyJob.IsComplete)
            {
                Debug.LogError($"[Semicon Deposition Smoke] Completion timer mismatch: {readyJob.RemainingSeconds:0.00}");
                Application.Quit(100);
                yield break;
            }
            var readyPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-deposition-ready-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(readyPath);
            if (!InvokeButton("Collect Production Button") || state.DepositedWaferStock != 1 ||
                state.AverageDepositedWaferQuality != 89 || state.GetProductionJob(0).HasJob)
            {
                Debug.LogError($"[Semicon Deposition Smoke] Collect mismatch: deposition={state.DepositedWaferStock}, " +
                    $"quality={state.AverageDepositedWaferQuality}, job={state.GetProductionJob(0).HasJob}");
                Application.Quit(100);
                yield break;
            }

            Debug.Log($"[Semicon Deposition Smoke] PASS / deposition experiment→wafer→oxide→photo→etch→depo / " +
                $"deposition=1 / quality=89 / Experiment={experimentPath} / Processing={processingPath} / Ready={readyPath}");
            state.ResetProgress();
            Application.Quit(0);
        }

        private IEnumerator RunMetalProductionSmokeTest()
        {
            yield return null;
            var state = SemiconGameState.Instance;
            var player = FindFirstObjectByType<SemiconPlayerController>();
            var cameraController = FindFirstObjectByType<SemiconThirdPersonCamera>();
            var metalTerminal = FindFirstObjectByType<SemiconMetalTerminal>(FindObjectsInactive.Include);
            var factoryEntrance = FindPortal("Factory Visitor Entrance");
            var productionMachine = FindObjectsByType<SemiconProductionMachine>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).FirstOrDefault(item => item.name == "SC-01 Assembly Machine 01");
            if (state == null || player == null || cameraController == null || metalTerminal == null ||
                factoryEntrance == null || productionMachine == null)
            {
                Debug.LogError("[Semicon Metal Smoke] Required metal component is missing.");
                Application.Quit(101);
                yield break;
            }

            state.ResetProgress();
            metalTerminal.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            if (!InvokeButton("Run Metal Experiment Button"))
            {
                Application.Quit(102);
                yield break;
            }
            yield return new WaitForSecondsRealtime(1.15f);
            if (!state.MetalRecipeQualified || state.MetalExperimentCount != 1 || state.ResearchPoints != 112 ||
                Mathf.Abs(state.BestMetalThickness - 450f) > 0.1f ||
                Mathf.Abs(state.BestMetalResistance - 0.119f) > 0.001f ||
                Mathf.Abs(state.BestMetalAdhesion - 98f) > 0.1f)
            {
                Debug.LogError($"[Semicon Metal Smoke] Experiment mismatch: qualified={state.MetalRecipeQualified}, " +
                    $"count={state.MetalExperimentCount}, research={state.ResearchPoints}, " +
                    $"thickness={state.BestMetalThickness:0.0}, resistance={state.BestMetalResistance:0.000}, " +
                    $"adhesion={state.BestMetalAdhesion:0.0}");
                Application.Quit(103);
                yield break;
            }

            var experimentPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-metal-experiment-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(experimentPath);
            InvokeButton("Metal Close Button");
            yield return new WaitForSecondsRealtime(0.22f);

            state.RecordOxidationExperiment(1000, 60, 100f, 98f, true);
            state.RecordPhotoExperiment(105, 0.05f, 97f, 98f, true);
            state.RecordEtchExperiment(250, 60, 120f, 98f, true);
            state.RecordDepositionExperiment(400, 6, 80f, 98f, 95f, true);
            state.TryBuyMaterial(SemiconMaterialKind.Silicon, 10);
            state.TryBuyMaterial(SemiconMaterialKind.ProcessGas, 10);
            state.TryBuyMaterial(SemiconMaterialKind.Chemicals, 10);
            state.TryBuyMaterial(SemiconMaterialKind.MetalTarget, 10);

            var preparationRecipes = new[]
            {
                SemiconRecipeKind.WaferSubstrate,
                SemiconRecipeKind.OxidizedWafer,
                SemiconRecipeKind.PhotoPatternedWafer,
                SemiconRecipeKind.EtchedWafer,
                SemiconRecipeKind.DepositedWafer
            };
            foreach (var preparationRecipe in preparationRecipes)
            {
                if (!state.TryStartProduction(0, preparationRecipe, 1, out var preparationJob, out var reason))
                {
                    Debug.LogError($"[Semicon Metal Smoke] Preparation start failed: {preparationRecipe} / {reason}");
                    Application.Quit(104);
                    yield break;
                }
                yield return new WaitForSecondsRealtime(preparationJob.RemainingSeconds + 0.25f);
                if (!state.TryCollectProduction(0, out _, out _, out _, out reason))
                {
                    Debug.LogError($"[Semicon Metal Smoke] Preparation collect failed: {preparationRecipe} / {reason}");
                    Application.Quit(104);
                    yield break;
                }
            }

            if (state.DepositedWaferStock != 1 || state.AverageDepositedWaferQuality != 89 ||
                state.MetalTargetStock != 10)
            {
                Debug.LogError($"[Semicon Metal Smoke] Preparation mismatch: deposition={state.DepositedWaferStock}, " +
                    $"quality={state.AverageDepositedWaferQuality}, target={state.MetalTargetStock}");
                Application.Quit(105);
                yield break;
            }

            factoryEntrance.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.2f);
            productionMachine.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            if (!InvokeButton("Select Metal Recipe Button") || !InvokeButton("Produce SC-01 Button"))
            {
                Application.Quit(106);
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.7f);
            var metalJob = state.GetProductionJob(0);
            if (!metalJob.HasJob || metalJob.Recipe != SemiconRecipeKind.MetalizedWafer || metalJob.IsComplete ||
                state.DepositedWaferStock != 0 || state.MetalTargetStock != 9 || state.ProcessGasStock != 7 ||
                state.MetalizedWaferStock != 0 || metalJob.Quality != 89)
            {
                Debug.LogError($"[Semicon Metal Smoke] Start mismatch: " +
                    $"job={metalJob.HasJob}/{metalJob.Recipe}/{metalJob.IsComplete}, " +
                    $"deposition={state.DepositedWaferStock}, target={state.MetalTargetStock}, " +
                    $"gas={state.ProcessGasStock}, metal={state.MetalizedWaferStock}, quality={metalJob.Quality}");
                Application.Quit(107);
                yield break;
            }

            var processingPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-metal-processing-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(processingPath);
            InvokeButton("Production Close Button");
            yield return new WaitForSecondsRealtime(0.22f);
            metalJob = state.GetProductionJob(0);
            yield return new WaitForSecondsRealtime(metalJob.RemainingSeconds + 0.25f);

            productionMachine.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            var readyJob = state.GetProductionJob(0);
            if (!readyJob.IsComplete)
            {
                Debug.LogError($"[Semicon Metal Smoke] Completion timer mismatch: {readyJob.RemainingSeconds:0.00}");
                Application.Quit(108);
                yield break;
            }
            var readyPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-metal-ready-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(readyPath);
            if (!InvokeButton("Collect Production Button") || state.MetalizedWaferStock != 1 ||
                state.AverageMetalizedWaferQuality != 89 || state.GetProductionJob(0).HasJob)
            {
                Debug.LogError($"[Semicon Metal Smoke] Collect mismatch: metal={state.MetalizedWaferStock}, " +
                    $"quality={state.AverageMetalizedWaferQuality}, job={state.GetProductionJob(0).HasJob}");
                Application.Quit(108);
                yield break;
            }

            Debug.Log($"[Semicon Metal Smoke] PASS / metal experiment→wafer→oxide→photo→etch→depo→metal / " +
                $"metal=1 / quality=89 / Experiment={experimentPath} / Processing={processingPath} / Ready={readyPath}");
            state.ResetProgress();
            Application.Quit(0);
        }

        private IEnumerator RunEdsProductionSmokeTest()
        {
            yield return null;
            var state = SemiconGameState.Instance;
            var player = FindFirstObjectByType<SemiconPlayerController>();
            var cameraController = FindFirstObjectByType<SemiconThirdPersonCamera>();
            var edsTerminal = FindFirstObjectByType<SemiconEdsTerminal>(FindObjectsInactive.Include);
            var factoryEntrance = FindPortal("Factory Visitor Entrance");
            var productionMachine = FindObjectsByType<SemiconProductionMachine>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).FirstOrDefault(item => item.name == "SC-01 Assembly Machine 01");
            if (state == null || player == null || cameraController == null || edsTerminal == null ||
                factoryEntrance == null || productionMachine == null)
            {
                Debug.LogError("[Semicon EDS Smoke] Required EDS component is missing.");
                Application.Quit(111);
                yield break;
            }

            state.ResetProgress();
            edsTerminal.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            if (!InvokeButton("Run EDS Experiment Button"))
            {
                Application.Quit(112);
                yield break;
            }
            yield return new WaitForSecondsRealtime(1.15f);
            if (!state.EdsRecipeQualified || state.EdsExperimentCount != 1 || state.ResearchPoints != 112 ||
                Mathf.Abs(state.BestEdsYield - 96f) > 0.1f ||
                Mathf.Abs(state.BestEdsDetection - 98f) > 0.1f ||
                Mathf.Abs(state.BestEdsFalseReject - 2f) > 0.1f)
            {
                Debug.LogError($"[Semicon EDS Smoke] Experiment mismatch: qualified={state.EdsRecipeQualified}, " +
                    $"count={state.EdsExperimentCount}, research={state.ResearchPoints}, " +
                    $"yield={state.BestEdsYield:0.0}, detection={state.BestEdsDetection:0.0}, " +
                    $"falseReject={state.BestEdsFalseReject:0.0}");
                Application.Quit(113);
                yield break;
            }

            var experimentPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-eds-experiment-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(experimentPath);
            InvokeButton("EDS Close Button");
            yield return new WaitForSecondsRealtime(0.22f);

            state.RecordOxidationExperiment(1000, 60, 100f, 98f, true);
            state.RecordPhotoExperiment(105, 0.05f, 97f, 98f, true);
            state.RecordEtchExperiment(250, 60, 120f, 98f, true);
            state.RecordDepositionExperiment(400, 6, 80f, 98f, 95f, true);
            state.RecordMetalExperiment(250, 60, 450f, 0.119f, 98f, true);
            state.TryBuyMaterial(SemiconMaterialKind.Silicon, 10);
            state.TryBuyMaterial(SemiconMaterialKind.ProcessGas, 10);
            state.TryBuyMaterial(SemiconMaterialKind.Chemicals, 10);
            state.TryBuyMaterial(SemiconMaterialKind.MetalTarget, 10);

            var preparationRecipes = new[]
            {
                SemiconRecipeKind.WaferSubstrate,
                SemiconRecipeKind.OxidizedWafer,
                SemiconRecipeKind.PhotoPatternedWafer,
                SemiconRecipeKind.EtchedWafer,
                SemiconRecipeKind.DepositedWafer,
                SemiconRecipeKind.MetalizedWafer
            };
            foreach (var preparationRecipe in preparationRecipes)
            {
                if (!state.TryStartProduction(0, preparationRecipe, 1, out var preparationJob, out var reason))
                {
                    Debug.LogError($"[Semicon EDS Smoke] Preparation start failed: {preparationRecipe} / {reason}");
                    Application.Quit(114);
                    yield break;
                }
                yield return new WaitForSecondsRealtime(preparationJob.RemainingSeconds + 0.25f);
                if (!state.TryCollectProduction(0, out _, out _, out _, out reason))
                {
                    Debug.LogError($"[Semicon EDS Smoke] Preparation collect failed: {preparationRecipe} / {reason}");
                    Application.Quit(114);
                    yield break;
                }
            }

            if (state.MetalizedWaferStock != 1 || state.AverageMetalizedWaferQuality != 89 ||
                state.MetalTargetStock != 9)
            {
                Debug.LogError($"[Semicon EDS Smoke] Preparation mismatch: metal={state.MetalizedWaferStock}, " +
                    $"quality={state.AverageMetalizedWaferQuality}, target={state.MetalTargetStock}");
                Application.Quit(115);
                yield break;
            }

            factoryEntrance.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.2f);
            productionMachine.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            if (!InvokeButton("Select EDS Recipe Button") || !InvokeButton("Produce SC-01 Button"))
            {
                Application.Quit(116);
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.7f);
            var edsJob = state.GetProductionJob(0);
            if (!edsJob.HasJob || edsJob.Recipe != SemiconRecipeKind.TestedWafer || edsJob.IsComplete ||
                state.MetalizedWaferStock != 0 || state.MetalTargetStock != 9 || state.ProcessGasStock != 7 ||
                state.TestedWaferStock != 0 || edsJob.Quality != 89)
            {
                Debug.LogError($"[Semicon EDS Smoke] Start mismatch: " +
                    $"job={edsJob.HasJob}/{edsJob.Recipe}/{edsJob.IsComplete}, metal={state.MetalizedWaferStock}, " +
                    $"target={state.MetalTargetStock}, gas={state.ProcessGasStock}, " +
                    $"tested={state.TestedWaferStock}, quality={edsJob.Quality}");
                Application.Quit(117);
                yield break;
            }

            var processingPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-eds-processing-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(processingPath);
            InvokeButton("Production Close Button");
            yield return new WaitForSecondsRealtime(0.22f);
            edsJob = state.GetProductionJob(0);
            yield return new WaitForSecondsRealtime(edsJob.RemainingSeconds + 0.25f);

            productionMachine.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            var readyJob = state.GetProductionJob(0);
            if (!readyJob.IsComplete)
            {
                Debug.LogError($"[Semicon EDS Smoke] Completion timer mismatch: {readyJob.RemainingSeconds:0.00}");
                Application.Quit(118);
                yield break;
            }
            var readyPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-eds-ready-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(readyPath);
            if (!InvokeButton("Collect Production Button") || state.TestedWaferStock != 1 ||
                state.AverageTestedWaferQuality != 89 || state.GetProductionJob(0).HasJob)
            {
                Debug.LogError($"[Semicon EDS Smoke] Collect mismatch: tested={state.TestedWaferStock}, " +
                    $"quality={state.AverageTestedWaferQuality}, job={state.GetProductionJob(0).HasJob}");
                Application.Quit(118);
                yield break;
            }

            Debug.Log($"[Semicon EDS Smoke] PASS / EDS experiment→wafer→oxide→photo→etch→depo→metal→EDS / " +
                $"tested=1 / quality=89 / Experiment={experimentPath} / Processing={processingPath} / Ready={readyPath}");
            state.ResetProgress();
            Application.Quit(0);
        }

        private IEnumerator RunPackageProductionSmokeTest()
        {
            yield return null;
            var state = SemiconGameState.Instance;
            var player = FindFirstObjectByType<SemiconPlayerController>();
            var cameraController = FindFirstObjectByType<SemiconThirdPersonCamera>();
            var packageTerminal = FindFirstObjectByType<SemiconPackageTerminal>(FindObjectsInactive.Include);
            var factoryEntrance = FindPortal("Factory Visitor Entrance");
            var productionMachine = FindObjectsByType<SemiconProductionMachine>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).FirstOrDefault(item => item.name == "SC-01 Assembly Machine 01");
            if (state == null || player == null || cameraController == null || packageTerminal == null ||
                factoryEntrance == null || productionMachine == null)
            {
                Debug.LogError("[Semicon Package Smoke] Required package component is missing.");
                Application.Quit(121);
                yield break;
            }

            state.ResetProgress();
            packageTerminal.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            if (!InvokeButton("Run Package Experiment Button"))
            {
                Application.Quit(122);
                yield break;
            }
            yield return new WaitForSecondsRealtime(1.2f);
            if (!state.PackageRecipeQualified || state.PackageExperimentCount != 1 ||
                state.ResearchPoints != 112 || Mathf.Abs(state.BestPackageBondStrength - 96f) > 0.1f ||
                Mathf.Abs(state.BestPackageIntegrity - 97f) > 0.1f ||
                Mathf.Abs(state.BestPackageFinalPass - 97f) > 0.1f)
            {
                Debug.LogError($"[Semicon Package Smoke] Experiment mismatch: " +
                    $"qualified={state.PackageRecipeQualified}, count={state.PackageExperimentCount}, " +
                    $"research={state.ResearchPoints}, strength={state.BestPackageBondStrength:0.0}, " +
                    $"integrity={state.BestPackageIntegrity:0.0}, final={state.BestPackageFinalPass:0.0}");
                Application.Quit(123);
                yield break;
            }

            var experimentPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-package-experiment-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(experimentPath);
            InvokeButton("Package Close Button");
            yield return new WaitForSecondsRealtime(0.22f);

            state.RecordOxidationExperiment(1000, 60, 100f, 98f, true);
            state.RecordPhotoExperiment(105, 0.05f, 97f, 98f, true);
            state.RecordEtchExperiment(250, 60, 120f, 98f, true);
            state.RecordDepositionExperiment(400, 6, 80f, 98f, 95f, true);
            state.RecordMetalExperiment(250, 60, 450f, 0.119f, 98f, true);
            state.RecordEdsExperiment(3, 30, 96f, 98f, 2f, true);
            state.TryBuyMaterial(SemiconMaterialKind.Silicon, 10);
            state.TryBuyMaterial(SemiconMaterialKind.ProcessGas, 10);
            state.TryBuyMaterial(SemiconMaterialKind.Chemicals, 10);
            state.TryBuyMaterial(SemiconMaterialKind.MetalTarget, 10);

            var preparationRecipes = new[]
            {
                SemiconRecipeKind.WaferSubstrate,
                SemiconRecipeKind.OxidizedWafer,
                SemiconRecipeKind.PhotoPatternedWafer,
                SemiconRecipeKind.EtchedWafer,
                SemiconRecipeKind.DepositedWafer,
                SemiconRecipeKind.MetalizedWafer,
                SemiconRecipeKind.TestedWafer
            };
            foreach (var preparationRecipe in preparationRecipes)
            {
                if (!state.TryStartProduction(0, preparationRecipe, 1, out var preparationJob, out var reason))
                {
                    Debug.LogError($"[Semicon Package Smoke] Preparation start failed: " +
                        $"{preparationRecipe} / {reason}");
                    Application.Quit(124);
                    yield break;
                }
                yield return new WaitForSecondsRealtime(preparationJob.RemainingSeconds + 0.25f);
                if (!state.TryCollectProduction(0, out _, out _, out _, out reason))
                {
                    Debug.LogError($"[Semicon Package Smoke] Preparation collect failed: " +
                        $"{preparationRecipe} / {reason}");
                    Application.Quit(124);
                    yield break;
                }
            }

            if (state.TestedWaferStock != 1 || state.AverageTestedWaferQuality != 89 ||
                state.ChemicalStock != 9 || state.MetalTargetStock != 9)
            {
                Debug.LogError($"[Semicon Package Smoke] Preparation mismatch: tested={state.TestedWaferStock}, " +
                    $"quality={state.AverageTestedWaferQuality}, chemical={state.ChemicalStock}, " +
                    $"target={state.MetalTargetStock}");
                Application.Quit(125);
                yield break;
            }

            factoryEntrance.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.2f);
            productionMachine.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            if (!InvokeButton("Select Package Recipe Button") || !InvokeButton("Produce SC-01 Button"))
            {
                Application.Quit(126);
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.7f);
            var packageJob = state.GetProductionJob(0);
            if (!packageJob.HasJob || packageJob.Recipe != SemiconRecipeKind.Sc01ControlSensor ||
                packageJob.IsComplete || state.TestedWaferStock != 0 || state.ChemicalStock != 8 ||
                state.FinishedProductStock != 0 || packageJob.Quality != 89)
            {
                Debug.LogError($"[Semicon Package Smoke] Start mismatch: " +
                    $"job={packageJob.HasJob}/{packageJob.Recipe}/{packageJob.IsComplete}, " +
                    $"tested={state.TestedWaferStock}, chemical={state.ChemicalStock}, " +
                    $"product={state.FinishedProductStock}, quality={packageJob.Quality}");
                Application.Quit(127);
                yield break;
            }

            var processingPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-package-processing-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(processingPath);
            InvokeButton("Production Close Button");
            yield return new WaitForSecondsRealtime(0.22f);
            packageJob = state.GetProductionJob(0);
            yield return new WaitForSecondsRealtime(packageJob.RemainingSeconds + 0.25f);

            productionMachine.Interact(player, cameraController);
            yield return new WaitForSecondsRealtime(0.3f);
            var readyJob = state.GetProductionJob(0);
            if (!readyJob.IsComplete)
            {
                Debug.LogError($"[Semicon Package Smoke] Completion timer mismatch: " +
                    $"{readyJob.RemainingSeconds:0.00}");
                Application.Quit(128);
                yield break;
            }
            var readyPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                $"semicon-package-ready-{Screen.width}x{Screen.height}.png"));
            yield return CaptureScreen(readyPath);
            if (!InvokeButton("Collect Production Button") || state.FinishedProductStock != 1 ||
                state.AverageFinishedProductQuality != 89 || state.GetFinishedProductSalePrice() != 4560 ||
                state.GetProductionJob(0).HasJob)
            {
                Debug.LogError($"[Semicon Package Smoke] Collect mismatch: product={state.FinishedProductStock}, " +
                    $"quality={state.AverageFinishedProductQuality}, sale={state.GetFinishedProductSalePrice()}, " +
                    $"job={state.GetProductionJob(0).HasJob}");
                Application.Quit(128);
                yield break;
            }

            if (!state.TrySellFinishedProducts(1) || state.FinishedProductStock != 0 || state.Credits != 23110)
            {
                Debug.LogError($"[Semicon Package Smoke] Sale mismatch: product={state.FinishedProductStock}, " +
                    $"credits={state.Credits}");
                Application.Quit(129);
                yield break;
            }

            Debug.Log($"[Semicon Package Smoke] PASS / package experiment→wafer→oxide→photo→etch→depo→" +
                $"metal→EDS→package→sale / quality=89 / sale=4560 / Experiment={experimentPath} / " +
                $"Processing={processingPath} / Ready={readyPath}");
            state.ResetProgress();
            Application.Quit(0);
        }

        private static SemiconScenePortal FindPortal(string objectName)
        {
            return FindObjectsByType<SemiconScenePortal>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(portal => portal.name == objectName);
        }

        private static bool InvokeButton(string objectName)
        {
            var target = GameObject.Find(objectName);
            var button = target != null ? target.GetComponent<Button>() : null;
            if (button == null)
            {
                Debug.LogError($"[Semicon Factory Smoke] Button missing: {objectName}");
                return false;
            }
            button.onClick.Invoke();
            return true;
        }

        private static bool ValidateWorldPhysics()
        {
            Physics.SyncTransforms();
            var samples = new[]
            {
                new Vector3(0f, 0.8f, 64f), new Vector3(0f, 0.8f, -58f),
                new Vector3(-44f, 0.8f, -6f), new Vector3(48f, 0.8f, -6f),
                new Vector3(40.8f, 0.8f, -13.9f), new Vector3(48.4f, 0.8f, -17.87f),
                new Vector3(-15.38f, 0.8f, -43.1f), new Vector3(1.56f, 0.8f, -68.4f)
            };
            foreach (var sample in samples)
            {
                if (Physics.Raycast(sample, Vector3.down, out _, 1.5f, ~0, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
                Debug.LogError($"[Semicon Factory Smoke] Walkable physics missing at {sample}");
                return false;
            }
            return true;
        }

        private static IEnumerator CaptureScreen(string outputPath)
        {
            if (Application.isBatchMode)
            {
                yield break;
            }
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
            ScreenCapture.CaptureScreenshot(outputPath, 1);
            yield return new WaitForEndOfFrame();
            var deadline = Time.realtimeSinceStartup + 4f;
            while (!File.Exists(outputPath) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }
    }
}
