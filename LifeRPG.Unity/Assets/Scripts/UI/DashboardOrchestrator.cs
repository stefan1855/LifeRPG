using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using LifeRPG;

namespace LifeRPG.UI
{
    /// <summary>
    /// 仪表盘总控制器 - 挂在 Canvas 上
    /// 持有所有子面板控制器的引用，协调数据刷新
    /// </summary>
    public class DashboardOrchestrator : MonoBehaviour
    {
        [Header("子面板控制器")]
        [SerializeField] private CharacterPanelController characterPanel;
        [SerializeField] private ExperienceBarController experienceBar;
        [SerializeField] private ContractRingController contractRing;
        [SerializeField] private EquipmentPanelController equipmentPanel;
        [SerializeField] private ActivityLogController activityLog;
        [SerializeField] private SkillListController skillList;
        [SerializeField] private QuickExpInputPanel quickExpPanel;

        [Header("顶部标题栏")]
        [SerializeField] private Text dashboardTitle;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Text refreshButtonText;
        [SerializeField] private Button quickExpButton;
        [SerializeField] private Text quickExpButtonText;

        private Image globalBackground;

        private void Awake()
        {
            Debug.Log("[DashboardOrchestrator] Awake 开始...");

            try
            {
                var canvas = GetComponent<Canvas>();
                if (canvas == null)
                {
                    canvas = gameObject.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }

                var scaler = GetComponent<CanvasScaler>();
                if (scaler == null)
                {
                    scaler = gameObject.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(UIConstants.DesignWidth, UIConstants.DesignHeight);
                    scaler.matchWidthOrHeight = 0.5f;
                }

                if (GetComponent<GraphicRaycaster>() == null)
                    gameObject.AddComponent<GraphicRaycaster>();

                Debug.Log("[DashboardOrchestrator] 创建背景...");
                SetupGlobalBackground();
                Debug.Log("[DashboardOrchestrator] 创建标题栏...");
                SetupTopBar();
                Debug.Log("[DashboardOrchestrator] 创建面板...");
                InitializeAllPanels();
                Debug.Log("[DashboardOrchestrator] Awake 完成 ✓");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DashboardOrchestrator] Awake 异常: {e.Message}\n{e.StackTrace}");
                // 兜底：创建一个可见的红色文字证明脚本在运行
                var errText = UIConstants.CreateText("ErrorMsg", $"错误: {e.Message}", transform,
                    new Rect(100, 100, 600, 60), 20, Color.red, TextAnchor.MiddleLeft, false);
            }
        }

        private void Start()
        {
            RefreshAll();
        }

