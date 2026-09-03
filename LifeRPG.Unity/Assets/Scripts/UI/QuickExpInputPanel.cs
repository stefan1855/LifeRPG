using UnityEngine;
using UnityEngine.UI;
using System;

namespace LifeRPG.UI
{
    /// <summary>
    /// 快速经验录入弹窗 - 暗影风格
    /// 半透明黑底 + 绿色边框 + 下拉选择契约节点 + EXP输入 + 描述输入 + 献祭按钮
    /// </summary>
    public class QuickExpInputPanel : MonoBehaviour
    {
        [Header("UI 组件引用")]
        [SerializeField] private Image overlayBackground;     // 全屏半透明黑遮罩
        [SerializeField] private Image popupPanel;            // 弹窗面板背景
        [SerializeField] private Text titleText;              // 标题
        [SerializeField] private Dropdown nodeDropdown;       // 契约节点下拉
        [SerializeField] private InputField expInputField;    // EXP数值输入
        [SerializeField] private InputField descInputField;   // 事迹描述输入
        [SerializeField] private Button submitButton;         // 献祭按钮
        [SerializeField] private Text submitButtonText;       // 献祭按钮文字
        [SerializeField] private Button cancelButton;         // 取消按钮
        [SerializeField] private Text cancelButtonText;       // 取消按钮文字

        /// <summary>经验提交回调: (节点名称, EXP数量, 描述)</summary>
        public event Action<string, int, string> OnExpSubmitted;

        /// <summary>弹窗尺寸</summary>
        private const float PopupWidth = 420f;
        private const float PopupHeight = 380f;

        /// <summary>
        /// 初始化弹窗
        /// </summary>
        public void Initialize(Transform parent)
        {
            // 全屏半透明遮罩
            var overlayGo = new GameObject("Overlay", typeof(RectTransform), typeof(Image), typeof(Button));
            overlayGo.transform.SetParent(transform, false);
            var overlayRt = overlayGo.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            overlayBackground = overlayGo.GetComponent<Image>();
            overlayBackground.color = UIConstants.OverlayBlack;
            overlayBackground.raycastTarget = true;

            // 点击遮罩关闭
            var overlayBtn = overlayGo.GetComponent<Button>();
            overlayBtn.onClick.AddListener(Hide);
            overlayBtn.targetGraphic = overlayBackground;

            // 弹窗面板主体
            var popupGo = new GameObject("PopupPanel", typeof(RectTransform), typeof(Image));
            popupGo.transform.SetParent(transform, false);
            var popupRt = popupGo.GetComponent<RectTransform>();
            popupRt.anchorMin = popupRt.anchorMax = new Vector2(0.5f, 0.5f);
            popupRt.sizeDelta = new Vector2(PopupWidth, PopupHeight);
            popupRt.anchoredPosition = Vector2.zero;
            popupPanel = popupGo.GetComponent<Image>();
            popupPanel.color = new Color(0.06f, 0.08f, 0.06f, 0.97f);
            popupPanel.raycastTarget = true;

            // 绿色边框装饰（使用 panel_border 九宫格）
            var borderGo = new GameObject("PopupBorder", typeof(RectTransform), typeof(Image));
            borderGo.transform.SetParent(popupRt, false);
            var borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-4, -4);
            borderRt.offsetMax = new Vector2(4, 4);
            var borderImg = borderGo.GetComponent<Image>();
            borderImg.type = Image.Type.Sliced;
            borderImg.color = UIConstants.Green;
            borderImg.raycastTarget = false;
            var borderTex = Resources.Load<Texture2D>(UIConstants.TexPanelBorder);
            if (borderTex != null)
                borderImg.sprite = Sprite.Create(borderTex, new Rect(0, 0, borderTex.width, borderTex.height), new Vector2(0.5f, 0.5f));

            // 标题 "献祭灵魂碎片"
            titleText = UIConstants.CreateTitleText("Title", "献 祭 灵 魂 碎 片", popupRt,
                new Rect(0, 15, PopupWidth, 32), 24, UIConstants.Gold, TextAnchor.MiddleCenter);

