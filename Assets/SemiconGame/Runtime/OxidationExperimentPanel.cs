using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    public sealed class OxidationExperimentPanel : MonoBehaviour
    {
        private const int ExperimentCost = 8;

        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private RectTransform panelFrame;
        [SerializeField] private Slider temperatureSlider;
        [SerializeField] private Slider processTimeSlider;
        [SerializeField] private Text temperatureValueText;
        [SerializeField] private Text processTimeValueText;
        [SerializeField] private Text thicknessValueText;
        [SerializeField] private Text uniformityValueText;
        [SerializeField] private Text defectValueText;
        [SerializeField] private Text resultStatusText;
        [SerializeField] private Text recipeText;
        [SerializeField] private Text experimentCountText;
        [SerializeField] private Button runButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform heatLine;
        [SerializeField] private SemiconHud hud;

        private SemiconPlayerController activePlayer;
        private SemiconThirdPersonCamera activeCamera;
        private Coroutine animationRoutine;
        private bool isOpen;
        private bool isRunning;

        private void Awake()
        {
            temperatureSlider?.onValueChanged.AddListener(_ => RefreshParameterLabels());
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
            if (isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public void Configure(CanvasGroup group, RectTransform frame, Slider temperature, Slider processTime,
            Text temperatureValue, Text processTimeValue, Text thicknessValue, Text uniformityValue,
            Text defectValue, Text status, Text recipe, Text experimentCount, Button run, Button close,
            RectTransform scan, SemiconHud targetHud)
        {
            panelGroup = group;
            panelFrame = frame;
            temperatureSlider = temperature;
            processTimeSlider = processTime;
            temperatureValueText = temperatureValue;
            processTimeValueText = processTimeValue;
            thicknessValueText = thicknessValue;
            uniformityValueText = uniformityValue;
            defectValueText = defectValue;
            resultStatusText = status;
            recipeText = recipe;
            experimentCountText = experimentCount;
            runButton = run;
            closeButton = close;
            heatLine = scan;
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
                resultStatusText.text = "FURNACE RUNNING  /  산화막 성장 결과 분석 중";
                resultStatusText.color = new Color32(41, 211, 207, 255);
            }

            if (heatLine != null)
            {
                var min = new Vector2(heatLine.anchoredPosition.x, -115f);
                var max = new Vector2(heatLine.anchoredPosition.x, 115f);
                heatLine.anchoredPosition = min;
                heatLine.gameObject.SetActive(true);
                var elapsed = 0f;
                const float duration = 0.82f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                    heatLine.anchoredPosition = Vector2.Lerp(min, max, t);
                    yield return null;
                }
                heatLine.gameObject.SetActive(false);
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
            var temperature = Mathf.RoundToInt(temperatureSlider != null ? temperatureSlider.value : 1000f);
            var processTime = Mathf.RoundToInt(processTimeSlider != null ? processTimeSlider.value : 60f);
            var thickness = 100f + (temperature - 1000f) * 0.18f + (processTime - 60f) * 1.1f;
            var temperatureScore = Mathf.Clamp01(1f - Mathf.Abs(temperature - 1000f) / 200f);
            var timeScore = Mathf.Clamp01(1f - Mathf.Abs(processTime - 60f) / 45f);
            var uniformity = Mathf.Clamp(76f + temperatureScore * 12f + timeScore * 10f, 55f, 98f);
            var defect = Mathf.Clamp(15.5f - uniformity * 0.12f + Mathf.Abs(thickness - 100f) * 0.10f,
                0.5f, 18f);
            var qualified = Mathf.Abs(thickness - 100f) <= 8f && uniformity >= 90f;

            if (thicknessValueText != null) thicknessValueText.text = $"{thickness:0.0} nm";
            if (uniformityValueText != null) uniformityValueText.text = $"{uniformity:0.0}%";
            if (defectValueText != null) defectValueText.text = $"{defect:0.0}%";
            if (resultStatusText != null)
            {
                resultStatusText.text = qualified
                    ? "RECIPE QUALIFIED  /  OXIDE-01 양산 조건을 확보했습니다."
                    : uniformity >= 84f
                        ? "NEAR PROCESS WINDOW  /  목표 막 두께에 더 가깝게 조정하세요."
                        : "OUT OF PROCESS WINDOW  /  온도와 산화 시간을 다시 조정하세요.";
                resultStatusText.color = qualified
                    ? new Color32(247, 169, 30, 255)
                    : uniformity >= 84f
                        ? new Color32(41, 211, 207, 255)
                        : new Color32(238, 103, 89, 255);
            }

            SemiconGameState.Instance?.RecordOxidationExperiment(temperature, processTime, thickness,
                uniformity, qualified);
            RefreshRecipeArchive();
        }

        private void RefreshParameterLabels()
        {
            if (temperatureValueText != null && temperatureSlider != null)
                temperatureValueText.text = $"{Mathf.RoundToInt(temperatureSlider.value)} °C";
            if (processTimeValueText != null && processTimeSlider != null)
                processTimeValueText.text = $"{Mathf.RoundToInt(processTimeSlider.value)} min";
        }

        private void RefreshRecipeArchive()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            if (recipeText != null)
            {
                recipeText.text = state.OxidationExperimentCount == 0
                    ? "아직 저장된 산화 실험 데이터가 없습니다.\n첫 실험을 실행해 공정창을 탐색하세요."
                    : $"BEST RUN  #{state.OxidationExperimentCount:00}\n\n온도      {state.BestOxidationTemperature} °C\n산화 시간   {state.BestOxidationTime} min\n\n막 두께     {state.BestOxideThickness:0.0} nm\n균일도      {state.BestOxideUniformity:0.0}%\n\n{(state.OxidationRecipeQualified ? "● OXIDE-01 레시피 등록 완료" : "○ 안정 범위 탐색 중")}";
            }
            if (experimentCountText != null)
                experimentCountText.text = $"EXPERIMENT LOG  /  {state.OxidationExperimentCount:00}";
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
