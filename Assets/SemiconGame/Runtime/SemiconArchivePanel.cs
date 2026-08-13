using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    public sealed class SemiconArchivePanel : MonoBehaviour
    {
        private enum ArchiveTab { Process, Product, Material, Personnel, Disk, Client }

        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private RectTransform panelFrame;
        [SerializeField] private Button[] tabButtons;
        [SerializeField] private Text sectionCodeText;
        [SerializeField] private Text sectionTitleText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Text contentText;
        [SerializeField] private Button closeButton;

        private ArchiveTab selectedTab;
        private SemiconPlayerController activePlayer;
        private SemiconThirdPersonCamera activeCamera;
        private Coroutine animationRoutine;
        private bool isOpen;

        public void Configure(CanvasGroup group, RectTransform frame, Button[] tabs, Text code, Text title,
            Text summary, Text content, Button close)
        {
            panelGroup = group; panelFrame = frame; tabButtons = tabs; sectionCodeText = code;
            sectionTitleText = title; summaryText = summary; contentText = content; closeButton = close;
        }

        private void Awake()
        {
            for (var index = 0; index < tabButtons.Length; index++)
            {
                var captured = index;
                tabButtons[index]?.onClick.AddListener(() => { selectedTab = (ArchiveTab)captured; Refresh(); });
            }
            closeButton?.onClick.AddListener(Close);
            SetVisible(false); gameObject.SetActive(false);
        }

        private void Start()
        {
            if (SemiconGameState.Instance != null) SemiconGameState.Instance.StateChanged += Refresh;
        }

        private void OnDestroy()
        {
            if (SemiconGameState.Instance != null) SemiconGameState.Instance.StateChanged -= Refresh;
        }

        private void Update()
        {
            if (isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        public void Open(SemiconPlayerController player, SemiconThirdPersonCamera followCamera)
        {
            if (isOpen) return;
            gameObject.SetActive(true); activePlayer = player; activeCamera = followCamera;
            activePlayer?.SetInputEnabled(false); activeCamera?.SetLookEnabled(false);
            SemiconPlayerController.SetCursorLocked(false); isOpen = true; Refresh();
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

        private void Refresh()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            var titles = new[] { "공정 도감", "제품 도감", "자재 도감", "인력·로봇", "디스크", "고객·계약" };
            if (sectionCodeText != null) sectionCodeText.text = $"ARCHIVE SECTION  /  {(int)selectedTab + 1:00}";
            if (sectionTitleText != null) sectionTitleText.text = titles[(int)selectedTab];
            if (summaryText != null)
                summaryText.text = $"PROCESS {state.UnlockedProcessCount} / 8     CONTRACT {state.CompletedContractKinds} / {SemiconContractCatalog.Count}     " +
                                   $"SAMPLE {state.CompletedSampleContractKinds} / 6";
            if (contentText != null) contentText.text = selectedTab switch
            {
                ArchiveTab.Process => BuildProcessText(state),
                ArchiveTab.Product => BuildProductText(state),
                ArchiveTab.Material => BuildMaterialText(state),
                ArchiveTab.Personnel => BuildPersonnelText(state),
                ArchiveTab.Disk => BuildDiskText(state),
                _ => BuildClientText(state)
            };
            for (var index = 0; index < tabButtons.Length; index++)
            {
                var label = tabButtons[index]?.GetComponentInChildren<Text>(true);
                if (label != null) label.text = titles[index] + (index == (int)selectedTab ? "  ◀" : string.Empty);
                var graphic = tabButtons[index]?.targetGraphic;
                if (graphic != null) graphic.color = index == (int)selectedTab
                    ? new Color32(31, 190, 185, 255)
                    : new Color32(6, 72, 77, 255);
            }
        }

        private static string BuildProcessText(SemiconGameState s)
        {
            var rows = new[]
            {
                $"01  WAFER       기초 웨이퍼     누적 {s.GetLifetimeProduced(SemiconRecipeKind.WaferSubstrate),4}  ·  STARTER RECIPE",
                ProcessRow(2, "OXIDATION", "온도 / 시간", s.OxidationRecipeQualified, s.GetLifetimeProduced(SemiconRecipeKind.OxidizedWafer), $"BEST {s.BestOxidationTemperature}°C · {s.BestOxidationTime}min · 균일도 {s.BestOxideUniformity:0.0}%", s),
                ProcessRow(3, "PHOTO", "노광량 / 초점", s.PhotoRecipeQualified, s.GetLifetimeProduced(SemiconRecipeKind.PhotoPatternedWafer), $"BEST {s.BestDose}mJ · {s.BestFocus:+0.00;-0.00;0.00}μm · 수율 {s.BestYield:0.0}%", s),
                ProcessRow(4, "ETCH", "RF 파워 / 가스", s.EtchRecipeQualified, s.GetLifetimeProduced(SemiconRecipeKind.EtchedWafer), $"BEST {s.BestEtchPower}W · {s.BestEtchGasFlow}sccm · 프로파일 {s.BestEtchProfile:0.0}%", s),
                ProcessRow(5, "DEPOSITION", "온도 / 압력", s.DepositionRecipeQualified, s.GetLifetimeProduced(SemiconRecipeKind.DepositedWafer), $"BEST {s.BestDepositionTemperature}°C · {s.BestDepositionPressure}Torr · 균일도 {s.BestDepositionUniformity:0.0}%", s),
                ProcessRow(6, "METAL", "파워 / 시간", s.MetalRecipeQualified, s.GetLifetimeProduced(SemiconRecipeKind.MetalizedWafer), $"BEST {s.BestMetalPower}W · {s.BestMetalTime}s · 저항 {s.BestMetalResistance:0.000}Ω", s),
                ProcessRow(7, "EDS", "전압 / 누설 기준", s.EdsRecipeQualified, s.GetLifetimeProduced(SemiconRecipeKind.TestedWafer), $"BEST {s.BestEdsVoltage}V · {s.BestEdsLeakageThreshold}μA · 검출 {s.BestEdsDetection:0.0}%", s),
                ProcessRow(8, "PACKAGE", "본딩 / 몰딩 온도", s.PackageRecipeQualified, s.GetLifetimeProduced(SemiconRecipeKind.Sc01ControlSensor), $"BEST {s.BestPackageBondingForce}gf · {s.BestPackageMoldingTemperature}°C · 합격 {s.BestPackageFinalPass:0.0}%", s)
            };
            return string.Join("\n\n", rows);
        }

        private static string ProcessRow(int number, string name, string variables, bool qualified, int produced,
            string best, SemiconGameState state)
        {
            if (!state.IsProcessUnlocked(number)) return $"{number:00}  {name,-11}  LOCKED  ·  이전 공정품 생산 필요";
            return $"{number:00}  {name,-11}  {variables}  ·  누적 {produced}\n      {(qualified ? "RECIPE QUALIFIED" : "RESEARCHING")}  /  {best}";
        }

        private static string BuildProductText(SemiconGameState s)
        {
            return ProductRow("SC-01", "산업용 센서 제어칩", SemiconRecipeKind.Sc01ControlSensor, s.FinishedProductStock, s.AverageFinishedProductQuality, true, s) + "\n\n" +
                   ProductRow("PM-10", "전력 관리 IC", SemiconRecipeKind.Pm10PowerManagement, s.Pm10Stock, s.AveragePm10Quality, s.IsContractUnlocked(SemiconContractKind.Pm10PowerManagement), s) + "\n\n" +
                   ProductRow("DD-20", "디스플레이 드라이버", SemiconRecipeKind.Dd20DisplayDriver, s.Dd20Stock, s.AverageDd20Quality, s.IsContractUnlocked(SemiconContractKind.Dd20DisplayDriver), s);
        }

        private static string ProductRow(string code, string name, SemiconRecipeKind recipe, int stock, int quality,
            bool unlocked, SemiconGameState s)
        {
            return unlocked
                ? $"{code}  /  {name}\n현재 재고 {stock} UNIT  ·  최고 품질 {s.GetBestProducedQuality(recipe)}  ·  누적 생산 {s.GetLifetimeProduced(recipe)}"
                : $"{code}  /  UNKNOWN PRODUCT\n계약 도감을 진행하면 제품 사양이 공개됩니다.";
        }

        private static string BuildMaterialText(SemiconGameState s) =>
            $"MAT-SI-01   고순도 실리콘 잉곳      재고 {s.SiliconStock,4} EA   ·   WAFER\n\n" +
            $"MAT-GAS-02  특수가스 패키지          재고 {s.ProcessGasStock,4} EA   ·   OXIDE / ETCH / DEPO\n\n" +
            $"MAT-CHM-03  공정 약품                 재고 {s.ChemicalStock,4} EA   ·   PHOTO / PACKAGE\n\n" +
            $"MAT-MTL-04  배선 금속 타깃            재고 {s.MetalTargetStock,4} EA   ·   METAL";

        private static string BuildPersonnelText(SemiconGameState s)
        {
            var builder = new StringBuilder("PERSONNEL DATABASE  /  보유 인력과 현재 배치\n\n");
            for (var index = 0; index < SemiconFactoryDefinitions.SlotCount; index++)
            {
                var slot = s.GetFactorySlot(index);
                builder.AppendLine($"SLOT {index + 1:00}   {(slot.machineInstalled ? "MACHINE ONLINE" : "EMPTY SLOT")}");
                builder.AppendLine($"          {SemiconFactoryDefinitions.GetWorkerName(slot.worker)}");
                builder.AppendLine($"          {SemiconFactoryDefinitions.GetWorkerBonus(slot.worker)}\n");
            }
            return builder.ToString();
        }

        private static string BuildDiskText(SemiconGameState s)
        {
            var builder = new StringBuilder("ABILITY DISK ARCHIVE  /  설비 장착 현황\n\n");
            for (var index = 0; index < SemiconFactoryDefinitions.SlotCount; index++)
            {
                var slot = s.GetFactorySlot(index);
                builder.AppendLine($"SLOT {index + 1:00}   {SemiconFactoryDefinitions.GetDiskName(slot.disk)}");
                builder.AppendLine($"          {SemiconFactoryDefinitions.GetDiskBonus(slot.disk)}\n");
            }
            return builder.ToString();
        }

        private static string BuildClientText(SemiconGameState s)
        {
            var builder = new StringBuilder();
            for (var index = 0; index < SemiconContractCatalog.Count; index++)
            {
                var item = SemiconContractCatalog.GetAt(index);
                var unlocked = s.IsContractUnlocked(item.Kind);
                builder.AppendLine(unlocked
                    ? $"{item.Code,-7}  {item.Client,-14}  ·  완료 {s.GetContractCompletionCount(item.Kind)}회"
                    : $"{item.Code,-7}  UNKNOWN CLIENT       ·  LOCKED");
                builder.AppendLine(unlocked ? $"          {item.Name}  /  품질 {item.MinimumQuality}+\n" : string.Empty);
            }
            return builder.ToString();
        }

        private IEnumerator AnimateOpen()
        {
            panelGroup.alpha = 0f; panelGroup.interactable = true; panelGroup.blocksRaycasts = true;
            var elapsed = 0f;
            while (elapsed < 0.2f) { elapsed += Time.unscaledDeltaTime; panelGroup.alpha = Mathf.Clamp01(elapsed / 0.2f); yield return null; }
        }

        private IEnumerator AnimateClose()
        {
            var elapsed = 0f;
            while (elapsed < 0.16f) { elapsed += Time.unscaledDeltaTime; panelGroup.alpha = 1f - Mathf.Clamp01(elapsed / 0.16f); yield return null; }
            SetVisible(false); gameObject.SetActive(false);
            activePlayer?.SetInputEnabled(true); activeCamera?.SetLookEnabled(true);
            SemiconPlayerController.SetCursorLocked(true); activePlayer = null; activeCamera = null;
        }

        private void SetVisible(bool visible)
        {
            if (panelGroup == null) return;
            panelGroup.alpha = visible ? 1f : 0f; panelGroup.interactable = visible; panelGroup.blocksRaycasts = visible;
        }
    }
}
