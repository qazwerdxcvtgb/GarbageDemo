/// <summary>
/// 卡片选择面板控制器
/// 管理卡片选择UI的逻辑
/// 创建日期：2026-01-20
/// </summary>

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ItemSystem;
using HandSystem;

namespace UISystem
{
    /// <summary>
    /// 卡片选择面板控制器
    /// 负责显示3张卡牌，处理单选逻辑，并执行确认操作
    /// </summary>
    public class CardSelectionPanel : MonoBehaviour
    {
        /// <summary>
        /// 面板UI状态
        /// </summary>
        private enum PanelState
        {
            Selection,    // 选择状态（显示体积）
            Revealed      // 揭示状态（显示详情）
        }

        #region UI组件引用
        //[Header("卡池配置")]
        //[Tooltip("抽取的卡牌深度")]
        //public FishDepth drawCardDepth = FishDepth.Depth1;

        [Header("UI组件")]
        [Tooltip("9个卡片按钮（3行×3列）")]
        public CardButton[] cardButtons = new CardButton[9];

        [Tooltip("3行容器对象（Row1、Row2、Row3）")]
        public GameObject[] cardRows = new GameObject[3];

        [Tooltip("3个深度标签（深度1、深度2、深度3）")]
        public TextMeshProUGUI[] depthLabels = new TextMeshProUGUI[3];

        [Tooltip("确认按钮")]
        public Button confirmButton;

        [Tooltip("捕获按钮")]
        public Button captureButton;

        [Tooltip("放弃按钮")]
        public Button abandonButton;

        [Tooltip("面板根对象（用于显示/隐藏）")]
        public GameObject panelRoot;

        #endregion

        #region 数据

        /// <summary>
        /// 当前选中的卡片按钮
        /// </summary>
        private CardButton selectedCardButton;

        /// <summary>
        /// 当前面板状态
        /// </summary>
        private PanelState currentState = PanelState.Selection;

        /// <summary>
        /// 玩家角色状态引用
        /// </summary>
        private CharacterState playerState;

        /// <summary>
        /// 玩家移动控制器引用
        /// </summary>
        private PlayerMove playerMove;

        #endregion

        #region 初始化

        private void Awake()
        {
            // 添加确认按钮点击事件
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            }

            // 添加捕获按钮点击事件
            if (captureButton != null)
            {
                captureButton.onClick.AddListener(OnCaptureButtonClicked);
            }

            // 添加放弃按钮点击事件
            if (abandonButton != null)
            {
                abandonButton.onClick.AddListener(OnAbandonButtonClicked);
            }

            // 初始时隐藏面板
            if (panelRoot != null)
            {
                // 检查panelRoot是否指向自己
                if (panelRoot == this.gameObject)
                {
                    Debug.LogWarning("[CardSelectionPanel] panelRoot指向自己，跳过初始隐藏。建议创建子对象作为panelRoot。");
                }
                else
                {
                    panelRoot.SetActive(false);
                }
            }
        }

        #endregion

        #region 公开方法

        #region 原抽卡逻辑
        /// <summary>
        /// 打开卡片选择面板
        /// 根据深度参数抽取多行卡牌
        /// </summary>
        /// <param name="drawCardDepth">钓鱼深度（决定显示几行）</param>
        //public void OpenPanel(FishDepth drawCardDepth)
        //{
        //    Debug.Log($"[CardSelectionPanel] OpenPanel被调用，深度: {drawCardDepth}");

        //    // 查找玩家对象并获取CharacterState组件
        //    GameObject player = GameObject.Find("player");
        //    if (player == null)
        //    {
        //        Debug.LogError("[CardSelectionPanel] 未找到玩家对象（名称应为'player'）");
        //        return;
        //    }

        //    playerState = player.GetComponent<CharacterState>();
        //    if (playerState == null)
        //    {
        //        Debug.LogError("[CardSelectionPanel] 玩家对象缺少CharacterState组件");
        //        return;
        //    }