            // 分隔线装饰
            var dividerGo = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            dividerGo.transform.SetParent(popupRt, false);
            var dividerRt = dividerGo.GetComponent<RectTransform>();
            dividerRt.anchorMin = dividerRt.anchorMax = new Vector2(0, 1);
            dividerRt.pivot = new Vector2(0, 1);
            dividerRt.anchoredPosition = new Vector2(30, -52);
            dividerRt.sizeDelta = new Vector2(PopupWidth - 60, 1);
            var dividerImg = dividerGo.GetComponent<Image>();
            dividerImg.color = UIConstants.Green;
            dividerImg.raycastTarget = false;

            // 下拉选择标签
            CreateText("NodeLabel", "契约节点:", popupRt,
                new Rect(30, 65, 100, 24), UIConstants.FontSizeSmall, UIConstants.White, TextAnchor.MiddleLeft, false);

            // 下拉选择
            var dropdownGo = new GameObject("NodeDropdown", typeof(RectTransform), typeof(Image), typeof(Dropdown));
            dropdownGo.transform.SetParent(popupRt, false);
            var dropdownRtGo = dropdownGo.GetComponent<RectTransform>();
            dropdownRtGo.anchorMin = dropdownRtGo.anchorMax = new Vector2(0, 1);
            dropdownRtGo.pivot = new Vector2(0, 1);
            dropdownRtGo.anchoredPosition = new Vector2(120, -65);
            dropdownRtGo.sizeDelta = new Vector2(PopupWidth - 150, 32);
            var dropdownImg = dropdownGo.GetComponent<Image>();
            dropdownImg.color = new Color(0.08f, 0.10f, 0.08f, 1f);

            nodeDropdown = dropdownGo.GetComponent<Dropdown>();
            nodeDropdown.options.Clear();
            foreach (var name in UIConstants.ContractNodeDisplayNames)
            {
                nodeDropdown.options.Add(new Dropdown.OptionData(name));
            }
            // 设置下拉模板样式（使用内置字体）
            SetupDropdownTemplate(nodeDropdown);

            // 创建下拉箭头的文字
            var arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(Text));
            arrowGo.transform.SetParent(dropdownGo.transform, false);
            var arrowRt = arrowGo.GetComponent<RectTransform>();
            arrowRt.anchorMin = new Vector2(1, 0.5f);
            arrowRt.anchorMax = new Vector2(1, 0.5f);
            arrowRt.pivot = new Vector2(1, 0.5f);
            arrowRt.anchoredPosition = new Vector2(-10, 0);
            arrowRt.sizeDelta = new Vector2(14, 14);
            var arrowText = arrowGo.GetComponent<Text>();
            arrowText.text = "▼";
            arrowText.fontSize = 10;
            arrowText.color = UIConstants.Green;
            arrowText.alignment = TextAnchor.MiddleCenter;
            arrowText.raycastTarget = false;
            arrowText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 创建下拉标签
            var dropLabelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            dropLabelGo.transform.SetParent(dropdownGo.transform, false);
            var dropLabelRt = dropLabelGo.GetComponent<RectTransform>();
            dropLabelRt.anchorMin = dropLabelRt.anchorMax = new Vector2(0, 0);
            dropLabelRt.offsetMin = new Vector2(8, 4);
            dropLabelRt.offsetMax = new Vector2(-30, -4);
            var dropLabelText = dropLabelGo.GetComponent<Text>();
            dropLabelText.text = UIConstants.ContractNodeDisplayNames[0];
            dropLabelText.fontSize = UIConstants.FontSizeSmall;
            dropLabelText.color = UIConstants.White;
            dropLabelText.alignment = TextAnchor.MiddleLeft;
            dropLabelText.raycastTarget = false;
            dropLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nodeDropdown.captionText = dropLabelText;

