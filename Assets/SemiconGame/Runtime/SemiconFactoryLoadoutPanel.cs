using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    public sealed class SemiconFactoryLoadoutPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private RectTransform panelFrame;
        [SerializeField] private Text slotTitleText;
        [SerializeField] private Text machineStatusText;
        [SerializeField] private Image robotImage;
        [SerializeField] private Text workerNameText;
        [SerializeField] private Text workerBonusText;
        [SerializeField] private Image diskImage;
        [SerializeField] private Text diskNameText;
        [SerializeField] private Text diskBonusText;
        [SerializeField] private Text performanceText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button[] slotButtons;
        [SerializeField] private Button[] crewButtons;
        [SerializeField] private Button[] workerButtons;
        [SerializeField] private Button[] diskButtons;
        [SerializeField] private Button installButton;
        [SerializeField] private Button productionButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private SemiconProductionPanel productionPanel;
        [SerializeField] private SemiconHud hud;

        private SemiconPlayerController activePlayer;
        private SemiconThirdPersonCamera activeCamera;
        private Coroutine animationRoutine;
        private int selectedSlot;
        private int selectedCrew;
        private bool isOpen;
        private SemiconRobotKind previewRobot = SemiconRobotKind.None;
        private int previewRobotEnhancement;
        private SemiconDiskKind previewDisk = SemiconDiskKind.None;
        private SemiconDiskGrade previewDiskGrade = SemiconDiskGrade.None;

        private void Awake()
        {
            for (var index = 0; index < slotButtons.Length; index++)
            {
                var captured = index;
                slotButtons[index]?.onClick.AddListener(() => SelectSlot(captured));
            }
            for (var index = 0; index < crewButtons.Length; index++)
            {
                var captured = index;
                crewButtons[index]?.onClick.AddListener(() => SelectCrew(captured));
            }
            if (workerButtons.Length > 0) workerButtons[0]?.onClick.AddListener(() => CycleRobot(-1));
            if (workerButtons.Length > 1) workerButtons[1]?.onClick.AddListener(() => CycleRobot(1));
            if (workerButtons.Length > 2) workerButtons[2]?.onClick.AddListener(AssignRobot);
            if (workerButtons.Length > 3) workerButtons[3]?.onClick.AddListener(() => AssignRobot(SemiconRobotKind.None));
            if (diskButtons.Length > 0) diskButtons[0]?.onClick.AddListener(() => CycleDisk(-1));
            if (diskButtons.Length > 1) diskButtons[1]?.onClick.AddListener(() => CycleDisk(1));
            if (diskButtons.Length > 2) diskButtons[2]?.onClick.AddListener(AssignDisk);
            if (diskButtons.Length > 3) diskButtons[3]?.onClick.AddListener(() => AssignDisk(SemiconDiskKind.None,
                SemiconDiskGrade.None));

            installButton?.onClick.AddListener(InstallMachine);
            productionButton?.onClick.AddListener(OpenProduction);
            closeButton?.onClick.AddListener(Close);
            ApplySelectionButtonLayout(workerButtons);
            ApplySelectionButtonLayout(diskButtons);
            SetVisible(false);
            gameObject.SetActive(false);
        }

        private void Start()
        {
            if (SemiconGameState.Instance != null) SemiconGameState.Instance.StateChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (SemiconGameState.Instance != null) SemiconGameState.Instance.StateChanged -= Refresh;
        }

        private void Update()
        {
            if (isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        public void Configure(CanvasGroup group, RectTransform frame, Text slotTitle, Text machineStatus,
            Image robotPreview, Text workerName, Text workerBonus, Image diskPreview, Text diskName, Text diskBonus,
            Text performance, Text status, Button[] slots, Button[] workers, Button[] disks, Button install,
            Button production, Button close, SemiconProductionPanel targetProductionPanel, SemiconHud targetHud,
            Button[] crews = null)
        {
            panelGroup = group;
            panelFrame = frame;
            slotTitleText = slotTitle;
            machineStatusText = machineStatus;
            robotImage = robotPreview;
            workerNameText = workerName;
            workerBonusText = workerBonus;
            diskImage = diskPreview;
            diskNameText = diskName;
            diskBonusText = diskBonus;
            performanceText = performance;
            statusText = status;
            slotButtons = slots;
            crewButtons = crews ?? System.Array.Empty<Button>();
            workerButtons = workers;
            diskButtons = disks;
            installButton = install;
            productionButton = production;
            closeButton = close;
            productionPanel = targetProductionPanel;
            hud = targetHud;
        }

        public void Open(int slotIndex, SemiconPlayerController player, SemiconThirdPersonCamera followCamera)
        {
            if (isOpen) return;
            gameObject.SetActive(true);
            selectedSlot = Mathf.Clamp(slotIndex, 0, SemiconFactoryDefinitions.SlotCount - 1);
            selectedCrew = 0;
            activePlayer = player;
            activeCamera = followCamera;
            activePlayer?.SetInputEnabled(false);
            activeCamera?.SetLookEnabled(false);
            SemiconPlayerController.SetCursorLocked(false);
            isOpen = true;
            SyncPreviewToSlot();
            SetStatus("CONFIGURATION READY  /  보유 로봇과 디스크를 선택하세요.", SemiconUiPalette.Muted);
            Refresh();
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(AnimateOpen());
        }

        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(AnimateClose(true));
        }

        private void SelectSlot(int index)
        {
            selectedSlot = Mathf.Clamp(index, 0, SemiconFactoryDefinitions.SlotCount - 1);
            selectedCrew = 0;
            SyncPreviewToSlot();
            SetStatus($"SLOT {selectedSlot + 1:00} SELECTED  /  구성 정보를 불러왔습니다.", SemiconUiPalette.Blue);
            Refresh();
        }

        private void SelectCrew(int index)
        {
            selectedCrew = Mathf.Clamp(index, 0, SemiconFactoryDefinitions.RobotsPerSlot - 1);
            SyncPreviewToSlot();
            SetStatus($"ROBOT BAY {selectedCrew + 1} SELECTED  /  로봇과 디스크를 구성하세요.",
                SemiconUiPalette.Blue);
            Refresh();
        }

        private void SyncPreviewToSlot()
        {
            var state = SemiconGameState.Instance;
            var slot = state?.GetFactorySlot(selectedSlot);
            slot?.EnsureCrewSlots();
            if (slot != null && slot.robots[selectedCrew] != SemiconRobotKind.None)
            {
                previewRobot = slot.robots[selectedCrew];
                previewRobotEnhancement = slot.robotEnhancements[selectedCrew];
            }
            else
            {
                previewRobot = FindFirstOwnedRobot(out previewRobotEnhancement);
            }
            if (slot != null && slot.disks[selectedCrew] != SemiconDiskKind.None)
            {
                previewDisk = slot.disks[selectedCrew];
                previewDiskGrade = slot.diskGrades[selectedCrew];
            }
            else
            {
                FindFirstOwnedDisk(out previewDisk, out previewDiskGrade);
            }
        }

        private void InstallMachine()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            if (!state.TryInstallFactoryMachine(selectedSlot, out var reason))
            {
                SetStatus("INSTALL FAILED  /  " + reason, new Color32(238, 103, 89, 255));
                hud?.ShowToast(reason);
                return;
            }
            SetStatus($"INSTALL COMPLETE  /  SLOT {selectedSlot + 1:00}에 생산 설비를 배치했습니다.",
                SemiconUiPalette.Amber);
        }

        private void CycleRobot(int direction)
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            var levels = SemiconFactoryDefinitions.MaxRobotEnhancement + 1;
            var entryCount = SemiconFactoryDefinitions.RobotCount * levels;
            var start = previewRobot == SemiconRobotKind.None
                ? 0
                : ((int)previewRobot - 1) * levels + previewRobotEnhancement;
            for (var step = 1; step <= entryCount; step++)
            {
                var index = (start + direction * step) % entryCount;
                if (index < 0) index += entryCount;
                var candidate = SemiconFactoryDefinitions.GetRobotByCatalogIndex(index / levels);
                var enhancement = index % levels;
                if (state.GetRobotOwnedCount(candidate, enhancement) <= 0) continue;
                previewRobot = candidate;
                previewRobotEnhancement = enhancement;
                SetStatus("ROBOT SELECTED  /  " + SemiconFactoryDefinitions.GetRobotName(candidate) + "  " +
                          SemiconFactoryDefinitions.GetRobotEnhancementText(enhancement),
                    SemiconUiPalette.Blue);
                Refresh();
                return;
            }
            SetStatus("ROBOT EMPTY  /  워크스페이스 보급 센터에서 로봇을 모집하세요.", SemiconUiPalette.Amber);
        }

        private void CycleDisk(int direction)
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            var start = previewDisk == SemiconDiskKind.None ? 0 : ((int)previewDisk - 1) * 3 + (int)previewDiskGrade - 1;
            for (var step = 1; step <= 9; step++)
            {
                var index = (start + direction * step) % 9;
                if (index < 0) index += 9;
                var disk = (SemiconDiskKind)(index / 3 + 1);
                var grade = (SemiconDiskGrade)(index % 3 + 1);
                if (state.GetDiskOwnedCount(disk, grade) <= 0) continue;
                previewDisk = disk;
                previewDiskGrade = grade;
                SetStatus("DISK SELECTED  /  " + SemiconFactoryDefinitions.GetDiskName(disk, grade),
                    SemiconUiPalette.Blue);
                Refresh();
                return;
            }
            SetStatus("DISK EMPTY  /  워크스페이스 보급 센터에서 디스크를 추첨하세요.", SemiconUiPalette.Amber);
        }

        private SemiconRobotKind FindFirstOwnedRobot(out int enhancement)
        {
            var state = SemiconGameState.Instance;
            if (state == null)
            {
                enhancement = 0;
                return SemiconRobotKind.None;
            }
            for (var index = 0; index < SemiconFactoryDefinitions.RobotCount; index++)
            {
                var robot = SemiconFactoryDefinitions.GetRobotByCatalogIndex(index);
                for (var level = SemiconFactoryDefinitions.MaxRobotEnhancement; level >= 0; level--)
                {
                    if (state.GetRobotOwnedCount(robot, level) <= 0) continue;
                    enhancement = level;
                    return robot;
                }
            }
            enhancement = 0;
            return SemiconRobotKind.None;
        }

        private static void FindFirstOwnedDisk(out SemiconDiskKind disk, out SemiconDiskGrade grade)
        {
            var state = SemiconGameState.Instance;
            for (var kindIndex = 1; kindIndex <= 3; kindIndex++)
            for (var gradeIndex = 1; gradeIndex <= 3; gradeIndex++)
            {
                var candidateDisk = (SemiconDiskKind)kindIndex;
                var candidateGrade = (SemiconDiskGrade)gradeIndex;
                if (state != null && state.GetDiskOwnedCount(candidateDisk, candidateGrade) > 0)
                {
                    disk = candidateDisk;
                    grade = candidateGrade;
                    return;
                }
            }
            disk = SemiconDiskKind.None;
            grade = SemiconDiskGrade.None;
        }

        private void AssignRobot() => AssignRobot(previewRobot);

        private void AssignRobot(SemiconRobotKind robot)
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            if (!state.TryAssignRobot(selectedSlot, selectedCrew, robot, previewRobotEnhancement, out var reason))
            {
                SetStatus("ASSIGN FAILED  /  " + reason, new Color32(238, 103, 89, 255));
                hud?.ShowToast(reason);
                return;
            }
            if (robot == SemiconRobotKind.None) previewRobot = FindFirstOwnedRobot(out previewRobotEnhancement);
            SetStatus($"ROBOT BAY {selectedCrew + 1} UPDATED  /  " +
                      SemiconFactoryDefinitions.GetRobotName(robot), SemiconUiPalette.Mint);
        }

        private void AssignDisk() => AssignDisk(previewDisk, previewDiskGrade);

        private void AssignDisk(SemiconDiskKind disk, SemiconDiskGrade grade)
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            if (!state.TryAssignDisk(selectedSlot, selectedCrew, disk, grade, out var reason))
            {
                SetStatus("MODULE FAILED  /  " + reason, new Color32(238, 103, 89, 255));
                hud?.ShowToast(reason);
                return;
            }
            if (disk == SemiconDiskKind.None) FindFirstOwnedDisk(out previewDisk, out previewDiskGrade);
            SetStatus("MODULE UPDATED  /  " + SemiconFactoryDefinitions.GetDiskName(disk, grade),
                SemiconUiPalette.Mint);
        }

        private void OpenProduction()
        {
            var state = SemiconGameState.Instance;
            var slot = state?.GetFactorySlot(selectedSlot);
            if (slot == null || !slot.machineInstalled)
            {
                SetStatus("PRODUCTION LOCKED  /  먼저 생산 설비를 배치하세요.", new Color32(238, 103, 89, 255));
                return;
            }
            var player = activePlayer;
            var cameraController = activeCamera;
            isOpen = false;
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            SetVisible(false);
            activePlayer = null;
            activeCamera = null;
            gameObject.SetActive(false);
            productionPanel?.Open(player, cameraController, selectedSlot);
        }

        private void Refresh()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            var slot = state.GetFactorySlot(selectedSlot);
            if (slot == null) return;
            slot.EnsureCrewSlots();
            var stats = state.GetProductionStats(selectedSlot);
            var job = state.GetProductionJob(selectedSlot);

            if (slotTitleText != null) slotTitleText.text = $"SLOT {selectedSlot + 1:00}  /  PRODUCTION CELL";
            if (machineStatusText != null)
            {
                machineStatusText.text = slot.machineInstalled ? "EQUIPMENT ONLINE  /  설비 배치 완료" : "EMPTY SLOT  /  설비 미배치";
                machineStatusText.color = slot.machineInstalled ? SemiconUiPalette.Mint : SemiconUiPalette.Amber;
            }

            var selectedRobot = previewRobot;
            if (robotImage != null)
            {
                robotImage.sprite = SemiconGachaArt.GetRobotSprite(selectedRobot);
                robotImage.preserveAspect = true;
                robotImage.enabled = selectedRobot != SemiconRobotKind.None;
            }
            if (workerNameText != null)
            {
                var definition = SemiconFactoryDefinitions.GetRobot(selectedRobot);
                workerNameText.text = selectedRobot == SemiconRobotKind.None
                    ? "미배정"
                    : $"{definition.Name}  " +
                      SemiconFactoryDefinitions.GetRobotEnhancementText(previewRobotEnhancement);
            }
            if (workerBonusText != null)
            {
                workerBonusText.text = selectedRobot == SemiconRobotKind.None
                    ? "보유 로봇 없음"
                    : $"{SemiconFactoryDefinitions.GetRobot(selectedRobot).Code}  ·  " +
                      $"{SemiconFactoryDefinitions.GetRobotRole(selectedRobot)}\n" +
                      $"{SemiconFactoryDefinitions.GetRobotBonus(selectedRobot, previewRobotEnhancement)}\n" +
                      $"해당 단계 보유 {state.GetRobotOwnedCount(selectedRobot, previewRobotEnhancement)}대  /  " +
                      $"배치 {state.GetRobotAssignedCountAtLevel(selectedRobot, previewRobotEnhancement)}대";
            }

            if (diskImage != null)
            {
                diskImage.sprite = SemiconGachaArt.GetDiskSprite(previewDisk, previewDiskGrade);
                diskImage.preserveAspect = true;
                diskImage.enabled = previewDisk != SemiconDiskKind.None;
            }
            if (diskNameText != null) diskNameText.text = SemiconFactoryDefinitions.GetDiskName(previewDisk, previewDiskGrade);
            if (diskBonusText != null)
            {
                diskBonusText.text = previewDisk == SemiconDiskKind.None
                    ? "보유 디스크 없음"
                    : $"{SemiconFactoryDefinitions.GetDiskBonus(previewDisk, previewDiskGrade)}\n" +
                      $"보유 {state.GetDiskOwnedCount(previewDisk, previewDiskGrade)}개  /  장착 {state.GetDiskAssignedCount(previewDisk, previewDiskGrade)}개";
            }

            if (performanceText != null)
            {
                var jobText = !job.HasJob ? "대기 중" : job.IsComplete ? "완료품 회수 대기" : $"생산 중 {job.RemainingSeconds:0.0}s";
                var crewSummary = string.Empty;
                for (var crewIndex = 0; crewIndex < SemiconFactoryDefinitions.RobotsPerSlot; crewIndex++)
                {
                    var crewRobot = slot.robots[crewIndex];
                    var shortName = crewRobot == SemiconRobotKind.None
                        ? "빈 자리"
                        : SemiconFactoryDefinitions.GetRobot(crewRobot).Name + " " +
                          SemiconFactoryDefinitions.GetRobotEnhancementText(slot.robotEnhancements[crewIndex]);
                    crewSummary += $"R{crewIndex + 1}  {shortName}  ·  " +
                                   $"{SemiconFactoryDefinitions.GetDiskName(slot.disks[crewIndex], slot.diskGrades[crewIndex])}\n";
                }
                performanceText.text = crewSummary + "\n" +
                    $"생산 {stats.Production}%  ·  속도 {stats.Speed}%  ·  품질 {stats.Quality}\n" +
                    $"설비 {jobText}  ·  산출 {stats.OutputPerCycle} UNIT";
            }

            for (var index = 0; index < slotButtons.Length; index++)
            {
                var targetSlot = state.GetFactorySlot(index);
                SetButtonLabel(slotButtons[index],
                    $"{index + 1:00}  {(targetSlot != null && targetSlot.machineInstalled ? "ONLINE" : "EMPTY")}" +
                    (index == selectedSlot ? "    ◀" : string.Empty));
                SemiconUiPalette.SetButtonSelection(slotButtons[index], index == selectedSlot,
                    targetSlot == null || !targetSlot.machineInstalled);
            }

            for (var index = 0; index < crewButtons.Length; index++)
            {
                var robot = slot.robots[index];
                var label = robot == SemiconRobotKind.None
                    ? $"R{index + 1}  EMPTY"
                    : $"R{index + 1}  {SemiconFactoryDefinitions.GetRobot(robot).Name}  " +
                      SemiconFactoryDefinitions.GetRobotEnhancementText(slot.robotEnhancements[index]);
                SetButtonLabel(crewButtons[index], label + (index == selectedCrew ? "  ◀" : string.Empty));
                SemiconUiPalette.SetButtonSelection(crewButtons[index], index == selectedCrew,
                    robot == SemiconRobotKind.None);
            }

            var canEdit = slot.machineInstalled && !job.HasJob;
            var robotEntriesOwned = CountOwnedRobotEntries(state);
            if (workerButtons.Length > 0) workerButtons[0].interactable = canEdit && robotEntriesOwned > 1;
            if (workerButtons.Length > 1) workerButtons[1].interactable = canEdit && robotEntriesOwned > 1;
            if (workerButtons.Length > 2)
            {
                workerButtons[2].interactable = canEdit && previewRobot != SemiconRobotKind.None &&
                                                state.IsRobotAvailable(previewRobot, previewRobotEnhancement,
                                                    selectedSlot, selectedCrew);
                SetButtonLabel(workerButtons[2], previewRobot == slot.robots[selectedCrew] &&
                    previewRobotEnhancement == slot.robotEnhancements[selectedCrew]
                    ? "현재 배치 중  ✓"
                    : $"R{selectedCrew + 1}에 선택 로봇 배치  ▶");
            }
            if (workerButtons.Length > 3)
                workerButtons[3].interactable = canEdit && slot.robots[selectedCrew] != SemiconRobotKind.None;

            var diskKindsOwned = CountOwnedDiskKinds(state);
            if (diskButtons.Length > 0) diskButtons[0].interactable = canEdit && diskKindsOwned > 1;
            if (diskButtons.Length > 1) diskButtons[1].interactable = canEdit && diskKindsOwned > 1;
            if (diskButtons.Length > 2)
            {
                diskButtons[2].interactable = canEdit && previewDisk != SemiconDiskKind.None &&
                                              slot.robots[selectedCrew] != SemiconRobotKind.None &&
                                              state.IsDiskAvailable(previewDisk, previewDiskGrade, selectedSlot,
                                                  selectedCrew);
                SetButtonLabel(diskButtons[2], previewDisk == slot.disks[selectedCrew] &&
                                                       previewDiskGrade == slot.diskGrades[selectedCrew]
                    ? "현재 장착 중  ✓"
                    : $"R{selectedCrew + 1}에 디스크 장착  ▶");
            }
            if (diskButtons.Length > 3)
                diskButtons[3].interactable = canEdit && slot.disks[selectedCrew] != SemiconDiskKind.None;
            if (installButton != null)
            {
                installButton.gameObject.SetActive(!slot.machineInstalled);
                installButton.interactable = state.Credits >= SemiconFactoryDefinitions.MachineInstallPrice;
            }
            if (productionButton != null) productionButton.interactable = slot.machineInstalled;
        }

        private static int CountOwnedRobotEntries(SemiconGameState state)
        {
            var count = 0;
            for (var index = 0; index < SemiconFactoryDefinitions.RobotCount; index++)
            for (var enhancement = 0; enhancement <= SemiconFactoryDefinitions.MaxRobotEnhancement; enhancement++)
                if (state.GetRobotOwnedCount(SemiconFactoryDefinitions.GetRobotByCatalogIndex(index), enhancement) > 0)
                    count++;
            return count;
        }

        private static int CountOwnedDiskKinds(SemiconGameState state)
        {
            var count = 0;
            for (var kind = 1; kind <= 3; kind++)
            for (var grade = 1; grade <= 3; grade++)
                if (state.GetDiskOwnedCount((SemiconDiskKind)kind, (SemiconDiskGrade)grade) > 0) count++;
            return count;
        }

        private void SetStatus(string message, Color color)
        {
            if (statusText == null) return;
            statusText.text = message;
            statusText.color = color;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            var text = button != null ? button.GetComponentInChildren<Text>(true) : null;
            if (text != null) text.text = label;
        }

        private static void ApplySelectionButtonLayout(Button[] buttons)
        {
            if (buttons == null) return;
            for (var index = 0; index < buttons.Length; index++)
            {
                var button = buttons[index];
                if (button == null) continue;
                var rect = button.GetComponent<RectTransform>();
                var top = -202f - index * 70f;
                rect.offsetMin = new Vector2(rect.offsetMin.x, top - 54f);
                rect.offsetMax = new Vector2(rect.offsetMax.x, top);
            }
        }

        private IEnumerator AnimateOpen()
        {
            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.interactable = true;
                panelGroup.blocksRaycasts = true;
            }
            var start = new Vector2(60f, 0f);
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

        private IEnumerator AnimateClose(bool releaseControl)
        {
            var elapsed = 0f;
            const float duration = 0.16f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                if (panelGroup != null) panelGroup.alpha = 1f - t;
                if (panelFrame != null) panelFrame.anchoredPosition = Vector2.Lerp(Vector2.zero, new Vector2(60f, 0f), t);
                yield return null;
            }
            SetVisible(false);
            if (releaseControl)
            {
                activePlayer?.SetInputEnabled(true);
                activeCamera?.SetLookEnabled(true);
                SemiconPlayerController.SetCursorLocked(true);
            }
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
