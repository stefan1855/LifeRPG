using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using LifeRPG;

namespace LifeRPG.UI
{
    /// <summary>
    /// 技能列表面板控制器 - 右下面板
    /// 显示展开技能与NPC/地图列表
    /// </summary>
    public class SkillListController : MonoBehaviour
    {
        [Header("UI 组件引用")]
        [SerializeField] private Text titleText;
        [SerializeField] private Transform skillListContainer;
        [SerializeField] private ScrollRect scrollRect;

        private List<SkillEntryUI> skillEntryUIs = new List<SkillEntryUI>();
        private const int MaxVisibleSkills = 10;

        /// <summary>初始化面板</summary>
        public void Initialize(Transform parent)
        {
            var panelRect = UIConstants.RightBottomPanelRect;

            titleText = UIConstants.CreateTitleText("SkillTitle", "展开技能与NPC/地图", parent,
                new Rect(panelRect.x + 10, panelRect.y + 5, panelRect.width - 20, 24),
                UIConstants.FontSizeSubtitle, UIConstants.Gold, TextAnchor.UpperCenter);

            // 滚动区域
            var scrollGo = UIConstants.CreateUIElement("SkillScrollView", parent);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = scrollRt.anchorMax = new Vector2(0, 1);
            scrollRt.pivot = new Vector2(0, 1);
            scrollRt.anchoredPosition = new Vector2(panelRect.x + 10, -(panelRect.y + 35));
            scrollRt.sizeDelta = new Vector2(panelRect.width - 20, panelRect.height - 45);

            scrollRect = scrollGo.AddComponent<ScrollRect>();
            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = new Color(0.05f, 0.07f, 0.05f, 0.5f);
            scrollImg.raycastTarget = true;

            // 内容容器
            var contentGo = UIConstants.CreateUIElement("SkillContent", scrollRt);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0, 1);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0, MaxVisibleSkills * 36 + 10);
            skillListContainer = contentRt;

            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // 预创建技能条目
            for (int i = 0; i < MaxVisibleSkills; i++)
                skillEntryUIs.Add(CreateSkillEntry($"Skill_{i}", skillListContainer, i));
        }

        private SkillEntryUI CreateSkillEntry(string name, Transform parent, int index)
        {
            var entryGo = UIConstants.CreateUIElement(name, parent);
            var entryRt = entryGo.GetComponent<RectTransform>();
            entryRt.anchorMin = entryRt.anchorMax = new Vector2(0, 1);
            entryRt.pivot = new Vector2(0, 1);
            entryRt.anchoredPosition = new Vector2(4, -(index * 36 + 4));
            float w = ((RectTransform)parent).sizeDelta.x - 8;
            entryRt.sizeDelta = new Vector2(w, 32);

            // 背景
            var bgGo = UIConstants.CreateUIElement("EntryBg", entryRt);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = bgRt.anchorMax = new Vector2(0, 0);
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.10f, 0.08f, 0.6f);
            bgImg.raycastTarget = false;

            var entryUI = new SkillEntryUI();

            // 图标
            var iconGo = UIConstants.CreateUIElement("Icon", entryRt);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0, 0.5f);
            iconRt.sizeDelta = new Vector2(20, 20);
            iconRt.anchoredPosition = new Vector2(4, 0);
            entryUI.IconImage = iconGo.AddComponent<Image>();
            entryUI.IconImage.raycastTarget = false;
            entryUI.IconImage.sprite = UIConstants.LoadSprite(UIConstants.TexStarIconGreen);

            // 技能名称
            entryUI.NameText = UIConstants.CreateText("SkillName", "", entryRt,
                new Rect(28, -4, w * 0.45f, 18),
                UIConstants.FontSizeSmall, UIConstants.White, TextAnchor.MiddleLeft, false);

            // 解锁进度 (如 "3/5")
            entryUI.LevelText = UIConstants.CreateText("SkillProgress", "", entryRt,
                new Rect(28 + w * 0.45f + 10, -4, 60, 18),
                UIConstants.FontSizeTiny, UIConstants.Green, TextAnchor.MiddleLeft, false);

            // 描述
            entryUI.DescriptionText = UIConstants.CreateText("SkillDesc", "", entryRt,
                new Rect(28 + w * 0.45f + 70, -4, w * 0.45f - 70, 18),
                9, UIConstants.Dim, TextAnchor.MiddleLeft, false);

            return entryUI;
        }

        /// <summary>更新技能列表</summary>
        public void UpdateDisplay(List<SkillEntry> skills)
        {
            if (skills == null) return;

            for (int i = 0; i < skillEntryUIs.Count; i++)
            {
                var ui = skillEntryUIs[i];
                bool hasData = i < skills.Count;
                ui.IconImage?.transform.parent?.gameObject.SetActive(hasData);

                if (hasData)
                {
                    if (ui.NameText != null)
                        ui.NameText.text = skills[i].SkillName;
                    if (ui.LevelText != null)
                        ui.LevelText.text = $"{skills[i].UnlockedCount}/{skills[i].TotalCount}";
                    if (ui.DescriptionText != null)
                        ui.DescriptionText.text = skills[i].Description;
                }
            }

            // 更新内容区域高度
            if (skillListContainer != null)
            {
                var contentRt = skillListContainer.GetComponent<RectTransform>();
                if (contentRt != null)
                {
                    int visibleCount = Mathf.Min(skills.Count, MaxVisibleSkills);
                    contentRt.sizeDelta = new Vector2(contentRt.sizeDelta.x, visibleCount * 36 + 10);
                }
            }
        }

        /// <summary>从 GameManager 加载并刷新</summary>
        public void RefreshFromGameManager()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("[SkillList] GameManager.Instance 为空");
                return;
            }
            UpdateDisplay(gm.Skills);
        }
    }

    /// <summary>技能条目 UI 组件引用</summary>
    [System.Serializable]
    public class SkillEntryUI
    {
        public Image IconImage;
        public Text NameText;
        public Text LevelText;
        public Text DescriptionText;
    }
}
