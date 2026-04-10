using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ItemSystem;
using FishCardSystem;

namespace FishingSystem
{
    /// <summary>
    /// 偷看牌堆流程管理器（场景级单例）。
    /// 管理偷看效果的完整 UI 流程：锁定面板 → 牌堆选择 → 展示 → 恢复。
    /// 支持四种偷看模式：单堆多张（Single）、同行各一（Row）、同列各一（Column）、全局偷看（All）。
    /// </summary>
    public class PeekPileHandler : MonoBehaviour
    {
        public enum PeekMode { Single, Row, Column, All }
        public static PeekPileHandler Instance { get; private set; }

        [Header("依赖引用")]
        [SerializeField] private HandPanelUI handPanelUI;
        [SerializeField] private GameObject selectionPanelPrefab;

        [Header("提示 UI")]
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private TextMeshProUGUI promptText;

        [Header("全局偷看 (All 模式)")]
        [SerializeField] private GameObject fishCardPrefab;
        [SerializeField] private Button exitPeekButton;
        [SerializeField] private int overlaySortingOrder = 165;

        private int peekCount;
        private PeekMode peekMode;
        private CardSelectionPanel activePanel;
        private readonly List<GameObject> activeOverlays = new List<GameObject>();
        public bool IsPeeking { get; private set; }

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (promptRoot != null)
                promptRoot.SetActive(false);

            if (exitPeekButton != null)
            {
                exitPeekButton.gameObject.SetActive(false);
                exitPeekButton.onClick.AddListener(OnExitPeekClicked);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                ClearAllOverlays();
                CardPile.ClickInterceptor = null;
                Instance = null;
            }

            if (exitPeekButton != null)
                exitPeekButton.onClick.RemoveListener(OnExitPeekClicked);
        }

        #endregion

        #region Public API

        /// <summary>
        /// 开始偷看流程：锁定 UI，进入牌堆选择模式
        /// </summary>
        /// <param name="count">偷看张数（仅 Single 模式使用，Row/Column 固定为每堆1张）</param>
        /// <param name="mode">偷看模式</param>
        public void StartPeek(int count, PeekMode mode = PeekMode.Single)
        {
            if (IsPeeking) return;

            IsPeeking = true;
            peekCount = count;
            peekMode = mode;

            handPanelUI?.LockCollapsed();
            EquipmentPanel.Instance?.LockClosed();

            if (mode == PeekMode.All)
            {
                StartAllPeek();
                return;
            }

            if (promptRoot != null)
                promptRoot.SetActive(true);

            CardPile.ClickInterceptor = OnPileSelected;

            Debug.Log($"[PeekPileHandler] 偷看模式开始，模式={peekMode}，偷看 {peekCount} 张牌");
        }

        #endregion

        #region Internal Flow

        private void OnPileSelected(CardPile pile)
        {
            if (pile == null || pile.CardCount == 0)
            {
                Debug.Log("[PeekPileHandler] 选择的牌堆为空");
                return;
            }

            var offered = new List<ItemData>();
            var labels = new List<string>();

            switch (peekMode)
            {
                case PeekMode.Single:
                {
                    var peeked = pile.PeekTopCards(peekCount, skipRevealed: true);
                    if (peeked.Count == 0)
                    {
                        Debug.Log("[PeekPileHandler] 该牌堆没有未揭示的卡牌，请选择其他牌堆");
                        return;
                    }
                    for (int i = 0; i < peeked.Count; i++)
                    {
                        offered.Add(peeked[i]);
                        labels.Add($"第{i + 1}张");
                    }
                    break;
                }
                case PeekMode.Row:
                {
                    var config = FishingTableManager.Instance.GetPileConfig(pile);
                    if (config == null)
                    {
                        Debug.LogError("[PeekPileHandler] 无法反查牌堆配置");
                        return;
                    }
                    var rowPiles = FishingTableManager.Instance.GetPilesByDepth(config.Value.depth);
                    foreach (var p in rowPiles)
                    {
                        var cards = p.PeekTopCards(1, skipRevealed: true);
                        if (cards.Count > 0)
                        {
                            offered.Add(cards[0]);
                            var cfg = FishingTableManager.Instance.GetPileConfig(p);
                            labels.Add($"牌堆 {cfg.Value.poolIndex + 1}");
                        }
                    }
                    if (offered.Count == 0)
                    {
                        Debug.Log("[PeekPileHandler] 该行没有未揭示的卡牌");
                        return;
                    }
                    break;
                }
                case PeekMode.Column:
                {
                    var config = FishingTableManager.Instance.GetPileConfig(pile);
                    if (config == null)
                    {
                        Debug.LogError("[PeekPileHandler] 无法反查牌堆配置");
                        return;
                    }
                    var colPiles = FishingTableManager.Instance.GetPilesByPoolIndex(config.Value.poolIndex);
                    foreach (var p in colPiles)
                    {
                        var cards = p.PeekTopCards(1, skipRevealed: true);
                        if (cards.Count > 0)
                        {
                            offered.Add(cards[0]);
                            var cfg = FishingTableManager.Instance.GetPileConfig(p);
                            labels.Add(cfg.Value.depth.ToChineseText());
                        }
                    }
                    if (offered.Count == 0)
                    {
                        Debug.Log("[PeekPileHandler] 该列没有未揭示的卡牌");
                        return;
                    }
                    break;
                }
            }

            if (promptRoot != null)
                promptRoot.SetActive(false);

            CardPile.ClickInterceptor = null;

            if (selectionPanelPrefab == null)
            {
                Debug.LogError("[PeekPileHandler] selectionPanelPrefab 未配置");
                EndPeek();
                return;
            }

            Canvas rootCanvas = GetComponentInParent<Canvas>();
            Transform parent = rootCanvas != null ? rootCanvas.transform : transform;
            GameObject panelObj = Instantiate(selectionPanelPrefab, parent);
            activePanel = panelObj.GetComponent<CardSelectionPanel>();

            if (activePanel == null)
            {
                Debug.LogError("[PeekPileHandler] selectionPanelPrefab 上缺少 CardSelectionPanel 组件");
                Destroy(panelObj);
                EndPeek();
                return;
            }

            activePanel.Open(offered, 0, OnPeekComplete, labels);

            Debug.Log($"[PeekPileHandler] 偷看 {offered.Count} 张牌，模式={peekMode}");
        }

