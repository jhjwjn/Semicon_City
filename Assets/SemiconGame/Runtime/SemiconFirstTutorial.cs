using System;
using System.Linq;
using UnityEngine;

namespace SemiconCity.Game
{
    public sealed class SemiconFirstTutorial : MonoBehaviour
    {
        private enum GuideStep
        {
            Active,
            Complete
        }

        [SerializeField] private SemiconHud hud;
        [SerializeField] private SemiconPlayerController player;
        [SerializeField] private SemiconScenePortal marketEntrance;
        [SerializeField] private SemiconMarketTerminal marketTerminal;
        [SerializeField] private SemiconScenePortal factoryEntrance;
        [SerializeField] private SemiconProductionMachine productionMachine;
        [SerializeField] private Transform objectiveBeacon;

        private SemiconOxidationTerminal oxidationTerminal;
        private SemiconInteractionTerminal photoTerminal;
        private SemiconEtchTerminal etchTerminal;
        private SemiconDepositionTerminal depositionTerminal;
        private SemiconMetalTerminal metalTerminal;
        private SemiconEdsTerminal edsTerminal;
        private SemiconPackageTerminal packageTerminal;
        private Transform currentTarget;
        private Vector3 beaconBasePosition;
        private bool skipForAutomatedTest;
        private float refreshTimer;
        private int lastUnlockedProcess;

        public string CurrentObjectiveKey { get; private set; } = "INITIALIZING";

        public void Configure(SemiconHud targetHud, SemiconPlayerController targetPlayer,
            SemiconScenePortal targetMarketEntrance, SemiconMarketTerminal targetMarketTerminal,
            SemiconScenePortal targetFactoryEntrance, SemiconProductionMachine targetProductionMachine,
            Transform beacon)
        {
            hud = targetHud;
            player = targetPlayer;
            marketEntrance = targetMarketEntrance;
            marketTerminal = targetMarketTerminal;
            factoryEntrance = targetFactoryEntrance;
            productionMachine = targetProductionMachine;
            objectiveBeacon = beacon;
        }

        private void Start()
        {
            var arguments = Environment.GetCommandLineArgs();
            var isSmokeTest = arguments.Any(argument => argument.StartsWith("--semicon-",
                StringComparison.Ordinal) && argument.EndsWith("-smoke-test", StringComparison.Ordinal));
            var isCampaignTest = arguments.Contains("--semicon-campaign-smoke-test");
            skipForAutomatedTest = isSmokeTest &&
                                   !arguments.Contains("--semicon-tutorial-smoke-test") && !isCampaignTest;
            if (skipForAutomatedTest)
            {
                if (objectiveBeacon != null) objectiveBeacon.gameObject.SetActive(false);
                return;
            }

            ResolveReferences();
            var state = SemiconGameState.Instance;
            lastUnlockedProcess = state != null ? state.UnlockedProcessCount : 1;
            if (state != null) state.StateChanged += HandleStateChanged;
            RefreshObjective();
        }

        private void OnDestroy()
        {
            if (SemiconGameState.Instance != null)
                SemiconGameState.Instance.StateChanged -= HandleStateChanged;
        }

        private void Update()
        {
            if (skipForAutomatedTest) return;
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = 0.1f;
                RefreshObjective();
            }

            if (objectiveBeacon != null && objectiveBeacon.gameObject.activeSelf)
            {
                objectiveBeacon.position = beaconBasePosition +
                                           Vector3.up * (Mathf.Sin(Time.unscaledTime * 2.6f) * 0.2f);
                objectiveBeacon.Rotate(Vector3.up, 55f * Time.unscaledDeltaTime, Space.World);
            }
        }

        private void HandleStateChanged()
        {
            var state = SemiconGameState.Instance;
            if (state != null && state.UnlockedProcessCount > lastUnlockedProcess)
            {
                lastUnlockedProcess = state.UnlockedProcessCount;
                if (state.UnlockedProcessCount <= 8)
                    hud?.ShowToast($"{state.UnlockedProcessCount:00} {GetProcessName(state.UnlockedProcessCount)} 공정 개방", 3.5f);
            }
            RefreshObjective();
        }

