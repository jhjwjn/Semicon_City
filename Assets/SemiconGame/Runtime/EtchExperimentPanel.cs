using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    public sealed class EtchExperimentPanel : MonoBehaviour
    {
        private const int ExperimentCost = SemiconGameState.ExperimentCreditCost;

        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private RectTransform panelFrame;
        [SerializeField] private Slider powerSlider;
        [SerializeField] private Slider gasFlowSlider;
        [SerializeField] private Text powerValueText;
        [SerializeField] private Text gasFlowValueText;
        [SerializeField] private Text depthValueText;
        [SerializeField] private Text profileValueText;
        [SerializeField] private Text selectivityValueText;
        [SerializeField] private Text resultStatusText;
        [SerializeField] private Text recipeText;
        [SerializeField] private Text experimentCountText;
        [SerializeField] private Button runButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform plasmaLine;
        [SerializeField] private SemiconHud hud;

        private SemiconPlayerController activePlayer;
        private SemiconThirdPersonCamera activeCamera;
        private Coroutine animationRoutine;
        private bool isOpen;
        private bool isRunning;

        private void Awake()
        {
            powerSlider?.onValueChanged.AddListener(_ => RefreshParameterLabels());
            gasFlowSlider?.onValueChanged.AddListener(_ => RefreshParameterLabels());
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

        public void Configure(CanvasGroup group, RectTransform frame, Slider power, Slider gasFlow,
            Text powerValue, Text gasFlowValue, Text depthValue, Text profileValue, Text selectivityValue,
            Text status, Text recipe, Text experimentCount, Button run, Button close, RectTransform scan,
            SemiconHud targetHud)
        {
            panelGroup = group;
            panelFrame = frame;
            powerSlider = power;
            gasFlowSlider = gasFlow;
            powerValueText = powerValue;
            gasFlowValueText = gasFlowValue;
            depthValueText = depthValue;
            profileValueText = profileValue;
            selectivityValueText = selectivityValue;
            resultStatusText = status;
            recipeText = recipe;
            experimentCountText = experimentCount;
            runButton = run;
            closeButton = close;
            plasmaLine = scan;
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
                resultStatusText.text = "PLASMA ACTIVE  /  식각 단면 분석 중";
                resultStatusText.color = SemiconUiPalette.Blue;
            }

            if (plasmaLine != null)
            {
                plasmaLine.gameObject.SetActive(true);
                var elapsed = 0f;
                const float duration = 0.82f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    var pulse = Mathf.PingPong(elapsed * 5f, 1f);
                    var scale = plasmaLine.localScale;
                    scale.y = Mathf.Lerp(0.25f, 1f, pulse);
                    plasmaLine.localScale = scale;
                    yield return null;
                }
                plasmaLine.localScale = Vector3.one;
                plasmaLine.gameObject.SetActive(false);
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
            var gasFlow = Mathf.RoundToInt(gasFlowSlider != null ? gasFlowSlider.value : 60f);
            var depth = 120f + (power - 250f) * 0.22f - (gasFlow - 60f) * 0.45f;
            var powerScore = Mathf.Clamp01(1f - Mathf.Abs(power - 250f) / 130f);
            var gasScore = Mathf.Clamp01(1f - Mathf.Abs(gasFlow - 60f) / 45f);
            var profile = Mathf.Clamp(76f + powerScore * 12f + gasScore * 10f, 55f, 98f);
            var selectivity = 2.8f + powerScore * 0.9f + gasScore * 0.8f;
            var qualified = Mathf.Abs(depth - 120f) <= 8f && profile >= 90f;

            if (depthValueText != null) depthValueText.text = $"{depth:0.0} nm";
            if (profileValueText != null) profileValueText.text = $"{profile:0.0}%";
            if (selectivityValueText != null) selectivityValueText.text = $"{selectivity:0.0} : 1";
            if (resultStatusText != null)
            {
                resultStatusText.text = qualified
                    ? "RECIPE QUALIFIED  /  ETCH-01 양산 조건을 확보했습니다."
                    : profile >= 84f
                        ? "NEAR PROCESS WINDOW  /  목표 식각 깊이에 더 가깝게 조정하세요."
                        : "OUT OF PROCESS WINDOW  /  RF 파워와 가스 유량을 다시 조정하세요.";
                resultStatusText.color = qualified
                    ? SemiconUiPalette.Amber
                    : profile >= 84f
                        ? SemiconUiPalette.Mint
                        : SemiconUiPalette.Danger;
            }

            SemiconGameState.Instance?.RecordEtchExperiment(power, gasFlow, depth, profile, qualified);
            RefreshRecipeArchive();
        }

        private void RefreshParameterLabels()
        {
            if (powerValueText != null && powerSlider != null)
                powerValueText.text = $"{Mathf.RoundToInt(powerSlider.value)} W";
            if (gasFlowValueText != null && gasFlowSlider != null)
                gasFlowValueText.text = $"{Mathf.RoundToInt(gasFlowSlider.value)} sccm";
        }

        private void RefreshRecipeArchive()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            if (recipeText != null)
            {
                recipeText.text = state.EtchExperimentCount == 0
                    ? "아직 저장된 식각 실험 데이터가 없습니다.\n첫 실험을 실행해 공정창을 탐색하세요."
                    : $"등록 레시피  {state.GetRecipeVariantCount(SemiconRecipeKind.EtchedWafer)}개\nBEST RUN  #{state.EtchExperimentCount:00}\n\nRF 파워    {state.BestEtchPower} W\n가스 유량   {state.BestEtchGasFlow} sccm\n\n식각 깊이   {state.BestEtchDepth:0.0} nm\n측벽 정밀도 {state.BestEtchProfile:0.0}%\n\n{(state.EtchRecipeQualified ? "● 합격 조건은 각각 레시피로 저장됩니다" : "○ 안정 범위 탐색 중")}";
            }
            if (experimentCountText != null)
                experimentCountText.text = $"EXPERIMENT LOG  /  {state.EtchExperimentCount:00}";
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
