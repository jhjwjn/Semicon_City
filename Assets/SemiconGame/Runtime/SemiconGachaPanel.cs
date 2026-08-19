using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    public sealed class SemiconGachaPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private RectTransform panelFrame;
        [SerializeField] private Text creditsText;
        [SerializeField] private Text collectionText;
        [SerializeField] private GameObject robotPage;
        [SerializeField] private GameObject diskPage;
        [SerializeField] private Button robotTabButton;
        [SerializeField] private Button diskTabButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button singleDrawButton;
        [SerializeField] private Button tenDrawButton;
        [SerializeField] private Button[] robotCardButtons;
        [SerializeField] private Image[] robotCardImages;
        [SerializeField] private Text[] robotCardLabels;
        [SerializeField] private Text[] robotOwnedLabels;
        [SerializeField] private Button[] diskCardButtons;
        [SerializeField] private Image[] diskCardImages;
        [SerializeField] private Text[] diskCardLabels;
        [SerializeField] private Text[] diskOwnedLabels;
        [SerializeField] private Image detailImage;
        [SerializeField] private Text detailRarityText;
        [SerializeField] private Text detailNameText;
        [SerializeField] private Text detailRoleText;
        [SerializeField] private Text detailBonusText;
        [SerializeField] private Text detailOwnedText;
        [SerializeField] private Text rateInfoText;
        [SerializeField] private Text guaranteeText;
        [SerializeField] private Text statusText;
        [SerializeField] private GameObject resultOverlay;
        [SerializeField] private Text resultTitleText;
        [SerializeField] private Text resultSummaryText;
        [SerializeField] private Button resultCloseButton;
        [SerializeField] private Button resultTopCloseButton;
        [SerializeField] private Button resultRepeatButton;
        [SerializeField] private GameObject[] resultCards;
        [SerializeField] private Image[] resultImages;
        [SerializeField] private Text[] resultNameTexts;
        [SerializeField] private Text[] resultGradeTexts;
        [SerializeField] private Text[] resultStateTexts;
        [SerializeField] private SemiconHud hud;

        private SemiconPlayerController activePlayer;
        private SemiconThirdPersonCamera activeCamera;
        private Coroutine animationRoutine;
        private bool robotMode = true;
        private bool isOpen;
        private SemiconRobotKind selectedRobot = SemiconRobotKind.Zenith15;
        private SemiconDiskKind selectedDisk = SemiconDiskKind.Quality;
        private SemiconDiskGrade selectedGrade = SemiconDiskGrade.III;
        private int lastDrawCount = 10;

        private void Awake()
        {
            robotTabButton?.onClick.AddListener(() => SetMode(true));
            diskTabButton?.onClick.AddListener(() => SetMode(false));
            closeButton?.onClick.AddListener(Close);
            singleDrawButton?.onClick.AddListener(() => Draw(1));
            tenDrawButton?.onClick.AddListener(() => Draw(10));
            resultCloseButton?.onClick.AddListener(HideResults);
            resultTopCloseButton?.onClick.AddListener(HideResults);
            resultRepeatButton?.onClick.AddListener(() => Draw(lastDrawCount));

            for (var index = 0; index < robotCardButtons.Length; index++)
            {
                var robot = SemiconFactoryDefinitions.GetRobotByCatalogIndex(index);
                robotCardButtons[index]?.onClick.AddListener(() => SelectRobot(robot));
            }
            for (var index = 0; index < diskCardButtons.Length; index++)
            {
                var disk = (SemiconDiskKind)(index % 3 + 1);
                var grade = (SemiconDiskGrade)(index / 3 + 1);
                diskCardButtons[index]?.onClick.AddListener(() => SelectDisk(disk, grade));
            }

            SetVisible(false);
            if (resultOverlay != null) resultOverlay.SetActive(false);
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
            if (!isOpen || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (resultOverlay != null && resultOverlay.activeSelf) HideResults();
            else Close();
        }

        public void Configure(CanvasGroup group, RectTransform frame, Text credits, Text collection,
            GameObject robots, GameObject disks, Button robotTab, Button diskTab, Button close, Button singleDraw,
            Button tenDraw, Button[] robotButtons, Image[] robotImages, Text[] robotLabels, Text[] robotOwned,
            Button[] diskButtons, Image[] diskImages, Text[] diskLabels, Text[] diskOwned, Image detailsImage,
            Text detailsRarity, Text detailsName, Text detailsRole, Text detailsBonus, Text detailsOwned, Text status,
            GameObject results, Text resultsTitle, Button resultsClose, GameObject[] resultsCards,
            Image[] resultsImages, Text[] resultsNames, Text[] resultsGrades, SemiconHud targetHud,
            Text resultsSummary = null, Button resultsRepeat = null, Text[] resultsStates = null,
            Text rates = null, Text guarantee = null, Button resultsTopClose = null)
        {
            panelGroup = group;
            panelFrame = frame;
            creditsText = credits;
            collectionText = collection;
            robotPage = robots;
            diskPage = disks;
            robotTabButton = robotTab;
            diskTabButton = diskTab;
            closeButton = close;
            singleDrawButton = singleDraw;
            tenDrawButton = tenDraw;
            robotCardButtons = robotButtons;
            robotCardImages = robotImages;
            robotCardLabels = robotLabels;
            robotOwnedLabels = robotOwned;
            diskCardButtons = diskButtons;
            diskCardImages = diskImages;
            diskCardLabels = diskLabels;
            diskOwnedLabels = diskOwned;
            detailImage = detailsImage;
            detailRarityText = detailsRarity;
            detailNameText = detailsName;
            detailRoleText = detailsRole;
            detailBonusText = detailsBonus;
            detailOwnedText = detailsOwned;
            rateInfoText = rates;
            guaranteeText = guarantee;
            statusText = status;
            resultOverlay = results;
            resultTitleText = resultsTitle;
            resultSummaryText = resultsSummary;
            resultCloseButton = resultsClose;
            resultTopCloseButton = resultsTopClose;
            resultRepeatButton = resultsRepeat;
            resultCards = resultsCards;
            resultImages = resultsImages;
            resultNameTexts = resultsNames;
            resultGradeTexts = resultsGrades;
            resultStateTexts = resultsStates;
            hud = targetHud;
        }

        public void Open(SemiconPlayerController player, SemiconThirdPersonCamera followCamera)
        {
            if (isOpen) return;
            gameObject.SetActive(true);
            activePlayer = player;
            activeCamera = followCamera;
            activePlayer?.SetInputEnabled(false);
            activeCamera?.SetLookEnabled(false);
            SemiconPlayerController.SetCursorLocked(false);
            isOpen = true;
            SetStatus("원하는 보급을 선택하세요.", SemiconUiPalette.Muted);
            Refresh();
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(AnimateOpen());
        }

        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            HideResults();
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(AnimateClose());
        }

        private void SetMode(bool showRobots)
        {
            robotMode = showRobots;
            SetStatus(showRobots
                ? "로봇 모집  ·  SR 10%  ·  10회 R 이상 확정"
                : "디스크 추첨  ·  III 10%  ·  10회 II 이상 확정", SemiconUiPalette.Blue);
            Refresh();
        }

        private void SelectRobot(SemiconRobotKind robot)
        {
            selectedRobot = robot;
            RefreshDetails();
        }

        private void SelectDisk(SemiconDiskKind disk, SemiconDiskGrade grade)
        {
            selectedDisk = disk;
            selectedGrade = grade;
            RefreshDetails();
        }

        private void Draw(int count)
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            var previousRobotCounts = new int[SemiconFactoryDefinitions.RobotCount + 1];
            var previousDiskCounts = new int[40];
            if (robotMode)
            {
                for (var index = 0; index < SemiconFactoryDefinitions.RobotCount; index++)
                {
                    var robot = SemiconFactoryDefinitions.GetRobotByCatalogIndex(index);
                    previousRobotCounts[(int)robot] = state.GetRobotOwnedCount(robot);
                }
            }
            else
            {
                for (var disk = 1; disk <= 3; disk++)
                for (var grade = 1; grade <= 3; grade++)
                {
                    previousDiskCounts[disk * 10 + grade] = state.GetDiskOwnedCount(
                        (SemiconDiskKind)disk, (SemiconDiskGrade)grade);
                }
            }
            SemiconGachaReward[] rewards;
            string reason;
            var success = robotMode
                ? state.TryDrawRobots(count, out rewards, out reason)
                : state.TryDrawDisks(count, out rewards, out reason);
            if (!success)
            {
                SetStatus("모집 실패  ·  " + reason, new Color32(196, 71, 65, 255));
                hud?.ShowToast(reason);
                return;
            }

            lastDrawCount = count;
            var isNew = new bool[rewards.Length];
            for (var index = 0; index < rewards.Length; index++)
            {
                var reward = rewards[index];
                if (reward.IsRobot)
                {
                    var key = (int)reward.Robot;
                    isNew[index] = previousRobotCounts[key] == 0;
                    previousRobotCounts[key]++;
                }
                else
                {
                    var key = (int)reward.Disk * 10 + (int)reward.Grade;
                    isNew[index] = previousDiskCounts[key] == 0;
                    previousDiskCounts[key]++;
                }
            }

            var upgrades = 0;
            for (var index = 0; index < rewards.Length; index++)
                if (rewards[index].UpgradeTriggered) upgrades++;
            SetStatus(upgrades > 0
                ? $"자동 합성 완료  ·  로봇 강화 {upgrades}회"
                : count == 10 ? "10회 모집 완료" : "1회 모집 완료",
                SemiconUiPalette.Mint);
            ShowResults(rewards, isNew);
        }

        private void ShowResults(SemiconGachaReward[] rewards, bool[] isNew)
        {
            if (resultOverlay == null) return;
            resultOverlay.SetActive(true);
            if (resultTitleText != null)
                resultTitleText.text = rewards.Length == 10 ? "10회 모집 결과" : "모집 결과";
            var topGrade = 0;
            var middleGrade = 0;
            var baseGrade = 0;
            for (var index = 0; index < rewards.Length; index++)
            {
                var reward = rewards[index];
                var grade = reward.IsRobot ? (int)SemiconFactoryDefinitions.GetRobot(reward.Robot).Rarity + 1 : (int)reward.Grade;
                if (grade >= 3) topGrade++;
                else if (grade == 2) middleGrade++;
                else baseGrade++;
            }
            if (resultSummaryText != null)
            {
                resultSummaryText.text = robotMode
                    ? $"SR {topGrade}   ·   R {middleGrade}   ·   N {baseGrade}"
                    : $"III {topGrade}   ·   II {middleGrade}   ·   I {baseGrade}";
            }
            for (var index = 0; index < resultCards.Length; index++)
            {
                var active = index < rewards.Length;
                resultCards[index]?.SetActive(active);
                if (!active) continue;
                ApplyResultCardLayout(index, rewards.Length);
                var reward = rewards[index];
                if (reward.IsRobot)
                {
                    var definition = SemiconFactoryDefinitions.GetRobot(reward.Robot);
                    var railColor = SemiconGachaArt.GetRobotRarityColor(reward.Robot);
                    if (resultImages[index] != null) resultImages[index].sprite = SemiconGachaArt.GetRobotSprite(reward.Robot);
                    if (resultNameTexts[index] != null) resultNameTexts[index].text = definition.Code + "  ·  " + definition.Name;
                    if (resultGradeTexts[index] != null)
                    {
                        resultGradeTexts[index].text = definition.Rarity.ToString();
                        resultGradeTexts[index].fontSize = 23;
                        resultGradeTexts[index].color = railColor;
                    }
                    if (resultStateTexts != null && index < resultStateTexts.Length && resultStateTexts[index] != null)
                    {
                        resultStateTexts[index].text = reward.UpgradeTriggered
                            ? $"AUTO MERGE  +{reward.RobotEnhancement}강"
                            : isNew != null && index < isNew.Length && isNew[index] ? "NEW  신규 로봇" : "보유 수량 +1";
                        resultStateTexts[index].color = reward.UpgradeTriggered ||
                                                       isNew != null && index < isNew.Length && isNew[index]
                            ? SemiconUiPalette.Blue
                            : SemiconUiPalette.Muted;
                    }
                    var rail = resultCards[index]?.transform.Find("Reward Rarity Rail")?.GetComponent<Graphic>();
                    if (rail != null) rail.color = railColor;
                    SetRewardGlow(resultCards[index], railColor);
                }
                else
                {
                    var railColor = SemiconGachaArt.GetDiskGradeColor(reward.Grade);
                    if (resultImages[index] != null) resultImages[index].sprite = SemiconGachaArt.GetDiskSprite(reward.Disk, reward.Grade);
                    if (resultNameTexts[index] != null) resultNameTexts[index].text = SemiconFactoryDefinitions.GetDiskName(reward.Disk);
                    if (resultGradeTexts[index] != null)
                    {
                        resultGradeTexts[index].text = reward.Grade.ToString();
                        resultGradeTexts[index].fontSize = 23;
                        resultGradeTexts[index].color = railColor;
                    }
                    if (resultStateTexts != null && index < resultStateTexts.Length && resultStateTexts[index] != null)
                    {
                        resultStateTexts[index].text = isNew != null && index < isNew.Length && isNew[index]
                            ? "NEW  신규 디스크"
                            : "보유 수량 +1";
                        resultStateTexts[index].color = isNew != null && index < isNew.Length && isNew[index]
                            ? SemiconUiPalette.Blue
                            : SemiconUiPalette.Muted;
                    }
                    var rail = resultCards[index]?.transform.Find("Reward Rarity Rail")?.GetComponent<Graphic>();
                    if (rail != null) rail.color = railColor;
                    SetRewardGlow(resultCards[index], railColor);
                }
                if (resultCards[index] != null) resultCards[index].transform.localScale = Vector3.zero;
            }
            Refresh();
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(AnimateResults(rewards.Length));
        }

        private void HideResults()
        {
            if (resultOverlay != null) resultOverlay.SetActive(false);
        }

        private void Refresh()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            if (creditsText != null) creditsText.text = $"보유 자금  ₩ {state.Credits:N0}";
            robotPage?.SetActive(robotMode);
            diskPage?.SetActive(!robotMode);
            SetSupplyTabSelection(robotTabButton, robotMode);
            SetSupplyTabSelection(diskTabButton, !robotMode);

            var ownedRobotKinds = 0;
            for (var index = 0; index < SemiconFactoryDefinitions.RobotCount; index++)
            {
                if (state.GetRobotOwnedCount(SemiconFactoryDefinitions.GetRobotByCatalogIndex(index)) > 0)
                    ownedRobotKinds++;
            }
            for (var index = 0; index < robotCardButtons.Length; index++)
            {
                var robot = SemiconFactoryDefinitions.GetRobotByCatalogIndex(index);
                var definition = SemiconFactoryDefinitions.GetRobot(robot);
                var owned = state.GetRobotOwnedCount(robot);
                var highest = state.GetHighestRobotEnhancement(robot);
                if (robotCardImages[index] != null)
                {
                    robotCardImages[index].sprite = SemiconGachaArt.GetRobotSprite(robot);
                    robotCardImages[index].color = owned > 0 ? Color.white : new Color(0.34f, 0.42f, 0.44f, 0.8f);
                }
                if (robotCardLabels[index] != null) robotCardLabels[index].text = $"{definition.Rarity}  {definition.Code}\n{definition.Name}";
                if (robotOwnedLabels[index] != null)
                    robotOwnedLabels[index].text = owned > 0
                        ? $"{SemiconFactoryDefinitions.GetRobotEnhancementText(highest)}  ×{owned}"
                        : "미보유";
                var rarityRail = robotCardButtons[index]?.transform.Find("Rarity Rail")?.GetComponent<Graphic>();
                if (rarityRail != null) rarityRail.color = SemiconGachaArt.GetRobotRarityColor(robot);
                SemiconUiPalette.SetButtonSelection(robotCardButtons[index], robot == selectedRobot, owned == 0);
            }

            var ownedDisks = 0;
            for (var disk = 1; disk <= 3; disk++)
            for (var grade = 1; grade <= 3; grade++)
                ownedDisks += state.GetDiskOwnedCount((SemiconDiskKind)disk, (SemiconDiskGrade)grade);
            for (var index = 0; index < diskCardButtons.Length; index++)
            {
                var disk = (SemiconDiskKind)(index % 3 + 1);
                var grade = (SemiconDiskGrade)(index / 3 + 1);
                var owned = state.GetDiskOwnedCount(disk, grade);
                if (diskCardImages[index] != null)
                {
                    diskCardImages[index].sprite = SemiconGachaArt.GetDiskSprite(disk, grade);
                    diskCardImages[index].color = owned > 0 ? Color.white : new Color(0.34f, 0.42f, 0.44f, 0.8f);
                }
                if (diskCardLabels[index] != null) diskCardLabels[index].text = SemiconFactoryDefinitions.GetDiskName(disk, grade);
                if (diskOwnedLabels[index] != null) diskOwnedLabels[index].text = owned > 0 ? $"보유 {owned}" : "미보유";
                var gradeRail = diskCardButtons[index]?.transform.Find("Grade Rail")?.GetComponent<Graphic>();
                if (gradeRail != null) gradeRail.color = SemiconGachaArt.GetDiskGradeColor(grade);
                SemiconUiPalette.SetButtonSelection(diskCardButtons[index], disk == selectedDisk && grade == selectedGrade,
                    owned == 0);
            }

            if (collectionText != null)
                collectionText.text = robotMode ? $"보유 로봇  {ownedRobotKinds} / 15" : $"보유 디스크  {ownedDisks}개";
            if (rateInfoText != null)
                rateInfoText.text = robotMode
                    ? "SR 10%   ·   R 30%   ·   N 60%"
                    : "III 10%   ·   II 30%   ·   I 60%";
            if (guaranteeText != null)
                guaranteeText.text = robotMode
                    ? "10회 모집 시 R 등급 이상 1대 확정"
                    : "10회 추첨 시 II 등급 이상 1개 확정";
            var onePrice = robotMode ? SemiconFactoryDefinitions.RobotSingleDrawPrice : SemiconFactoryDefinitions.DiskSingleDrawPrice;
            var tenPrice = robotMode ? SemiconFactoryDefinitions.RobotTenDrawPrice : SemiconFactoryDefinitions.DiskTenDrawPrice;
            SetButtonLabel(singleDrawButton, $"1회 {(robotMode ? "모집" : "추첨")}    ₩ {onePrice:N0}  ▶");
            SetButtonLabel(tenDrawButton, $"10회 {(robotMode ? "모집" : "추첨")}    ₩ {tenPrice:N0}  ▶");
            if (singleDrawButton != null) singleDrawButton.interactable = state.Credits >= onePrice;
            if (tenDrawButton != null) tenDrawButton.interactable = state.Credits >= tenPrice;
            if (resultRepeatButton != null)
            {
                var repeatPrice = lastDrawCount == 10 ? tenPrice : onePrice;
                resultRepeatButton.interactable = state.Credits >= repeatPrice;
                SetButtonLabel(resultRepeatButton,
                    $"다시 {lastDrawCount}회    ₩ {repeatPrice:N0}  ▶");
            }
            RefreshDetails();
        }

        private void RefreshDetails()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            if (robotMode)
            {
                var definition = SemiconFactoryDefinitions.GetRobot(selectedRobot);
                if (detailImage != null) detailImage.sprite = SemiconGachaArt.GetRobotSprite(selectedRobot);
                if (detailRarityText != null)
                {
                    detailRarityText.text = definition.Rarity + "  ROBOT";
                    detailRarityText.color = SemiconGachaArt.GetRobotRarityColor(selectedRobot);
                }
                if (detailNameText != null) detailNameText.text = definition.Code + "  " + definition.Name;
                if (detailRoleText != null) detailRoleText.text = "담당  /  " + definition.Role;
                var highest = state.GetHighestRobotEnhancement(selectedRobot);
                if (detailBonusText != null)
                    detailBonusText.text = $"최고 단계  {SemiconFactoryDefinitions.GetRobotEnhancementText(highest)}\n" +
                                           SemiconFactoryDefinitions.GetRobotBonus(selectedRobot, highest);
                if (detailOwnedText != null)
                    detailOwnedText.text = BuildRobotInventorySummary(state, selectedRobot);
            }
            else
            {
                if (detailImage != null) detailImage.sprite = SemiconGachaArt.GetDiskSprite(selectedDisk, selectedGrade);
                if (detailRarityText != null)
                {
                    detailRarityText.text = "GRADE " + selectedGrade;
                    detailRarityText.color = SemiconGachaArt.GetDiskGradeColor(selectedGrade);
                }
                if (detailNameText != null) detailNameText.text = SemiconFactoryDefinitions.GetDiskName(selectedDisk);
                if (detailRoleText != null) detailRoleText.text = "로봇 특성 모듈  /  장착 후 즉시 적용";
                if (detailBonusText != null)
                    detailBonusText.text = "장착 시 적용 효과\n" +
                                           SemiconFactoryDefinitions.GetDiskBonus(selectedDisk, selectedGrade);
                if (detailOwnedText != null) detailOwnedText.text = $"보유 {state.GetDiskOwnedCount(selectedDisk, selectedGrade)}개  ·  장착 {state.GetDiskAssignedCount(selectedDisk, selectedGrade)}개";
            }
            if (detailImage != null) detailImage.preserveAspect = true;
        }

        private void SetStatus(string message, Color color)
        {
            if (statusText == null) return;
            statusText.text = message;
            statusText.color = color;
        }

        private static void ApplyResultCardLayout(int index, int total)
        {
            var card = GameObject.Find($"Supply Result Card {index + 1:00}");
            if (card == null) return;
            var rect = card.GetComponent<RectTransform>();
            if (rect == null) return;
            var column = total == 1 ? 2 : index % 5;
            var row = total == 1 ? 0 : index / 5;
            var left = 48f + column * 316f;
            var top = -154f - row * 328f;
            rect.offsetMin = new Vector2(left, top - 300f);
            rect.offsetMax = new Vector2(left + 286f, top);
        }

        private static void SetRewardGlow(GameObject card, Color color)
        {
            var glow = card != null ? card.transform.Find("Reward Glow")?.GetComponent<Graphic>() : null;
            if (glow == null) return;
            glow.color = new Color(color.r, color.g, color.b, 0.18f);
        }

        private static void SetSupplyTabSelection(Button button, bool selected)
        {
            if (button == null) return;
            if (button.targetGraphic != null)
                button.targetGraphic.color = selected
                    ? new Color32(16, 139, 194, 255)
                    : new Color32(225, 238, 243, 255);
            var label = button.GetComponentInChildren<Text>(true);
            if (label != null) label.color = selected ? Color.white : SemiconUiPalette.Ink;
        }

        private static string BuildRobotInventorySummary(SemiconGameState state, SemiconRobotKind robot)
        {
            var summary = $"총 {state.GetRobotOwnedCount(robot)}대  ·  배치 {state.GetRobotAssignedCount(robot)}대\n";
            var hasLevel = false;
            for (var level = 0; level <= SemiconFactoryDefinitions.MaxRobotEnhancement; level++)
            {
                var count = state.GetRobotOwnedCount(robot, level);
                if (count <= 0) continue;
                if (hasLevel) summary += "   ";
                summary += $"{SemiconFactoryDefinitions.GetRobotEnhancementText(level)} ×{count}";
                hasLevel = true;
            }
            return summary;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null) return;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
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
            var start = new Vector2(50f, 0f);
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

        private IEnumerator AnimateResults(int count)
        {
            for (var index = 0; index < count && index < resultCards.Length; index++)
            {
                var card = resultCards[index];
                if (card == null) continue;
                var group = card.GetComponent<CanvasGroup>();
                if (group == null) group = card.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                card.transform.localEulerAngles = new Vector3(0f, 78f, 0f);
                var elapsed = 0f;
                const float duration = 0.09f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    var t = Mathf.Clamp01(elapsed / duration);
                    var eased = 1f - Mathf.Pow(1f - t, 3f);
                    group.alpha = eased;
                    card.transform.localScale = Vector3.one * Mathf.Lerp(0.72f, 1f, eased);
                    card.transform.localEulerAngles = new Vector3(0f, Mathf.Lerp(78f, 0f, eased), 0f);
                    yield return null;
                }
                group.alpha = 1f;
                card.transform.localScale = Vector3.one;
                card.transform.localEulerAngles = Vector3.zero;
            }
        }

        private IEnumerator AnimateClose()
        {
            var elapsed = 0f;
            const float duration = 0.15f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                if (panelGroup != null) panelGroup.alpha = 1f - t;
                yield return null;
            }
            SetVisible(false);
            activePlayer?.SetInputEnabled(true);
            activeCamera?.SetLookEnabled(true);
            SemiconPlayerController.SetCursorLocked(true);
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