        private void ResolveReferences()
        {
            if (player == null) player = FindFirstObjectByType<SemiconPlayerController>();
            if (hud == null) hud = FindFirstObjectByType<SemiconHud>();
            var portals = FindObjectsByType<SemiconScenePortal>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            if (marketEntrance == null)
                marketEntrance = portals.FirstOrDefault(portal => portal.name == "Materials Hall Entrance");
            if (factoryEntrance == null)
                factoryEntrance = portals.FirstOrDefault(portal => portal.name == "Factory Visitor Entrance");
            if (marketTerminal == null)
                marketTerminal = FindFirstObjectByType<SemiconMarketTerminal>(FindObjectsInactive.Include);
            if (productionMachine == null)
                productionMachine = FindObjectsByType<SemiconProductionMachine>(FindObjectsInactive.Include,
                    FindObjectsSortMode.None).FirstOrDefault(machine => machine.name.Contains("01"));
            oxidationTerminal = FindFirstObjectByType<SemiconOxidationTerminal>(FindObjectsInactive.Include);
            photoTerminal = FindFirstObjectByType<SemiconInteractionTerminal>(FindObjectsInactive.Include);
            etchTerminal = FindFirstObjectByType<SemiconEtchTerminal>(FindObjectsInactive.Include);
            depositionTerminal = FindFirstObjectByType<SemiconDepositionTerminal>(FindObjectsInactive.Include);
            metalTerminal = FindFirstObjectByType<SemiconMetalTerminal>(FindObjectsInactive.Include);
            edsTerminal = FindFirstObjectByType<SemiconEdsTerminal>(FindObjectsInactive.Include);
            packageTerminal = FindFirstObjectByType<SemiconPackageTerminal>(FindObjectsInactive.Include);
        }

        private void RefreshObjective()
        {
            var state = SemiconGameState.Instance;
            if (state == null || hud == null) return;

            if (!state.FirstTutorialCompleted)
            {
                RefreshWaferTutorial(state);
                return;
            }

            if (state.FirstOrderCompleted)
            {
                RefreshPostgameContract(state);
                return;
            }

            var process = Mathf.Clamp(state.UnlockedProcessCount, 2, 8);
            if (process == 8)
            {
                RefreshPackageAndOrder(state);
                return;
            }

            RefreshProcess(state, process);
        }

        private void RefreshPostgameContract(SemiconGameState state)
        {
            if (state.ActiveContract == SemiconContractKind.None)
            {
                SetObjective("POSTGAME_CONTRACT", "POST CAMPAIGN", "신규 납품 계약 확인",
                    "마켓 내부 계약 보드에서 공정 샘플 또는 완제품 주문을 수락하세요.", GetMarketTarget());
                return;
            }

            var definition = SemiconContractCatalog.Get(state.ActiveContract);
            var stock = state.GetRecipeOutputStock(definition.RequiredRecipe);
            var quality = state.GetRecipeAverageQuality(definition.RequiredRecipe);
            if (stock >= definition.RequiredAmount && quality >= definition.MinimumQuality)
            {
                SetObjective("POSTGAME_DELIVER", "ACTIVE CONTRACT", $"{definition.Code} 계약 납품",
                    $"납품 준비 완료  ·  {stock} / {definition.RequiredAmount}  ·  품질 {quality}", GetMarketTarget());
                return;
            }

            SetObjective("POSTGAME_PRODUCE", "ACTIVE CONTRACT", $"{definition.Code} 납품품 생산",
                $"{SemiconFactoryDefinitions.GetRecipeName(definition.RequiredRecipe)}  {stock} / {definition.RequiredAmount}  ·  요구 품질 {definition.MinimumQuality}",
                GetFactoryTarget());
        }

