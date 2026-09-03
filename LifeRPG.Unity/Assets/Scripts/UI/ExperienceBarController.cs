using UnityEngine;
using UnityEngine.UI;
using LifeRPG;

namespace LifeRPG.UI
{
    /// <summary>
    /// 经验条控制器 - 中上面板
    /// 显示标题、灵魂能量、距离下一级、日期、进度条
    /// </summary>
    public class ExperienceBarController : MonoBehaviour
    {
        [Header("UI 组件引用")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text soulValueText;
        [SerializeField] private Text nextLevelText;
        [SerializeField] private Text dateText;
        [SerializeField] private Image progressBarBg;
        [SerializeField] private Image progressBarFill;
        [SerializeField] private Text progressPercentText;
        [SerializeField] private Text progressExpText;

        /// <summary>初始化面板</summary>
        public void Initialize(Transform parent)
        {
            var pr = UIConstants.ExpAreaRect;

            titleText = UIConstants.CreateTitleText("ExpTitle", "经 验 区 域", parent,
                new Rect(pr.x + 10, pr.y + 5, pr.width - 20, 24),
                UIConstants.FontSizeSubtitle, UIConstants.Gold, TextAnchor.UpperCenter);

            // 三列标签
            UIConstants.CreateText("SoulLabel", "灵魂能量", parent,
                new Rect(UIConstants.SoulValueRect.x, UIConstants.SoulValueRect.y - 18, UIConstants.SoulValueRect.width, 14),
                UIConstants.FontSizeTiny, UIConstants.Dim, TextAnchor.MiddleCenter, false);
            UIConstants.CreateText("NextLabel", "距离下一级", parent,
                new Rect(UIConstants.NextLevelRect.x, UIConstants.NextLevelRect.y - 18, UIConstants.NextLevelRect.width, 14),
                UIConstants.FontSizeTiny, UIConstants.Dim, TextAnchor.MiddleCenter, false);
            UIConstants.CreateText("DateLabel", "契约之日", parent,
                new Rect(UIConstants.DateRect.x, UIConstants.DateRect.y - 18, UIConstants.DateRect.width, 14),
                UIConstants.FontSizeTiny, UIConstants.Dim, TextAnchor.MiddleCenter, false);

            soulValueText = UIConstants.CreateText("SoulValue", "0", parent,
                UIConstants.SoulValueRect, UIConstants.FontSizeSubtitle,
                UIConstants.Green, TextAnchor.MiddleCenter, true);
            nextLevelText = UIConstants.CreateText("NextValue", "0 EXP", parent,
                UIConstants.NextLevelRect, UIConstants.FontSizeBody,
                UIConstants.White, TextAnchor.MiddleCenter, false);
            dateText = UIConstants.CreateText("DateValue", "----/--/--", parent,
                UIConstants.DateRect, UIConstants.FontSizeSmall,
                UIConstants.Gray, TextAnchor.MiddleCenter, false);

            // 进度条底条
            var barBgGo = UIConstants.CreateUIElement("ProgressBarBg", parent);
            var barBgRt = barBgGo.GetComponent<RectTransform>();
            barBgRt.anchorMin = barBgRt.anchorMax = new Vector2(0, 1);
            barBgRt.pivot = new Vector2(0, 1);
            barBgRt.anchoredPosition = new Vector2(UIConstants.ProgressBarRect.x, -UIConstants.ProgressBarRect.y);
            barBgRt.sizeDelta = new Vector2(UIConstants.ProgressBarRect.width, UIConstants.ProgressBarRect.height);
            progressBarBg = barBgGo.AddComponent<Image>();
            progressBarBg.sprite = UIConstants.LoadSprite(UIConstants.TexProgressBg);
            progressBarBg.type = Image.Type.Sliced;
            progressBarBg.color = Color.white;
            progressBarBg.raycastTarget = false;

            // 进度条填充
            var barFillGo = UIConstants.CreateUIElement("ProgressBarFill", barBgGo.transform);
            var barFillRt = barFillGo.GetComponent<RectTransform>();
            barFillRt.anchorMin = new Vector2(0, 0);
            barFillRt.anchorMax = new Vector2(0, 1);
            barFillRt.pivot = new Vector2(0, 0.5f);
            barFillRt.offsetMin = Vector2.zero;
            barFillRt.offsetMax = Vector2.zero;
            progressBarFill = barFillGo.AddComponent<Image>();
            progressBarFill.sprite = UIConstants.LoadSprite(UIConstants.TexProgressFill);
            progressBarFill.color = Color.white;
            progressBarFill.raycastTarget = false;
            progressBarFill.type = Image.Type.Filled;
            progressBarFill.fillMethod = Image.FillMethod.Horizontal;

            progressPercentText = UIConstants.CreateText("ProgPct", "0%", parent,
                UIConstants.ProgressBarRect, UIConstants.FontSizeBody,
                UIConstants.Green, TextAnchor.UpperCenter, true);
            progressExpText = UIConstants.CreateText("ProgExp", "0 / 0 EXP", parent,
                UIConstants.ProgressExpRect, UIConstants.FontSizeTiny,
                UIConstants.Gray, TextAnchor.UpperCenter, false);
        }

        /// <summary>更新经验条显示</summary>
        public void UpdateDisplay(int totalExp, int expToNextLevel)
        {
            // 使用 ExpToNextLevel 作为当前级别的"经验上限"来计算进度
            int effectiveMax = Mathf.Max(totalExp, expToNextLevel);
            float fill = (effectiveMax > 0) ? Mathf.Clamp01((float)totalExp / effectiveMax) : 0f;

            if (soulValueText != null)
                soulValueText.text = totalExp.ToString("N0");
            if (nextLevelText != null)
                nextLevelText.text = $"{expToNextLevel:N0} EXP";
            if (progressBarFill != null)
                progressBarFill.fillAmount = fill;
            if (progressPercentText != null)
                progressPercentText.text = $"{fill * 100f:F1}%";
            if (progressExpText != null)
                progressExpText.text = $"{totalExp:N0} / {effectiveMax:N0} EXP";
        }

        /// <summary>从 GameManager 加载并刷新</summary>
        public void RefreshFromGameManager()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("[ExperienceBar] GameManager.Instance 为空");
                return;
            }

            if (gm.Character != null)
            {
                UpdateDisplay(gm.Character.TotalExp, gm.Character.ExpToNextLevel);
            }

            if (dateText != null)
            {
                dateText.text = gm.Character?.LastUpdated.ToString("yyyy-MM-dd") ?? "----/--/--";
            }
        }
    }
}
