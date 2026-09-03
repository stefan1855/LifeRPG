using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using LifeRPG;

namespace LifeRPG.UI
{
    /// <summary>
    /// 活动日志控制器 - 右上面板右列
    /// 显示最近变化日志列表
    /// </summary>
    public class ActivityLogController : MonoBehaviour
    {
        [Header("UI 组件引用")]
        [SerializeField] private Text titleText;
        [SerializeField] private Transform logListContainer;
        [SerializeField] private Button viewAllButton;
        [SerializeField] private Text viewAllButtonText;

        private List<ActivityLogEntryUI> logEntryUIs = new List<ActivityLogEntryUI>();
        private const int MaxVisibleEntries = 7;

        /// <summary>初始化面板</summary>
        public void Initialize(Transform parent)
        {
            var panelRect = UIConstants.RightTopPanelRect;
            float logColumnX = 250;
            float logColumnW = panelRect.width - logColumnX - 10;

            titleText = UIConstants.CreateTitleText("LogTitle", "最 近 变 化", parent,
                new Rect(panelRect.x + logColumnX, panelRect.y + 5, logColumnW, 24),
                UIConstants.FontSizeSubtitle, UIConstants.Gold, TextAnchor.UpperCenter);

            // 日志列表容器
            var listGo = UIConstants.CreateUIElement("LogListContainer", parent);
            var listRt = listGo.GetComponent<RectTransform>();
            listRt.anchorMin = listRt.anchorMax = new Vector2(0, 1);
            listRt.pivot = new Vector2(0, 1);
            listRt.anchoredPosition = new Vector2(panelRect.x + logColumnX + 5, -(panelRect.y + 35));
            listRt.sizeDelta = new Vector2(logColumnW - 10, MaxVisibleEntries * 28 + 10);
            logListContainer = listRt;

            for (int i = 0; i < MaxVisibleEntries; i++)
                logEntryUIs.Add(CreateLogEntry($"LogEntry_{i}", logListContainer, i));

            // "查看全部日志"按钮
            var btnGo = UIConstants.CreateUIElement("ViewAllBtn", parent);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(0, 1);
            btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.anchoredPosition = new Vector2(panelRect.x + panelRect.width * 0.5f, -(panelRect.y + panelRect.height - 20));
            btnRt.sizeDelta = new Vector2(180, 30);

            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.10f, 0.13f, 0.10f, 0.9f);
            viewAllButton = btnGo.AddComponent<Button>();
            viewAllButton.targetGraphic = btnImg;

            viewAllButtonText = UIConstants.CreateText("ViewAllText", "查看全部日志 →",
                btnRt, genRect(btnRt), UIConstants.FontSizeSmall,
                UIConstants.Green, TextAnchor.MiddleCenter, true);
            viewAllButtonText.raycastTarget = false;

            viewAllButton.onClick.AddListener(OnViewAllClicked);
        }

        private Rect genRect(RectTransform rt) => new Rect(0, 0, rt.sizeDelta.x, rt.sizeDelta.y);

        private ActivityLogEntryUI CreateLogEntry(string name, Transform parent, int index)
        {
            var entryGo = UIConstants.CreateUIElement(name, parent);
            var entryRt = entryGo.GetComponent<RectTransform>();
            entryRt.anchorMin = entryRt.anchorMax = new Vector2(0, 1);
            entryRt.pivot = new Vector2(0, 1);
            entryRt.anchoredPosition = new Vector2(4, -(index * 28 + 4));
            entryRt.sizeDelta = new Vector2(((RectTransform)parent).sizeDelta.x - 8, 24);

            var entryUI = new ActivityLogEntryUI();

            // 绿色图标
            var iconGo = UIConstants.CreateUIElement("Icon", entryRt);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0, 0.5f);
            iconRt.sizeDelta = new Vector2(14, 14);
            iconRt.anchoredPosition = new Vector2(0, 0);
            entryUI.IconImage = iconGo.AddComponent<Image>();
            entryUI.IconImage.raycastTarget = false;
            entryUI.IconImage.sprite = UIConstants.LoadSprite(UIConstants.TexStarIconGreen);

            // 标题
            entryUI.TitleText = UIConstants.CreateText("Title", "", entryRt,
                new Rect(18, -4, entryRt.sizeDelta.x * 0.35f, 14),
                UIConstants.FontSizeSmall, UIConstants.Green, TextAnchor.MiddleLeft, false);

            // EXP值
            entryUI.ExpText = UIConstants.CreateText("Exp", "", entryRt,
                new Rect(18 + entryRt.sizeDelta.x * 0.35f + 10, -4, 80, 14),
                UIConstants.FontSizeTiny, UIConstants.White, TextAnchor.MiddleLeft, false);

            // 时间
            entryUI.TimeText = UIConstants.CreateText("Time", "", entryRt,
                new Rect(entryRt.sizeDelta.x - 60, -4, 60, 14),
                9, UIConstants.Dim, TextAnchor.MiddleRight, false);

            return entryUI;
        }

        /// <summary>更新日志列表</summary>
        public void UpdateDisplay(List<ActivityLog> logs)
        {
            if (logs == null) return;

            for (int i = 0; i < logEntryUIs.Count; i++)
            {
                var ui = logEntryUIs[i];
                bool hasData = i < logs.Count;

                ui.IconImage?.gameObject.SetActive(hasData);
                ui.TitleText?.gameObject.SetActive(hasData);
                ui.ExpText?.gameObject.SetActive(hasData);
                ui.TimeText?.gameObject.SetActive(hasData);

                if (hasData)
                {
                    if (ui.TitleText != null)
                        ui.TitleText.text = logs[i].Title;
                    if (ui.ExpText != null)
                        ui.ExpText.text = $"+{logs[i].ExpGained} EXP";
                    if (ui.TimeText != null)
                        ui.TimeText.text = logs[i].TimeAgoDisplay;
                }
            }
        }

        /// <summary>从 GameManager 加载并刷新</summary>
        public void RefreshFromGameManager()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("[ActivityLog] GameManager.Instance 为空");
                return;
            }
            UpdateDisplay(gm.GetRecentActivities(MaxVisibleEntries));
        }

        private void OnViewAllClicked()
        {
            Debug.Log("[ActivityLog] 查看全部日志");
            var gm = GameManager.Instance;
            if (gm != null)
            {
                var allLogs = gm.GetRecentActivities(50);
                UpdateDisplay(allLogs);
            }
        }
    }

    /// <summary>活动日志条目 UI 引用</summary>
    [System.Serializable]
    public class ActivityLogEntryUI
    {
        public Image IconImage;
        public Text TitleText;
        public Text ExpText;
        public Text TimeText;
    }
}
