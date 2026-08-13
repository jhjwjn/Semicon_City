using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    public sealed class SemiconProductionPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private RectTransform panelFrame;
        [SerializeField] private Text recipeHeaderText;
        [SerializeField] private Text recipeStatusText;
        [SerializeField] private Text recipeProductText;
        [SerializeField] private Text recipeDescriptionText;
        [SerializeField] private Text recipeCostText;
        [SerializeField] private Text siliconLabelText;
        [SerializeField] private Text gasLabelText;
        [SerializeField] private Text chemicalLabelText;
        [SerializeField] private Text siliconStockText;
        [SerializeField] private Text gasStockText;
        [SerializeField] private Text chemicalStockText;
        [SerializeField] private Text outputProductText;
        [SerializeField] private Text outputLabelText;
        [SerializeField] private Text finishedStockText;
        [SerializeField] private Text productionStatusText;
        [SerializeField] private Text slotText;
        [SerializeField] private Text loadoutText;
        [SerializeField] private Text performanceText;
        [SerializeField] private Text queueStatusText;
        [SerializeField] private RectTransform progressFill;
        [SerializeField] private Button waferRecipeButton;
        [SerializeField] private Button oxidationRecipeButton;
        [SerializeField] private Button photoRecipeButton;
        [SerializeField] private Button etchRecipeButton;
        [SerializeField] private Button depositionRecipeButton;
        [SerializeField] private Button metalRecipeButton;
        [SerializeField] private Button edsRecipeButton;
        [SerializeField] private Button sc01RecipeButton;
        [SerializeField] private Button pm10RecipeButton;
        [SerializeField] private Button dd20RecipeButton;
        [SerializeField] private Button produceButton;
        [SerializeField] private Button collectButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private SemiconHud hud;

        private SemiconPlayerController activePlayer;
        private SemiconThirdPersonCamera activeCamera;
        private Coroutine animationRoutine;
        private bool isOpen;
        private int activeSlotIndex;
        private SemiconRecipeKind selectedRecipe = SemiconRecipeKind.WaferSubstrate;
        private bool wasJobComplete;

        private void Awake()
        {
            waferRecipeButton?.onClick.AddListener(() => SelectRecipe(SemiconRecipeKind.WaferSubstrate));
            oxidationRecipeButton?.onClick.AddListener(() => SelectRecipe(SemiconRecipeKind.OxidizedWafer));
            photoRecipeButton?.onClick.AddListener(() => SelectRecipe(SemiconRecipeKind.PhotoPatternedWafer));
            etchRecipeButton?.onClick.AddListener(() => SelectRecipe(SemiconRecipeKind.EtchedWafer));
            depositionRecipeButton?.onClick.AddListener(() => SelectRecipe(SemiconRecipeKind.DepositedWafer));
            metalRecipeButton?.onClick.AddListener(() => SelectRecipe(SemiconRecipeKind.MetalizedWafer));
            edsRecipeButton?.onClick.AddListener(() => SelectRecipe(SemiconRecipeKind.TestedWafer));
            sc01RecipeButton?.onClick.AddListener(() => SelectRecipe(SemiconRecipeKind.Sc01ControlSensor));
            pm10RecipeButton?.onClick.AddListener(() => SelectRecipe(SemiconRecipeKind.Pm10PowerManagement));
            dd20RecipeButton?.onClick.AddListener(() => SelectRecipe(SemiconRecipeKind.Dd20DisplayDriver));
            produceButton?.onClick.AddListener(StartProduction);
            collectButton?.onClick.AddListener(CollectProduction);
            closeButton?.onClick.AddListener(Close);
            SetVisible(false);
            gameObject.SetActive(false);
        }

        private void Start()
        {
            if (SemiconGameState.Instance != null)
            {
                SemiconGameState.Instance.StateChanged += Refresh;
            }
            Refresh();
        }

        private void OnDestroy()
        {
            if (SemiconGameState.Instance != null)
            {
                SemiconGameState.Instance.StateChanged -= Refresh;
            }
        }

        private void Update()
        {
            if (!isOpen) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }
            RefreshLiveState();
        }

        public void Configure(CanvasGroup group, RectTransform frame, Text recipeHeader, Text recipeStatus,
            Text recipeProduct, Text recipeDescription, Text recipeCost, Text siliconStock, Text gasStock,
            Text chemicalStock, Text outputProduct, Text outputLabel, Text finishedStock, Text productionStatus,
            Button waferRecipe, Button oxidationRecipe, Button photoRecipe, Button etchRecipe,
            Button depositionRecipe, Button metalRecipe, Button edsRecipe, Button sc01Recipe, Button produce,
            Button collect, Button close, Button pm10Recipe, Button dd20Recipe,
            SemiconHud targetHud,
            Text selectedSlot = null, Text loadout = null, Text performance = null, Text queueStatus = null,
            RectTransform targetProgressFill = null, Text siliconLabel = null, Text gasLabel = null,
            Text chemicalLabel = null)
        {
            panelGroup = group;
            panelFrame = frame;
            recipeHeaderText = recipeHeader;
            recipeStatusText = recipeStatus;
            recipeProductText = recipeProduct;
            recipeDescriptionText = recipeDescription;
            recipeCostText = recipeCost;
            siliconStockText = siliconStock;
            gasStockText = gasStock;
            chemicalStockText = chemicalStock;
            outputProductText = outputProduct;
            outputLabelText = outputLabel;
            finishedStockText = finishedStock;
            productionStatusText = productionStatus;
            waferRecipeButton = waferRecipe;
            oxidationRecipeButton = oxidationRecipe;
            photoRecipeButton = photoRecipe;
            etchRecipeButton = etchRecipe;
            depositionRecipeButton = depositionRecipe;
            metalRecipeButton = metalRecipe;
            edsRecipeButton = edsRecipe;
            sc01RecipeButton = sc01Recipe;
            pm10RecipeButton = pm10Recipe;
            dd20RecipeButton = dd20Recipe;
            produceButton = produce;
            collectButton = collect;
            closeButton = close;
            hud = targetHud;
            slotText = selectedSlot;
            loadoutText = loadout;
            performanceText = performance;
            queueStatusText = queueStatus;
            progressFill = targetProgressFill;
            siliconLabelText = siliconLabel;
            gasLabelText = gasLabel;
            chemicalLabelText = chemicalLabel;
        }

        public void Open(SemiconPlayerController player, SemiconThirdPersonCamera followCamera)
        {
            Open(player, followCamera, 0);
        }

        public void Open(SemiconPlayerController player, SemiconThirdPersonCamera followCamera, int slotIndex)
        {
            if (isOpen) return;
            gameObject.SetActive(true);
            activeSlotIndex = Mathf.Clamp(slotIndex, 0, SemiconFactoryDefinitions.SlotCount - 1);
            var job = SemiconGameState.Instance?.GetProductionJob(activeSlotIndex) ?? default;
            selectedRecipe = job.HasJob ? job.Recipe : SemiconRecipeKind.WaferSubstrate;
            activePlayer = player;
            activeCamera = followCamera;
            activePlayer?.SetInputEnabled(false);
            activeCamera?.SetLookEnabled(false);
            SemiconPlayerController.SetCursorLocked(false);
            isOpen = true;
            SetStatus(job.HasJob
                ? "ACTIVE PROCESS  /  저장된 생산 작업을 불러왔습니다."
                : "PROCESS CELL READY  /  레시피를 선택하고 생산을 시작하세요.",
                new Color32(134, 164, 168, 255));
            Refresh();

            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(AnimateOpen());
        }

        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(AnimateClose());
        }

        private void SelectRecipe(SemiconRecipeKind recipe)
        {
            var state = SemiconGameState.Instance;
            if (state == null || state.GetProductionJob(activeSlotIndex).HasJob) return;
            selectedRecipe = recipe;
            SetStatus("RECIPE SELECTED  /  " + SemiconFactoryDefinitions.GetRecipeName(recipe),
                new Color32(41, 211, 207, 255));
            Refresh();
        }

        private void StartProduction()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            if (!state.TryStartProduction(activeSlotIndex, selectedRecipe, 1, out var job, out var reason))
            {
                SetStatus("PROCESS WAITING  /  " + reason, new Color32(238, 103, 89, 255));
                hud?.ShowToast(reason);
                return;
            }

            SetStatus($"PROCESS STARTED  /  {SemiconFactoryDefinitions.GetRecipeName(job.Recipe)}  ·  " +
                      $"예상 {job.TotalSeconds:0.0}초", new Color32(247, 169, 30, 255));
            hud?.ShowToast($"SLOT {activeSlotIndex + 1:00} 생산을 시작했습니다.");
            Refresh();
        }

        private void CollectProduction()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            if (!state.TryCollectProduction(activeSlotIndex, out var recipe, out var output, out var quality,
                    out var reason))
            {
                SetStatus("COLLECT WAITING  /  " + reason, new Color32(238, 103, 89, 255));
                hud?.ShowToast(reason);
                return;
            }

            selectedRecipe = recipe;
            SetStatus($"COLLECT COMPLETE  /  {SemiconFactoryDefinitions.GetRecipeName(recipe)} +{output}  ·  품질 {quality}",
                new Color32(247, 169, 30, 255));
            hud?.ShowToast($"생산품 {output}개를 창고로 회수했습니다.");
            Refresh();
        }

        private void Refresh()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            var job = state.GetProductionJob(activeSlotIndex);
            if (job.HasJob) selectedRecipe = job.Recipe;
            var slot = state.GetFactorySlot(activeSlotIndex);
            var stats = state.GetProductionStats(activeSlotIndex);
            var isSc01 = selectedRecipe == SemiconRecipeKind.Sc01ControlSensor;
            var isPm10 = selectedRecipe == SemiconRecipeKind.Pm10PowerManagement;
            var isDd20 = selectedRecipe == SemiconRecipeKind.Dd20DisplayDriver;
            var isPackagedProduct = isSc01 || isPm10 || isDd20;
            var isOxidation = selectedRecipe == SemiconRecipeKind.OxidizedWafer;
            var isPhoto = selectedRecipe == SemiconRecipeKind.PhotoPatternedWafer;
            var isEtch = selectedRecipe == SemiconRecipeKind.EtchedWafer;
            var isDeposition = selectedRecipe == SemiconRecipeKind.DepositedWafer;
            var isMetal = selectedRecipe == SemiconRecipeKind.MetalizedWafer;
            var isEds = selectedRecipe == SemiconRecipeKind.TestedWafer;
            var recipeCode = GetRecipeCode(selectedRecipe);

            if (recipeHeaderText != null)
                recipeHeaderText.text = $"ACTIVE RECIPE  /  {recipeCode}";
            if (recipeStatusText != null)
            {
                var available = selectedRecipe switch
                {
                    SemiconRecipeKind.OxidizedWafer => state.OxidationRecipeQualified,
                    SemiconRecipeKind.PhotoPatternedWafer => state.PhotoRecipeQualified,
                    SemiconRecipeKind.EtchedWafer => state.EtchRecipeQualified,
                    SemiconRecipeKind.DepositedWafer => state.DepositionRecipeQualified,
                    SemiconRecipeKind.MetalizedWafer => state.MetalRecipeQualified,
                    SemiconRecipeKind.TestedWafer => state.EdsRecipeQualified,
                    SemiconRecipeKind.Sc01ControlSensor => state.PackageRecipeQualified,
                    SemiconRecipeKind.Pm10PowerManagement => state.PackageRecipeQualified &&
                                                             state.IsContractUnlocked(SemiconContractKind.Pm10PowerManagement),
                    SemiconRecipeKind.Dd20DisplayDriver => state.PackageRecipeQualified &&
                                                           state.IsContractUnlocked(SemiconContractKind.Dd20DisplayDriver),
                    _ => true
                };
                recipeStatusText.text = available
                    ? (selectedRecipe == SemiconRecipeKind.WaferSubstrate
                        ? "STARTER RECIPE  /  AVAILABLE"
                        : $"{recipeCode} RECIPE  /  QUALIFIED")
                    : $"{recipeCode} RECIPE  /  LOCKED";
                recipeStatusText.color = available
                    ? new Color32(41, 211, 207, 255)
                    : new Color32(238, 103, 89, 255);
            }
            if (recipeProductText != null) recipeProductText.text = SemiconFactoryDefinitions.GetRecipeName(selectedRecipe);
            if (recipeDescriptionText != null)
                recipeDescriptionText.text = SemiconFactoryDefinitions.GetRecipeDescription(selectedRecipe);
            if (recipeCostText != null) recipeCostText.text = SemiconFactoryDefinitions.GetRecipeCostText(selectedRecipe);

            if (siliconLabelText != null)
                siliconLabelText.text = isPackagedProduct ? "01  EDS 선별 웨이퍼" : isEds ? "01  배선 웨이퍼" : isMetal ? "01  박막 웨이퍼" : isDeposition ? "01  식각 웨이퍼" : isEtch ? "01  패턴 웨이퍼" : isPhoto ? "01  산화 웨이퍼" :
                    isOxidation ? "01  기초 웨이퍼" : "01  고순도 실리콘";
            if (gasLabelText != null)
                gasLabelText.text = isPackagedProduct ? "02  몰딩 컴파운드" : isEds ? "02  검사 프로브" : isMetal ? "02  배선 금속 타깃" : isDeposition ? "02  증착 가스" : isEtch ? "02  식각 가스" : isPhoto ? "02  포토레지스트" :
                    isOxidation ? "02  산화 가스" : "02  특수가스";
            if (chemicalLabelText != null)
                chemicalLabelText.text = isPackagedProduct ? "03  패키징 툴" : isPhoto || isEtch || isDeposition || isMetal || isEds ? "03  보조 재료" : "03  공정 약품";
            if (siliconStockText != null)
                siliconStockText.text = isPackagedProduct
                    ? $"{state.TestedWaferStock:N0}  /  {SemiconGameState.PackageTestedWaferCost}"
                    : isEds
                    ? $"{state.MetalizedWaferStock:N0}  /  {SemiconGameState.EdsMetalizedWaferCost}"
                    : isMetal
                    ? $"{state.DepositedWaferStock:N0}  /  {SemiconGameState.MetalDepositedWaferCost}"
                    : isDeposition
                    ? $"{state.EtchedWaferStock:N0}  /  {SemiconGameState.DepositionEtchedWaferCost}"
                    : isEtch
                        ? $"{state.PatternedWaferStock:N0}  /  {SemiconGameState.EtchPatternedWaferCost}"
                        : isPhoto
                        ? $"{state.OxidizedWaferStock:N0}  /  {SemiconGameState.PhotoOxidizedWaferCost}"
                        : isOxidation
                        ? $"{state.WaferStock:N0}  /  {SemiconGameState.OxidationWaferCost}"
                        : $"{state.SiliconStock:N0}  /  {SemiconGameState.WaferSiliconCost}";
            if (gasStockText != null)
                gasStockText.text = isPackagedProduct
                    ? $"{state.ChemicalStock:N0}  /  {SemiconGameState.PackageChemicalCost}"
                    : isEds
                    ? "READY  /  REUSE"
                    : isMetal
                    ? $"{state.MetalTargetStock:N0}  /  {SemiconGameState.MetalTargetCost}"
                    : isDeposition
                    ? $"{state.ProcessGasStock:N0}  /  {SemiconGameState.DepositionGasCost}"
                    : isEtch
                        ? $"{state.ProcessGasStock:N0}  /  {SemiconGameState.EtchGasCost}"
                        : isPhoto
                        ? $"{state.ChemicalStock:N0}  /  {SemiconGameState.PhotoChemicalCost}"
                        : isOxidation
                        ? $"{state.ProcessGasStock:N0}  /  {SemiconGameState.OxidationGasCost}"
                        : $"{state.ProcessGasStock:N0}  /  --";
            if (chemicalStockText != null)
                chemicalStockText.text = isPackagedProduct
                    ? "READY  /  REUSE"
                    : isPhoto || isEtch || isDeposition || isMetal || isEds
                    ? "--  /  --"
                    : $"{state.ChemicalStock:N0}  /  --";

            if (outputProductText != null)
                outputProductText.text = selectedRecipe switch
                {
                    SemiconRecipeKind.OxidizedWafer => "OXIDE-01 산화 웨이퍼",
                    SemiconRecipeKind.PhotoPatternedWafer => "PHOTO-01 패턴 웨이퍼",
                    SemiconRecipeKind.EtchedWafer => "ETCH-01 식각 웨이퍼",
                    SemiconRecipeKind.DepositedWafer => "DEPO-01 박막 웨이퍼",
                    SemiconRecipeKind.MetalizedWafer => "METAL-01 배선 웨이퍼",
                    SemiconRecipeKind.TestedWafer => "EDS-01 선별 웨이퍼",
                    SemiconRecipeKind.Sc01ControlSensor => "SC-01 제어 센서 패키지",
                    SemiconRecipeKind.Pm10PowerManagement => "PM-10 전력 관리 IC",
                    SemiconRecipeKind.Dd20DisplayDriver => "DD-20 디스플레이 드라이버",
                    _ => "WAFER-01 기초 웨이퍼"
                };
            if (outputLabelText != null) outputLabelText.text = isPackagedProduct ? "완제품 창고" : "중간 공정품 창고";
            if (finishedStockText != null)
                finishedStockText.text = $"{state.GetRecipeOutputStock(selectedRecipe):N0} UNIT";
            if (slotText != null) slotText.text = $"ACTIVE CELL  /  SLOT {activeSlotIndex + 1:00}";
            if (loadoutText != null && slot != null)
            {
                loadoutText.text = $"{SemiconFactoryDefinitions.GetWorkerName(slot.worker)}\n" +
                                   $"{SemiconFactoryDefinitions.GetDiskName(slot.disk)}";
            }

            var cycleSeconds = SemiconFactoryDefinitions.GetBaseCycleSeconds(selectedRecipe) * 100f /
                               Mathf.Max(1, stats.Speed);
            var performanceQuality = (isOxidation || isPhoto || isEtch || isDeposition || isMetal || isEds || isPackagedProduct)
                ? job.HasJob
                    ? job.Quality
                    : isOxidation
                        ? Mathf.Clamp(Mathf.RoundToInt(state.AverageWaferQuality * 0.35f + stats.Quality * 0.30f +
                                                       state.BestOxideUniformity * 0.35f), 1, 100)
                        : isPhoto
                            ? Mathf.Clamp(Mathf.RoundToInt(state.AverageOxidizedWaferQuality * 0.40f +
                                                           stats.Quality * 0.30f +
                                                           (state.BestYield + state.BestPrecision) * 0.15f), 1, 100)
                            : isEtch
                                ? Mathf.Clamp(Mathf.RoundToInt(state.AveragePatternedWaferQuality * 0.40f +
                                                               stats.Quality * 0.30f + state.BestEtchProfile * 0.30f),
                                    1, 100)
                                : isDeposition
                                    ? Mathf.Clamp(Mathf.RoundToInt(state.AverageEtchedWaferQuality * 0.40f +
                                                                   stats.Quality * 0.30f +
                                                                   state.BestDepositionUniformity * 0.18f +
                                                                   state.BestDepositionCoverage * 0.12f), 1, 100)
                                    : isMetal
                                        ? Mathf.Clamp(Mathf.RoundToInt(state.AverageDepositedWaferQuality * 0.40f +
                                                                       stats.Quality * 0.30f +
                                                                       state.BestMetalAdhesion * 0.20f +
                                                                       Mathf.Clamp(100f - Mathf.Abs(
                                                                           state.BestMetalResistance - 0.1f) * 250f,
                                                                           60f, 100f) * 0.10f), 1, 100)
                                        : isEds
                                            ? Mathf.Clamp(Mathf.RoundToInt(state.AverageMetalizedWaferQuality * 0.40f +
                                                                           stats.Quality * 0.30f +
                                                                           state.BestEdsDetection * 0.20f +
                                                                           state.BestEdsYield * 0.10f), 1, 100)
                                            : Mathf.Clamp(Mathf.RoundToInt(state.AverageTestedWaferQuality * 0.40f +
                                                                           stats.Quality * 0.25f +
                                                                           state.BestPackageBondStrength * 0.15f +
                                                                           state.BestPackageIntegrity * 0.10f +
                                                                           state.BestPackageFinalPass * 0.10f), 1, 100)
                : stats.Quality;
            if (performanceText != null)
            {
                performanceText.text = $"CYCLE TIME    {cycleSeconds:0.0}s\nOUTPUT        {stats.OutputPerCycle} UNIT\nQUALITY       {performanceQuality}";
            }
            if (waferRecipeButton != null) waferRecipeButton.interactable = !job.HasJob;
            if (oxidationRecipeButton != null) oxidationRecipeButton.interactable = !job.HasJob;
            if (photoRecipeButton != null) photoRecipeButton.interactable = !job.HasJob;
            if (etchRecipeButton != null) etchRecipeButton.interactable = !job.HasJob;
            if (depositionRecipeButton != null) depositionRecipeButton.interactable = !job.HasJob;
            if (metalRecipeButton != null) metalRecipeButton.interactable = !job.HasJob;
            if (edsRecipeButton != null) edsRecipeButton.interactable = !job.HasJob;
            if (sc01RecipeButton != null) sc01RecipeButton.interactable = !job.HasJob;
            if (pm10RecipeButton != null) pm10RecipeButton.interactable = !job.HasJob &&
                state.IsContractUnlocked(SemiconContractKind.Pm10PowerManagement);
            if (dd20RecipeButton != null) dd20RecipeButton.interactable = !job.HasJob &&
                state.IsContractUnlocked(SemiconContractKind.Dd20DisplayDriver);
            SetButtonLabel(waferRecipeButton, "WAFER" + (selectedRecipe == SemiconRecipeKind.WaferSubstrate ? "  ◀" : string.Empty));
            SetButtonLabel(oxidationRecipeButton, "OXIDE" + (selectedRecipe == SemiconRecipeKind.OxidizedWafer ? "  ◀" : string.Empty));
            SetButtonLabel(photoRecipeButton, "PHOTO" + (selectedRecipe == SemiconRecipeKind.PhotoPatternedWafer ? "  ◀" : string.Empty));
            SetButtonLabel(etchRecipeButton, "ETCH" + (selectedRecipe == SemiconRecipeKind.EtchedWafer ? "  ◀" : string.Empty));
            SetButtonLabel(depositionRecipeButton, "DEPO" + (selectedRecipe == SemiconRecipeKind.DepositedWafer ? "  ◀" : string.Empty));
            SetButtonLabel(metalRecipeButton, "METAL" + (selectedRecipe == SemiconRecipeKind.MetalizedWafer ? "  ◀" : string.Empty));
            SetButtonLabel(edsRecipeButton, "EDS" + (selectedRecipe == SemiconRecipeKind.TestedWafer ? "  ◀" : string.Empty));
            SetButtonLabel(sc01RecipeButton, "PACKAGE" + (selectedRecipe == SemiconRecipeKind.Sc01ControlSensor ? "  ◀" : string.Empty));
            SetButtonLabel(pm10RecipeButton, "PM-10" + (selectedRecipe == SemiconRecipeKind.Pm10PowerManagement ? "  ◀" : string.Empty));
            SetButtonLabel(dd20RecipeButton, "DD-20" + (selectedRecipe == SemiconRecipeKind.Dd20DisplayDriver ? "  ◀" : string.Empty));
            wasJobComplete = job.IsComplete;
            RefreshLiveState();
            RefreshTextMeshes();
        }

        private void RefreshLiveState()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            var job = state.GetProductionJob(activeSlotIndex);
            var completionChanged = job.IsComplete != wasJobComplete;
            if (progressFill != null)
            {
                var max = progressFill.anchorMax;
                max.x = job.HasJob ? Mathf.Clamp01(job.Progress) : 0f;
                progressFill.anchorMax = max;
            }

            if (queueStatusText != null)
            {
                if (!job.HasJob)
                {
                    queueStatusText.text = "QUEUE EMPTY  /  START READY";
                    queueStatusText.color = new Color32(134, 164, 168, 255);
                }
                else if (job.IsComplete)
                {
                    queueStatusText.text = $"PROCESS COMPLETE  /  {job.OutputAmount} UNIT 회수 대기";
                    queueStatusText.color = new Color32(247, 169, 30, 255);
                }
                else
                {
                    queueStatusText.text = $"PROCESSING  /  {job.RemainingSeconds:0.0}s  ·  {job.Progress * 100f:0}%";
                    queueStatusText.color = new Color32(41, 211, 207, 255);
                }
            }

            if (produceButton != null)
            {
                produceButton.gameObject.SetActive(!job.HasJob);
                produceButton.interactable = !job.HasJob && CanStartSelected(state);
                SetButtonLabel(produceButton, "1 사이클 시작    ▶    " + GetRecipeCode(selectedRecipe));
            }
            if (collectButton != null)
            {
                collectButton.gameObject.SetActive(job.HasJob);
                collectButton.interactable = job.IsComplete;
                SetButtonLabel(collectButton, job.IsComplete ? "생산 완료품 회수    ▶" : $"생산 중    {job.RemainingSeconds:0.0}s");
            }
            if (job.HasJob && job.IsComplete && productionStatusText != null &&
                !productionStatusText.text.StartsWith("COLLECT COMPLETE"))
            {
                SetStatus("PROCESS COMPLETE  /  기계와 상호작용하여 생산품을 회수하세요.",
                    new Color32(247, 169, 30, 255));
            }
            if (completionChanged)
            {
                wasJobComplete = job.IsComplete;
                RefreshTextMeshes();
            }
        }

        private bool CanStartSelected(SemiconGameState state)
        {
            if (selectedRecipe == SemiconRecipeKind.WaferSubstrate)
                return state.SiliconStock >= SemiconGameState.WaferSiliconCost;
            if (selectedRecipe == SemiconRecipeKind.OxidizedWafer)
                return state.OxidationRecipeQualified && state.WaferStock >= SemiconGameState.OxidationWaferCost &&
                       state.ProcessGasStock >= SemiconGameState.OxidationGasCost;
            if (selectedRecipe == SemiconRecipeKind.PhotoPatternedWafer)
                return state.PhotoRecipeQualified &&
                       state.OxidizedWaferStock >= SemiconGameState.PhotoOxidizedWaferCost &&
                       state.ChemicalStock >= SemiconGameState.PhotoChemicalCost;
            if (selectedRecipe == SemiconRecipeKind.EtchedWafer)
                return state.EtchRecipeQualified &&
                       state.PatternedWaferStock >= SemiconGameState.EtchPatternedWaferCost &&
                       state.ProcessGasStock >= SemiconGameState.EtchGasCost;
            if (selectedRecipe == SemiconRecipeKind.DepositedWafer)
                return state.DepositionRecipeQualified &&
                       state.EtchedWaferStock >= SemiconGameState.DepositionEtchedWaferCost &&
                       state.ProcessGasStock >= SemiconGameState.DepositionGasCost;
            if (selectedRecipe == SemiconRecipeKind.MetalizedWafer)
                return state.MetalRecipeQualified &&
                       state.DepositedWaferStock >= SemiconGameState.MetalDepositedWaferCost &&
                       state.MetalTargetStock >= SemiconGameState.MetalTargetCost;
            if (selectedRecipe == SemiconRecipeKind.TestedWafer)
                return state.EdsRecipeQualified &&
                       state.MetalizedWaferStock >= SemiconGameState.EdsMetalizedWaferCost;
            if (selectedRecipe == SemiconRecipeKind.Pm10PowerManagement &&
                !state.IsContractUnlocked(SemiconContractKind.Pm10PowerManagement)) return false;
            if (selectedRecipe == SemiconRecipeKind.Dd20DisplayDriver &&
                !state.IsContractUnlocked(SemiconContractKind.Dd20DisplayDriver)) return false;
            return state.PackageRecipeQualified &&
                   state.TestedWaferStock >= SemiconGameState.PackageTestedWaferCost &&
                   state.ChemicalStock >= SemiconGameState.PackageChemicalCost;
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
                SemiconRecipeKind.Sc01ControlSensor => "PACKAGE-01",
                SemiconRecipeKind.Pm10PowerManagement => "PM-10",
                SemiconRecipeKind.Dd20DisplayDriver => "DD-20",
                _ => "WAFER-01"
            };
        }

        private void SetStatus(string message, Color color)
        {
            if (productionStatusText == null) return;
            productionStatusText.text = message;
            productionStatusText.color = color;
        }

        private static void SetButtonLabel(Button button, string value)
        {
            var label = button != null ? button.GetComponentInChildren<Text>(true) : null;
            if (label != null) label.text = value;
        }

        private void RefreshTextMeshes()
        {
            var labels = GetComponentsInChildren<Text>(true);
            foreach (var label in labels)
            {
                if (label == null || label.font == null || string.IsNullOrEmpty(label.text)) continue;
                label.font.RequestCharactersInTexture(label.text, label.fontSize, label.fontStyle);
            }
            foreach (var label in labels)
            {
                if (label == null) continue;
                label.SetLayoutDirty();
                label.SetVerticesDirty();
            }
            Canvas.ForceUpdateCanvases();
        }

        private IEnumerator AnimateOpen()
        {
            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.interactable = true;
                panelGroup.blocksRaycasts = true;
            }
            var start = new Vector2(70f, 0f);
            if (panelFrame != null) panelFrame.anchoredPosition = start;
            var elapsed = 0f;
            const float duration = 0.2f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f);
                if (panelGroup != null) panelGroup.alpha = t;
                if (panelFrame != null) panelFrame.anchoredPosition = Vector2.Lerp(start, Vector2.zero, t);
                yield return null;
            }
        }

        private IEnumerator AnimateClose()
        {
            var start = panelFrame != null ? panelFrame.anchoredPosition : Vector2.zero;
            var elapsed = 0f;
            const float duration = 0.16f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                if (panelGroup != null) panelGroup.alpha = 1f - t;
                if (panelFrame != null) panelFrame.anchoredPosition = Vector2.Lerp(start, new Vector2(70f, 0f), t);
                yield return null;
            }
            SetVisible(false);
            activePlayer?.SetInputEnabled(true);
            activeCamera?.SetLookEnabled(true);
            SemiconPlayerController.SetCursorLocked(true);
            activePlayer = null;
            activeCamera = null;
            gameObject.SetActive(false);
        }

        private void SetVisible(bool visible)
        {
            if (panelGroup == null) return;
            panelGroup.alpha = visible ? 1f : 0f;
            panelGroup.interactable = visible;
            panelGroup.blocksRaycasts = visible;
        }
    }
}