        private void RefreshWaferTutorial(SemiconGameState state)
        {
            if (state.WaferStock > 0)
            {
                if (state.CompleteFirstTutorial())
                    hud.ShowToast($"첫 생산 임무 완료  ·  ₩{SemiconGameState.FirstTutorialCreditReward:N0}  ·  " +
                                  $"연구 데이터 +{SemiconGameState.FirstTutorialResearchReward}", 4f);
                return;
            }

            var job = state.GetProductionJob(0);
            if (job.HasJob && job.Recipe == SemiconRecipeKind.WaferSubstrate)
            {
                if (job.IsComplete)
                    SetObjective("WAFER_COLLECT", "PROCESS 01  /  08", "완료된 WAFER-01 회수",
                        "SLOT 01 기계와 상호작용해 완료품을 창고로 회수하세요.", GetFactoryTarget());
                else
                    SetObjective("WAFER_WAIT", "PROCESS 01  /  08", "WAFER-01 생산 진행 중",
                        $"생산 완료까지 {job.RemainingSeconds:0.0}초  ·  완료되면 기계에서 회수하세요.",
                        GetFactoryTarget());
                return;
            }

            if (state.SiliconStock < SemiconGameState.WaferSiliconCost)
            {
                SetObjective("WAFER_MATERIAL", "PROCESS 01  /  08", "마켓에서 고순도 실리콘 확보",
                    $"자재 거래소에서 실리콘 묶음을 구매하세요.  보유 {state.SiliconStock} / {SemiconGameState.WaferSiliconCost}",
                    GetMarketTarget());
                return;
            }

            SetObjective("WAFER_START", "PROCESS 01  /  08", "FAB 01에서 WAFER-01 생산",
                "SLOT 01 공정 제어에서 WAFER를 선택하고 1 사이클을 시작하세요.", GetFactoryTarget());
        }

        private void RefreshProcess(SemiconGameState state, int process)
        {
            var recipe = GetRecipe(process);
            if (!IsRecipeQualified(state, process))
            {
                SetObjective($"EXPERIMENT_{process:00}", $"PROCESS {process:00}  /  08",
                    $"{GetProcessName(process)} 실험으로 {GetRecipeCode(recipe)} 개방",
                    $"두 핵심 변수를 조절해 품질 목표를 달성하세요.  실험 비용: 연구 데이터 8",
                    GetExperimentTarget(process));
                return;
            }

            GuideProduction(state, process, recipe);
        }

        private void RefreshPackageAndOrder(SemiconGameState state)
        {
            if (!state.PackageRecipeQualified)
            {
                SetObjective("EXPERIMENT_08", "PROCESS 08  /  08", "패키징 실험으로 PACKAGE-01 개방",
                    "본딩 압력과 몰딩 온도를 조절해 최종 합격 목표를 달성하세요.  실험 비용: 연구 데이터 8",
                    GetExperimentTarget(8));
                return;
            }

            if (!state.FirstOrderAccepted)
            {
                SetObjective("ORDER_ACCEPT", "FIRST CONTRACT  /  01", "마켓에서 첫 고객 주문 수락",
                    $"CONTRACT 01: SC-01 제어 센서 패키지 1개  ·  보상 ₩{SemiconGameState.FirstOrderCreditReward:N0}",
                    GetMarketTarget());
                return;
            }

            if (state.FinishedProductStock < 1)
            {
                GuideProduction(state, 8, SemiconRecipeKind.Sc01ControlSensor);
                return;
            }

            SetObjective("ORDER_DELIVER", "FIRST CONTRACT  /  01", "완성된 SC-01 주문 납품",
                $"자재 거래소 주문 버튼으로 1개를 납품하세요.  보상 ₩{SemiconGameState.FirstOrderCreditReward:N0} + 연구 데이터 {SemiconGameState.FirstOrderResearchReward}",
                GetMarketTarget());
        }