        // ── 全局背景 ──
        private void SetupGlobalBackground()
        {
            var bgGo = new GameObject("GlobalBackground", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            globalBackground = bgGo.GetComponent<Image>();
            globalBackground.color = UIConstants.DarkBg;
            globalBackground.raycastTarget = false;
            globalBackground.sprite = UIConstants.LoadSprite(UIConstants.TexGlobalBg);
        }

        // ── 顶部标题栏 ──
        private void SetupTopBar()
        {
            var barRect = UIConstants.TopBarRect;

            var barBgGo = new GameObject("TopBarBg", typeof(RectTransform), typeof(Image));
            barBgGo.transform.SetParent(transform, false);
            var barBgRt = barBgGo.GetComponent<RectTransform>();
            barBgRt.anchorMin = barBgRt.anchorMax = new Vector2(0, 1);
            barBgRt.pivot = new Vector2(0, 1);
            barBgRt.anchoredPosition = Vector2.zero;
            barBgRt.sizeDelta = new Vector2(barRect.width, barRect.height);
            var barBgImg = barBgGo.GetComponent<Image>();
            barBgImg.sprite = UIConstants.LoadSprite(UIConstants.TexTitleBar);
            barBgImg.type = Image.Type.Sliced;
            barBgImg.color = Color.white;
            barBgImg.raycastTarget = false;

            // 标题 "角色仪表盘"
            dashboardTitle = UIConstants.CreateTitleText("DashboardTitle", "角 色 仪 表 盘",
                barBgRt, new Rect(barRect.width / 2 - 200, 12, 400, 36),
                UIConstants.FontSizeTitle, UIConstants.Gold, TextAnchor.MiddleCenter);

            // "刷新数据"按钮
            var refBtn = CreateButton("RefreshBtn", "刷 新 数 据",
                new Vector2(barRect.width - 130, 8), new Vector2(110, 36),
                barBgRt, UIConstants.Green, UIConstants.DarkBg);
            refreshButton = refBtn.GetComponent<Button>();
            refreshButtonText = refBtn.GetComponentInChildren<Text>();
            refreshButton.onClick.AddListener(OnRefreshClicked);

            // "快速献祭"按钮
            var expBtn = CreateButton("QuickExpBtn", "快速献祭",
                new Vector2(barRect.width - 260, 8), new Vector2(110, 36),
                barBgRt, UIConstants.Gold, UIConstants.DarkBg);
            quickExpButton = expBtn.GetComponent<Button>();
            quickExpButtonText = expBtn.GetComponentInChildren<Text>();
            quickExpButton.onClick.AddListener(OnQuickExpClicked);
        }

        // ── 初始化子面板 ──
        private void InitializeAllPanels()
        {
            var leftGo = CreatePanelContainer("LeftPanel", UIConstants.LeftPanelRect);
            characterPanel = leftGo.AddComponent<CharacterPanelController>();
            characterPanel.Initialize(leftGo.transform);

            var expGo = CreatePanelContainer("ExpPanel", UIConstants.ExpAreaRect);
            experienceBar = expGo.AddComponent<ExperienceBarController>();
            experienceBar.Initialize(expGo.transform);

            var ringGo = CreatePanelContainer("ContractRingPanel", UIConstants.ContractRingRect);
            contractRing = ringGo.AddComponent<ContractRingController>();
            contractRing.Initialize(ringGo.transform);

            var rightTopGo = CreatePanelContainer("RightTopPanel", UIConstants.RightTopPanelRect);
            equipmentPanel = rightTopGo.AddComponent<EquipmentPanelController>();
            equipmentPanel.Initialize(rightTopGo.transform);
            activityLog = rightTopGo.AddComponent<ActivityLogController>();
            activityLog.Initialize(rightTopGo.transform);

            var rightBottomGo = CreatePanelContainer("RightBottomPanel", UIConstants.RightBottomPanelRect);
            skillList = rightBottomGo.AddComponent<SkillListController>();
            skillList.Initialize(rightBottomGo.transform);

            var quickExpGo = CreatePanelContainer("QuickExpPanel",
                new Rect(0, 0, UIConstants.DesignWidth, UIConstants.DesignHeight));
            quickExpPanel = quickExpGo.AddComponent<QuickExpInputPanel>();
            quickExpPanel.Initialize(quickExpGo.transform);
            quickExpGo.SetActive(false);
        }

        private GameObject CreatePanelContainer(string name, Rect layout)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(layout.x, -layout.y);
            rt.sizeDelta = new Vector2(layout.width, layout.height);

            // 面板内纹理背景
            var innerBgGo = new GameObject("InnerBg", typeof(RectTransform), typeof(Image));
            innerBgGo.transform.SetParent(go.transform, false);
            var innerRt = innerBgGo.GetComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(8, 8); innerRt.offsetMax = new Vector2(-8, -8);
            var innerImg = innerBgGo.GetComponent<Image>();
            innerImg.sprite = UIConstants.LoadSprite(UIConstants.TexPanelInnerV2);
            innerImg.type = Image.Type.Tiled;
            innerImg.color = UIConstants.DarkBg;
            innerImg.raycastTarget = false;

            // 面板边框 (九宫格)
            var borderGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
            borderGo.transform.SetParent(go.transform, false);
            var borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero; borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = Vector2.zero; borderRt.offsetMax = Vector2.zero;
            var borderImg = borderGo.GetComponent<Image>();
            borderImg.sprite = UIConstants.LoadSprite(UIConstants.TexPanelBorderV2);
            borderImg.type = Image.Type.Sliced;
            borderImg.color = Color.white;
            borderImg.raycastTarget = false;

            return go;
        }

