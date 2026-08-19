using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    public sealed class PackageExperimentPanel : MonoBehaviour
    {
        private const int ExperimentCost = SemiconGameState.ExperimentCreditCost;

        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private RectTransform panelFrame;
        [SerializeField] private Slider bondingForceSlider;
        [SerializeField] private Slider moldingTemperatureSlider;
        [SerializeField] private Text bondingForceValueText;
        [SerializeField] private Text moldingTemperatureValueText;
        [SerializeField] private Text bondStrengthValueText;
        [SerializeField] private Text packageIntegrityValueText;
        [SerializeField] private Text finalPassValueText;
        [SerializeField] private Text resultStatusText;
        [SerializeField] private Text recipeText;
        [SerializeField] private Text experimentCountText;
        [SerializeField] private Button runButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform sealScanLine;
        [SerializeField] private SemiconHud hud;

        private SemiconPlayerController activePlayer;
        private SemiconThirdPersonCamera activeCamera;
        private Coroutine animationRoutine;
        private bool isOpen;
        private bool isRunning;

        private void Awake()
        {
            bondingForceSlider?.onValueChanged.AddListener(_ => RefreshParameterLabels());
            moldingTemperatureSlider?.onValueChanged.AddListener(_ => RefreshParameterLabels());
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

        public void Configure(CanvasGroup group, RectTransform frame, Slider bondingForce,
            Slider moldingTemperature, Text bondingForceValue, Text moldingTemperatureValue,
            Text bondStrengthValue, Text packageIntegrityValue, Text finalPassValue, Text status,
            Text recipe, Text experimentCount, Button run, Button close, RectTransform scanLine,
            SemiconHud targetHud)
        {
            panelGroup = group;
            panelFrame = frame;
            bondingForceSlider = bondingForce;
            moldingTemperatureSlider = moldingTemperature;
            bondingForceValueText = bondingForceValue;
            moldingTemperatureValueText = moldingTemperatureValue;
            bondStrengthValueText = bondStrengthValue;
            packageIntegrityValueText = packageIntegrityValue;
            finalPassValueText = finalPassValue;
            resultStatusText = status;
            recipeText = recipe;
            experimentCountText = experimentCount;
            runButton = run;
            closeButton = close;
            sealScanLine = scanLine;
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
                resultStatusText.text = "PACKAGE RELIABILITY TEST  /  본딩 및 몰딩 신뢰성 분석 중";
                resultStatusText.color = SemiconUiPalette.Blue;
            }

            if (sealScanLine != null)
            {
                sealScanLine.gameObject.SetActive(true);
                var elapsed = 0f;
                const float duration = 0.9f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                    sealScanLine.anchoredPosition = Vector2.Lerp(new Vector2(-150f, 0f),
                        new Vector2(150f, 0f), t);
                    yield return null;
                }
                sealScanLine.gameObject.SetActive(false);
            }
            else
            {
                yield return new WaitForSecondsRealtime(0.9f);
            }

            EvaluateExperiment();
            if (runButton != null) runButton.interactable = true;
            isRunning = false;
        }

        private void EvaluateExperiment()
        {
            var bondingForce = Mathf.RoundToInt(bondingForceSlider != null ? bondingForceSlider.value : 35f);
            var moldingTemperature = Mathf.RoundToInt(moldingTemperatureSlider != null
                ? moldingTemperatureSlider.value
                : 175f);
            var forceScore = Mathf.Clamp01(1f - Mathf.Abs(bondingForce - 35f) / 18f);
            var temperatureScore = Mathf.Clamp01(1f - Mathf.Abs(moldingTemperature - 175f) / 18f);
            var bondStrength = Mathf.Clamp(68f + forceScore * 28f, 45f, 96f);
            var packageIntegrity = Mathf.Clamp(72f + temperatureScore * 25f, 50f, 97f);
            var finalPass = Mathf.Clamp(72f + forceScore * 12f + temperatureScore * 13f, 50f, 97f);
            var qualified = bondStrength >= 90f && packageIntegrity >= 92f && finalPass >= 94f;

            if (bondStrengthValueText != null) bondStrengthValueText.text = $"{bondStrength:0.0}%";
            if (packageIntegrityValueText != null) packageIntegrityValueText.text = $"{packageIntegrity:0.0}%";
            if (finalPassValueText != null) finalPassValueText.text = $"{finalPass:0.0}%";
            if (resultStatusText != null)
            {
                resultStatusText.text = qualified
                    ? "RECIPE QUALIFIED  /  PACKAGE-01 양산 조건을 확보했습니다."
                    : finalPass >= 88f
                        ? "NEAR PROCESS WINDOW  /  본딩 강도와 몰딩 온도의 균형을 조정하세요."
                        : "OUT OF PROCESS WINDOW  /  패키지 균열과 접합 불량 위험이 높습니다.";
                resultStatusText.color = qualified
                    ? SemiconUiPalette.Amber
                    : finalPass >= 88f
                        ? SemiconUiPalette.Mint
                        : SemiconUiPalette.Danger;
            }

            SemiconGameState.Instance?.RecordPackageExperiment(bondingForce, moldingTemperature,
                bondStrength, packageIntegrity, finalPass, qualified);
            RefreshRecipeArchive();
        }

        private void RefreshParameterLabels()
        {
            if (bondingForceValueText != null && bondingForceSlider != null)
                bondingForceValueText.text = $"{Mathf.RoundToInt(bondingForceSlider.value)} gf";
            if (moldingTemperatureValueText != null && moldingTemperatureSlider != null)
                moldingTemperatureValueText.text = $"{Mathf.RoundToInt(moldingTemperatureSlider.value)} °C";
        }

        private void RefreshRecipeArchive()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            if (recipeText != null)
            {
                recipeText.text = state.PackageExperimentCount == 0
                    ? "아직 저장된 패키징 실험 데이터가 없습니다.\n첫 실험을 실행해 공정창을 탐색하세요."
                    : $"등록 레시피  {state.GetRecipeVariantCount(SemiconRecipeKind.Sc01ControlSensor)}개\nBEST RUN  #{state.PackageExperimentCount:00}\n\n본딩 압력   {state.BestPackageBondingForce} gf\n몰딩 온도   {state.BestPackageMoldingTemperature} °C\n\n본딩 강도   {state.BestPackageBondStrength:0.0}%\n패키지 무결성 {state.BestPackageIntegrity:0.0}%\n최종 합격률 {state.BestPackageFinalPass:0.0}%\n\n{(state.PackageRecipeQualified ? "● 합격 조건은 각각 레시피로 저장됩니다" : "○ 패키징 공정창 탐색 중")}";
            }
            if (experimentCountText != null)
                experimentCountText.text = $"EXPERIMENT LOG  /  {state.PackageExperimentCount:00}";
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
