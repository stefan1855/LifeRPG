using UnityEngine;
using UnityEngine.UI;
using LifeRPG;

namespace LifeRPG.UI
{
    /// <summary>
    /// 角色面板控制器 - 左面板
    /// 显示角色立绘、等级、名字、称号、描述
    /// </summary>
    public class CharacterPanelController : MonoBehaviour
    {
        [Header("UI 组件引用")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text levelText;
        [SerializeField] private Text nameText;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;

        /// <summary>
        /// 初始化面板，自动创建子组件
        /// </summary>
        public void Initialize(Transform parent)
        {
            // 角色立绘
            var portraitGo = UIConstants.CreateUIElement("Portrait", parent);
            var portraitRt = portraitGo.GetComponent<RectTransform>();
            portraitRt.anchorMin = portraitRt.anchorMax = new Vector2(0, 1);
            portraitRt.pivot = new Vector2(0, 1);
            portraitRt.anchoredPosition = new Vector2(UIConstants.PortraitRect.x, -UIConstants.PortraitRect.y);
            portraitRt.sizeDelta = new Vector2(UIConstants.PortraitRect.width, UIConstants.PortraitRect.height);
            portraitImage = portraitGo.AddComponent<Image>();
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;
            portraitImage.sprite = UIConstants.LoadSprite(UIConstants.TexFacelessPortrait);

            // 等级文字 (如 "Lv.27")
            levelText = UIConstants.CreateText("LevelText", "Lv.1", parent,
                UIConstants.LevelTextRect, UIConstants.FontSizeSubtitle,
                UIConstants.Green, TextAnchor.MiddleCenter, true);

            // 名字
            nameText = UIConstants.CreateTitleText("NameText", "无 面 者", parent,
                UIConstants.NameTextRect, UIConstants.FontSizeTitle,
                UIConstants.White, TextAnchor.UpperCenter);

            // 称号
            titleText = UIConstants.CreateText("TitleText", "暗 影 学 徒", parent,
                UIConstants.TitleTextRect, UIConstants.FontSizeBody,
                UIConstants.Green, TextAnchor.UpperCenter, true);

            // 描述
            descriptionText = UIConstants.CreateText("DescText", "", parent,
                UIConstants.DescTextRect, UIConstants.FontSizeTiny,
                UIConstants.Gray, TextAnchor.UpperCenter, false);
        }

        /// <summary>根据角色数据更新面板</summary>
        public void UpdateDisplay(CharacterData data)
        {
            if (data == null) return;

            if (levelText != null)
                levelText.text = $"Lv.{data.TotalLevel}";

            if (nameText != null)
                nameText.text = data.CharacterName ?? "无名者";

            if (titleText != null)
                titleText.text = data.Title ?? "无称号";

            if (descriptionText != null)
                descriptionText.text = data.Description ?? "";
        }

        /// <summary>从 GameManager 加载数据并刷新</summary>
        public void RefreshFromGameManager()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("[CharacterPanel] GameManager.Instance 为空");
                return;
            }
            UpdateDisplay(gm.Character);
        }
    }
}
