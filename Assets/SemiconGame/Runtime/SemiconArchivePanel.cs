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
        [SerializeField] private GameObject robotCollectionPage;
        [SerializeField] private Image[] robotCollectionImages;
        [SerializeField] private Text[] robotCollectionNames;
        [SerializeField] private Text[] robotCollectionStates;
        [SerializeField] private GameObject diskCollectionPage;
        [SerializeField] private Image[] diskCollectionImages;
        [SerializeField] private Text[] diskCollectionNames;
        [SerializeField] private Text[] diskCollectionStates;
        [SerializeField] private Button closeButton;

        private ArchiveTab selectedTab;
        private SemiconPlayerController activePlayer;
        private SemiconThirdPersonCamera activeCamera;
        private Coroutine animationRoutine;
        private bool isOpen;

        public void Configure(CanvasGroup group, RectTransform frame, Button[] tabs, Text code, Text title,
            Text summary, Text content, GameObject robotPage, Image[] robotImages, Text[] robotNames,
            Text[] robotStates, GameObject diskPage, Image[] diskImages, Text[] diskNames, Text[] diskStates,
            Button close)
        {
            panelGroup = group; panelFrame = frame; tabButtons = tabs; sectionCodeText = code;
            sectionTitleText = title; summaryText = summary; contentText = content;
            robotCollectionPage = robotPage; robotCollectionImages = robotImages;
            robotCollectionNames = robotNames; robotCollectionStates = robotStates;
            diskCollectionPage = diskPage; diskCollectionImages = diskImages;
            diskCollectionNames = diskNames; diskCollectionStates = diskStates;
            closeButton = close;
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
            var titles = new[] { "공정 도감", "제품 도감", "자재 도감", "작업 로봇", "디스크", "고객·계약" };
            if (sectionCodeText != null) sectionCodeText.text = $"ARCHIVE SECTION  /  {(int)selectedTab + 1:00}";
            if (sectionTitleText != null) sectionTitleText.text = titles[(int)selectedTab];
            if (summaryText != null)
                summaryText.text = $"PROCESS {state.UnlockedProcessCount} / 8     CONTRACT {state.CompletedContractKinds} / {SemiconContractCatalog.Count}     " +
                                   $"SAMPLE {state.CompletedSampleContractKinds} / 6";
            var showRobots = selectedTab == ArchiveTab.Personnel;
            var showDisks = selectedTab == ArchiveTab.Disk;
            robotCollectionPage?.SetActive(showRobots);
            diskCollectionPage?.SetActive(showDisks);
            if (contentText != null) contentText.gameObject.SetActive(!showRobots && !showDisks);
            if (contentText != null) contentText.text = selectedTab switch
            {
                ArchiveTab.Process => BuildProcessText(state),
                ArchiveTab.Product => BuildProductText(state),
                ArchiveTab.Material => BuildMaterialText(state),
                ArchiveTab.Personnel => BuildPersonnelText(state),
                ArchiveTab.Disk => BuildDiskText(state),
                _ => BuildClientText(state)
            };
            if (showRobots) RefreshRobotCollection(state);
            if (showDisks) RefreshDiskCollection(state);
            for (var index = 0; index < tabButtons.Length; index++)
            {
                var label = tabButtons[index]?.GetComponentInChildren<Text>(true);
                if (label != null) label.text = titles[index] + (index == (int)selectedTab ? "  ◀" : string.Empty);
                SemiconUiPalette.SetButtonSelection(tabButtons[index], index == (int)selectedTab);
            }
        }

        private void RefreshRobotCollection(SemiconGameState state)
        {
            var count = Mathf.Min(SemiconFactoryDefinitions.RobotCount,
                robotCollectionImages != null ? robotCollectionImages.Length : 0);
            for (var index = 0; index < count; index++)
            {
                var robot = SemiconFactoryDefinitions.GetRobotByCatalogIndex(index);
                var definition = SemiconFactoryDefinitions.GetRobot(robot);
                var owned = state.GetRobotOwnedCount(robot);
                var highest = state.GetHighestRobotEnhancement(robot);
                if (robotCollectionImages[index] != null)
                {
                    robotCollectionImages[index].sprite = SemiconGachaArt.GetRobotSprite(robot);
                    robotCollectionImages[index].color = owned > 0
                        ? Color.white
                        : new Color(0.42f, 0.5f, 0.53f, 0.72f);
                }
                if (robotCollectionNames != null && index < robotCollectionNames.Length &&
                    robotCollectionNames[index] != null)
                {
                    robotCollectionNames[index].text = $"{definition.Code}\n{definition.Name}";
                }
                if (robotCollectionStates != null && index < robotCollectionStates.Length &&
                    robotCollectionStates[index] != null)
                {
                    robotCollectionStates[index].text = owned > 0
                        ? $"{definition.Rarity}  ·  보유 {owned}  ·  {SemiconFactoryDefinitions.GetRobotEnhancementText(highest)}"
                        : $"{definition.Rarity}  ·  미보유";
                    robotCollectionStates[index].color = definition.Rarity switch
                    {
                        SemiconRobotRarity.SR => new Color32(184, 103, 0, 255),
                        SemiconRobotRarity.R => new Color32(16, 139, 194, 255),
                        _ => new Color32(76, 103, 119, 255)
                    };
                }
            }
        }

        private void RefreshDiskCollection(SemiconGameState state)
        {
            var count = Mathf.Min(9, diskCollectionImages != null ? diskCollectionImages.Length : 0);
            for (var index = 0; index < count; index++)
            {
                var disk = (SemiconDiskKind)(index % 3 + 1);
                var grade = (SemiconDiskGrade)(index / 3 + 1);
                var owned = state.GetDiskOwnedCount(disk, grade);
                if (diskCollectionImages[index] != null)
                {
                    diskCollectionImages[index].sprite = SemiconGachaArt.GetDiskSprite(disk, grade);
                    diskCollectionImages[index].color = owned > 0
                        ? Color.white
                        : new Color(0.42f, 0.5f, 0.53f, 0.72f);
                }
                if (diskCollectionNames != null && index < diskCollectionNames.Length &&
                    diskCollectionNames[index] != null)
                {
                    diskCollectionNames[index].text = SemiconFactoryDefinitions.GetDiskName(disk);
                }
                if (diskCollectionStates != null && index < diskCollectionStates.Length &&
                    diskCollectionStates[index] != null)
                {
                    diskCollectionStates[index].text = owned > 0
                        ? $"GRADE {grade}  ·  보유 {owned}  ·  {SemiconFactoryDefinitions.GetDiskBonus(disk, grade)}"
                        : $"GRADE {grade}  ·  미보유";
                    diskCollectionStates[index].color = grade switch
                    {
                        SemiconDiskGrade.III => new Color32(184, 103, 0, 255),
                        SemiconDiskGrade.II => new Color32(16, 139, 194, 255),
                        _ => new Color32(76, 103, 119, 255)
                    };
                }
            }
        }

        private static string BuildProcessText(SemiconGameState s)
        {
            var rows = new[]
            {
                $"01  WAFER       기초 웨이퍼     누적 {s.GetLifetimeProduced(SemiconRecipeKind.WaferSubstrate),4}  ·  STARTER RECIPE",
                ProcessRow(2, "OXIDATION", "온도 / 시간", s.OxidationRecipeQualified, s.GetLifetimeProduced(SemiconRecipeKind.OxidizedWafer), $"등록 {s.GetRecipeVariantCount(SemiconRecipeKind.OxidizedWafer)}개 · BEST {s.BestOxidationTemperature}°C · {s.BestOxidationTime}min · 균일도 {s.BestOxideUniformity:0.0}%", s),
                ProcessRow(3, "PHOTO", "노광량 / 초점", s.PhotoRecipeQualified, s.GetLifetimeProduced(SemiconRecipeKind.PhotoPatternedWafer), $"등록 {s.GetRecipeVariantCount(SemiconRecipeKind.PhotoPatternedWafer)}개 · BEST {s.BestDose}mJ · {s.BestFocus:+0.00;-0.00;0.00}μm · 수율 {s.BestYield:0.0}%", s),
                ProcessRow(4, "ETCH", "RF 파워 / 가스", s.EtchRecipeQualified, s.GetLifetimeProduced(SemiconRecipeKind.EtchedWafer), $"등록 {s.GetRecipeVariantCount(SemiconRecipeKind.EtchedWafer)}개 · BEST {s.BestEtchPower}W · {s.BestEtchGasFlow}sccm · 프로파일 {s.BestEtchProfile:0.0}%", s),
                ProcessRow(5, "DEPOSITION", "온도 / 압력", s.DepositionRecipeQualified, s.GetLifetimeProduced(SemiconRecipeKind.DepositedWafer), $"등록 {s.GetRecipeVariantCount(SemiconRecipeKind.DepositedWafer)}개 · BEST {s.BestDepositionTemperature}°C · {s.BestDepositionPressure}Torr · 균일도 {s.BestDepositionUniformity:0.0}%", s),
                ProcessRow(6, "METAL", "파워 / 시간", s.MetalRecipeQualified, s.GetLifetimeProduced(SemiconRecipeKind.MetalizedWafer), $"등록 {s.GetRecipeVariantCount(SemiconRecipeKind.MetalizedWafer)}개 · BEST {s.BestMetalPower}W · {s.BestMetalTime}s · 저항 {s.BestMetalResistance:0.000}Ω", s),
                ProcessRow(7, "EDS", "전압 / 누설 기준", s.EdsRecipeQualified, s.GetLifetimeProduced(SemiconRecipeKind.TestedWafer), $"등록 {s.GetRecipeVariantCount(SemiconRecipeKind.TestedWafer)}개 · BEST {s.BestEdsVoltage}V · {s.BestEdsLeakageThreshold}μA · 검출 {s.BestEdsDetection:0.0}%", s),
                ProcessRow(8, "PACKAGE", "본딩 / 몰딩 온도", s.PackageRecipeQualified, s.GetLifetimeProduced(SemiconRecipeKind.Sc01ControlSensor), $"등록 {s.GetRecipeVariantCount(SemiconRecipeKind.Sc01ControlSensor)}개 · BEST {s.BestPackageBondingForce}gf · {s.BestPackageMoldingTemperature}°C · 합격 {s.BestPackageFinalPass:0.0}%", s)
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
            var builder = new StringBuilder("OPERATION ROBOT DATABASE  /  보유 로봇과 현재 배치\n\n");
            for (var index = 0; index < SemiconFactoryDefinitions.SlotCount; index++)
            {
                var slot = s.GetFactorySlot(index);
                slot.EnsureCrewSlots();
                builder.AppendLine($"SLOT {index + 1:00}   {(slot.machineInstalled ? "MACHINE ONLINE" : "EMPTY SLOT")}");
                for (var crew = 0; crew < SemiconFactoryDefinitions.RobotsPerSlot; crew++)
                    builder.AppendLine($"  R{crew + 1}  {SemiconFactoryDefinitions.GetRobotName(slot.robots[crew])}  " +
                                       SemiconFactoryDefinitions.GetRobotEnhancementText(slot.robotEnhancements[crew]));
                builder.AppendLine();
            }
            builder.AppendLine("OWNED COLLECTION");
            for (var index = 0; index < SemiconFactoryDefinitions.RobotCount; index++)
            {
                var robot = SemiconFactoryDefinitions.GetRobotByCatalogIndex(index);
                var count = s.GetRobotOwnedCount(robot);
                if (count <= 0) continue;
                for (var level = 0; level <= SemiconFactoryDefinitions.MaxRobotEnhancement; level++)
                {
                    var levelCount = s.GetRobotOwnedCount(robot, level);
                    if (levelCount > 0) builder.AppendLine($"{SemiconFactoryDefinitions.GetRobotRarityText(robot),-2}  " +
                        $"{SemiconFactoryDefinitions.GetRobotName(robot),-24}  " +
                        $"{SemiconFactoryDefinitions.GetRobotEnhancementText(level),-4} × {levelCount}");
                }
            }
            return builder.ToString();
        }

        private static string BuildDiskText(SemiconGameState s)
        {
            var builder = new StringBuilder("TRAIT DISK ARCHIVE  /  보유 및 장착 현황\n\n");
            for (var index = 0; index < SemiconFactoryDefinitions.SlotCount; index++)
            {
                var slot = s.GetFactorySlot(index);
                slot.EnsureCrewSlots();
                builder.AppendLine($"SLOT {index + 1:00}");
                for (var crew = 0; crew < SemiconFactoryDefinitions.RobotsPerSlot; crew++)
                    builder.AppendLine($"  R{crew + 1}  {SemiconFactoryDefinitions.GetDiskName(slot.disks[crew], slot.diskGrades[crew])}");
                builder.AppendLine();
            }
            builder.AppendLine("OWNED COLLECTION");
            for (var kind = 1; kind <= 3; kind++)
            for (var grade = 1; grade <= 3; grade++)
            {
                var disk = (SemiconDiskKind)kind;
                var diskGrade = (SemiconDiskGrade)grade;
                var count = s.GetDiskOwnedCount(disk, diskGrade);
                if (count > 0) builder.AppendLine($"{SemiconFactoryDefinitions.GetDiskName(disk, diskGrade),-28}  × {count}");
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