        private void OnPeekComplete(List<ItemData> selected, List<ItemData> rejected)
        {
            if (activePanel != null)
            {
                Destroy(activePanel.gameObject);
                activePanel = null;
            }

            EndPeek();
        }

        private void EndPeek()
        {
            handPanelUI?.UnlockCollapsed();
            EquipmentPanel.Instance?.UnlockClosed();

            IsPeeking = false;

            Debug.Log("[PeekPileHandler] 偷看流程结束");
        }

        #endregion

        #region All Mode (全局偷看)

        private void StartAllPeek()
        {
            CardPile.ClickInterceptor = _ => { };

            var allPiles = FishingTableManager.Instance.GetAllPiles();
            foreach (var pile in allPiles)
                CreatePeekOverlay(pile);

            if (exitPeekButton != null)
                exitPeekButton.gameObject.SetActive(true);

            Debug.Log($"[PeekPileHandler] 全局偷看开始，创建了 {activeOverlays.Count} 个浮层");
        }

        private void CreatePeekOverlay(CardPile pile)
        {
            var peeked = pile.PeekTopCards(1, skipRevealed: true);
            if (peeked.Count == 0) return;

            FishData data = peeked[0];

            var overlay = new GameObject("PeekOverlay", typeof(RectTransform));
            var rt = overlay.GetComponent<RectTransform>();
            rt.SetParent(pile.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            var canvas = overlay.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = overlaySortingOrder;

            overlay.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var blocker = overlay.AddComponent<UnityEngine.UI.Image>();
            blocker.color = Color.clear;
            blocker.raycastTarget = true;

            var cg = overlay.AddComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = true;

            if (fishCardPrefab == null)
            {
                Debug.LogError("[PeekPileHandler] fishCardPrefab 未配置，无法创建偷看浮层");
                Destroy(overlay);
                return;
            }

            var cardObj = Instantiate(fishCardPrefab, overlay.transform);
            cardObj.transform.localPosition = Vector3.zero;
            cardObj.transform.localScale = Vector3.one;

            var card = cardObj.GetComponent<ItemCard>();
            if (card != null)
            {
                card.visualParentOverride = overlay.transform;
                card.Initialize(data);
                card.SetContextMode(CardContextMode.Pile);
                card.isLocked = true;
            }

            activeOverlays.Add(overlay);
        }

        private void OnExitPeekClicked()
        {
            ClearAllOverlays();
            CardPile.ClickInterceptor = null;

            if (exitPeekButton != null)
                exitPeekButton.gameObject.SetActive(false);

            EndPeek();
        }

        private void ClearAllOverlays()
        {
            foreach (var overlay in activeOverlays)
            {
                if (overlay != null) Destroy(overlay);
            }
            activeOverlays.Clear();
        }

        #endregion
    }
}
