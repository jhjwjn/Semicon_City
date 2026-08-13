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
        [SerializeField] private Text workerNameText;
        [SerializeField] private Text workerBonusText;
        [SerializeField] private Text diskNameText;
        [SerializeField] private Text diskBonusText;
        [SerializeField] private Text performanceText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button[] slotButtons;
        [SerializeField] private Button[] workerButtons;
        [SerializeField] private Button[] diskButtons;
        [SerializeField] private Button installButton;
        [SerializeField] private Button productionButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private SemiconProductionPanel productionPanel;
        [SerializeField] private SemiconHud hud;

        private static readonly SemiconWorkerKind[] WorkerKinds =
        {
            SemiconWorkerKind.Mina,
            SemiconWorkerKind.Rex,
            SemiconWorkerKind.Bo7,
            SemiconWorkerKind.None
        };

        private static readonly SemiconDiskKind[] DiskKinds =
        {
            SemiconDiskKind.Production,
            SemiconDiskKind.Speed,
            SemiconDiskKind.Quality,
            SemiconDiskKind.None
        };

        private SemiconPlayerController activePlayer;
        private SemiconThirdPersonCamera activeCamera;
        private Coroutine animationRoutine;
        private int selectedSlot;
        private bool isOpen;

        private void Awake()
        {
            for (var index = 0; index < slotButtons.Length; index++)
            {
                var captured = index;
                slotButtons[index]?.onClick.AddListener(() => SelectSlot(captured));
            }
            for (var index = 0; index < workerButtons.Length && index < WorkerKinds.Length; index++)
            {
                var captured = WorkerKinds[index];
                workerButtons[index]?.onClick.AddListener(() => AssignWorker(captured));
            }
            for (var index = 0; index < diskButtons.Length && index < DiskKinds.Length; index++)
            {
                var captured = DiskKinds[index];
                diskButtons[index]?.onClick.AddListener(() => AssignDisk(captured));
            }

            installButton?.onClick.AddListener(InstallMachine);
            productionButton?.onClick.AddListener(OpenProduction);
            closeButton?.onClick.AddListener(Close);
            SetVisible(false);
            gameObject.SetActive(false);
        }

        private void Start()
        {
            if (SemiconGameState.Instance != null)
            {
                SemiconGameState.Instance.StateChanged += Refresh;
            }
            Refresh();
        }

        private void OnDestroy()
        {
            if (SemiconGameState.Instance != null)
            {
                SemiconGameState.Instance.StateChanged -= Refresh;
            }
        }

        private void Update()
        {
            if (isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public void Configure(CanvasGroup group, RectTransform frame, Text slotTitle, Text machineStatus,
            Text workerName, Text workerBonus, Text diskName, Text diskBonus, Text performance, Text status,
            Button[] slots, Button[] workers, Button[] disks, Button install, Button production, Button close,
            SemiconProductionPanel targetProductionPanel, SemiconHud targetHud)
        {
            panelGroup = group;
            panelFrame = frame;
            slotTitleText = slotTitle;
            machineStatusText = machineStatus;
            workerNameText = workerName;
            workerBonusText = workerBonus;
            diskNameText = diskName;
            diskBonusText = diskBonus;
            performanceText = performance;
            statusText = status;
            slotButtons = slots;
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
            activePlayer = player;
            activeCamera = followCamera;
            activePlayer?.SetInputEnabled(false);
            activeCamera?.SetLookEnabled(false);
            SemiconPlayerController.SetCursorLocked(false);
            isOpen = true;
            SetStatus("CONFIGURATION READY  /  설비·인력·디스크를 선택하세요.", new Color32(134, 164, 168, 255));
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
            SetStatus($"SLOT {selectedSlot + 1:00} SELECTED  /  구성 정보를 불러왔습니다.", new Color32(41, 211, 207, 255));
            Refresh();
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
            SetStatus($"INSTALL COMPLETE  /  SLOT {selectedSlot + 1:00}에 SC-01 설비를 배치했습니다.",
                new Color32(247, 169, 30, 255));
        }

        private void AssignWorker(SemiconWorkerKind worker)
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            if (!state.TryAssignWorker(selectedSlot, worker, out var reason))
            {
                SetStatus("ASSIGN FAILED  /  " + reason, new Color32(238, 103, 89, 255));
                hud?.ShowToast(reason);
                return;
            }
            SetStatus("PERSONNEL UPDATED  /  " + SemiconFactoryDefinitions.GetWorkerName(worker),
                new Color32(41, 211, 207, 255));
        }

        private void AssignDisk(SemiconDiskKind disk)
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            if (!state.TryAssignDisk(selectedSlot, disk, out var reason))
            {
                SetStatus("MODULE FAILED  /  " + reason, new Color32(238, 103, 89, 255));
                hud?.ShowToast(reason);
                return;
            }
            SetStatus("MODULE UPDATED  /  " + SemiconFactoryDefinitions.GetDiskName(disk),
                new Color32(41, 211, 207, 255));
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
            var stats = state.GetProductionStats(selectedSlot);
            var job = state.GetProductionJob(selectedSlot);

            if (slotTitleText != null) slotTitleText.text = $"SLOT {selectedSlot + 1:00}  /  SC-01 ASSEMBLY CELL";
            if (machineStatusText != null)
            {
                machineStatusText.text = slot.machineInstalled ? "EQUIPMENT ONLINE  /  설비 배치 완료" : "EMPTY SLOT  /  설비 미배치";
                machineStatusText.color = slot.machineInstalled
                    ? new Color32(41, 211, 207, 255)
                    : new Color32(247, 169, 30, 255);
            }
            if (workerNameText != null) workerNameText.text = SemiconFactoryDefinitions.GetWorkerName(slot.worker);
            if (workerBonusText != null) workerBonusText.text = SemiconFactoryDefinitions.GetWorkerBonus(slot.worker);
            if (diskNameText != null) diskNameText.text = SemiconFactoryDefinitions.GetDiskName(slot.disk);
            if (diskBonusText != null) diskBonusText.text = SemiconFactoryDefinitions.GetDiskBonus(slot.disk);
            if (performanceText != null)
            {
                var jobText = !job.HasJob ? "대기 중" : job.IsComplete ? "완료품 회수 대기" : $"생산 중 {job.RemainingSeconds:0.0}s";
                performanceText.text = $"생산 효율    {stats.Production}%\n작업 속도    {stats.Speed}%\n품질 지수    {stats.Quality}\n\n설비 상태    {jobText}\n사이클 산출  {stats.OutputPerCycle} UNIT";
            }

            for (var index = 0; index < slotButtons.Length; index++)
            {
                var targetSlot = state.GetFactorySlot(index);
                SetButtonLabel(slotButtons[index],
                    $"{index + 1:00}  {(targetSlot != null && targetSlot.machineInstalled ? "ONLINE" : "EMPTY")}" +
                    (index == selectedSlot ? "    ◀" : string.Empty));
            }
            for (var index = 0; index < workerButtons.Length && index < WorkerKinds.Length; index++)
            {
                workerButtons[index].interactable = slot.machineInstalled && !job.HasJob &&
                                                    state.IsWorkerAvailable(WorkerKinds[index], selectedSlot);
            }
            for (var index = 0; index < diskButtons.Length && index < DiskKinds.Length; index++)
            {
                diskButtons[index].interactable = slot.machineInstalled && !job.HasJob &&
                                                  state.IsDiskAvailable(DiskKinds[index], selectedSlot);
            }
            if (installButton != null)
            {
                installButton.gameObject.SetActive(!slot.machineInstalled);
                installButton.interactable = state.Credits >= SemiconFactoryDefinitions.MachineInstallPrice;
            }
            if (productionButton != null) productionButton.interactable = slot.machineInstalled;
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