        //    // 获取PlayerMove组件
        //    playerMove = player.GetComponent<PlayerMove>();
        //    if (playerMove == null)
        //    {
        //        Debug.LogError("[CardSelectionPanel] 玩家对象缺少PlayerMove组件");
        //        return;
        //    }

        //    // 清空所有卡片按钮的选中状态
        //    ClearAllCardButtonSelections();

        //    // 确定要抽取的层数
        //    int layerCount = (int)drawCardDepth; // Depth1=1, Depth2=2, Depth3=3

        //    int emptyCardCount = 0; // 空牌计数
        //    int buttonIndex = 0;    // 当前按钮索引（0-8）

        //    // 循环处理每一层
        //    for (int layer = 1; layer <= 3; layer++)
        //    {
        //        int rowIndex = layer - 1; // 行索引（0-2）

        //        if (layer <= layerCount)
        //        {
        //            // 当前层需要显示

        //            // 激活当前行
        //            if (cardRows[rowIndex] != null)
        //            {
        //                cardRows[rowIndex].SetActive(true);
        //            }

        //            // 设置深度标签文本
        //            if (depthLabels[rowIndex] != null)
        //            {
        //                depthLabels[rowIndex].text = $"深度{layer}";
        //            }

        //            // 从当前深度抽取最多3张卡牌
        //            FishDepth currentDepth = (FishDepth)layer;
        //            List<FishData> layerCards = ItemPool.Instance.DrawUniqueCards(3, currentDepth, null);

        //            // 填充当前行的3个按钮
        //            for (int i = 0; i < 3; i++)
        //            {
        //                CardButton btn = cardButtons[buttonIndex];

        //                if (btn == null)
        //                {
        //                    Debug.LogWarning($"[CardSelectionPanel] cardButtons[{buttonIndex}] 为空");
        //                    buttonIndex++;
        //                    continue;
        //                }

        //                if (i < layerCards.Count)
        //                {
        //                    // 有真实卡牌
        //                    btn.SetCardData(layerCards[i], this, CardButton.DisplayMode.Brief);
        //                    btn.gameObject.SetActive(true);
        //                }
        //                else
        //                {
        //                    // 不足3张，用空牌占位
        //                    btn.SetAsEmptyCard(this);
        //                    btn.gameObject.SetActive(true);
        //                    emptyCardCount++;
        //                }

        //                buttonIndex++;
        //            }

        //            Debug.Log($"[CardSelectionPanel] 深度{layer}：抽取{layerCards.Count}张真实卡牌，{3 - layerCards.Count}张空牌");
        //        }
        //        else
        //        {
        //            // 当前层不需要显示，隐藏整行
        //            if (cardRows[rowIndex] != null)
        //            {
        //                cardRows[rowIndex].SetActive(false);
        //            }

        //            // 跳过这3个按钮的索引
        //            buttonIndex += 3;
        //        }
        //    }

        //    // 记录空牌抽取数量
        //    if (emptyCardCount > 0)
        //    {
        //        GameManager.Instance.RecordEmptyCardDrawn(emptyCardCount);
        //        Debug.Log($"[CardSelectionPanel] 本次抽取了{emptyCardCount}张空牌");
        //    }

        //    // 重置选中状态
        //    selectedCardButton = null;
        //    currentState = PanelState.Selection;

        //    // 显示确定按钮，隐藏捕获/放弃按钮
        //    if (confirmButton != null)
        //    {
        //        confirmButton.gameObject.SetActive(true);
        //    }
        //    if (captureButton != null)
        //    {
        //        captureButton.gameObject.SetActive(false);
        //    }
        //    if (abandonButton != null)
        //    {
        //        abandonButton.gameObject.SetActive(false);
        //    }

        //    // 启用所有卡片按钮（选择阶段允许点击）
        //    SetCardButtonsInteractable(true);

        //    UpdateConfirmButtonState();

        //    // 禁用玩家移动和交互（在确认面板能够打开后）
        //    playerMove.SetInputEnabled(false);

