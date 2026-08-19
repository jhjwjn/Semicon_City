using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    public sealed class SemiconContractPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private RectTransform panelFrame;
        [SerializeField] private Button[] contractButtons;
        [SerializeField] private Text contractCodeText;
        [SerializeField] private Text contractNameText;
        [SerializeField] private Text clientText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text requirementText;
        [SerializeField] private Text rewardText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button deliverButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private SemiconHud hud;

        private SemiconContractKind selected = SemiconContractKind.OxideEvaluation;
        private SemiconPlayerController activePlayer;
        private SemiconThirdPersonCamera activeCamera;
        private Coroutine animationRoutine;
        private bool isOpen;

        public void Configure(CanvasGroup group, RectTransform frame, Button[] buttons, Text code, Text title,
            Text client, Text description, Text requirement, Text reward, Text status, Button accept,
            Button deliver, Button close, SemiconHud targetHud)
        {
            panelGroup = group; panelFrame = frame; contractButtons = buttons; contractCodeText = code;
            contractNameText = title; clientText = client; descriptionText = description;
            requirementText = requirement; rewardText = reward; statusText = status;
            acceptButton = accept; deliverButton = deliver; closeButton = close; hud = targetHud;
        }

        private void Awake()
        {
            for (var index = 0; index < contractButtons.Length; index++)
            {
                var captured = index;
                contractButtons[index]?.onClick.AddListener(() => Select((SemiconContractKind)captured));
            }
            acceptButton?.onClick.AddListener(Accept);
            deliverButton?.onClick.AddListener(Deliver);
            closeButton?.onClick.AddListener(Close);
            SetVisible(false);
            gameObject.SetActive(false);
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
            gameObject.SetActive(true);
            activePlayer = player; activeCamera = followCamera;
            activePlayer?.SetInputEnabled(false); activeCamera?.SetLookEnabled(false);
            SemiconPlayerController.SetCursorLocked(false);
            isOpen = true;
            var state = SemiconGameState.Instance;
            if (state != null && state.ActiveContract != SemiconContractKind.None) selected = state.ActiveContract;
            Refresh();
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

        private void Select(SemiconContractKind kind) { selected = kind; Refresh(); }

        private void Accept()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            if (!state.TryAcceptContract(selected, out var reason))
            {
                hud?.ShowToast(reason); return;
            }
            var definition = SemiconContractCatalog.Get(selected);
            hud?.ShowToast($"{definition.Code} 계약을 수락했습니다.", 3f);
        }

        private void Deliver()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            var definition = SemiconContractCatalog.Get(state.ActiveContract);
            if (!state.TryDeliverActiveContract(out var reason))
            {
                hud?.ShowToast(reason); return;
            }
            hud?.ShowToast($"{definition.Code} 납품 완료  ·  ₩{definition.CreditReward:N0}", 4f);
        }

        private void Refresh()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            var definition = SemiconContractCatalog.Get(selected);
            var unlocked = state.IsContractUnlocked(selected);
            var active = state.ActiveContract == selected;
            var stock = state.GetRecipeOutputStock(definition.RequiredRecipe);
            var quality = state.GetRecipeAverageQuality(definition.RequiredRecipe);

            if (contractCodeText != null) contractCodeText.text = $"{definition.Code}  /  DELIVERY CONTRACT";
            if (contractNameText != null) contractNameText.text = unlocked ? definition.Name : "LOCKED CONTRACT";
            if (clientText != null) clientText.text = unlocked ? $"CLIENT  /  {definition.Client}" : GetUnlockHint(selected);
            if (descriptionText != null) descriptionText.text = unlocked ? definition.Description : "선행 계약을 완료하면 상세 주문 정보가 공개됩니다.";
            if (requirementText != null)
                requirementText.text = $"납품 품목   {SemiconFactoryDefinitions.GetRecipeName(definition.RequiredRecipe)}\n" +
                                       $"필요 수량   {stock} / {definition.RequiredAmount}\n" +
                                       $"평균 품질   {quality} / {definition.MinimumQuality} 이상";
            if (rewardText != null) rewardText.text = $"납품 보상\n₩ {definition.CreditReward:N0}";
            if (statusText != null)
                statusText.text = active ? "ACTIVE CONTRACT  /  납품 준비 중" :
                    state.GetContractCompletionCount(selected) > 0 ? $"COMPLETED  /  누적 {state.GetContractCompletionCount(selected)}회" :
                    unlocked ? "AVAILABLE  /  수락 가능" : "LOCKED";
            if (acceptButton != null)
            {
                acceptButton.gameObject.SetActive(!active);
                acceptButton.interactable = unlocked && state.ActiveContract == SemiconContractKind.None;
            }
            if (deliverButton != null)
            {
                deliverButton.gameObject.SetActive(active);
                deliverButton.interactable = active && stock >= definition.RequiredAmount && quality >= definition.MinimumQuality;
            }
            for (var index = 0; index < contractButtons.Length; index++)
            {
                var item = SemiconContractCatalog.GetAt(index);
                var label = contractButtons[index]?.GetComponentInChildren<Text>(true);
                if (label != null) label.text = $"{item.Code}  {(state.IsContractUnlocked(item.Kind) ? item.Name : "LOCKED")}" +
                                                   (selected == item.Kind ? "  ◀" : string.Empty);
                SemiconUiPalette.SetButtonSelection(contractButtons[index], selected == item.Kind,
                    !state.IsContractUnlocked(item.Kind));
            }
        }

        private static string GetUnlockHint(SemiconContractKind kind)
        {
            if (kind == SemiconContractKind.Pm10PowerManagement) return "UNLOCK  /  공정 샘플 계약 2종 완료";
            if (kind == SemiconContractKind.Dd20DisplayDriver) return "UNLOCK  /  샘플 4종 + PM-10 계약 완료";
            return "UNLOCK  /  첫 주문과 8대 공정 완료";
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