        private void GuideProduction(SemiconGameState state, int process, SemiconRecipeKind recipe)
        {
            var job = state.GetProductionJob(0);
            if (job.HasJob)
            {
                var activeCode = GetRecipeCode(job.Recipe);
                if (job.IsComplete)
                    SetObjective($"COLLECT_{process:00}", $"PROCESS {process:00}  /  08",
                        $"완료된 {activeCode} 회수", "SLOT 01에서 생산 완료품을 회수하면 다음 단계가 열립니다.",
                        GetFactoryTarget());
                else
                    SetObjective($"WAIT_{process:00}", $"PROCESS {process:00}  /  08",
                        $"{activeCode} 생산 진행 중", $"완료까지 {job.RemainingSeconds:0.0}초  ·  이후 기계에서 회수하세요.",
                        GetFactoryTarget());
                return;
            }

            if (TryGetMissingMaterial(state, recipe, out var materialName, out var owned, out var needed))
            {
                SetObjective($"MATERIAL_{process:00}", $"PROCESS {process:00}  /  08",
                    $"{GetRecipeCode(recipe)} 생산 자재 확보",
                    $"자재 거래소에서 {materialName} 묶음을 구매하세요.  보유 {owned} / {needed}", GetMarketTarget());
                return;
            }

            var orderPrefix = recipe == SemiconRecipeKind.Sc01ControlSensor ? "주문품 " : string.Empty;
            SetObjective($"START_{process:00}", $"PROCESS {process:00}  /  08",
                $"FAB 01에서 {orderPrefix}{GetRecipeCode(recipe)} 생산",
                $"SLOT 01 공정 제어에서 {GetRecipeCode(recipe)}를 선택하고 1 사이클을 시작하세요.",
                GetFactoryTarget());
        }

        private bool TryGetMissingMaterial(SemiconGameState state, SemiconRecipeKind recipe,
            out string materialName, out int owned, out int needed)
        {
            materialName = string.Empty;
            owned = 0;
            needed = 0;
            switch (recipe)
            {
                case SemiconRecipeKind.OxidizedWafer when state.ProcessGasStock < SemiconGameState.OxidationGasCost:
                    materialName = "특수가스"; owned = state.ProcessGasStock; needed = SemiconGameState.OxidationGasCost; return true;
                case SemiconRecipeKind.PhotoPatternedWafer when state.ChemicalStock < SemiconGameState.PhotoChemicalCost:
                    materialName = "공정 약품"; owned = state.ChemicalStock; needed = SemiconGameState.PhotoChemicalCost; return true;
                case SemiconRecipeKind.EtchedWafer when state.ProcessGasStock < SemiconGameState.EtchGasCost:
                    materialName = "특수가스"; owned = state.ProcessGasStock; needed = SemiconGameState.EtchGasCost; return true;
                case SemiconRecipeKind.DepositedWafer when state.ProcessGasStock < SemiconGameState.DepositionGasCost:
                    materialName = "특수가스"; owned = state.ProcessGasStock; needed = SemiconGameState.DepositionGasCost; return true;
                case SemiconRecipeKind.MetalizedWafer when state.MetalTargetStock < SemiconGameState.MetalTargetCost:
                    materialName = "배선 금속 타깃"; owned = state.MetalTargetStock; needed = SemiconGameState.MetalTargetCost; return true;
                case SemiconRecipeKind.Sc01ControlSensor when state.ChemicalStock < SemiconGameState.PackageChemicalCost:
                    materialName = "공정 약품"; owned = state.ChemicalStock; needed = SemiconGameState.PackageChemicalCost; return true;
                default:
                    return false;
            }
        }

        private Transform GetMarketTarget()
        {
            return marketTerminal != null && marketTerminal.gameObject.activeInHierarchy
                ? marketTerminal.transform
                : marketEntrance != null ? marketEntrance.transform : null;
        }