        //    // 显示面板
        //    if (panelRoot != null)
        //    {
        //        Debug.Log($"[CardSelectionPanel] panelRoot不为空，对象名称: {panelRoot.name}");
        //        Debug.Log($"[CardSelectionPanel] 调用SetActive前 - activeSelf: {panelRoot.activeSelf}, activeInHierarchy: {panelRoot.activeInHierarchy}");

        //        // 检查父对象
        //        Transform parent = panelRoot.transform.parent;
        //        if (parent != null)
        //        {
        //            Debug.Log($"[CardSelectionPanel] 父对象: {parent.name}, activeSelf: {parent.gameObject.activeSelf}");
        //        }

        //        panelRoot.SetActive(true);
        //        Debug.Log($"[CardSelectionPanel] 调用SetActive后 - activeSelf: {panelRoot.activeSelf}, activeInHierarchy: {panelRoot.activeInHierarchy}");
        //    }
        //    else
        //    {
        //        Debug.LogError("[CardSelectionPanel] panelRoot为空！无法显示面板！");
        //    }

        //    Debug.Log($"[CardSelectionPanel] 打开面板，显示{layerCount}行卡牌（简略模式）");
        //}

        #endregion

        #region 新抽卡逻辑
        /// <summary>
        /// 打开卡片选择面板
        /// </summary>
        public void OpenPanel(FishDepth drawCardDepth)
        {
            Debug.Log($"[CardSelectionPanel] OpenPanel被调用，深度: {drawCardDepth}");

            // 1. === 基础检查与初始化（保持原逻辑） ===
            GameObject player = GameObject.Find("player");
            if (player == null)
            {
                Debug.LogError("[CardSelectionPanel] 未找到玩家对象（名称应为'player'）");
                return;
            }

            playerState = player.GetComponent<CharacterState>();
            if (playerState == null)
            {
                Debug.LogError("[CardSelectionPanel] 玩家对象缺少CharacterState组件");
                return;
            }

            playerMove = player.GetComponent<PlayerMove>();
            if (playerMove == null)
            {
                Debug.LogError("[CardSelectionPanel] 玩家对象缺少PlayerMove组件");
                return;
            }

            // 清空所有卡片按钮的选中状态
            ClearAllCardButtonSelections();


            // 2. === 数据获取（核心修改） ===
            // 确定当前关卡深度对应的层数 (Depth1=1, Depth2=2, Depth3=3)
            int layerCount = (int)drawCardDepth;

            // 【修改点】一次性获取本关卡所有层级需要的卡牌列表
            // 列表顺序固定为：浅水层3张 -> 中层3张 -> 深水层3张
            List<FishData> allStageCards = ItemPool.Instance.GetCardsForStage(drawCardDepth);

            Debug.Log($"[CardSelectionPanel] 准备显示深度 {drawCardDepth} 的面板，共获取到 {allStageCards.Count} 张卡牌数据");

            int emptyCardCount = 0;     // 空牌计数
            int buttonIndex = 0;        // UI按钮总索引（0-8）
            int cardReadIndex = 0;      // 数据读取指针，用于遍历 allStageCards

            // 3. === UI 填充循环 ===
            // 循环处理每一层 (Row 1 -> Row 2 -> Row 3)
            for (int layer = 1; layer <= 3; layer++)
            {
                int rowIndex = layer - 1; // 行索引（0-2）

                // 判断当前行是否需要显示
                if (layer <= layerCount)
                {
                    // --- A. 显示行与标签 ---
                    if (cardRows[rowIndex] != null)
                    {
                        cardRows[rowIndex].SetActive(true);
                    }

                    if (depthLabels[rowIndex] != null)
                    {
                        depthLabels[rowIndex].text = $"深度{layer}";
                    }

                    // --- B. 填充该行的3个按钮 ---
                    int validCardsInRow = 0;

                    for (int i = 0; i < 3; i++)
                    {
                        CardButton btn = cardButtons[buttonIndex];

                        if (btn == null)
                        {
                            Debug.LogWarning($"[CardSelectionPanel] cardButtons[{buttonIndex}] 为空");
                            buttonIndex++;
                            continue; // 防御性跳过
                        }

                        // 检查数据列表中是否有下一张卡
                        if (cardReadIndex < allStageCards.Count)
                        {
                            // 取出一张卡，指针后移
                            FishData cardData = allStageCards[cardReadIndex];
                            cardReadIndex++;

                            if (cardData != null)
                            {
                                // 这是一个真实存在的卡牌
                                btn.SetCardData(cardData, this, CardButton.DisplayMode.Brief);
                                btn.gameObject.SetActive(true);
                                validCardsInRow++;
                            }
                            else
                            {
                                // 数据是null（理论上不应发生，除非池子返回了null占位）
                                btn.SetAsEmptyCard(this);
                                btn.gameObject.SetActive(true);
                                emptyCardCount++;
                            }
                        }
                        else
                        {
                            // 池子里的牌不够了（例如：牌堆被抽空了），用空牌补位
                            btn.SetAsEmptyCard(this);
                            btn.gameObject.SetActive(true);
                            emptyCardCount++;
                        }

                        // 指向下一个按钮
                        buttonIndex++;
                    }

                    Debug.Log($"[CardSelectionPanel] 深度{layer}行填充完毕：{validCardsInRow} 张实卡");
                }
                else
                {
                    // --- 当前层不需要显示，隐藏整行 ---
                    if (cardRows[rowIndex] != null)
                    {
                        cardRows[rowIndex].SetActive(false);
                    }

                    // 重要：虽然界面隐藏了，但 buttonIndex 必须跳过这3个位置
                    // 以保证 buttonIndex 始终对应正确的 cardButtons 数组索引
                    buttonIndex += 3;
                }
            }


            // 4. === 后续状态处理（保持原逻辑） ===

            // 记录空牌抽取数量
            if (emptyCardCount > 0)
            {
                GameManager.Instance.RecordEmptyCardDrawn(emptyCardCount);
                Debug.Log($"[CardSelectionPanel] 本次面板显示中包含 {emptyCardCount} 张空牌");
            }

            // 重置选中状态
            selectedCardButton = null;
            currentState = PanelState.Selection;

            // 按钮显隐状态管理
            if (confirmButton != null) confirmButton.gameObject.SetActive(true);
            if (captureButton != null) captureButton.gameObject.SetActive(false);
            if (abandonButton != null) abandonButton.gameObject.SetActive(false);

            // 启用交互
            SetCardButtonsInteractable(true);
            UpdateConfirmButtonState();

            // 禁用玩家操作
            playerMove.SetInputEnabled(false);

            // 显示面板根对象
            if (panelRoot != null)
            {
                // 调试父级状态以排查显示问题
                if (panelRoot.transform.parent != null)
                {
                    // Debug.Log($"[CardSelectionPanel] 父对象状态: {panelRoot.transform.parent.gameObject.activeSelf}");
                }

                panelRoot.SetActive(true);
                Debug.Log($"[CardSelectionPanel] 面板已激活");
            }
            else
            {
                Debug.LogError("[CardSelectionPanel] panelRoot为空！无法显示面板！");
            }
        }
        #endregion