            // EXP输入标签
            CreateText("ExpLabel", "获得EXP:", popupRt,
                new Rect(30, 115, 100, 24), UIConstants.FontSizeSmall, UIConstants.White, TextAnchor.MiddleLeft, false);

            // EXP输入框
            var expInputGo = new GameObject("ExpInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            expInputGo.transform.SetParent(popupRt, false);
            var expInputRt = expInputGo.GetComponent<RectTransform>();
            expInputRt.anchorMin = expInputRt.anchorMax = new Vector2(0, 1);
            expInputRt.pivot = new Vector2(0, 1);
            expInputRt.anchoredPosition = new Vector2(120, -115);
            expInputRt.sizeDelta = new Vector2(120, 32);
            var expInputImg = expInputGo.GetComponent<Image>();
            expInputImg.color = new Color(0.08f, 0.10f, 0.08f, 1f);

            expInputField = expInputGo.GetComponent<InputField>();
            expInputField.contentType = InputField.ContentType.IntegerNumber;
            expInputField.text = "100";

            // EXP输入框文字
            var expInputTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            expInputTextGo.transform.SetParent(expInputGo.transform, false);
            var expInputTextRt = expInputTextGo.GetComponent<RectTransform>();
            expInputTextRt.anchorMin = expInputTextRt.anchorMax = new Vector2(0, 0);
            expInputTextRt.offsetMin = new Vector2(8, 4);
            expInputTextRt.offsetMax = new Vector2(-8, -4);
            var expInputText = expInputTextGo.GetComponent<Text>();
            expInputText.text = "100";
            expInputText.fontSize = UIConstants.FontSizeBody;
            expInputText.color = UIConstants.White;
            expInputText.alignment = TextAnchor.MiddleLeft;
            expInputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            expInputText.raycastTarget = true;
            expInputField.textComponent = expInputText;

            // EXP输入占位符
            var expPlaceholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            expPlaceholderGo.transform.SetParent(expInputGo.transform, false);
            var expPlaceholderRt = expPlaceholderGo.GetComponent<RectTransform>();
            expPlaceholderRt.anchorMin = expPlaceholderRt.anchorMax = new Vector2(0, 0);
            expPlaceholderRt.offsetMin = new Vector2(8, 4);
            expPlaceholderRt.offsetMax = new Vector2(-8, -4);
            var expPlaceholderText = expPlaceholderGo.GetComponent<Text>();
            expPlaceholderText.text = "输入EXP...";
            expPlaceholderText.fontSize = UIConstants.FontSizeSmall;
            expPlaceholderText.color = UIConstants.Dim;
            expPlaceholderText.alignment = TextAnchor.MiddleLeft;
            expPlaceholderText.fontStyle = FontStyle.Italic;
            expPlaceholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            expPlaceholderText.raycastTarget = false;
            expInputField.placeholder = expPlaceholderText;

            // 描述输入标签
            CreateText("DescLabel", "事迹描述:", popupRt,
                new Rect(30, 165, 100, 24), UIConstants.FontSizeSmall, UIConstants.White, TextAnchor.MiddleLeft, false);

            // 描述输入框（多行区域）
            var descInputGo = new GameObject("DescInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            descInputGo.transform.SetParent(popupRt, false);
            var descInputRt = descInputGo.GetComponent<RectTransform>();
            descInputRt.anchorMin = descInputRt.anchorMax = new Vector2(0, 1);
            descInputRt.pivot = new Vector2(0, 1);
            descInputRt.anchoredPosition = new Vector2(30, -195);
            descInputRt.sizeDelta = new Vector2(PopupWidth - 60, 68);
            var descInputImg = descInputGo.GetComponent<Image>();
            descInputImg.color = new Color(0.08f, 0.10f, 0.08f, 1f);

            descInputField = descInputGo.GetComponent<InputField>();
            descInputField.contentType = InputField.ContentType.Standard;
            descInputField.lineType = InputField.LineType.MultiLineNewline;
            descInputField.text = "";

            // 描述输入框文字
            var descInputTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            descInputTextGo.transform.SetParent(descInputGo.transform, false);
            var descInputTextRt = descInputTextGo.GetComponent<RectTransform>();
            descInputTextRt.anchorMin = descInputTextRt.anchorMax = new Vector2(0, 0);
            descInputTextRt.offsetMin = new Vector2(8, 4);
            descInputTextRt.offsetMax = new Vector2(-8, -4);
            var descInputText = descInputTextGo.GetComponent<Text>();
            descInputText.text = "";
            descInputText.fontSize = UIConstants.FontSizeSmall;
            descInputText.color = UIConstants.White;
            descInputText.alignment = TextAnchor.UpperLeft;
            descInputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descInputText.raycastTarget = true;
            descInputField.textComponent = descInputText;

            // 描述输入占位符
            var descPlaceholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            descPlaceholderGo.transform.SetParent(descInputGo.transform, false);
            var descPlaceholderRt = descPlaceholderGo.GetComponent<RectTransform>();
            descPlaceholderRt.anchorMin = descPlaceholderRt.anchorMax = new Vector2(0, 0);
            descPlaceholderRt.offsetMin = new Vector2(8, 4);
            descPlaceholderRt.offsetMax = new Vector2(-8, -4);
            var descPlaceholderText = descPlaceholderGo.GetComponent<Text>();
            descPlaceholderText.text = "记录此契约事迹...";
            descPlaceholderText.fontSize = UIConstants.FontSizeSmall;
            descPlaceholderText.color = UIConstants.Dim;
            descPlaceholderText.alignment = TextAnchor.UpperLeft;
            descPlaceholderText.fontStyle = FontStyle.Italic;
            descPlaceholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descPlaceholderText.raycastTarget = false;
            descInputField.placeholder = descPlaceholderText;

            // 分隔线2
            var divider2Go = new GameObject("Divider2", typeof(RectTransform), typeof(Image));
            divider2Go.transform.SetParent(popupRt, false);
            var divider2Rt = divider2Go.GetComponent<RectTransform>();
            divider2Rt.anchorMin = divider2Rt.anchorMax = new Vector2(0, 1);
            divider2Rt.pivot = new Vector2(0, 1);
            divider2Rt.anchoredPosition = new Vector2(30, -280);
            divider2Rt.sizeDelta = new Vector2(PopupWidth - 60, 1);
            var divider2Img = divider2Go.GetComponent<Image>();
            divider2Img.color = UIConstants.Green;
            divider2Img.raycastTarget = false;

            // "献祭"按钮（绿色发光）
            var submitGo = new GameObject("SubmitBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            submitGo.transform.SetParent(popupRt, false);
            var submitRt = submitGo.GetComponent<RectTransform>();
            submitRt.anchorMin = submitRt.anchorMax = new Vector2(0, 1);
            submitRt.pivot = new Vector2(0.5f, 1);
            submitRt.anchoredPosition = new Vector2(PopupWidth * 0.31f, -300);
            submitRt.sizeDelta = new Vector2(140, 40);
            var submitImg = submitGo.GetComponent<Image>();
            submitImg.color = new Color(0.15f, 0.30f, 0.10f, 1f);
            submitImg.raycastTarget = true;

            submitButton = submitGo.GetComponent<Button>();
            submitButton.targetGraphic = submitImg;
            var submitColors = submitButton.colors;
            submitColors.normalColor = new Color(0.15f, 0.30f, 0.10f, 1f);
            submitColors.highlightedColor = new Color(0.25f, 0.50f, 0.15f, 1f);
            submitColors.pressedColor = new Color(0.10f, 0.20f, 0.05f, 1f);
            submitButton.colors = submitColors;
            submitButton.onClick.AddListener(OnSubmitClicked);

            submitButtonText = CreateText("SubmitText", "献 祭", submitRt,
                new Rect(0, 0, 140, 40), 18, UIConstants.Green, TextAnchor.MiddleCenter, true);
            submitButtonText.transform.SetParent(submitRt, false);
            var submitTextRtLocal = submitButtonText.GetComponent<RectTransform>();
            submitTextRtLocal.anchorMin = Vector2.zero;
            submitTextRtLocal.anchorMax = Vector2.one;
            submitTextRtLocal.offsetMin = Vector2.zero;
            submitTextRtLocal.offsetMax = Vector2.zero;

            // "取消"按钮
            var cancelGo = new GameObject("CancelBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            cancelGo.transform.SetParent(popupRt, false);
            var cancelRt = cancelGo.GetComponent<RectTransform>();
            cancelRt.anchorMin = cancelRt.anchorMax = new Vector2(0, 1);
            cancelRt.pivot = new Vector2(0.5f, 1);
            cancelRt.anchoredPosition = new Vector2(PopupWidth * 0.69f, -300);
            cancelRt.sizeDelta = new Vector2(140, 40);
            var cancelImg = cancelGo.GetComponent<Image>();
            cancelImg.color = new Color(0.10f, 0.12f, 0.10f, 1f);
            cancelImg.raycastTarget = true;

            cancelButton = cancelGo.GetComponent<Button>();
            cancelButton.targetGraphic = cancelImg;
            cancelButton.onClick.AddListener(Hide);

            cancelButtonText = CreateText("CancelText", "取 消", cancelRt,
                new Rect(0, 0, 140, 40), 18, UIConstants.Dim, TextAnchor.MiddleCenter, true);
            cancelButtonText.transform.SetParent(cancelRt, false);
            var cancelTextRtLocal = cancelButtonText.GetComponent<RectTransform>();
            cancelTextRtLocal.anchorMin = Vector2.zero;
            cancelTextRtLocal.anchorMax = Vector2.one;
            cancelTextRtLocal.offsetMin = Vector2.zero;
            cancelTextRtLocal.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 设置下拉框的模板样式
        /// </summary>
        private void SetupDropdownTemplate(Dropdown dropdown)
        {
            // 创建下拉列表模板
            var templateGo = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            templateGo.transform.SetParent(dropdown.transform, false);
            var templateRt = templateGo.GetComponent<RectTransform>();
            templateRt.anchorMin = new Vector2(0, 0);
            templateRt.anchorMax = new Vector2(1, 0);
            templateRt.pivot = new Vector2(0.5f, 1);
            templateRt.sizeDelta = new Vector2(0, 150);
            templateRt.anchoredPosition = new Vector2(0, -2);
            var templateImg = templateGo.GetComponent<Image>();
            templateImg.color = new Color(0.06f, 0.08f, 0.06f, 1f);
            templateImg.raycastTarget = true;

            var templateScroll = templateGo.GetComponent<ScrollRect>();
            templateGo.SetActive(false); // 初始隐藏

            // Viewport
            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(templateRt, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.anchorMin = viewportRt.anchorMax = new Vector2(0, 0);
            viewportRt.offsetMin = viewportRt.offsetMax = Vector2.zero;
            var viewportImg = viewportGo.GetComponent<Image>();
            viewportImg.color = new Color(0.06f, 0.08f, 0.06f, 1f);

            // Content
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportRt, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(0, UIConstants.ContractNodeDisplayNames.Length * 28);

            templateScroll.viewport = viewportRt;
            templateScroll.content = contentRt;
            templateScroll.horizontal = false;
            templateScroll.vertical = true;
            templateScroll.movementType = ScrollRect.MovementType.Clamped;

            // 用脚本设置 dropdown.template（通过反射或直接赋值）
            dropdown.template = templateRt;

            // 创建选项条目
            var itemGo = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            itemGo.transform.SetParent(contentRt, false);
            var itemRt = itemGo.GetComponent<RectTransform>();
            itemRt.anchorMin = itemRt.anchorMax = new Vector2(0, 1);
            itemRt.pivot = new Vector2(0.5f, 1);
            itemRt.sizeDelta = new Vector2(0, 28);
            itemRt.anchoredPosition = Vector2.zero;

            // Item Background
            var itemBgGo = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
            itemBgGo.transform.SetParent(itemRt, false);
            var itemBgRt = itemBgGo.GetComponent<RectTransform>();
            itemBgRt.anchorMin = itemBgRt.anchorMax = new Vector2(0, 0);
            itemBgRt.offsetMin = itemBgRt.offsetMax = Vector2.zero;
            var itemBgImg = itemBgGo.GetComponent<Image>();
            itemBgImg.color = new Color(0.10f, 0.13f, 0.10f, 1f);

            // Item Checkmark
            var checkGo = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image));
            checkGo.transform.SetParent(itemRt, false);
            var checkRt = checkGo.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0, 0.5f);
            checkRt.anchorMax = new Vector2(0, 0.5f);
            checkRt.pivot = new Vector2(0, 0.5f);
            checkRt.sizeDelta = new Vector2(18, 18);
            checkRt.anchoredPosition = new Vector2(8, 0);
            var checkImg = checkGo.GetComponent<Image>();
            checkImg.color = UIConstants.Green;

            // Item Label
            var itemLabelGo = new GameObject("Item Label", typeof(RectTransform), typeof(Text));
            itemLabelGo.transform.SetParent(itemRt, false);
            var itemLabelRt = itemLabelGo.GetComponent<RectTransform>();
            itemLabelRt.anchorMin = itemLabelRt.anchorMax = new Vector2(0, 0);
            itemLabelRt.offsetMin = new Vector2(30, 0);
            itemLabelRt.offsetMax = new Vector2(0, 0);
            var itemLabel = itemLabelGo.GetComponent<Text>();
            itemLabel.text = "";
            itemLabel.fontSize = UIConstants.FontSizeSmall;
            itemLabel.color = UIConstants.White;
            itemLabel.alignment = TextAnchor.MiddleLeft;
            itemLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var toggle = itemGo.GetComponent<Toggle>();
            toggle.targetGraphic = itemBgImg;
            toggle.graphic = checkImg;
            toggle.isOn = false;

            dropdown.itemText = itemLabel;
        }

        /// <summary>
        /// "献祭"按钮点击
        /// </summary>
        private void OnSubmitClicked()
        {
            // 获取选择的节点
            string nodeName = UIConstants.ContractNodeDisplayNames[0];
            if (nodeDropdown != null && nodeDropdown.value < UIConstants.ContractNodeDisplayNames.Length)
                nodeName = UIConstants.ContractNodeDisplayNames[nodeDropdown.value];

            // 解析EXP数值
            int expAmount = 0;
            if (expInputField != null && !string.IsNullOrEmpty(expInputField.text))
            {
                int.TryParse(expInputField.text, out expAmount);
            }

            if (expAmount <= 0)
            {
                Debug.LogWarning("[QuickExpInput] EXP数值必须大于0");
                return;
            }

            // 描述
            string description = descInputField != null ? descInputField.text : "";

            Debug.Log($"[QuickExpInput] 献祭: {nodeName} +{expAmount} EXP \"{description}\"");

            // 触发回调
            OnExpSubmitted?.Invoke(nodeName, expAmount, description);
        }

        /// <summary>
        /// 隐藏弹窗
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 重置输入框
        /// </summary>
        public void ResetInputs()
        {
            if (nodeDropdown != null) nodeDropdown.value = 0;
            if (expInputField != null) expInputField.text = "100";
            if (descInputField != null) descInputField.text = "";
        }

        private void OnEnable()
        {
            ResetInputs();
        }

        // ── 辅助方法 ──

        private Text CreateText(string name, string text, Transform parent,
            Rect layout, int fontSize, Color color, TextAnchor alignment, bool bold)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(layout.x, -layout.y);
            rt.sizeDelta = new Vector2(layout.width, layout.height);

            var txt = go.GetComponent<Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = alignment;
            txt.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            txt.raycastTarget = false;
            txt.font = UIConstants.BodyFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return txt;
        }
    }
}
