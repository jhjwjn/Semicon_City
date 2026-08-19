using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    public sealed class DepositionExperimentPanel : MonoBehaviour
    {
        private const int ExperimentCost = SemiconGameState.ExperimentCreditCost;

        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private RectTransform panelFrame;
        [SerializeField] private Slider temperatureSlider;
        [SerializeField] private Slider pressureSlider;
        [SerializeField] private Text temperatureValueText;
        [SerializeField] private Text pressureValueText;
        [SerializeField] private Text thicknessValueText;
        [SerializeField] private Text uniformityValueText;
        [SerializeField] private Text coverageValueText;
        [SerializeField] private Text resultStatusText;
        [SerializeField] private Text recipeText;
        [SerializeField] private Text experimentCountText;
        [SerializeField] private Button runButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform depositionLine;
        [SerializeField] private SemiconHud hud;

        private SemiconPlayerController activePlayer;
        private SemiconThirdPersonCamera activeCamera;
        private Coroutine animationRoutine;
        private bool isOpen;
        private bool isRunning;

        private void Awake()
        {
            temperatureSlider?.onValueChanged.AddListener(_ => RefreshParameterLabels());
            pressureSlider?.onValueChanged.AddListener(_ => RefreshParameterLabels());
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

        public void Configure(CanvasGroup group, RectTransform frame, Slider temperature, Slider pressure,
            Text temperatureValue, Text pressureValue, Text thicknessValue, Text uniformityValue,
            Text coverageValue, Text status, Text recipe, Text experimentCount, Button run, Button close,
            RectTransform scan, SemiconHud targetHud)
        {
            panelGroup = group;
            panelFrame = frame;
            temperatureSlider = temperature;
            pressureSlider = pressure;
            temperatureValueText = temperatureValue;
            pressureValueText = pressureValue;
            thicknessValueText = thicknessValue;
            uniformityValueText = uniformityValue;
            coverageValueText = coverageValue;
            resultStatusText = status;
            recipeText = recipe;
            experimentCountText = experimentCount;
            runButton = run;
            closeButton = close;
            depositionLine = scan;
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
                resultStatusText.text = "CHAMBER ACTIVE  /  박막 성장 결과 분석 중";
                resultStatusText.color = SemiconUiPalette.Blue;
            }

            if (depositionLine != null)
            {
                var initialMax = depositionLine.anchorMax;
                initialMax.y = 0.12f;
                depositionLine.anchorMax = initialMax;
                depositionLine.gameObject.SetActive(true);
                var elapsed = 0f;
                const float duration = 0.82f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    var max = depositionLine.anchorMax;
                    max.y = Mathf.SmoothStep(0.12f, 1f, elapsed / duration);
                    depositionLine.anchorMax = max;
                    yield return null;
                }
                depositionLine.gameObject.SetActive(false);
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
            var temperature = Mathf.RoundToInt(temperatureSlider != null ? temperatureSlider.value : 400f);
            var pressure = Mathf.RoundToInt(pressureSlider != null ? pressureSlider.value : 6f);
            var thickness = 80f + (temperature - 400f) * 0.12f - (pressure - 6f) * 2.5f;
            var temperatureScore = Mathf.Clamp01(1f - Mathf.Abs(temperature - 400f) / 120f);
            var pressureScore = Mathf.Clamp01(1f - Mathf.Abs(pressure - 6f) / 5f);
            var uniformity = Mathf.Clamp(76f + temperatureScore * 12f + pressureScore * 10f, 55f, 98f);
            var coverage = Mathf.Clamp(80f + temperatureScore * 7f + pressureScore * 8f, 60f, 95f);
            var qualified = Mathf.Abs(thickness - 80f) <= 6f && uniformity >= 90f && coverage >= 88f;

            if (thicknessValueText != null) thicknessValueText.text = $"{thickness:0.0} nm";
            if (uniformityValueText != null) uniformityValueText.text = $"{uniformity:0.0}%";
            if (coverageValueText != null) coverageValueText.text = $"{coverage:0.0}%";
            if (resultStatusText != null)
            {
                resultStatusText.text = qualified
                    ? "RECIPE QUALIFIED  /  DEPO-01 양산 조건을 확보했습니다."
                    : uniformity >= 84f
                        ? "NEAR PROCESS WINDOW  /  목표 박막 두께에 더 가깝게 조정하세요."
                        : "OUT OF PROCESS WINDOW  /  온도와 챔버 압력을 다시 조정하세요.";
                resultStatusText.color = qualified
                    ? SemiconUiPalette.Amber
                    : uniformity >= 84f
                        ? SemiconUiPalette.Mint
                        : SemiconUiPalette.Danger;
            }

            SemiconGameState.Instance?.RecordDepositionExperiment(temperature, pressure, thickness, uniformity,
                coverage, qualified);
            RefreshRecipeArchive();
        }

        private void RefreshParameterLabels()
        {
            if (temperatureValueText != null && temperatureSlider != null)
                temperatureValueText.text = $"{Mathf.RoundToInt(temperatureSlider.value)} °C";
            if (pressureValueText != null && pressureSlider != null)
                pressureValueText.text = $"{Mathf.RoundToInt(pressureSlider.value)} Torr";
        }

        private void RefreshRecipeArchive()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            if (recipeText != null)
            {
                recipeText.text = state.DepositionExperimentCount == 0
                    ? "아직 저장된 증착 실험 데이터가 없습니다.\n첫 실험을 실행해 공정창을 탐색하세요."
                    : $"등록 레시피  {state.GetRecipeVariantCount(SemiconRecipeKind.DepositedWafer)}개\nBEST RUN  #{state.DepositionExperimentCount:00}\n\n증착 온도   {state.BestDepositionTemperature} °C\n챔버 압력   {state.BestDepositionPressure} Torr\n\n박막 두께   {state.BestDepositionThickness:0.0} nm\n균일도      {state.BestDepositionUniformity:0.0}%\n단차 피복성 {state.BestDepositionCoverage:0.0}%\n\n{(state.DepositionRecipeQualified ? "● 합격 조건은 각각 레시피로 저장됩니다" : "○ 안정 범위 탐색 중")}";
            }
            if (experimentCountText != null)
                experimentCountText.text = $"EXPERIMENT LOG  /  {state.DepositionExperimentCount:00}";
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
