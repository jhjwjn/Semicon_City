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
        [SerializeField] private Text recipeVariantText;
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
        [SerializeField] private Text cycleCountText;
        [SerializeField] private Text cycleSummaryText;
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
        [SerializeField] private Button previousVariantButton;
        [SerializeField] private Button nextVariantButton;
        [SerializeField] private Button cycleDecreaseButton;
        [SerializeField] private Button cycleIncreaseButton;
        [SerializeField] private Button cycleOneButton;
        [SerializeField] private Button cycleFiveButton;
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
        private int selectedVariantIndex = -1;
        private int selectedBatches = 1;
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
            previousVariantButton?.onClick.AddListener(() => CycleVariant(-1));
            nextVariantButton?.onClick.AddListener(() => CycleVariant(1));
            cycleDecreaseButton?.onClick.AddListener(() => SetBatchCount(selectedBatches - 1));
            cycleIncreaseButton?.onClick.AddListener(() => SetBatchCount(selectedBatches + 1));
            cycleOneButton?.onClick.AddListener(() => SetBatchCount(1));
            cycleFiveButton?.onClick.AddListener(() => SetBatchCount(5));
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
            Text chemicalLabel = null, Text recipeVariant = null, Button previousVariant = null,
            Button nextVariant = null, Text cycleCount = null, Text cycleSummary = null,
            Button cycleDecrease = null, Button cycleIncrease = null, Button cycleOne = null,
            Button cycleFive = null)
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
            recipeVariantText = recipeVariant;
            previousVariantButton = previousVariant;
            nextVariantButton = nextVariant;
            cycleCountText = cycleCount;
            cycleSummaryText = cycleSummary;
            cycleDecreaseButton = cycleDecrease;
            cycleIncreaseButton = cycleIncrease;
            cycleOneButton = cycleOne;
            cycleFiveButton = cycleFive;
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
            selectedBatches = job.HasJob ? Mathf.Clamp(job.Batches, 1, 10) : 1;
            selectedVariantIndex = SemiconGameState.Instance?.GetRecommendedRecipeVariantIndex(selectedRecipe) ?? -1;
            activePlayer = player;
            activeCamera = followCamera;
            activePlayer?.SetInputEnabled(false);
            activeCamera?.SetLookEnabled(false);
            SemiconPlayerController.SetCursorLocked(false);
            isOpen = true;
            SetStatus(job.HasJob
                ? "ACTIVE PROCESS  /  저장된 생산 작업을 불러왔습니다."
                : "1 레시피 선택  →  2 횟수 지정  →  3 재료 확인  →  4 생산 시작·회수",
                SemiconUiPalette.Muted);
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
            selectedVariantIndex = state.GetRecommendedRecipeVariantIndex(recipe);
            SetStatus("RECIPE SELECTED  /  " + SemiconFactoryDefinitions.GetRecipeName(recipe),
                SemiconUiPalette.Mint);
            Refresh();
        }

        private void CycleVariant(int direction)
        {
            var state = SemiconGameState.Instance;
            if (state == null || state.GetProductionJob(activeSlotIndex).HasJob) return;
            var count = state.GetRecipeVariantCount(selectedRecipe);
            if (count <= 1) return;
            selectedVariantIndex = (selectedVariantIndex + direction + count) % count;
            var variant = state.GetRecipeVariant(selectedRecipe, selectedVariantIndex);
            if (variant != null)
                SetStatus($"RECIPE SELECTED  /  {variant.DisplayCode}  ·  품질 {variant.qualityScore}",
                    SemiconUiPalette.Mint);
            Refresh();
        }

        private void SetBatchCount(int value)
        {
            var state = SemiconGameState.Instance;
            if (state != null && state.GetProductionJob(activeSlotIndex).HasJob) return;
            selectedBatches = Mathf.Clamp(value, 1, 10);
            SetStatus($"생산 수량 선택  /  {selectedBatches}회 사이클", SemiconUiPalette.Mint);
            Refresh();
        }

        private void StartProduction()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            if (!state.TryStartProduction(activeSlotIndex, selectedRecipe, selectedBatches, selectedVariantIndex,
                    out var job, out var reason))
            {
                SetStatus("PROCESS WAITING  /  " + reason, new Color32(238, 103, 89, 255));
                hud?.ShowToast(reason);
                return;
            }

            SetStatus($"PROCESS STARTED  /  {SemiconFactoryDefinitions.GetRecipeName(job.Recipe)}  ·  " +
                      $"예상 {job.TotalSeconds:0.0}초", SemiconUiPalette.Amber);
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
                SemiconUiPalette.Amber);
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
            var variantCount = state.GetRecipeVariantCount(selectedRecipe);
            if (!job.HasJob)
            {
                if (variantCount == 0) selectedVariantIndex = -1;
                else if (selectedVariantIndex < 0 || selectedVariantIndex >= variantCount)
                    selectedVariantIndex = state.GetRecommendedRecipeVariantIndex(selectedRecipe);
            }
            var selectedVariant = state.GetRecipeVariant(selectedRecipe, selectedVariantIndex);

            if (recipeHeaderText != null)
                recipeHeaderText.text = $"1 레시피 선택  /  {(selectedVariant != null ? selectedVariant.DisplayCode : recipeCode)}";
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
                    ? SemiconUiPalette.Mint
                    : new Color32(238, 103, 89, 255);
            }
            if (recipeVariantText != null)
            {
                recipeVariantText.text = selectedRecipe == SemiconRecipeKind.WaferSubstrate
                    ? "기초 레시피  ·  자동 적용"
                    : selectedVariant != null
                        ? $"{selectedVariantIndex + 1} / {variantCount}   {selectedVariant.Grade}등급 · {selectedVariant.StyleName} · 품질 {selectedVariant.qualityScore}"
                        : "등록된 레시피 없음  ·  실험실에서 합격 조건을 찾으세요";
                recipeVariantText.color = selectedVariant != null || selectedRecipe == SemiconRecipeKind.WaferSubstrate
                    ? SemiconUiPalette.Mint
                    : new Color32(238, 103, 89, 255);
            }
            if (previousVariantButton != null)
                previousVariantButton.interactable = !job.HasJob && variantCount > 1;
            if (nextVariantButton != null)
                nextVariantButton.interactable = !job.HasJob && variantCount > 1;
            if (recipeProductText != null) recipeProductText.text = SemiconFactoryDefinitions.GetRecipeName(selectedRecipe);
            if (recipeDescriptionText != null)
                recipeDescriptionText.text = selectedVariant != null
                    ? $"선택 조건  /  {selectedVariant.ParameterSummary}\n예상 결과  /  {selectedVariant.ResultSummary}"
                    : SemiconFactoryDefinitions.GetRecipeDescription(selectedRecipe);

            GetMaterialDisplayNames(selectedRecipe, out var primaryMaterialName, out var secondaryMaterialName);
            if (siliconLabelText != null) siliconLabelText.text = "주재료  /  " + primaryMaterialName;
            if (gasLabelText != null)
                gasLabelText.text = string.IsNullOrEmpty(secondaryMaterialName)
                    ? "보조재료  /  필요 없음"
                    : "보조재료  /  " + secondaryMaterialName;
            if (chemicalLabelText != null) chemicalLabelText.text = "생산 준비 상태";

            GetMaterialRequirements(state, selectedRecipe, selectedBatches, out var primaryStock,
                out var primaryRequired, out var secondaryStock, out var secondaryRequired);
            var primaryReady = primaryStock >= primaryRequired;
            var secondaryReady = secondaryRequired <= 0 || secondaryStock >= secondaryRequired;
            if (siliconStockText != null)
            {
                siliconStockText.text = $"보유 {primaryStock:N0}개  ·  필요 {primaryRequired:N0}개";
                siliconStockText.color = primaryReady ? SemiconUiPalette.Mint : new Color32(238, 103, 89, 255);
            }
            if (gasStockText != null)
            {
                gasStockText.text = secondaryRequired > 0
                    ? $"보유 {secondaryStock:N0}개  ·  필요 {secondaryRequired:N0}개"
                    : "추가 투입 없음";
                gasStockText.color = secondaryReady ? SemiconUiPalette.Mint : new Color32(238, 103, 89, 255);
            }
            if (chemicalStockText != null)
            {
                var missingTypes = (primaryReady ? 0 : 1) + (secondaryReady ? 0 : 1);
                chemicalStockText.text = missingTypes == 0 ? "준비 완료  ·  생산 가능" : $"재료 {missingTypes}종 부족";
                chemicalStockText.color = primaryReady && secondaryReady
                    ? SemiconUiPalette.Mint : new Color32(238, 103, 89, 255);
            }
            if (recipeCostText != null)
                recipeCostText.text = $"생산 {selectedBatches}회 총 투입량\n\n" +
                                      $"{primaryMaterialName}  {primaryRequired:N0}개" +
                                      (secondaryRequired > 0 ? $"\n{secondaryMaterialName}  {secondaryRequired:N0}개" : string.Empty);

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
                slot.EnsureCrewSlots();
                var crewCount = 0;
                for (var crew = 0; crew < SemiconFactoryDefinitions.RobotsPerSlot; crew++)
                    if (slot.robots[crew] != SemiconRobotKind.None) crewCount++;
                loadoutText.text = $"운용 로봇  {crewCount} / {SemiconFactoryDefinitions.RobotsPerSlot}  ·  " +
                                   $"생산 {stats.Production}%  /  속도 {stats.Speed}%  /  품질 {stats.Quality}";
            }

            var cycleSeconds = SemiconFactoryDefinitions.GetBaseCycleSeconds(selectedRecipe) * 100f /
                               Mathf.Max(1, stats.Speed);
            var previewBatches = job.HasJob ? job.Batches : selectedBatches;
            if (job.HasJob) selectedBatches = Mathf.Clamp(job.Batches, 1, 10);
            var performanceQuality = job.HasJob
                ? job.Quality
                : state.PreviewProductionQuality(activeSlotIndex, selectedRecipe, selectedVariantIndex);
            if (cycleCountText != null)
            {
                cycleCountText.text = $"{previewBatches} 회";
                cycleCountText.color = SemiconUiPalette.Ink;
            }
            if (cycleSummaryText != null)
                cycleSummaryText.text = $"총 {cycleSeconds * previewBatches:0.0}초  ·  예상 {stats.OutputPerCycle * previewBatches}개";
            if (performanceText != null)
            {
                performanceText.text = $"총 작업 시간     {cycleSeconds * previewBatches:0.0}초\n" +
                                       $"예상 생산량      {stats.OutputPerCycle * previewBatches}개\n" +
                                       $"예상 품질        {performanceQuality}";
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
            if (cycleDecreaseButton != null) cycleDecreaseButton.interactable = !job.HasJob && selectedBatches > 1;
            if (cycleIncreaseButton != null) cycleIncreaseButton.interactable = !job.HasJob && selectedBatches < 10;
            if (cycleOneButton != null) cycleOneButton.interactable = !job.HasJob;
            if (cycleFiveButton != null) cycleFiveButton.interactable = !job.HasJob;
            SetButtonLabel(waferRecipeButton, "WAFER" + (selectedRecipe == SemiconRecipeKind.WaferSubstrate ? "  ◀" : string.Empty));
            SetButtonLabel(oxidationRecipeButton, "OXIDE" + (selectedRecipe == SemiconRecipeKind.OxidizedWafer ? "  ◀" : string.Empty));
            SetButtonLabel(photoRecipeButton, "PHOTO" + (selectedRecipe == SemiconRecipeKind.PhotoPatternedWafer ? "  ◀" : string.Empty));
            SetButtonLabel(etchRecipeButton, "ETCH" + (selectedRecipe == SemiconRecipeKind.EtchedWafer ? "  ◀" : string.Empty));
            SetButtonLabel(depositionRecipeButton, "DEPO" + (selectedRecipe == SemiconRecipeKind.DepositedWafer ? "  ◀" : string.Empty));
            SetButtonLabel(metalRecipeButton, "METAL" + (selectedRecipe == SemiconRecipeKind.MetalizedWafer ? "  ◀" : string.Empty));
            SetButtonLabel(edsRecipeButton, "EDS" + (selectedRecipe == SemiconRecipeKind.TestedWafer ? "  ◀" : string.Empty));
            SetButtonLabel(sc01RecipeButton, "PACKAGE" + (selectedRecipe == SemiconRecipeKind.Sc01ControlSensor ? "  ◀" : string.Empty));
            SetButtonLabel(pm10RecipeButton, "PM-10  전력 IC" + (selectedRecipe == SemiconRecipeKind.Pm10PowerManagement ? "  ◀" : string.Empty));
            SetButtonLabel(dd20RecipeButton, "DD-20  화면 IC" + (selectedRecipe == SemiconRecipeKind.Dd20DisplayDriver ? "  ◀" : string.Empty));
            SemiconUiPalette.SetButtonSelection(waferRecipeButton, selectedRecipe == SemiconRecipeKind.WaferSubstrate);
            SemiconUiPalette.SetButtonSelection(oxidationRecipeButton, selectedRecipe == SemiconRecipeKind.OxidizedWafer);
            SemiconUiPalette.SetButtonSelection(photoRecipeButton, selectedRecipe == SemiconRecipeKind.PhotoPatternedWafer);
            SemiconUiPalette.SetButtonSelection(etchRecipeButton, selectedRecipe == SemiconRecipeKind.EtchedWafer);
            SemiconUiPalette.SetButtonSelection(depositionRecipeButton, selectedRecipe == SemiconRecipeKind.DepositedWafer);
            SemiconUiPalette.SetButtonSelection(metalRecipeButton, selectedRecipe == SemiconRecipeKind.MetalizedWafer);
            SemiconUiPalette.SetButtonSelection(edsRecipeButton, selectedRecipe == SemiconRecipeKind.TestedWafer);
            SemiconUiPalette.SetButtonSelection(sc01RecipeButton, selectedRecipe == SemiconRecipeKind.Sc01ControlSensor);
            SemiconUiPalette.SetButtonSelection(pm10RecipeButton, selectedRecipe == SemiconRecipeKind.Pm10PowerManagement,
                !state.IsContractUnlocked(SemiconContractKind.Pm10PowerManagement));
            SemiconUiPalette.SetButtonSelection(dd20RecipeButton, selectedRecipe == SemiconRecipeKind.Dd20DisplayDriver,
                !state.IsContractUnlocked(SemiconContractKind.Dd20DisplayDriver));
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
                    var ready = CanStartSelected(state);
                    queueStatusText.text = ready ? "생산 준비 완료" : "재료 부족  /  가운데 재료 목록을 확인하세요";
                    queueStatusText.color = ready ? SemiconUiPalette.Mint : SemiconUiPalette.Danger;
                }
                else if (job.IsComplete)
                {
                    queueStatusText.text = $"PROCESS COMPLETE  /  {job.OutputAmount} UNIT 회수 대기";
                    queueStatusText.color = SemiconUiPalette.Amber;
                }
                else
                {
                    queueStatusText.text = $"PROCESSING  /  {job.RemainingSeconds:0.0}s  ·  {job.Progress * 100f:0}%";
                    queueStatusText.color = SemiconUiPalette.Mint;
                }
            }

            if (produceButton != null)
            {
                produceButton.gameObject.SetActive(!job.HasJob);
                produceButton.interactable = !job.HasJob && CanStartSelected(state);
                SetButtonLabel(produceButton, $"{selectedBatches}회 생산 시작    ▶    " + GetRecipeCode(selectedRecipe));
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
                    SemiconUiPalette.Amber);
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
                return state.SiliconStock >= SemiconGameState.WaferSiliconCost * selectedBatches;
            if (selectedRecipe == SemiconRecipeKind.OxidizedWafer)
                return state.OxidationRecipeQualified && state.WaferStock >= SemiconGameState.OxidationWaferCost * selectedBatches &&
                       state.ProcessGasStock >= SemiconGameState.OxidationGasCost * selectedBatches;
            if (selectedRecipe == SemiconRecipeKind.PhotoPatternedWafer)
                return state.PhotoRecipeQualified &&
                       state.OxidizedWaferStock >= SemiconGameState.PhotoOxidizedWaferCost * selectedBatches &&
                       state.ChemicalStock >= SemiconGameState.PhotoChemicalCost * selectedBatches;
            if (selectedRecipe == SemiconRecipeKind.EtchedWafer)
                return state.EtchRecipeQualified &&
                       state.PatternedWaferStock >= SemiconGameState.EtchPatternedWaferCost * selectedBatches &&
                       state.ProcessGasStock >= SemiconGameState.EtchGasCost * selectedBatches;
            if (selectedRecipe == SemiconRecipeKind.DepositedWafer)
                return state.DepositionRecipeQualified &&
                       state.EtchedWaferStock >= SemiconGameState.DepositionEtchedWaferCost * selectedBatches &&
                       state.ProcessGasStock >= SemiconGameState.DepositionGasCost * selectedBatches;
            if (selectedRecipe == SemiconRecipeKind.MetalizedWafer)
                return state.MetalRecipeQualified &&
                       state.DepositedWaferStock >= SemiconGameState.MetalDepositedWaferCost * selectedBatches &&
                       state.MetalTargetStock >= SemiconGameState.MetalTargetCost * selectedBatches;
            if (selectedRecipe == SemiconRecipeKind.TestedWafer)
                return state.EdsRecipeQualified &&
                       state.MetalizedWaferStock >= SemiconGameState.EdsMetalizedWaferCost * selectedBatches;
            if (selectedRecipe == SemiconRecipeKind.Pm10PowerManagement &&
                !state.IsContractUnlocked(SemiconContractKind.Pm10PowerManagement)) return false;
            if (selectedRecipe == SemiconRecipeKind.Dd20DisplayDriver &&
                !state.IsContractUnlocked(SemiconContractKind.Dd20DisplayDriver)) return false;
            return state.PackageRecipeQualified &&
                   state.TestedWaferStock >= SemiconGameState.PackageTestedWaferCost * selectedBatches &&
                   state.ChemicalStock >= SemiconGameState.PackageChemicalCost * selectedBatches;
        }

        private static void GetMaterialRequirements(SemiconGameState state, SemiconRecipeKind recipe, int batches,
            out int primaryStock, out int primaryRequired, out int secondaryStock, out int secondaryRequired)
        {
            primaryStock = state.SiliconStock;
            primaryRequired = SemiconGameState.WaferSiliconCost * batches;
            secondaryStock = 0;
            secondaryRequired = 0;
            switch (recipe)
            {
                case SemiconRecipeKind.OxidizedWafer:
                    primaryStock = state.WaferStock;
                    primaryRequired = SemiconGameState.OxidationWaferCost * batches;
                    secondaryStock = state.ProcessGasStock;
                    secondaryRequired = SemiconGameState.OxidationGasCost * batches;
                    break;
                case SemiconRecipeKind.PhotoPatternedWafer:
                    primaryStock = state.OxidizedWaferStock;
                    primaryRequired = SemiconGameState.PhotoOxidizedWaferCost * batches;
                    secondaryStock = state.ChemicalStock;
                    secondaryRequired = SemiconGameState.PhotoChemicalCost * batches;
                    break;
                case SemiconRecipeKind.EtchedWafer:
                    primaryStock = state.PatternedWaferStock;
                    primaryRequired = SemiconGameState.EtchPatternedWaferCost * batches;
                    secondaryStock = state.ProcessGasStock;
                    secondaryRequired = SemiconGameState.EtchGasCost * batches;
                    break;
                case SemiconRecipeKind.DepositedWafer:
                    primaryStock = state.EtchedWaferStock;
                    primaryRequired = SemiconGameState.DepositionEtchedWaferCost * batches;
                    secondaryStock = state.ProcessGasStock;
                    secondaryRequired = SemiconGameState.DepositionGasCost * batches;
                    break;
                case SemiconRecipeKind.MetalizedWafer:
                    primaryStock = state.DepositedWaferStock;
                    primaryRequired = SemiconGameState.MetalDepositedWaferCost * batches;
                    secondaryStock = state.MetalTargetStock;
                    secondaryRequired = SemiconGameState.MetalTargetCost * batches;
                    break;
                case SemiconRecipeKind.TestedWafer:
                    primaryStock = state.MetalizedWaferStock;
                    primaryRequired = SemiconGameState.EdsMetalizedWaferCost * batches;
                    break;
                case SemiconRecipeKind.Sc01ControlSensor:
                case SemiconRecipeKind.Pm10PowerManagement:
                case SemiconRecipeKind.Dd20DisplayDriver:
                    primaryStock = state.TestedWaferStock;
                    primaryRequired = SemiconGameState.PackageTestedWaferCost * batches;
                    secondaryStock = state.ChemicalStock;
                    secondaryRequired = SemiconGameState.PackageChemicalCost * batches;
                    break;
            }
        }

        private static void GetMaterialDisplayNames(SemiconRecipeKind recipe, out string primary, out string secondary)
        {
            primary = "고순도 실리콘";
            secondary = string.Empty;
            switch (recipe)
            {
                case SemiconRecipeKind.OxidizedWafer:
                    primary = "기초 웨이퍼"; secondary = "산화 공정 가스"; break;
                case SemiconRecipeKind.PhotoPatternedWafer:
                    primary = "산화 웨이퍼"; secondary = "포토 공정 약품"; break;
                case SemiconRecipeKind.EtchedWafer:
                    primary = "패턴 웨이퍼"; secondary = "식각 공정 가스"; break;
                case SemiconRecipeKind.DepositedWafer:
                    primary = "식각 웨이퍼"; secondary = "증착 공정 가스"; break;
                case SemiconRecipeKind.MetalizedWafer:
                    primary = "박막 웨이퍼"; secondary = "배선 금속 타깃"; break;
                case SemiconRecipeKind.TestedWafer:
                    primary = "배선 웨이퍼"; break;
                case SemiconRecipeKind.Sc01ControlSensor:
                case SemiconRecipeKind.Pm10PowerManagement:
                case SemiconRecipeKind.Dd20DisplayDriver:
                    primary = "EDS 선별 웨이퍼"; secondary = "패키징 공정 약품"; break;
            }
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
