using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    public sealed class SemiconMarketPanel : MonoBehaviour
    {
        private const int PurchaseBundleSize = 10;

        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private RectTransform panelFrame;
        [SerializeField] private Text creditsText;
        [SerializeField] private Text siliconStockText;
        [SerializeField] private Text gasStockText;
        [SerializeField] private Text chemicalStockText;
        [SerializeField] private Text metalTargetStockText;
        [SerializeField] private Text finishedStockText;
        [SerializeField] private Text firstOrderStatusText;
        [SerializeField] private Text transactionText;
        [SerializeField] private Button buySiliconButton;
        [SerializeField] private Button buyGasButton;
        [SerializeField] private Button buyChemicalButton;
        [SerializeField] private Button buyMetalTargetButton;
        [SerializeField] private Button sellFinishedButton;
        [SerializeField] private Button firstOrderButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private SemiconHud hud;

        private SemiconPlayerController activePlayer;
        private SemiconThirdPersonCamera activeCamera;
        private Coroutine animationRoutine;
        private bool isOpen;

        private void Awake()
        {
            buySiliconButton?.onClick.AddListener(() => Buy(SemiconMaterialKind.Silicon, "고순도 실리콘"));
            buyGasButton?.onClick.AddListener(() => Buy(SemiconMaterialKind.ProcessGas, "특수가스"));
            buyChemicalButton?.onClick.AddListener(() => Buy(SemiconMaterialKind.Chemicals, "공정 약품"));
            buyMetalTargetButton?.onClick.AddListener(() => Buy(SemiconMaterialKind.MetalTarget, "배선 금속 타깃"));
            sellFinishedButton?.onClick.AddListener(SellFinishedProduct);
            firstOrderButton?.onClick.AddListener(HandleFirstOrder);
            closeButton?.onClick.AddListener(Close);
            SetVisible(false);
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

        public void Configure(
            CanvasGroup group,
            RectTransform frame,
            Text credits,
            Text siliconStock,
            Text gasStock,
            Text chemicalStock,
            Text metalTargetStock,
            Text finishedStock,
            Text firstOrderStatus,
            Text transaction,
            Button buySilicon,
            Button buyGas,
            Button buyChemical,
            Button buyMetalTarget,
            Button sellFinished,
            Button firstOrder,
            Button close,
            SemiconHud targetHud)
        {
            panelGroup = group;
            panelFrame = frame;
            creditsText = credits;
            siliconStockText = siliconStock;
            gasStockText = gasStock;
            chemicalStockText = chemicalStock;
            metalTargetStockText = metalTargetStock;
            finishedStockText = finishedStock;
            firstOrderStatusText = firstOrderStatus;
            transactionText = transaction;
            buySiliconButton = buySilicon;
            buyGasButton = buyGas;
            buyChemicalButton = buyChemical;
            buyMetalTargetButton = buyMetalTarget;
            sellFinishedButton = sellFinished;
            firstOrderButton = firstOrder;
            closeButton = close;
            hud = targetHud;
        }

        public void Open(SemiconPlayerController player, SemiconThirdPersonCamera followCamera)
        {
            if (isOpen)
            {
                return;
            }

            activePlayer = player;
            activeCamera = followCamera;
            activePlayer?.SetInputEnabled(false);
            activeCamera?.SetLookEnabled(false);
            SemiconPlayerController.SetCursorLocked(false);
            isOpen = true;
            SetTransaction("EXCHANGE READY  /  거래할 품목을 선택하세요.", new Color32(134, 164, 168, 255));
            Refresh();

            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
            }
            animationRoutine = StartCoroutine(AnimateOpen());
        }

        public void Close()
        {
            if (!isOpen)
            {
                return;
            }
            isOpen = false;
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
            }
            animationRoutine = StartCoroutine(AnimateClose());
        }

        private void Buy(SemiconMaterialKind kind, string displayName)
        {
            var state = SemiconGameState.Instance;
            if (state == null)
            {
                return;
            }

            var totalPrice = state.GetMaterialUnitPrice(kind) * PurchaseBundleSize;
            if (!state.TryBuyMaterial(kind, PurchaseBundleSize))
            {
                SetTransaction($"거래 실패  /  {displayName} 구매에 ₩ {totalPrice:N0}이 필요합니다.",
                    new Color32(238, 103, 89, 255));
                hud?.ShowToast("보유 크레딧이 부족합니다.");
                return;
            }

            SetTransaction($"매입 완료  /  {displayName} +{PurchaseBundleSize}  ·  -₩ {totalPrice:N0}",
                new Color32(41, 211, 207, 255));
        }

        private void SellFinishedProduct()
        {
            var state = SemiconGameState.Instance;
            if (state == null)
            {
                return;
            }

            var salePrice = state.GetFinishedProductSalePrice();
            if (!state.TrySellFinishedProducts(1))
            {
                SetTransaction("판매 실패  /  출하 가능한 SC-01 제어 센서 패키지가 없습니다.",
                    new Color32(238, 103, 89, 255));
                hud?.ShowToast("공장에서 완제품을 먼저 생산하세요.");
                return;
            }

            SetTransaction($"출하 완료  /  SC-01 제어 센서 패키지 -1  ·  +₩ {salePrice:N0}",
                new Color32(247, 169, 30, 255));
        }

        private void HandleFirstOrder()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;

            if (!state.FirstOrderAccepted)
            {
                if (!state.TryAcceptFirstOrder(out var acceptReason))
                {
                    SetTransaction("ORDER LOCKED  /  " + acceptReason, new Color32(238, 103, 89, 255));
                    hud?.ShowToast(acceptReason);
                    return;
                }

                SetTransaction("ORDER ACCEPTED  /  SC-01 제어 센서 패키지 1개를 생산하세요.",
                    new Color32(41, 211, 207, 255));
                hud?.ShowToast("첫 고객 주문을 수락했습니다. FAB 01에서 SC-01을 생산하세요.", 4f);
                return;
            }

            if (!state.TryCompleteFirstOrder(out var completionReason))
            {
                SetTransaction("DELIVERY WAITING  /  " + completionReason, new Color32(238, 103, 89, 255));
                hud?.ShowToast(completionReason);
                return;
            }

            SetTransaction($"CONTRACT COMPLETE  /  +₩ {SemiconGameState.FirstOrderCreditReward:N0}  ·  " +
                           $"연구 데이터 +{SemiconGameState.FirstOrderResearchReward}",
                new Color32(247, 169, 30, 255));
            hud?.ShowToast("첫 주문 납품 완료. 8대 공정 생산 루프가 개방되었습니다.", 5f);
        }

        private void Refresh()
        {
            var state = SemiconGameState.Instance;
            if (state == null)
            {
                return;
            }

            if (creditsText != null) creditsText.text = $"₩ {state.Credits:N0}";
            if (siliconStockText != null) siliconStockText.text = $"{state.SiliconStock:N0} EA";
            if (gasStockText != null) gasStockText.text = $"{state.ProcessGasStock:N0} EA";
            if (chemicalStockText != null) chemicalStockText.text = $"{state.ChemicalStock:N0} EA";
            if (metalTargetStockText != null) metalTargetStockText.text = $"{state.MetalTargetStock:N0} EA";
            if (finishedStockText != null) finishedStockText.text = $"{state.FinishedProductStock:N0} UNIT";
            if (sellFinishedButton != null)
            {
                sellFinishedButton.interactable = state.FinishedProductStock > 0 &&
                                                  (!state.FirstOrderAccepted || state.FirstOrderCompleted);
                var label = sellFinishedButton.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.text = $"1개 출하    ▶    +₩ {state.GetFinishedProductSalePrice():N0}";
                }
            }
            if (firstOrderStatusText != null)
            {
                firstOrderStatusText.text = state.FirstOrderCompleted
                    ? "CONTRACT 01  /  납품 완료"
                    : state.FirstOrderAccepted
                        ? $"CONTRACT 01  /  생산 진행 {Mathf.Min(1, state.FinishedProductStock)} / 1"
                        : state.PackageRecipeQualified && state.UnlockedProcessCount >= 8
                            ? "CONTRACT 01  /  수락 가능"
                            : $"CONTRACT 01  /  공정 개방 {state.UnlockedProcessCount} / 8";
                firstOrderStatusText.color = state.FirstOrderCompleted
                    ? new Color32(247, 169, 30, 255)
                    : state.FirstOrderAccepted || (state.PackageRecipeQualified && state.UnlockedProcessCount >= 8)
                        ? new Color32(41, 211, 207, 255)
                        : new Color32(134, 164, 168, 255);
            }
            if (firstOrderButton != null)
            {
                var readyToAccept = !state.FirstOrderAccepted && !state.FirstOrderCompleted &&
                                    state.PackageRecipeQualified && state.UnlockedProcessCount >= 8;
                var readyToDeliver = state.FirstOrderAccepted && !state.FirstOrderCompleted &&
                                     state.FinishedProductStock > 0;
                firstOrderButton.interactable = readyToAccept || readyToDeliver;
                var label = firstOrderButton.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.text = state.FirstOrderCompleted
                        ? "주문 완료"
                        : state.FirstOrderAccepted
                            ? readyToDeliver
                                ? $"주문 납품  ▶  +₩ {SemiconGameState.FirstOrderCreditReward:N0}"
                                : "SC-01 생산 대기  0 / 1"
                            : readyToAccept ? "첫 주문 수락  ▶" : "8대 공정 개방 필요";
                }
            }
        }

        private void SetTransaction(string message, Color color)
        {
            if (transactionText == null)
            {
                return;
            }
            transactionText.text = message;
            transactionText.color = color;
        }

        private IEnumerator AnimateOpen()
        {
            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.interactable = true;
                panelGroup.blocksRaycasts = true;
            }

            var start = new Vector2(-80f, 0f);
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
            var end = new Vector2(-80f, 0f);
            var elapsed = 0f;
            const float duration = 0.16f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                if (panelGroup != null) panelGroup.alpha = 1f - t;
                if (panelFrame != null) panelFrame.anchoredPosition = Vector2.Lerp(start, end, t);
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
            if (panelGroup == null)
            {
                return;
            }
            panelGroup.alpha = visible ? 1f : 0f;
            panelGroup.interactable = visible;
            panelGroup.blocksRaycasts = visible;
        }
    }
}
