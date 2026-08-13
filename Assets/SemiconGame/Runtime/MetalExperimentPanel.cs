using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    public sealed class MetalExperimentPanel : MonoBehaviour
    {
        private const int ExperimentCost = 8;

        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private RectTransform panelFrame;
        [SerializeField] private Slider powerSlider;
        [SerializeField] private Slider processTimeSlider;
        [SerializeField] private Text powerValueText;
        [SerializeField] private Text processTimeValueText;
        [SerializeField] private Text thicknessValueText;
        [SerializeField] private Text resistanceValueText;
        [SerializeField] private Text adhesionValueText;
        [SerializeField] private Text resultStatusText;
        [SerializeField] private Text recipeText;
        [SerializeField] private Text experimentCountText;
        [SerializeField] private Button runButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform metalScanLine;
        [SerializeField] private SemiconHud hud;

        private SemiconPlayerController activePlayer;
        private SemiconThirdPersonCamera activeCamera;
        private Coroutine animationRoutine;
        private bool isOpen;
        private bool isRunning;

        private void Awake()
        {
            powerSlider?.onValueChanged.AddListener(_ => RefreshParameterLabels());
            processTimeSlider?.onValueChanged.AddListener(_ => RefreshParameterLabels());
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

        public void Configure(CanvasGroup group, RectTransform frame, Slider power, Slider processTime,
            Text powerValue, Text processTimeValue, Text thicknessValue, Text resistanceValue,
            Text adhesionValue, Text status, Text recipe, Text experimentCount, Button run, Button close,
            RectTransform scanLine, SemiconHud targetHud)
        {
            panelGroup = group;
            panelFrame = frame;
            powerSlider = power;
            processTimeSlider = processTime;
            powerValueText = powerValue;
            processTimeValueText = processTimeValue;
            thicknessValueText = thicknessValue;
            resistanceValueText = resistanceValue;
            adhesionValueText = adhesionValue;
            resultStatusText = status;
            recipeText = recipe;
            experimentCountText = experimentCount;
            runButton = run;
            closeButton = close;
            metalScanLine = scanLine;
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
            if (state == null || !state.TrySpendResearch(ExperimentCost))
            {
                hud?.ShowToast($"연구 데이터가 부족합니다. 실험에는 {ExperimentCost}개가 필요합니다.");
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
                resultStatusText.text = "SPUTTER ACTIVE  /  금속 배선 전기 특성 분석 중";
                resultStatusText.color = new Color32(41, 211, 207, 255);
            }

            if (metalScanLine != null)
            {
                var initialMax = metalScanLine.anchorMax;
                initialMax.x = 0.08f;
                metalScanLine.anchorMax = initialMax;
                metalScanLine.gameObject.SetActive(true);
                var elapsed = 0f;
                const float duration = 0.82f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    var max = metalScanLine.anchorMax;
                    max.x = Mathf.SmoothStep(0.08f, 1f, elapsed / duration);
                    metalScanLine.anchorMax = max;
                    yield return null;
                }
                metalScanLine.gameObject.SetActive(false);
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
            var power = Mathf.RoundToInt(powerSlider != null ? powerSlider.value : 250f);
            var processTime = Mathf.RoundToInt(processTimeSlider != null ? processTimeSlider.value : 60f);
            var thickness = 450f + (power - 250f) * 0.65f + (processTime - 60f) * 2.2f;
            var powerScore = Mathf.Clamp01(1f - Mathf.Abs(power - 250f) / 120f);
            var timeScore = Mathf.Clamp01(1f - Mathf.Abs(processTime - 60f) / 40f);
            var resistance = Mathf.Clamp(0.155f - thickness * 0.00008f + (1f - powerScore) * 0.035f,
                0.08f, 0.22f);
            var adhesion = Mathf.Clamp(76f + powerScore * 12f + timeScore * 10f, 55f, 98f);
            var qualified = Mathf.Abs(thickness - 450f) <= 35f && resistance <= 0.13f && adhesion >= 90f;

            if (thicknessValueText != null) thicknessValueText.text = $"{thickness:0} nm";
            if (resistanceValueText != null) resistanceValueText.text = $"{resistance:0.000} Ω/□";
            if (adhesionValueText != null) adhesionValueText.text = $"{adhesion:0.0}%";
            if (resultStatusText != null)
            {
                resultStatusText.text = qualified
                    ? "RECIPE QUALIFIED  /  METAL-01 양산 조건을 확보했습니다."
                    : adhesion >= 84f
                        ? "NEAR PROCESS WINDOW  /  목표 배선 두께와 저항에 더 가깝게 조정하세요."
                        : "OUT OF PROCESS WINDOW  /  파워와 공정 시간을 다시 조정하세요.";
                resultStatusText.color = qualified
                    ? new Color32(247, 169, 30, 255)
                    : adhesion >= 84f
                        ? new Color32(41, 211, 207, 255)
                        : new Color32(238, 103, 89, 255);
            }

            SemiconGameState.Instance?.RecordMetalExperiment(power, processTime, thickness, resistance, adhesion,
                qualified);
            RefreshRecipeArchive();
        }

        private void RefreshParameterLabels()
        {
            if (powerValueText != null && powerSlider != null)
                powerValueText.text = $"{Mathf.RoundToInt(powerSlider.value)} W";
            if (processTimeValueText != null && processTimeSlider != null)
                processTimeValueText.text = $"{Mathf.RoundToInt(processTimeSlider.value)} sec";
        }

        private void RefreshRecipeArchive()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            if (recipeText != null)
            {
                recipeText.text = state.MetalExperimentCount == 0
                    ? "아직 저장된 금속 배선 실험 데이터가 없습니다.\n첫 실험을 실행해 공정창을 탐색하세요."
                    : $"BEST RUN  #{state.MetalExperimentCount:00}\n\n스퍼터 파워 {state.BestMetalPower} W\n공정 시간   {state.BestMetalTime} sec\n\n배선 두께   {state.BestMetalThickness:0} nm\n시트 저항   {state.BestMetalResistance:0.000} Ω/□\n접합 신뢰도 {state.BestMetalAdhesion:0.0}%\n\n{(state.MetalRecipeQualified ? "● METAL-01 레시피 등록 완료" : "○ 안정 범위 탐색 중")}";
            }
            if (experimentCountText != null)
                experimentCountText.text = $"EXPERIMENT LOG  /  {state.MetalExperimentCount:00}";
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
