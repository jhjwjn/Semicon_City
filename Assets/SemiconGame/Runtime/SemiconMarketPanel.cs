using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    public sealed class SemiconMarketPanel : MonoBehaviour
    {
        private const int BundleSize = 10;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private RectTransform panelFrame;
        [SerializeField] private Text creditsText, siliconStockText, gasStockText, chemicalStockText;
        [SerializeField] private Text metalTargetStockText, finishedStockText, finishedQualityText, finishedPriceText;
        [SerializeField] private Text firstOrderStatusText, transactionText;
        [SerializeField] private Button buySiliconButton, buyGasButton, buyChemicalButton, buyMetalTargetButton;
        [SerializeField] private Text[] cartQuantityTexts, cartSubtotalTexts;
        [SerializeField] private Button[] cartMinusButtons, cartPlusButtons;
        [SerializeField] private Text cartTotalText;
        [SerializeField] private Button checkoutButton, clearCartButton;
        [SerializeField] private CanvasGroup purchasePageGroup, salesPageGroup;
        [SerializeField] private Button purchaseTabButton, salesTabButton;
        [SerializeField] private Button sc01SaleProductButton, pm10SaleProductButton, dd20SaleProductButton;
        [SerializeField] private Text saleProductCodeText, saleProductCoreCodeText;
        [SerializeField] private Text saleProductNameText, saleProductDescriptionText;
        [SerializeField] private CanvasGroup inventoryModalGroup;
        [SerializeField] private Text inventoryCreditsText, processInventoryText;
        [SerializeField] private Button openInventoryButton, closeInventoryButton;
        [SerializeField] private Button sellFinishedButton, firstOrderButton, closeButton;
        [SerializeField] private SemiconHud hud;

        private readonly int[] cart = new int[4];
        private SemiconPlayerController activePlayer;
        private SemiconThirdPersonCamera activeCamera;
        private Coroutine animationRoutine;
        private bool isOpen, isInventoryVisible, isSalesPage;
        private SemiconRecipeKind selectedSaleProduct = SemiconRecipeKind.Sc01ControlSensor;

        private void Awake()
        {
            buySiliconButton?.onClick.AddListener(() => AddToCart(0));
            buyGasButton?.onClick.AddListener(() => AddToCart(1));
            buyChemicalButton?.onClick.AddListener(() => AddToCart(2));
            buyMetalTargetButton?.onClick.AddListener(() => AddToCart(3));
            for (var index = 0; index < 4; index++)
            {
                var captured = index;
                if (cartMinusButtons != null && index < cartMinusButtons.Length)
                    cartMinusButtons[index]?.onClick.AddListener(() => AdjustCart(captured, -BundleSize));
                if (cartPlusButtons != null && index < cartPlusButtons.Length)
                    cartPlusButtons[index]?.onClick.AddListener(() => AdjustCart(captured, BundleSize));
            }
            checkoutButton?.onClick.AddListener(CheckoutCart);
            clearCartButton?.onClick.AddListener(ClearCart);
            purchaseTabButton?.onClick.AddListener(() => SetMarketPage(false));
            salesTabButton?.onClick.AddListener(() => SetMarketPage(true));
            sc01SaleProductButton?.onClick.AddListener(() => SelectSaleProduct(SemiconRecipeKind.Sc01ControlSensor));
            pm10SaleProductButton?.onClick.AddListener(() => SelectSaleProduct(SemiconRecipeKind.Pm10PowerManagement));
            dd20SaleProductButton?.onClick.AddListener(() => SelectSaleProduct(SemiconRecipeKind.Dd20DisplayDriver));
            openInventoryButton?.onClick.AddListener(() => SetInventoryVisible(true));
            closeInventoryButton?.onClick.AddListener(() => SetInventoryVisible(false));
            sellFinishedButton?.onClick.AddListener(SellFinishedProduct);
            firstOrderButton?.onClick.AddListener(HandleFirstOrder);
            closeButton?.onClick.AddListener(Close);
            SetInventoryVisible(false);
            SetVisible(false);
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
            if (isInventoryVisible) SetInventoryVisible(false); else Close();
        }

        public void Configure(CanvasGroup group, RectTransform frame, Text credits, Text siliconStock,
            Text gasStock, Text chemicalStock, Text metalTargetStock, Text finishedStock, Text firstOrderStatus,
            Text transaction, Button buySilicon, Button buyGas, Button buyChemical, Button buyMetalTarget,
            Button sellFinished, Button firstOrder, Button close, SemiconHud targetHud,
            Text[] cartQuantities = null, Text[] cartSubtotals = null, Button[] cartMinus = null,
            Button[] cartPlus = null, Text cartTotal = null, Button checkout = null, Button clearCart = null,
            CanvasGroup inventoryGroup = null, Text inventoryCredits = null, Text processInventory = null,
            Button openInventory = null, Button closeInventory = null,
            CanvasGroup purchasePage = null, CanvasGroup salesPage = null,
            Button purchaseTab = null, Button salesTab = null,
            Text finishedQuality = null, Text finishedPrice = null,
            Button sc01SaleProduct = null, Button pm10SaleProduct = null, Button dd20SaleProduct = null,
            Text saleProductCode = null, Text saleProductCoreCode = null,
            Text saleProductName = null, Text saleProductDescription = null)
        {
            panelGroup = group; panelFrame = frame; creditsText = credits;
            siliconStockText = siliconStock; gasStockText = gasStock; chemicalStockText = chemicalStock;
            metalTargetStockText = metalTargetStock; finishedStockText = finishedStock;
            firstOrderStatusText = firstOrderStatus; transactionText = transaction;
            buySiliconButton = buySilicon; buyGasButton = buyGas; buyChemicalButton = buyChemical;
            buyMetalTargetButton = buyMetalTarget; sellFinishedButton = sellFinished;
            firstOrderButton = firstOrder; closeButton = close; hud = targetHud;
            cartQuantityTexts = cartQuantities; cartSubtotalTexts = cartSubtotals;
            cartMinusButtons = cartMinus; cartPlusButtons = cartPlus; cartTotalText = cartTotal;
            checkoutButton = checkout; clearCartButton = clearCart; inventoryModalGroup = inventoryGroup;
            inventoryCreditsText = inventoryCredits; processInventoryText = processInventory;
            openInventoryButton = openInventory; closeInventoryButton = closeInventory;
            purchasePageGroup = purchasePage; salesPageGroup = salesPage;
            purchaseTabButton = purchaseTab; salesTabButton = salesTab;
            finishedQualityText = finishedQuality; finishedPriceText = finishedPrice;
            sc01SaleProductButton = sc01SaleProduct; pm10SaleProductButton = pm10SaleProduct;
            dd20SaleProductButton = dd20SaleProduct; saleProductCodeText = saleProductCode;
            saleProductCoreCodeText = saleProductCoreCode; saleProductNameText = saleProductName;
            saleProductDescriptionText = saleProductDescription;
        }

        public void Open(SemiconPlayerController player, SemiconThirdPersonCamera followCamera)
        {
            if (isOpen) return;
            activePlayer = player; activeCamera = followCamera;
            activePlayer?.SetInputEnabled(false); activeCamera?.SetLookEnabled(false);
            SemiconPlayerController.SetCursorLocked(false);
            isOpen = true;
            selectedSaleProduct = SemiconRecipeKind.Sc01ControlSensor;
            SetInventoryVisible(false);
            SetMarketPage(false);
            SetTransaction("거래 준비 완료  /  필요한 자재를 장바구니에 담아주세요.", SemiconUiPalette.Muted);
            Refresh();
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(AnimateOpen());
        }

        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            SetInventoryVisible(false);
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(AnimateClose());
        }

        private void AddToCart(int index)
        {
            AdjustCart(index, BundleSize);
            SetTransaction("장바구니에 담았습니다. 수량을 확인한 뒤 한 번에 결제하세요.", SemiconUiPalette.Mint);
        }

        private void AdjustCart(int index, int delta)
        {
            if (index < 0 || index >= cart.Length) return;
            cart[index] = Mathf.Clamp(cart[index] + delta, 0, 990);
            RefreshCart();
        }

        private void ClearCart()
        {
            for (var index = 0; index < cart.Length; index++) cart[index] = 0;
            SetTransaction("장바구니를 비웠습니다.", SemiconUiPalette.Muted);
            RefreshCart();
        }

        private void CheckoutCart()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            if (!state.TryBuyMaterials(cart[0], cart[1], cart[2], cart[3], out var price, out var reason))
            {
                SetTransaction("결제 실패  /  " + reason, new Color32(238, 103, 89, 255));
                hud?.ShowToast(reason);
                return;
            }
            var units = 0;
            for (var index = 0; index < cart.Length; index++) { units += cart[index]; cart[index] = 0; }
            SetTransaction($"일괄 구매 완료  /  자재 {units}개 입고  ·  -₩ {price:N0}", SemiconUiPalette.Mint);
            hud?.ShowToast($"자재 {units}개가 창고에 입고되었습니다.");
            RefreshCart();
        }

        private void SellFinishedProduct()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            var code = GetProductCode(selectedSaleProduct);
            var productName = GetProductName(selectedSaleProduct);
            var price = state.GetSaleProductPrice(selectedSaleProduct);
            if (selectedSaleProduct == SemiconRecipeKind.Sc01ControlSensor &&
                state.FirstOrderAccepted && !state.FirstOrderCompleted)
            {
                SetTransaction("판매 보류  /  SC-01 1개를 먼저 계약 주문에 납품하세요.", new Color32(238, 103, 89, 255));
                hud?.ShowToast("진행 중인 첫 주문에 SC-01을 먼저 납품해야 합니다.");
                return;
            }
            if (!state.TrySellProduct(selectedSaleProduct, 1))
            {
                SetTransaction($"판매 실패  /  출하 가능한 {code} {productName} 재고가 없습니다.", new Color32(238, 103, 89, 255));
                hud?.ShowToast($"공장에서 {code} 완제품을 먼저 생산하세요.");
                return;
            }
            SetTransaction($"출하 완료  /  {code} {productName} -1  ·  +₩ {price:N0}", SemiconUiPalette.Amber);
            hud?.ShowToast($"{code} 1개 판매 완료  ·  +₩ {price:N0}");
        }

        private void SelectSaleProduct(SemiconRecipeKind recipe)
        {
            var state = SemiconGameState.Instance;
            if (state == null || !IsSaleProductUnlocked(state, recipe)) return;
            selectedSaleProduct = recipe;
            RefreshSaleProduct(state);
            SetTransaction($"판매 품목 선택  /  {GetProductCode(recipe)} {GetProductName(recipe)}", SemiconUiPalette.Mint);
        }

        private void HandleFirstOrder()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            if (!state.FirstOrderAccepted)
            {
                if (!state.TryAcceptFirstOrder(out var reason))
                {
                    SetTransaction("주문 잠김  /  " + reason, new Color32(238, 103, 89, 255));
                    hud?.ShowToast(reason); return;
                }
                SetTransaction("주문 수락  /  SC-01 제어 센서 패키지 1개를 생산하세요.", SemiconUiPalette.Mint);
                hud?.ShowToast("첫 고객 주문을 수락했습니다. FAB 01에서 SC-01을 생산하세요.", 4f);
                return;
            }
            if (!state.TryCompleteFirstOrder(out var completionReason))
            {
                SetTransaction("납품 대기  /  " + completionReason, new Color32(238, 103, 89, 255));
                hud?.ShowToast(completionReason); return;
            }
            SetTransaction($"계약 완료  /  +₩ {SemiconGameState.FirstOrderCreditReward:N0}", SemiconUiPalette.Amber);
            hud?.ShowToast("첫 주문 납품 완료. 8대 공정 생산 루프가 개방되었습니다.", 5f);
        }

        private void Refresh()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            if (creditsText != null) creditsText.text = $"₩ {state.Credits:N0}";
            if (inventoryCreditsText != null) inventoryCreditsText.text = $"₩ {state.Credits:N0}";
            if (siliconStockText != null) siliconStockText.text = $"{state.SiliconStock:N0} EA";
            if (gasStockText != null) gasStockText.text = $"{state.ProcessGasStock:N0} EA";
            if (chemicalStockText != null) chemicalStockText.text = $"{state.ChemicalStock:N0} EA";
            if (metalTargetStockText != null) metalTargetStockText.text = $"{state.MetalTargetStock:N0} EA";
            if (processInventoryText != null)
                processInventoryText.text =
                    $"기초 웨이퍼   {state.WaferStock:N0}개        산화 웨이퍼   {state.OxidizedWaferStock:N0}개\n" +
                    $"패턴 웨이퍼   {state.PatternedWaferStock:N0}개        식각 웨이퍼   {state.EtchedWaferStock:N0}개\n" +
                    $"박막 웨이퍼   {state.DepositedWaferStock:N0}개        배선 웨이퍼   {state.MetalizedWaferStock:N0}개\n" +
                    $"EDS 선별 웨이퍼   {state.TestedWaferStock:N0}개\n\n" +
                    $"SC-01   {state.FinishedProductStock:N0}개     PM-10   {state.Pm10Stock:N0}개     DD-20   {state.Dd20Stock:N0}개";
            RefreshShipping(state);
            RefreshCart();
        }

        private void RefreshShipping(SemiconGameState state)
        {
            RefreshSaleProduct(state);
            if (firstOrderStatusText != null)
                       {
                firstOrderStatusText.text = state.FirstOrderCompleted ? "CONTRACT 01  /  납품 완료" :
                    state.FirstOrderAccepted ? $"CONTRACT 01  /  생산 진행 {Mathf.Min(1, state.FinishedProductStock)} / 1" :
                    state.PackageRecipeQualified && state.UnlockedProcessCount >= 8 ? "CONTRACT 01  /  수락 가능" :
                    $"CONTRACT 01  /  공정 개방 {state.UnlockedProcessCount} / 8";
                firstOrderStatusText.color = state.FirstOrderCompleted ? SemiconUiPalette.Amber :
                    state.FirstOrderAccepted || (state.PackageRecipeQualified && state.UnlockedProcessCount >= 8) ? SemiconUiPalette.Mint : SemiconUiPalette.Muted;
            }
            if (firstOrderButton == null) return;
            var canAccept = !state.FirstOrderAccepted && !state.FirstOrderCompleted && state.PackageRecipeQualified && state.UnlockedProcessCount >= 8;
            var canDeliver = state.FirstOrderAccepted && !state.FirstOrderCompleted && state.FinishedProductStock > 0;
            firstOrderButton.interactable = canAccept || canDeliver;
            SetButtonLabel(firstOrderButton, state.FirstOrderCompleted ? "주문 완료" : state.FirstOrderAccepted ?
                canDeliver ? "주문 납품  ▶" : "SC-01 생산 대기" : canAccept ? "첫 주문 수락  ▶" : "8대 공정 개방 필요");
        }

        private void RefreshSaleProduct(SemiconGameState state)
        {
            if (!IsSaleProductUnlocked(state, selectedSaleProduct))
                selectedSaleProduct = SemiconRecipeKind.Sc01ControlSensor;

            var pm10Locked = !IsSaleProductUnlocked(state, SemiconRecipeKind.Pm10PowerManagement);
            var dd20Locked = !IsSaleProductUnlocked(state, SemiconRecipeKind.Dd20DisplayDriver);
            SemiconUiPalette.SetButtonSelection(sc01SaleProductButton,
                selectedSaleProduct == SemiconRecipeKind.Sc01ControlSensor);
            SemiconUiPalette.SetButtonSelection(pm10SaleProductButton,
                selectedSaleProduct == SemiconRecipeKind.Pm10PowerManagement, pm10Locked);
            SemiconUiPalette.SetButtonSelection(dd20SaleProductButton,
                selectedSaleProduct == SemiconRecipeKind.Dd20DisplayDriver, dd20Locked);
            SetButtonLabel(sc01SaleProductButton, "SC-01");
            SetButtonLabel(pm10SaleProductButton, pm10Locked ? "PM-10  잠김" : "PM-10");
            SetButtonLabel(dd20SaleProductButton, dd20Locked ? "DD-20  잠김" : "DD-20");

            var code = GetProductCode(selectedSaleProduct);
            var name = GetProductName(selectedSaleProduct);
            var stock = state.GetSaleProductStock(selectedSaleProduct);
            var quality = state.GetSaleProductQuality(selectedSaleProduct);
            var price = state.GetSaleProductPrice(selectedSaleProduct);
            if (saleProductCodeText != null) saleProductCodeText.text = code;
            if (saleProductCoreCodeText != null) saleProductCoreCodeText.text = GetCoreCode(selectedSaleProduct);
            if (saleProductNameText != null) saleProductNameText.text = $"{code} {name}";
            if (saleProductDescriptionText != null) saleProductDescriptionText.text = GetProductDescription(selectedSaleProduct);
            if (finishedStockText != null) finishedStockText.text = $"{stock:N0}개";
            if (finishedQualityText != null) finishedQualityText.text = $"{quality:N0}점";
            if (finishedPriceText != null) finishedPriceText.text = $"₩ {price:N0}";
            if (sellFinishedButton != null)
            {
                var reservedForContract = selectedSaleProduct == SemiconRecipeKind.Sc01ControlSensor &&
                                          state.FirstOrderAccepted && !state.FirstOrderCompleted;
                sellFinishedButton.interactable = stock > 0 && !reservedForContract;
                SetButtonLabel(sellFinishedButton, reservedForContract
                    ? "SC-01 계약 납품이 우선입니다"
                    : $"{code} 1개 일반 출하  ▶  +₩ {price:N0}");
            }
        }

        private static bool IsSaleProductUnlocked(SemiconGameState state, SemiconRecipeKind recipe)
        {
            return recipe switch
            {
                SemiconRecipeKind.Pm10PowerManagement => state.IsContractUnlocked(SemiconContractKind.Pm10PowerManagement),
                SemiconRecipeKind.Dd20DisplayDriver => state.IsContractUnlocked(SemiconContractKind.Dd20DisplayDriver),
                _ => true
            };
        }

        private static string GetProductCode(SemiconRecipeKind recipe) => recipe switch
        {
            SemiconRecipeKind.Pm10PowerManagement => "PM-10",
            SemiconRecipeKind.Dd20DisplayDriver => "DD-20",
            _ => "SC-01"
        };

        private static string GetCoreCode(SemiconRecipeKind recipe) => recipe switch
        {
            SemiconRecipeKind.Pm10PowerManagement => "P10",
            SemiconRecipeKind.Dd20DisplayDriver => "D20",
            _ => "S01"
        };

        private static string GetProductName(SemiconRecipeKind recipe) => recipe switch
        {
            SemiconRecipeKind.Pm10PowerManagement => "전력 관리 IC",
            SemiconRecipeKind.Dd20DisplayDriver => "디스플레이 드라이버",
            _ => "제어 센서 패키지"
        };

        private static string GetProductDescription(SemiconRecipeKind recipe) => recipe switch
        {
            SemiconRecipeKind.Pm10PowerManagement => "산업 장비의 안정적인 전력 분배를 담당하는 고효율 IC입니다.\n품질이 높을수록 일반 판매 단가가 상승합니다.",
            SemiconRecipeKind.Dd20DisplayDriver => "정밀 패턴과 균일한 출력이 필요한 디스플레이 구동 IC입니다.\n품질이 높을수록 일반 판매 단가가 상승합니다.",
            _ => "8대 공정을 통과한 교육용 제어 센서입니다.\n품질이 높을수록 일반 판매 단가가 상승합니다."
        };

        private void RefreshCart()
        {
            var state = SemiconGameState.Instance;
            if (state == null) return;
            var totalPrice = 0; var totalUnits = 0;
            for (var index = 0; index < cart.Length; index++)
            {
                var subtotal = cart[index] * state.GetMaterialUnitPrice(GetKind(index));
                totalPrice += subtotal; totalUnits += cart[index];
                if (cartQuantityTexts != null && index < cartQuantityTexts.Length && cartQuantityTexts[index] != null)
                    cartQuantityTexts[index].text = $"{cart[index]:N0}개";
                if (cartSubtotalTexts != null && index < cartSubtotalTexts.Length && cartSubtotalTexts[index] != null)
                    cartSubtotalTexts[index].text = $"₩ {subtotal:N0}";
                if (cartMinusButtons != null && index < cartMinusButtons.Length && cartMinusButtons[index] != null)
                    cartMinusButtons[index].interactable = cart[index] > 0;
            }
            if (cartTotalText != null)
            {
                cartTotalText.text = $"총 {totalUnits:N0}개    ₩ {totalPrice:N0}";
                cartTotalText.color = totalPrice > state.Credits ? new Color32(238, 103, 89, 255) : SemiconUiPalette.Ink;
            }
            if (checkoutButton != null) checkoutButton.interactable = totalUnits > 0 && totalPrice <= state.Credits;
            if (clearCartButton != null) clearCartButton.interactable = totalUnits > 0;
            SetButtonLabel(checkoutButton, totalUnits > 0 ? $"일괄 결제  ▶  ₩ {totalPrice:N0}" : "장바구니가 비어 있습니다");
        }

        private static SemiconMaterialKind GetKind(int index) => index switch
        {
            0 => SemiconMaterialKind.Silicon, 1 => SemiconMaterialKind.ProcessGas,
            2 => SemiconMaterialKind.Chemicals, _ => SemiconMaterialKind.MetalTarget
        };

        private void SetInventoryVisible(bool visible)
        {
            isInventoryVisible = visible;
            if (inventoryModalGroup == null) return;
            inventoryModalGroup.alpha = visible ? 1f : 0f;
            inventoryModalGroup.interactable = visible;
            inventoryModalGroup.blocksRaycasts = visible;
            if (visible) Refresh();
        }

        private void SetMarketPage(bool sales)
        {
            isSalesPage = sales;
            SetPageVisible(purchasePageGroup, !sales);
            SetPageVisible(salesPageGroup, sales);
            SemiconUiPalette.SetButtonSelection(purchaseTabButton, !sales);
            SemiconUiPalette.SetButtonSelection(salesTabButton, sales);
            if (isOpen)
                SetTransaction(sales
                    ? "완성품 판매  /  재고와 계약 조건을 확인한 뒤 출하하세요."
                    : "원자재 구매  /  필요한 자재를 장바구니에 담아 한 번에 결제하세요.",
                    SemiconUiPalette.Muted);
        }

        private static void SetPageVisible(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        private void SetTransaction(string message, Color color)
        {
            if (transactionText == null) return;
            transactionText.text = message; transactionText.color = color;
        }

        private static void SetButtonLabel(Button button, string value)
        {
            var label = button != null ? button.GetComponentInChildren<Text>(true) : null;
            if (label != null) label.text = value;
        }

        private IEnumerator AnimateOpen()
        {
            if (panelGroup != null) { panelGroup.alpha = 0f; panelGroup.interactable = true; panelGroup.blocksRaycasts = true; }
            var start = new Vector2(-80f, 0f); if (panelFrame != null) panelFrame.anchoredPosition = start;
            var elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / 0.2f), 3f);
                if (panelGroup != null) panelGroup.alpha = t;
                if (panelFrame != null) panelFrame.anchoredPosition = Vector2.Lerp(start, Vector2.zero, t);
                yield return null;
            }
        }

        private IEnumerator AnimateClose()
        {
            var start = panelFrame != null ? panelFrame.anchoredPosition : Vector2.zero; var elapsed = 0f;
            while (elapsed < 0.16f)
            {
                elapsed += Time.unscaledDeltaTime; var t = Mathf.Clamp01(elapsed / 0.16f);
                if (panelGroup != null) panelGroup.alpha = 1f - t;
                if (panelFrame != null) panelFrame.anchoredPosition = Vector2.Lerp(start, new Vector2(-80f, 0f), t);
                yield return null;
            }
            SetVisible(false); activePlayer?.SetInputEnabled(true); activeCamera?.SetLookEnabled(true);
            SemiconPlayerController.SetCursorLocked(true); activePlayer = null; activeCamera = null;
        }

        private void SetVisible(bool visible)
        {
            if (panelGroup == null) return;
            panelGroup.alpha = visible ? 1f : 0f; panelGroup.interactable = visible; panelGroup.blocksRaycasts = visible;
        }
    }
}