        /// <summary>
        /// 关闭卡片选择面板
        /// </summary>
        public void ClosePanel()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            // 清空所有卡片按钮的选中状态
            ClearAllCardButtonSelections();

            // 清理选中状态
            selectedCardButton = null;
            currentState = PanelState.Selection;
            playerState = null;

            // 恢复玩家移动和交互
            if (playerMove != null)
            {
                playerMove.SetInputEnabled(true);
                playerMove = null;
            }

            Debug.Log("[CardSelectionPanel] 关闭面板");
        }

        /// <summary>
        /// 卡片按钮点击回调
        /// 处理单选逻辑
        /// </summary>
        /// <param name="clickedButton">被点击的卡片按钮</param>
        public void OnCardButtonClicked(CardButton clickedButton)
        {
            // 空牌不响应点击
            if (clickedButton.IsEmptyCard())
            {
                Debug.Log("[CardSelectionPanel] 点击了空牌，忽略");
                return;
            }
            
            // 如果处于揭示阶段，不处理卡片点击（不允许切换或取消选择）
            if (currentState == PanelState.Revealed)
            {
                Debug.Log("[CardSelectionPanel] 当前处于揭示阶段，无法切换卡牌选择");
                return;
            }

            // 如果点击的是已选中的按钮，取消选中
            if (selectedCardButton == clickedButton)
            {
                selectedCardButton.SetSelected(false);
                selectedCardButton = null;
            }
            else
            {
                // 取消之前选中的按钮
                if (selectedCardButton != null)
                {
                    selectedCardButton.SetSelected(false);
                }

                // 选中新按钮
                selectedCardButton = clickedButton;
                selectedCardButton.SetSelected(true);
            }

            // 更新确认按钮状态
            UpdateConfirmButtonState();

            Debug.Log($"[CardSelectionPanel] 选中卡牌: {(selectedCardButton != null ? selectedCardButton.GetCardData().itemName : "无")}");
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 更新确认按钮的可用状态
        /// </summary>
        private void UpdateConfirmButtonState()
        {
            if (confirmButton != null)
            {
                // 只有选中了卡牌时才能点击确认按钮
                confirmButton.interactable = (selectedCardButton != null);
            }
        }

        /// <summary>
        /// 确认按钮点击事件
        /// 触发揭示效果并切换到揭示状态
        /// </summary>
        private void OnConfirmButtonClicked()
        {
            if (selectedCardButton == null)
            {
                Debug.LogWarning("[CardSelectionPanel] 未选中任何卡牌，无法确认");
                return;
            }

            ItemData selectedCard = selectedCardButton.GetCardData();

            if (selectedCard == null)
            {
                Debug.LogWarning("[CardSelectionPanel] 选中的卡牌数据为空");
                return;
            }
            
            // 类型检查：钓鱼只会产生鱼类
            if (!(selectedCard is FishData fish))
            {
                Debug.LogError($"[CardSelectionPanel] 卡牌类型错误，应为鱼类，实际为: {selectedCard.category.ToChineseText()}");
                return;
            }

            Debug.Log($"[CardSelectionPanel] 揭示卡牌: {fish.itemName}");

            // 1. 触发揭示效果
            fish.TriggerRevealEffects();

            // 2. 将选中的CardButton切换为详细显示模式
            selectedCardButton.SetDisplayMode(CardButton.DisplayMode.Detailed);

            // 3. 隐藏确定按钮，显示捕获/放弃按钮
            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(false);
            }
            if (captureButton != null)
            {
                captureButton.gameObject.SetActive(true);
            }
            if (abandonButton != null)
            {
                abandonButton.gameObject.SetActive(true);
            }

            // 4. 禁用其他卡片按钮（揭示阶段不允许切换选择，但保持选中按钮可用）
            SetCardButtonsInteractable(false, selectedCardButton);

            // 5. 更新捕获按钮状态（根据体力是否足够）
            UpdateCaptureButtonState(fish);

            // 6. 切换状态为揭示状态
            currentState = PanelState.Revealed;

            Debug.Log($"[CardSelectionPanel] 已切换到揭示状态");
        }

        /// <summary>
        /// 捕获按钮点击事件
        /// 扣除体力、触发捕获效果、添加到手牌、关闭UI
        /// </summary>
        private void OnCaptureButtonClicked()
        {
            if (selectedCardButton == null)
            {
                Debug.LogWarning("[CardSelectionPanel] 未选中任何卡牌，无法捕获");
                return;
            }

            ItemData selectedCard = selectedCardButton.GetCardData();

            if (selectedCard == null)
            {
                Debug.LogWarning("[CardSelectionPanel] 选中的卡牌数据为空");
                return;
            }
            
            // 类型检查：钓鱼只会产生鱼类
            if (!(selectedCard is FishData fish))
            {
                Debug.LogError($"[CardSelectionPanel] 卡牌类型错误，应为鱼类，实际为: {selectedCard.category.ToChineseText()}");
                return;
            }

            if (playerState == null)
            {
                Debug.LogError("[CardSelectionPanel] 玩家状态为空，无法捕获");
                return;
            }

            // 检查体力是否足够（虽然按钮已置灰，但仍需检查）
            if (playerState.CurrentHealth < fish.staminaCost)
            {
                Debug.LogWarning($"[CardSelectionPanel] 体力不足！当前体力: {playerState.CurrentHealth}，需要: {fish.staminaCost}");
                return;
            }

            Debug.Log($"[CardSelectionPanel] 捕获卡牌: {fish.itemName}");

            // 1. 扣除体力
            playerState.ModifyHealth(-fish.staminaCost);
            Debug.Log($"[CardSelectionPanel] 扣除体力 -{fish.staminaCost}，剩余体力: {playerState.CurrentHealth}");

            // 2. 触发捕获效果
            fish.TriggerCaptureEffects();

            // 3. 将卡牌加入手牌
            HandManager.Instance.AddCard(fish);

            // 4. 从牌库删除该卡牌（防止重复获取）
            if (ItemPool.Instance.RemoveCard(fish))
            {
                Debug.Log($"[CardSelectionPanel] 已将{fish.itemName}从牌库中移除");
            }
            else
            {
                Debug.Log($"[CardSelectionPanel] 从牌库中移除{fish.itemName}失败");
            }


                // 5. 关闭窗口
                ClosePanel();
        }

        /// <summary>
        /// 放弃按钮点击事件
        /// 直接关闭UI
        /// </summary>
        private void OnAbandonButtonClicked()
        {
            Debug.Log("[CardSelectionPanel] 放弃卡牌，关闭面板");
            ClosePanel();
        }

        /// <summary>
        /// 更新捕获按钮的可用状态
        /// 根据玩家体力是否足够来决定按钮是否可用
        /// </summary>
        /// <param name="fish">要捕获的鱼类</param>
        private void UpdateCaptureButtonState(FishData fish)
        {
            if (captureButton == null || playerState == null || fish == null)
            {
                return;
            }

            // 检查体力是否足够
            bool hasEnoughStamina = playerState.CurrentHealth >= fish.staminaCost;

            // 设置按钮可用状态
            captureButton.interactable = hasEnoughStamina;

            if (hasEnoughStamina)
            {
                Debug.Log($"[CardSelectionPanel] 体力足够，可以捕获（当前体力: {playerState.CurrentHealth}，消耗: {fish.staminaCost}）");
            }
            else
            {
                Debug.LogWarning($"[CardSelectionPanel] 体力不足，无法捕获（当前体力: {playerState.CurrentHealth}，消耗: {fish.staminaCost}）");
            }
        }

        /// <summary>
        /// 清空所有卡片按钮的选中状态
        /// </summary>
        private void ClearAllCardButtonSelections()
        {
            foreach (var cardButton in cardButtons)
            {
                if (cardButton != null)
                {
                    cardButton.SetSelected(false);
                }
            }
            
            Debug.Log("[CardSelectionPanel] 清空所有卡片按钮的选中状态");
        }

        /// <summary>
        /// 设置所有卡片按钮的可交互状态
        /// </summary>
        /// <param name="interactable">是否可交互</param>
        /// <param name="exceptButton">排除的按钮（保持可交互状态），默认为null</param>
        private void SetCardButtonsInteractable(bool interactable, CardButton exceptButton = null)
        {
            foreach (var cardButton in cardButtons)
            {
                if (cardButton != null)
                {
                    // 如果是排除的按钮，跳过
                    if (exceptButton != null && cardButton == exceptButton)
                    {
                        continue;
                    }
                    
                    Button button = cardButton.GetComponent<Button>();
                    if (button != null)
                    {
                        button.interactable = interactable;
                    }
                }
            }
            
            string exceptInfo = exceptButton != null ? $"（排除选中按钮）" : "";
            Debug.Log($"[CardSelectionPanel] 设置卡片按钮可交互状态: {interactable} {exceptInfo}");
        }

        #endregion
    }
}
