using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    public sealed class PhotoExperimentPanel : MonoBehaviour
    {
        public enum PresentationState
        {
            Hidden,
            Ready,
            Processing,
            Result
        }

        private readonly struct ExperimentResult
        {
            public readonly int Dose;
            public readonly float Focus;
            public readonly float YieldValue;
            public readonly float Precision;
            public readonly float Defects;
            public readonly bool Qualified;
            public readonly float PreviousYield;
            public readonly float PreviousPrecision;
            public readonly float PreviousDefects;

            public ExperimentResult(int dose, float focus, float yieldValue, float precision, float defects,
                bool qualified, float previousYield, float previousPrecision, float previousDefects)
            {
                Dose = dose;
                Focus = focus;
                YieldValue = yieldValue;
                Precision = precision;
                Defects = defects;
                Qualified = qualified;
                PreviousYield = previousYield;
                PreviousPrecision = previousPrecision;
                PreviousDefects = previousDefects;
            }
        }

        private const int ExperimentCost = 8;
        private const float ReadyPatternReveal = 0.34f;

        [Header("Root")]
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private RectTransform panelFrame;
        [SerializeField] private SemiconHud hud;

        [Header("Ready state")]
        [SerializeField] private CanvasGroup readyGroup;
        [SerializeField] private RectTransform parameterPanel;
        [SerializeField] private RectTransform predictionPanel;
        [SerializeField] private Slider doseSlider;
        [SerializeField] private Slider focusSlider;
        [SerializeField] private TMP_Text doseValueText;
        [SerializeField] private TMP_Text focusValueText;
        [SerializeField] private TMP_Text previewYieldText;
        [SerializeField] private TMP_Text previewPrecisionText;
        [SerializeField] private TMP_Text previewDefectText;
        [SerializeField] private TMP_Text recipeText;
        [SerializeField] private TMP_Text experimentCountText;
        [SerializeField] private TMP_Text researchBalanceText;
        [SerializeField] private Button doseMinusButton;
        [SerializeField] private Button dosePlusButton;
        [SerializeField] private Button focusMinusButton;
        [SerializeField] private Button focusPlusButton;
        [SerializeField] private Button runButton;
        [SerializeField] private Button closeButton;

        [Header("Shared wafer")]
        [SerializeField] private RectTransform waferRoot;
        [SerializeField] private SemiconPhotoWaferGraphic waferGraphic;

        [Header("Processing state")]
        [SerializeField] private CanvasGroup processingGroup;
        [SerializeField] private TMP_Text processingStatusText;
        [SerializeField] private TMP_Text processingProgressText;
        [SerializeField] private Button skipButton;

        [Header("Result state")]
        [SerializeField] private CanvasGroup resultGroup;
        [SerializeField] private RectTransform resultPanel;
        [SerializeField] private TMP_Text resultYieldText;
        [SerializeField] private TMP_Text resultYieldDeltaText;
        [SerializeField] private TMP_Text resultYieldTargetText;
        [SerializeField] private TMP_Text resultPrecisionText;
        [SerializeField] private TMP_Text resultPrecisionDeltaText;
        [SerializeField] private TMP_Text resultPrecisionTargetText;
        [SerializeField] private TMP_Text resultDefectText;
        [SerializeField] private TMP_Text resultDefectDeltaText;
        [SerializeField] private TMP_Text resultDefectTargetText;
        [SerializeField] private TMP_Text resultRecipeText;
        [SerializeField] private TMP_Text resultRecipeDetailText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button repeatButton;

        private SemiconPlayerController activePlayer;
        private SemiconThirdPersonCamera activeCamera;
        private Coroutine animationRoutine;
        private Vector2 readyParameterPosition;
        private Vector2 readyPredictionPosition;
        private Vector2 readyWaferPosition;
        private Vector2 resultPanelPosition;
        private Vector3 readyWaferScale;
        private bool isOpen;
        private bool isRunning;
        private bool skipRequested;

        public PresentationState CurrentPresentationState { get; private set; } = PresentationState.Hidden;
        public bool IsRunning => isRunning;

        private void Awake()
        {
            readyParameterPosition = parameterPanel != null ? parameterPanel.anchoredPosition : Vector2.zero;
            readyPredictionPosition = predictionPanel != null ? predictionPanel.anchoredPosition : Vector2.zero;
            readyWaferPosition = waferRoot != null ? waferRoot.anchoredPosition : Vector2.zero;
            readyWaferScale = waferRoot != null ? waferRoot.localScale : Vector3.one;
            resultPanelPosition = resultPanel != null ? resultPanel.anchoredPosition : Vector2.zero;

            doseSlider?.onValueChanged.AddListener(_ => RefreshParameterLabels());
            focusSlider?.onValueChanged.AddListener(_ => RefreshParameterLabels());
            doseMinusButton?.onClick.AddListener(() => AdjustSlider(doseSlider, -1f));
            dosePlusButton?.onClick.AddListener(() => AdjustSlider(doseSlider, 1f));
            focusMinusButton?.onClick.AddListener(() => AdjustSlider(focusSlider, -0.01f));
            focusPlusButton?.onClick.AddListener(() => AdjustSlider(focusSlider, 0.01f));
            runButton?.onClick.AddListener(RunExperiment);
            closeButton?.onClick.AddListener(Close);
            confirmButton?.onClick.AddListener(Close);
            repeatButton?.onClick.AddListener(ReturnToReady);
            skipButton?.onClick.AddListener(RequestSkip);

            RestoreReadyImmediate();
            SetVisible(false);
        }

        private void Start()
        {
            RefreshParameterLabels();
            RefreshRecipeArchive();
        }

        private void Update()
        {
            if (!isOpen || Keyboard.current == null)
            {
                return;
            }

            if (isRunning && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                RequestSkip();
            }
            else if (!isRunning && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public void Open(SemiconPlayerController player, SemiconThirdPersonCamera followCamera)
        {
            if (isOpen)
            {
                return;
            }

            activePlayer = player;
            activeCamera = followCamera;
            activePlayer?.SetInputEnabled(false);
            activeCamera?.SetLookEnabled(false);
            SemiconPlayerController.SetCursorLocked(false);
            isOpen = true;
            isRunning = false;
            skipRequested = false;
            RestoreReadyImmediate();
            RefreshParameterLabels();
            RefreshRecipeArchive();

            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
            }
            animationRoutine = StartCoroutine(AnimateOpen());
        }

        public void Close()
        {
            if (!isOpen || isRunning)
            {
                return;
            }

            isOpen = false;
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
            }
            animationRoutine = StartCoroutine(AnimateClose());
        }

        public void Configure(
            CanvasGroup group,
            RectTransform frame,
            CanvasGroup ready,
            RectTransform parameters,
            RectTransform prediction,
            Slider dose,
            Slider focus,
            TMP_Text doseValue,
            TMP_Text focusValue,
            TMP_Text previewYield,
            TMP_Text previewPrecision,
            TMP_Text previewDefect,
            TMP_Text recipe,
            TMP_Text experimentCount,
            TMP_Text researchBalance,
            Button doseMinus,
            Button dosePlus,
            Button focusMinus,
            Button focusPlus,
            Button run,
            Button close,
            RectTransform wafer,
            SemiconPhotoWaferGraphic waferDisplay,
            CanvasGroup processing,
            TMP_Text processingStatus,
            TMP_Text processingProgress,
            Button skip,
            CanvasGroup result,
            RectTransform resultSheet,
            TMP_Text resultYield,
            TMP_Text resultYieldDelta,
            TMP_Text resultYieldTarget,
            TMP_Text resultPrecision,
            TMP_Text resultPrecisionDelta,
            TMP_Text resultPrecisionTarget,
            TMP_Text resultDefect,
            TMP_Text resultDefectDelta,
            TMP_Text resultDefectTarget,
            TMP_Text resultRecipe,
            TMP_Text resultRecipeDetail,
            Button confirm,
            Button repeat,
            SemiconHud targetHud)
        {
            panelGroup = group;
            panelFrame = frame;
            readyGroup = ready;
            parameterPanel = parameters;
            predictionPanel = prediction;
            doseSlider = dose;
            focusSlider = focus;
            doseValueText = doseValue;
            focusValueText = focusValue;
            previewYieldText = previewYield;
            previewPrecisionText = previewPrecision;
            previewDefectText = previewDefect;
            recipeText = recipe;
            experimentCountText = experimentCount;
            researchBalanceText = researchBalance;
            doseMinusButton = doseMinus;
            dosePlusButton = dosePlus;
            focusMinusButton = focusMinus;
            focusPlusButton = focusPlus;
            runButton = run;
            closeButton = close;
            waferRoot = wafer;
            waferGraphic = waferDisplay;
            processingGroup = processing;
            processingStatusText = processingStatus;
            processingProgressText = processingProgress;
            skipButton = skip;
            resultGroup = result;
            resultPanel = resultSheet;
            resultYieldText = resultYield;
            resultYieldDeltaText = resultYieldDelta;
            resultYieldTargetText = resultYieldTarget;
            resultPrecisionText = resultPrecision;
            resultPrecisionDeltaText = resultPrecisionDelta;
            resultPrecisionTargetText = resultPrecisionTarget;
            resultDefectText = resultDefect;
            resultDefectDeltaText = resultDefectDelta;
            resultDefectTargetText = resultDefectTarget;
            resultRecipeText = resultRecipe;
            resultRecipeDetailText = resultRecipeDetail;
            confirmButton = confirm;
            repeatButton = repeat;
            hud = targetHud;
        }

        private void RunExperiment()
        {
            if (isRunning || CurrentPresentationState != PresentationState.Ready)
            {
                return;
            }

            var state = SemiconGameState.Instance;
            if (state == null || !state.TrySpendResearch(ExperimentCost))
            {
                hud?.ShowToast($"연구 데이터가 부족합니다. 실험에는 {ExperimentCost}개가 필요합니다.");
                return;
            }

            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
            }
            animationRoutine = StartCoroutine(RunExperimentSequence());
        }

        private IEnumerator RunExperimentSequence()
        {
            isRunning = true;
            skipRequested = false;
            CurrentPresentationState = PresentationState.Processing;
            SetReadyControls(false);
            if (closeButton != null) closeButton.interactable = false;
            if (waferGraphic != null) waferGraphic.Completed = false;

            yield return AnimateReadyToProcessing(0.34f);

            SetProcessingCopy("MASK ALIGNMENT", "마스크 정렬 중", "마스크 정렬  ·  18%");
            yield return WaitPhase(0.28f, progress =>
            {
                if (processingProgressText != null)
                {
                    processingProgressText.text = $"마스크 정렬  ·  {Mathf.RoundToInt(Mathf.Lerp(4f, 18f, progress))}%";
                }
            });

            SetProcessingCopy("PHOTO EXPOSURE", "노광 진행 중", "노광 진행  ·  18%");
            if (waferGraphic != null)
            {
                waferGraphic.ShowScan = true;
                waferGraphic.ScanProgress = 0f;
                waferGraphic.PatternReveal = 0f;
            }
            yield return WaitPhase(1.2f, progress =>
            {
                if (waferGraphic != null)
                {
                    waferGraphic.ScanProgress = progress;
                    waferGraphic.PatternReveal = progress;
                }
                if (processingProgressText != null)
                {
                    processingProgressText.text = $"노광 진행  ·  {Mathf.RoundToInt(Mathf.Lerp(18f, 86f, progress))}%";
                }
            });

            if (waferGraphic != null)
            {
                waferGraphic.ShowScan = false;
                waferGraphic.PatternReveal = 1f;
            }
            SetProcessingCopy("PATTERN DEVELOPMENT", "패턴 현상 중", "패턴 현상  ·  86%");
            yield return WaitPhase(0.25f, progress =>
            {
                if (processingProgressText != null)
                {
                    processingProgressText.text = $"패턴 현상  ·  {Mathf.RoundToInt(Mathf.Lerp(86f, 100f, progress))}%";
                }
            });

            var result = EvaluateAndRecordExperiment();
            PrepareResultCopy(result);
            yield return AnimateProcessingToResult(0.38f);
            yield return AnimateResultValues(result, 0.45f);

            isRunning = false;
            CurrentPresentationState = PresentationState.Result;
            if (closeButton != null) closeButton.interactable = true;
            animationRoutine = null;
        }

        private ExperimentResult EvaluateAndRecordExperiment()
        {
            var dose = Mathf.RoundToInt(doseSlider != null ? doseSlider.value : 90f);
            var focus = focusSlider != null ? focusSlider.value : -0.15f;
            CalculatePrediction(dose, focus, out var yieldValue, out var precision, out var defects,
                out var qualified);

            var state = SemiconGameState.Instance;
            var previousYield = state != null && state.ExperimentCount > 0 ? state.BestYield : 95.8f;
            var previousPrecision = state != null && state.ExperimentCount > 0 ? state.BestPrecision : 97.1f;
            var previousDefects = state != null && state.ExperimentCount > 0
                ? EstimateDefects(state.BestYield, state.BestPrecision)
                : 1.8f;

            state?.RecordPhotoExperiment(dose, focus, yieldValue, precision, qualified);
            RefreshRecipeArchive();
            return new ExperimentResult(dose, focus, yieldValue, precision, defects, qualified,
                previousYield, previousPrecision, previousDefects);
        }

        private static void CalculatePrediction(int dose, float focus, out float yieldValue, out float precision,
            out float defects, out bool qualified)
        {
            var doseScore = Mathf.Clamp01(1f - Mathf.Abs(dose - 105f) / 38f);
            var focusScore = Mathf.Clamp01(1f - Mathf.Abs(focus - 0.05f) / 0.46f);
            var combined = doseScore * 0.58f + focusScore * 0.42f;
            yieldValue = 43f + combined * 54f;
            precision = 48f + (doseScore * 0.48f + focusScore * 0.52f) * 50f;
            defects = Mathf.Max(0.6f, 18.5f - combined * 17.2f);
            qualified = yieldValue >= 88f && precision >= 90f;
        }

        private static float EstimateDefects(float yieldValue, float precision)
        {
            var normalized = Mathf.Clamp01(((yieldValue - 43f) / 54f + (precision - 48f) / 50f) * 0.5f);
            return Mathf.Max(0.6f, 18.5f - normalized * 17.2f);
        }

        private void PrepareResultCopy(ExperimentResult result)
        {
            SetText(resultYieldText, "0.0%");
            SetText(resultPrecisionText, "0.0%");
            SetText(resultDefectText, "0.0%");
            SetText(resultYieldDeltaText,
                $"이전 {result.PreviousYield:0.0}%   {FormatDelta(result.YieldValue - result.PreviousYield, false)}");
            SetText(resultPrecisionDeltaText,
                $"이전 {result.PreviousPrecision:0.0}%   {FormatDelta(result.Precision - result.PreviousPrecision, false)}");
            SetText(resultDefectDeltaText,
                $"이전 {result.PreviousDefects:0.0}%   {FormatDelta(result.Defects - result.PreviousDefects, true)}");
            SetText(resultYieldTargetText,
                $"목표 ≥ 88.0%\n{(result.YieldValue >= 88f ? "목표 달성" : "목표 미달")}");
            SetText(resultPrecisionTargetText,
                $"목표 ≥ 90.0%\n{(result.Precision >= 90f ? "목표 달성" : "목표 미달")}");
            SetText(resultDefectTargetText,
                $"목표 ≤ 2.0%\n{(result.Defects <= 2f ? "목표 달성" : "목표 미달")}");

            if (resultRecipeText != null)
            {
                resultRecipeText.text = result.Qualified ? "PHOTO-01 레시피 획득" : "공정 조건 재조정 필요";
                resultRecipeText.color = result.Qualified
                    ? new Color32(20, 155, 111, 255)
                    : new Color32(205, 91, 72, 255);
            }
            if (resultRecipeDetailText != null)
            {
                resultRecipeDetailText.text = result.Qualified
                    ? "생산 라인 사용 가능"
                    : "목표 범위를 다시 확인하세요.";
            }
        }

        private static string FormatDelta(float delta, bool lowerIsBetter)
        {
            var improved = lowerIsBetter ? delta <= 0f : delta >= 0f;
            var arrow = delta >= 0f ? "▲" : "▼";
            return $"{arrow}{Mathf.Abs(delta):0.0}%{(improved ? string.Empty : " 주의")}";
        }

        private IEnumerator AnimateResultValues(ExperimentResult result, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration && !skipRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                SetText(resultYieldText, $"{Mathf.Lerp(0f, result.YieldValue, t):0.0}%");
                SetText(resultPrecisionText, $"{Mathf.Lerp(0f, result.Precision, t):0.0}%");
                SetText(resultDefectText, $"{Mathf.Lerp(0f, result.Defects, t):0.0}%");
                yield return null;
            }
            SetText(resultYieldText, $"{result.YieldValue:0.0}%");
            SetText(resultPrecisionText, $"{result.Precision:0.0}%");
            SetText(resultDefectText, $"{result.Defects:0.0}%");
        }

        private IEnumerator AnimateReadyToProcessing(float duration)
        {
            SetGroup(processingGroup, 0f, false);
            var elapsed = 0f;
            var processingWaferPosition = new Vector2(0f, -8f);
            var processingScale = readyWaferScale * 1.34f;
            while (elapsed < duration && !skipRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = EaseOut(Mathf.Clamp01(elapsed / duration));
                SetGroupAlpha(readyGroup, 1f - t);
                SetGroupAlpha(processingGroup, t);
                if (parameterPanel != null)
                {
                    parameterPanel.anchoredPosition = Vector2.Lerp(readyParameterPosition,
                        readyParameterPosition + Vector2.left * 170f, t);
                }
                if (predictionPanel != null)
                {
                    predictionPanel.anchoredPosition = Vector2.Lerp(readyPredictionPosition,
                        readyPredictionPosition + Vector2.right * 170f, t);
                }
                if (waferRoot != null)
                {
                    waferRoot.anchoredPosition = Vector2.Lerp(readyWaferPosition, processingWaferPosition, t);
                    waferRoot.localScale = Vector3.Lerp(readyWaferScale, processingScale, t);
                }
                yield return null;
            }
            SetGroup(readyGroup, 0f, false);
            SetGroup(processingGroup, 1f, true);
            if (waferRoot != null)
            {
                waferRoot.anchoredPosition = processingWaferPosition;
                waferRoot.localScale = processingScale;
            }
        }

        private IEnumerator AnimateProcessingToResult(float duration)
        {
            if (waferGraphic != null)
            {
                waferGraphic.Completed = true;
                waferGraphic.ShowScan = false;
                waferGraphic.PatternReveal = 1f;
            }
            SetGroup(resultGroup, 0f, false);
            if (resultPanel != null)
            {
                resultPanel.anchoredPosition = resultPanelPosition + Vector2.right * 170f;
            }
            var startWaferPosition = waferRoot != null ? waferRoot.anchoredPosition : Vector2.zero;
            var startWaferScale = waferRoot != null ? waferRoot.localScale : Vector3.one;
            var resultWaferPosition = new Vector2(-350f, -28f);
            var resultWaferScale = readyWaferScale * 1.28f;
            var elapsed = 0f;
            while (elapsed < duration && !skipRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = EaseOut(Mathf.Clamp01(elapsed / duration));
                SetGroupAlpha(processingGroup, 1f - t);
                SetGroupAlpha(resultGroup, t);
                if (waferRoot != null)
                {
                    waferRoot.anchoredPosition = Vector2.Lerp(startWaferPosition, resultWaferPosition, t);
                    waferRoot.localScale = Vector3.Lerp(startWaferScale, resultWaferScale, t);
                }
                if (resultPanel != null)
                {
                    resultPanel.anchoredPosition = Vector2.Lerp(resultPanelPosition + Vector2.right * 170f,
                        resultPanelPosition, t);
                }
                yield return null;
            }
            SetGroup(processingGroup, 0f, false);
            SetGroup(resultGroup, 1f, true);
            if (waferRoot != null)
            {
                waferRoot.anchoredPosition = resultWaferPosition;
                waferRoot.localScale = resultWaferScale;
            }
            if (resultPanel != null) resultPanel.anchoredPosition = resultPanelPosition;
        }

        private void ReturnToReady()
        {
            if (isRunning || CurrentPresentationState != PresentationState.Result)
            {
                return;
            }
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
            }
            animationRoutine = StartCoroutine(AnimateReturnToReady());
        }

        private IEnumerator AnimateReturnToReady()
        {
            isRunning = true;
            CurrentPresentationState = PresentationState.Ready;
            var startPosition = waferRoot != null ? waferRoot.anchoredPosition : Vector2.zero;
            var startScale = waferRoot != null ? waferRoot.localScale : Vector3.one;
            var elapsed = 0f;
            const float duration = 0.34f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = EaseOut(Mathf.Clamp01(elapsed / duration));
                SetGroupAlpha(resultGroup, 1f - t);
                SetGroupAlpha(readyGroup, t);
                if (parameterPanel != null)
                {
                    parameterPanel.anchoredPosition = Vector2.Lerp(readyParameterPosition + Vector2.left * 80f,
                        readyParameterPosition, t);
                }
                if (predictionPanel != null)
                {
                    predictionPanel.anchoredPosition = Vector2.Lerp(readyPredictionPosition + Vector2.right * 80f,
                        readyPredictionPosition, t);
                }
                if (waferRoot != null)
                {
                    waferRoot.anchoredPosition = Vector2.Lerp(startPosition, readyWaferPosition, t);
                    waferRoot.localScale = Vector3.Lerp(startScale, readyWaferScale, t);
                }
                yield return null;
            }
            RestoreReadyImmediate();
            isRunning = false;
            animationRoutine = null;
        }

        private IEnumerator WaitPhase(float duration, System.Action<float> update)
        {
            var elapsed = 0f;
            while (elapsed < duration && !skipRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                update?.Invoke(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            update?.Invoke(1f);
        }

        private void RefreshParameterLabels()
        {
            if (doseValueText != null && doseSlider != null)
            {
                doseValueText.text = $"{Mathf.RoundToInt(doseSlider.value)} mJ/cm²";
            }
            if (focusValueText != null && focusSlider != null)
            {
                focusValueText.text = $"{focusSlider.value:+0.00;-0.00;0.00} μm";
            }

            var dose = Mathf.RoundToInt(doseSlider != null ? doseSlider.value : 90f);
            var focus = focusSlider != null ? focusSlider.value : -0.15f;
            CalculatePrediction(dose, focus, out var yieldValue, out var precision, out var defects,
                out var qualified);
            SetText(previewYieldText, $"{yieldValue:0.0}%");
            SetText(previewPrecisionText, $"{precision:0.0}%");
            SetText(previewDefectText, $"{defects:0.0}%");
            if (waferGraphic != null && CurrentPresentationState == PresentationState.Ready)
            {
                waferGraphic.PatternReveal = Mathf.Lerp(0.18f, 0.58f,
                    Mathf.InverseLerp(45f, 97f, yieldValue));
                waferGraphic.Completed = qualified;
            }
        }

        private void RefreshRecipeArchive()
        {
            var state = SemiconGameState.Instance;
            if (state == null)
            {
                return;
            }

            if (recipeText != null)
            {
                recipeText.text = state.ExperimentCount == 0
                    ? "이전 최고 기록   ·   아직 저장된 실험이 없습니다."
                    : $"이전 최고 기록   ·   수율 {state.BestYield:0.0}%   ·   정밀도 {state.BestPrecision:0.0}%   ·   " +
                      $"노광량 {state.BestDose} mJ/cm²   ·   초점 {state.BestFocus:+0.00;-0.00;0.00} μm";
            }
            if (experimentCountText != null)
            {
                experimentCountText.text = $"EXPERIMENT LOG  /  {state.ExperimentCount:00}";
            }
            if (researchBalanceText != null)
            {
                researchBalanceText.text = $"연구 데이터 {state.ResearchPoints}";
            }
        }

        private void RestoreReadyImmediate()
        {
            CurrentPresentationState = isOpen ? PresentationState.Ready : PresentationState.Hidden;
            SetGroup(readyGroup, 1f, true);
            SetGroup(processingGroup, 0f, false);
            SetGroup(resultGroup, 0f, false);
            if (parameterPanel != null) parameterPanel.anchoredPosition = readyParameterPosition;
            if (predictionPanel != null) predictionPanel.anchoredPosition = readyPredictionPosition;
            if (resultPanel != null) resultPanel.anchoredPosition = resultPanelPosition;
            if (waferRoot != null)
            {
                waferRoot.anchoredPosition = readyWaferPosition;
                waferRoot.localScale = readyWaferScale;
            }
            if (waferGraphic != null)
            {
                waferGraphic.ShowScan = false;
                waferGraphic.ScanProgress = 0f;
                waferGraphic.PatternReveal = ReadyPatternReveal;
                waferGraphic.Completed = false;
            }
            SetReadyControls(true);
            if (closeButton != null) closeButton.interactable = true;
            RefreshParameterLabels();
        }

        private void SetReadyControls(bool enabled)
        {
            if (doseSlider != null) doseSlider.interactable = enabled;
            if (focusSlider != null) focusSlider.interactable = enabled;
            if (doseMinusButton != null) doseMinusButton.interactable = enabled;
            if (dosePlusButton != null) dosePlusButton.interactable = enabled;
            if (focusMinusButton != null) focusMinusButton.interactable = enabled;
            if (focusPlusButton != null) focusPlusButton.interactable = enabled;
            if (runButton != null) runButton.interactable = enabled;
        }

        private void SetProcessingCopy(string english, string korean, string progress)
        {
            SetText(processingStatusText, $"{english}\n{korean}");
            SetText(processingProgressText, progress);
        }

        private void AdjustSlider(Slider slider, float amount)
        {
            if (slider != null && slider.interactable)
            {
                slider.value = Mathf.Clamp(slider.value + amount, slider.minValue, slider.maxValue);
            }
        }

        private void RequestSkip()
        {
            if (isRunning && CurrentPresentationState == PresentationState.Processing)
            {
                skipRequested = true;
            }
        }

        private IEnumerator AnimateOpen()
        {
            SetVisible(true);
            panelGroup.alpha = 0f;
            var start = new Vector2(54f, 0f);
            var end = Vector2.zero;
            if (panelFrame != null) panelFrame.anchoredPosition = start;
            var elapsed = 0f;
            const float duration = 0.2f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = EaseOut(Mathf.Clamp01(elapsed / duration));
                if (panelGroup != null) panelGroup.alpha = t;
                if (panelFrame != null) panelFrame.anchoredPosition = Vector2.Lerp(start, end, t);
                yield return null;
            }
            if (panelGroup != null) panelGroup.alpha = 1f;
            if (panelFrame != null) panelFrame.anchoredPosition = end;
            CurrentPresentationState = PresentationState.Ready;
            animationRoutine = null;
        }

        private IEnumerator AnimateClose()
        {
            var start = panelFrame != null ? panelFrame.anchoredPosition : Vector2.zero;
            var end = new Vector2(54f, 0f);
            var elapsed = 0f;
            const float duration = 0.16f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                if (panelGroup != null) panelGroup.alpha = 1f - t;
                if (panelFrame != null) panelFrame.anchoredPosition = Vector2.Lerp(start, end, t);
                yield return null;
            }
            SetVisible(false);
            CurrentPresentationState = PresentationState.Hidden;
            activePlayer?.SetInputEnabled(true);
            activeCamera?.SetLookEnabled(true);
            SemiconPlayerController.SetCursorLocked(true);
            activePlayer = null;
            activeCamera = null;
            animationRoutine = null;
        }

        private void SetVisible(bool visible)
        {
            if (panelGroup == null)
            {
                return;
            }
            panelGroup.alpha = visible ? 1f : 0f;
            panelGroup.interactable = visible;
            panelGroup.blocksRaycasts = visible;
        }

        private static void SetGroup(CanvasGroup group, float alpha, bool interactive)
        {
            if (group == null)
            {
                return;
            }
            group.alpha = alpha;
            group.interactable = interactive;
            group.blocksRaycasts = interactive;
        }

        private static void SetGroupAlpha(CanvasGroup group, float alpha)
        {
            if (group != null)
            {
                group.alpha = alpha;
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private static float EaseOut(float value)
        {
            return 1f - Mathf.Pow(1f - value, 3f);
        }
    }
}