        private Transform GetFactoryTarget()
        {
            return productionMachine != null && productionMachine.gameObject.activeInHierarchy
                ? productionMachine.transform
                : factoryEntrance != null ? factoryEntrance.transform : null;
        }

        private Transform GetExperimentTarget(int process)
        {
            var terminal = process switch
            {
                2 => oxidationTerminal != null ? oxidationTerminal.transform : null,
                3 => photoTerminal != null ? photoTerminal.transform : null,
                4 => etchTerminal != null ? etchTerminal.transform : null,
                5 => depositionTerminal != null ? depositionTerminal.transform : null,
                6 => metalTerminal != null ? metalTerminal.transform : null,
                7 => edsTerminal != null ? edsTerminal.transform : null,
                8 => packageTerminal != null ? packageTerminal.transform : null,
                _ => null
            };
            if (terminal != null && terminal.gameObject.activeInHierarchy) return terminal;
            var doorName = $"Process {process:00} Building Entrance";
            var portals = FindObjectsByType<SemiconScenePortal>(FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            return portals.FirstOrDefault(portal => portal.name == doorName)?.transform;
        }

        private void SetObjective(string key, string index, string title, string detail, Transform target)
        {
            CurrentObjectiveKey = key;
            SetStep(GuideStep.Active, target);
            var distance = currentTarget != null && player != null
                ? Vector3.Distance(player.transform.position, currentTarget.position)
                : -1f;
            hud.SetObjective(index, title, detail, distance);
        }

        private void SetStep(GuideStep step, Transform target)
        {
            currentTarget = target;
            if (objectiveBeacon == null) return;
            var show = step != GuideStep.Complete && target != null && target.gameObject.activeInHierarchy;
            objectiveBeacon.gameObject.SetActive(show);
            if (!show) return;
            beaconBasePosition = target.position + Vector3.up * 2.8f;
            objectiveBeacon.position = beaconBasePosition;
        }

        private static SemiconRecipeKind GetRecipe(int process)
        {
            return process switch
            {
                2 => SemiconRecipeKind.OxidizedWafer,
                3 => SemiconRecipeKind.PhotoPatternedWafer,
                4 => SemiconRecipeKind.EtchedWafer,
                5 => SemiconRecipeKind.DepositedWafer,
                6 => SemiconRecipeKind.MetalizedWafer,
                7 => SemiconRecipeKind.TestedWafer,
                8 => SemiconRecipeKind.Sc01ControlSensor,
                _ => SemiconRecipeKind.WaferSubstrate
            };
        }

        private static bool IsRecipeQualified(SemiconGameState state, int process)
        {
            return process switch
            {
                2 => state.OxidationRecipeQualified,
                3 => state.PhotoRecipeQualified,
                4 => state.EtchRecipeQualified,
                5 => state.DepositionRecipeQualified,
                6 => state.MetalRecipeQualified,
                7 => state.EdsRecipeQualified,
                8 => state.PackageRecipeQualified,
                _ => true
            };
        }

        private static string GetProcessName(int process)
        {
            return process switch
            {
                1 => "웨이퍼",
                2 => "산화",
                3 => "포토",
                4 => "식각",
                5 => "증착",
                6 => "금속 배선",
                7 => "EDS 검사",
                8 => "패키징",
                _ => "반도체"
            };
        }

        private static string GetRecipeCode(SemiconRecipeKind recipe)
        {
            return recipe switch
            {
                SemiconRecipeKind.OxidizedWafer => "OXIDE-01",
                SemiconRecipeKind.PhotoPatternedWafer => "PHOTO-01",
                SemiconRecipeKind.EtchedWafer => "ETCH-01",
                SemiconRecipeKind.DepositedWafer => "DEPO-01",
                SemiconRecipeKind.MetalizedWafer => "METAL-01",
                SemiconRecipeKind.TestedWafer => "EDS-01",
                SemiconRecipeKind.Sc01ControlSensor => "SC-01",
                _ => "WAFER-01"
            };
        }
    }
}
