using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    public sealed class EdsExperimentPanel : MonoBehaviour
    {
        private const int ExperimentCost = SemiconGameState.ExperimentCreditCost;

        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private RectTransform panelFrame;
        [SerializeField] private Slider voltageSlider;
        [SerializeField] private Slider leakageThresholdSlider;
        [SerializeField] private Text voltageValueText;
        [SerializeField] private Text leakageThresholdValueText;
        [SerializeField] private Text yieldValueText;
        [SerializeField] private Text detectionValueText;
        [SerializeField] private Text falseRejectValueText;
        [SerializeField] private Text resultStatusText;
        [SerializeField] private Text recipeText;
        [SerializeField] private Text experimentCountText;
        [SerializeField] private Button runButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform probeScanLine;
        [SerializeField] private SemiconHud hud;

        private SemiconPlayerController activePlayer;
        private SemiconThirdPersonCamera activeCamera;
        private Coroutine animationRoutine;
        private bool isOpen;
        private bool isRunning;

        private void Awake()
        {
            voltageSlider?.onValueChanged.AddListener(_ => RefreshParameterLabels());
            leakageThresholdSlider?.onValueChanged.AddListener(_ => RefreshParameterLabels());
            runButton?.onClick.AddListener(RunExperiment);
            closeButton?.onClick.AddListener(Close);
            SetVisible(false);
        }

        private void Start()
        {
            RefreshParameterLabels();
            RefreshRecipeArchive();
        }

        private void Update()
        {
            if (isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        public void Configure(CanvasGroup group, RectTransform frame, Slider voltage, Slider leakageThreshold,
            Text voltageValue, Text leakageThresholdValue, Text yieldValue, Text detectionValue,
            Text falseRejectValue, Text status, Text recipe, Text experimentCount, Button run, Button close,
            RectTransform scanLine, SemiconHud targetHud)
        {
            panelGroup = group;
            panelFrame = frame;
            voltageSlider = voltage;
            leakageThresholdSlider = leakageThreshold;
            voltageValueText = voltageValue;
            leakageThresholdValueText = leakageThresholdValue;
            yieldValueText = yieldValue;
            detectionValueText = detectionValue;
            falseRejectValueText = falseRejectValue;
            resultStatusText = status;
            recipeText = recipe;
            experimentCountText = experimentCount;
            runButton = run;
            closeButton = close;
            probeScanLine = scanLine;
            hud = targetHud;
        }

        public void Open(SemiconPlayerController player, SemiconThirdPersonCamera followCamera)
        {
            if (isOpen) return;
            activePlayer = player;
            activeCamera = followCamera;
            activePlayer?.SetInputEnabled(false);
            activeCamera?.SetLookEnabled(false);
            SemiconPlayerController.SetCursorLocked(false);
            isOpen = true;
            RefreshRecipeArchive();
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(AnimateOpen());
        }

        public void Close()
        {
            if (!isOpen || isRunning) return;
            isOpen = false;
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(AnimateClose());
        }

        private void RunExperiment()
        {
            if (isRunning) return;
            var state = SemiconGameState.Instance;
            if (state == null || !state.TrySpendCredits(ExperimentCost))
            {
                hud?.ShowToast($"실험 비용이 부족합니다. 1회에 ₩{ExperimentCost:N0}이 필요합니다.");
                return;
            }
            StartCoroutine(RunExperimentSequence());
        }

        private IEnumerator RunExperimentSequence()
        {
            isRunning = true;
            if (runButton != null) runButton.interactable = false;
            if (resultStatusText != null)
            {
                resultStatusText.text = "PROBE TEST ACTIVE  /  다이 전기 특성 판정 중";
                resultStatusText.color = SemiconUiPalette.Blue;
            }

            if (probeScanLine != null)
            {
                probeScanLine.gameObject.SetActive(true);
                var elapsed = 0f;
                const float duration = 0.82f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                    probeScanLine.anchoredPosition = Vector2.Lerp(new Vector2(0f, 55f),
                        new Vector2(0f, -55f), t);
                    yield return null;
                }
                probeScanLine.gameObject.SetActive(false);
            }
            else
            {
                yield return new WaitForSecondsRealtime(0.82f);
            }

            EvaluateExperiment();
            if (runButton != null) runButton.interactable = true;
            isRunning = false;
        }

        private void EvaluateExperiment()
        {
            var voltage = Mathf.RoundToInt(voltageSlider != null ? voltageSlider.value : 3f);
            var leakageThreshold = Mathf.RoundToInt(leakageThresholdSlider != null
                ? leakageThresholdSlider.value
                : 30f);
            var voltageScore = Mathf.Clamp01(1f - Mathf.Abs(voltage - 3f) / 2.5f);
            var thresholdScore = Mathf.Clamp01(1f - Mathf.Abs(leakageThreshold - 30f) / 25f);
            var yield = Mathf.Clamp(82f + voltageScore * 8f + thresholdScore * 6f, 60f, 96f);
            var detection = Mathf.Clamp(78f + voltageScore * 10f + thresholdScore * 10f, 55f, 98f);
            var falseReject = Mathf.Clamp(8f - voltageScore * 3f - thresholdScore * 3f, 1f, 12f);
            var qualified = yield >= 92f && detection >= 94f && falseReject <= 4f;

            if (yieldValueText != null) yieldValueText.text = $"{yield:0.0}%";
            if (detectionValueText != null) detectionValueText.text = $"{detection:0.0}%";
            if (falseRejectValueText != null) falseRejectValueText.text = $"{falseReject:0.0}%";
            if (resultStatusText != null)
            {
                resultStatusText.text = qualified
                    ? "RECIPE QUALIFIED  /  EDS-01 양산 검사 조건을 확보했습니다."
                    : detection >= 88f
                        ? "NEAR TEST WINDOW  /  검출률과 오판정률의 균형을 조정하세요."
                        : "OUT OF TEST WINDOW  /  전압과 누설 기준을 다시 조정하세요.";
                resultStatusText.color = qualified
                    ? SemiconUiPalette.Amber
                    : detection >= 88f
                        ? SemiconUiPalette.Mint
                        : SemiconUiPalette.Danger;
            }

            SemiconGameState.Instance?.RecordEdsExperiment(voltage, leakageThreshold, yield, detection,
                falseReject, qualified);
            RefreshRecipeArchive();
        }

        private void RefreshParameterLabels()
        {
            if (voltageValueText != null && voltageSlider != null)
                voltageValueText.text = $"{Mathf.RoundToInt(voltageSlider.value)} V";
            if (leakageThresholdValueText != null && leakageThresholdSlider != null)
                leakageThresholdValueText.text = $"{Mathf.RoundToInt(leakageThresholdSlider.value)} μA";
        }

        private void RefreshRecipeArchive()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            if (recipeText != null)
            {
                recipeText.text = state.EdsExperimentCount == 0
                    ? "아직 저장된 EDS 실험 데이터가 없습니다.\n첫 실험을 실행해 검사창을 탐색하세요."
                    : $"등록 레시피  {state.GetRecipeVariantCount(SemiconRecipeKind.TestedWafer)}개\nBEST RUN  #{state.EdsExperimentCount:00}\n\n테스트 전압 {state.BestEdsVoltage} V\n누설 기준   {state.BestEdsLeakageThreshold} μA\n\n양품 수율   {state.BestEdsYield:0.0}%\n결함 검출률 {state.BestEdsDetection:0.0}%\n오판정률    {state.BestEdsFalseReject:0.0}%\n\n{(state.EdsRecipeQualified ? "● 합격 조건은 각각 레시피로 저장됩니다" : "○ 검사 범위 탐색 중")}";
            }
            if (experimentCountText != null)
                experimentCountText.text = $"EXPERIMENT LOG  /  {state.EdsExperimentCount:00}";
        }

        private IEnumerator AnimateOpen()
        {
            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.interactable = true;
                panelGroup.blocksRaycasts = true;
            }
            var start = new Vector2(80f, 0f);
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
                if (panelFrame != null) panelFrame.anchoredPosition = Vector2.Lerp(start, new Vector2(80f, 0f), t);
                yield return null;
            }
            SetVisible(false);
            activePlayer?.SetInputEnabled(true);
            activeCamera?.SetLookEnabled(true);
            SemiconPlayerController.SetCursorLocked(true);
            activePlayer = null;
            activeCamera = null;
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