        private GameObject CreateButton(string name, string text, Vector2 pos, Vector2 size,
            Transform parent, Color textColor, Color bgColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(pos.x, -pos.y);
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.sprite = UIConstants.LoadSprite(UIConstants.TexButtonMetal);
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = new Color(bgColor.r * 1.3f, bgColor.g * 1.3f, bgColor.b * 1.3f, bgColor.a);
            colors.pressedColor = new Color(bgColor.r * 0.8f, bgColor.g * 0.8f, bgColor.b * 0.8f, bgColor.a);
            btn.colors = colors;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = textRt.anchorMax = new Vector2(0, 0);
            textRt.offsetMin = textRt.offsetMax = Vector2.zero;
            var txt = textGo.GetComponent<Text>();
            txt.text = text;
            txt.fontSize = UIConstants.FontSizeButton;
            txt.color = textColor;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontStyle = FontStyle.Bold;
            txt.raycastTarget = false;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return go;
        }

        // ── 数据刷新 ──
        /// <summary>刷新所有面板数据</summary>
        public void RefreshAll()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("[DashboardOrchestrator] GameManager.Instance 为空");
                LoadDefaultData();
                return;
            }

            Debug.Log("[DashboardOrchestrator] 刷新所有面板...");
            characterPanel?.RefreshFromGameManager();
            experienceBar?.RefreshFromGameManager();
            contractRing?.RefreshFromGameManager();
            equipmentPanel?.RefreshFromGameManager();
            activityLog?.RefreshFromGameManager();
            skillList?.RefreshFromGameManager();
        }

        /// <summary>默认测试数据（GameManager 不可用时）</summary>
        private void LoadDefaultData()
        {
            Debug.Log("[DashboardOrchestrator] 使用默认测试数据填充面板");

            characterPanel?.UpdateDisplay(new CharacterData
            {
                TotalLevel = 27,
                CharacterName = "无 面 者",
                Title = "☠ 暗 影 学 徒 ☠",
                Description = "\"在阴影中行走，聆听灵魂的低语...\"",
                TotalExp = 125760,
                ExpToNextLevel = 24240
            });

            experienceBar?.UpdateDisplay(125760, 24240);

            var nodes = new List<ContractNode>
            {
                new ContractNode { NodeName = "暗影血肉", NodeId = "ShadowFlesh", Level = 5, Exp = 340, MaxExp = 500 },
                new ContractNode { NodeName = "邪能造物", NodeId = "FelCreation", Level = 3, Exp = 120, MaxExp = 300 },
                new ContractNode { NodeName = "虚空之智", NodeId = "VoidWisdom", Level = 7, Exp = 680, MaxExp = 800 },
                new ContractNode { NodeName = "灵魂羁绊", NodeId = "SoulBond", Level = 4, Exp = 210, MaxExp = 400 },
                new ContractNode { NodeName = "不灭意志", NodeId = "EternalWill", Level = 6, Exp = 450, MaxExp = 600 }
            };
            contractRing?.UpdateDisplay(nodes);

            var equipSlots = new List<EquipmentSlot>
            {
                new EquipmentSlot { SlotName = "头部", EquippedItemName = "暗影兜帽" },
                new EquipmentSlot { SlotName = "肩部", EquippedItemName = "" },
                new EquipmentSlot { SlotName = "胸部", EquippedItemName = "灵魂编织者" },
                new EquipmentSlot { SlotName = "护腕", EquippedItemName = "" },
                new EquipmentSlot { SlotName = "手套", EquippedItemName = "" },
                new EquipmentSlot { SlotName = "腰带", EquippedItemName = "虚空之缚" },
                new EquipmentSlot { SlotName = "腿部", EquippedItemName = "" },
                new EquipmentSlot { SlotName = "靴子", EquippedItemName = "暗影行者" }
            };
            equipmentPanel?.UpdateDisplay(equipSlots);

            var logs = new List<ActivityLog>
            {
                new ActivityLog { Title = "暗影施勿", ExpGained = 300, Timestamp = System.DateTime.Now.AddHours(-3) },
                new ActivityLog { Title = "创能黄金", ExpGained = 100, Timestamp = System.DateTime.Now.AddHours(-5) },
                new ActivityLog { Title = "虚空显志", ExpGained = 180, Timestamp = System.DateTime.Now.AddHours(-1) },
                new ActivityLog { Title = "暗影回符", ExpGained = 240, Timestamp = System.DateTime.Now.AddHours(-2) },
                new ActivityLog { Title = "诅咒荣光", ExpGained = 120, Timestamp = System.DateTime.Now.AddHours(-4) },
                new ActivityLog { Title = "低语之舌", ExpGained = 90,  Timestamp = System.DateTime.Now.AddHours(-6) },
                new ActivityLog { Title = "灵魂碎片", ExpGained = 150, Timestamp = System.DateTime.Now.AddHours(-8) }
            };
            activityLog?.UpdateDisplay(logs);

            var skills = new List<SkillEntry>
            {
                new SkillEntry { SkillName = "暗影步", UnlockedCount = 3, TotalCount = 5, Description = "短距传送" },
                new SkillEntry { SkillName = "灵魂吸取", UnlockedCount = 2, TotalCount = 3, Description = "吸取生命" },
                new SkillEntry { SkillName = "虚空之矛", UnlockedCount = 7, TotalCount = 10, Description = "远程攻击" },
                new SkillEntry { SkillName = "暗影护盾", UnlockedCount = 4, TotalCount = 5, Description = "防御技能" },
                new SkillEntry { SkillName = "契约召唤", UnlockedCount = 1, TotalCount = 3, Description = "召唤助手" },
                new SkillEntry { SkillName = "低语侦测", UnlockedCount = 2, TotalCount = 2, Description = "感知敌人" },
                new SkillEntry { SkillName = "邪能爆发", UnlockedCount = 8, TotalCount = 10, Description = "范围伤害" },
                new SkillEntry { SkillName = "灵魂链接", UnlockedCount = 1, TotalCount = 5, Description = "共享伤害" }
            };
            skillList?.UpdateDisplay(skills);
        }

        // ── 按钮事件 ──
        public void OnRefreshClicked()
        {
            Debug.Log("[DashboardOrchestrator] 手动刷新数据");
            RefreshAll();
            if (refreshButtonText != null)
                StartCoroutine(FlashButtonText(refreshButtonText, "已刷新!", UIConstants.White));
        }

        public void OnQuickExpClicked()
        {
            Debug.Log("[DashboardOrchestrator] 打开快速经验录入");
            if (quickExpPanel != null)
            {
                quickExpPanel.gameObject.SetActive(true);
                quickExpPanel.OnExpSubmitted += OnExpSubmitted;
            }
        }

        private void OnExpSubmitted(string nodeTag, int expAmount, string description)
        {
            Debug.Log($"[DashboardOrchestrator] 献祭: {nodeTag} +{expAmount} EXP - {description}");

            var gm = GameManager.Instance;
            if (gm != null)
            {
                // 用节点标签作为 title，描述作为 description
                gm.AddExp(nodeTag, expAmount, nodeTag, description);
            }

            if (quickExpPanel != null)
            {
                quickExpPanel.gameObject.SetActive(false);
                quickExpPanel.OnExpSubmitted -= OnExpSubmitted;
            }

            RefreshAll();
        }

        private System.Collections.IEnumerator FlashButtonText(Text textComponent, string flashText, Color flashColor)
        {
            string originalText = textComponent.text;
            Color originalColor = textComponent.color;
            textComponent.text = flashText;
            textComponent.color = flashColor;
            yield return new WaitForSeconds(0.5f);
            textComponent.text = originalText;
            textComponent.color = originalColor;
        }

        private void OnDestroy()
        {
            if (quickExpPanel != null)
                quickExpPanel.OnExpSubmitted -= OnExpSubmitted;
        }
    }
}
