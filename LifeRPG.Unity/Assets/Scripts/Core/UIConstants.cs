using UnityEngine;

namespace LifeRPG.UI
{
    /// <summary>
    /// 仪表盘 UI 全局静态常量：
    /// 布局区域矩形、颜色、字体大小、资源路径
    /// </summary>
    public static class UIConstants
    {
        // ── 设计分辨率 ──
        public const float DesignWidth = 1460f;
        public const float DesignHeight = 920f;

        // ── 布局区域矩形 (x, y, width, height) ──
        /// <summary>顶部标题栏: y=0~60</summary>
        public static readonly Rect TopBarRect = new Rect(0, 0, 1460, 60);
        /// <summary>左面板 - 角色信息: x=0~336</summary>
        public static readonly Rect LeftPanelRect = new Rect(0, 0, 336, 920);
        /// <summary>中面板: x=336~960</summary>
        public static readonly Rect CenterPanelRect = new Rect(336, 0, 624, 920);
        /// <summary>中上面板 - 经验区域: y=60~280</summary>
        public static readonly Rect ExpAreaRect = new Rect(336, 60, 624, 220);
        /// <summary>中下面板 - 暗影契约技能环: y=280~700</summary>
        public static readonly Rect ContractRingRect = new Rect(336, 280, 624, 420);
        /// <summary>右上面板: x=960~1460, y=60~500</summary>
        public static readonly Rect RightTopPanelRect = new Rect(960, 60, 500, 440);
        /// <summary>右下面板: x=960~1460, y=500~920</summary>
        public static readonly Rect RightBottomPanelRect = new Rect(960, 500, 500, 420);

        public static readonly Rect PortraitRect = new Rect(0, 0, 336, 520);
        public static readonly Rect LevelTextRect = new Rect(56, 121, 60, 28);
        public static readonly Rect NameTextRect = new Rect(138, 686, 276, 36);
        public static readonly Rect TitleTextRect = new Rect(138, 745, 276, 22);
        public static readonly Rect DescTextRect = new Rect(36, 782, 240, 44);
        public static readonly Rect SoulValueRect = new Rect(400, 150, 213, 26);
        public static readonly Rect NextLevelRect = new Rect(620, 154, 213, 18);
        public static readonly Rect DateRect = new Rect(840, 156, 213, 16);
        public static readonly Rect ProgressBarRect = new Rect(304, 216, 640, 22);
        public static readonly Rect ProgressExpRect = new Rect(304, 240, 640, 16);

        // ── 颜色常量 ──
        public static readonly Color Green = new Color(0.49f, 0.99f, 0.0f, 1f);
        public static readonly Color White = new Color(0.84f, 0.94f, 0.76f, 1f);
        public static readonly Color Gray = new Color(0.50f, 0.55f, 0.46f, 1f);
        public static readonly Color Dim = new Color(0.38f, 0.50f, 0.42f, 1f);
        public static readonly Color Gold = new Color(0.84f, 0.73f, 0.42f, 1f);
        public static readonly Color DarkBg = new Color(0.04f, 0.06f, 0.05f, 1f);
        public static readonly Color OverlayBlack = new Color(0f, 0f, 0f, 0.7f);

        // ── 字体大小 ──
        public const int FontSizeTitle = 28;
        public const int FontSizeSubtitle = 22;
        public const int FontSizeBody = 15;
        public const int FontSizeSmall = 13;
        public const int FontSizeTiny = 11;
        public const int FontSizeButton = 16;

        // ── 资源路径 ──
        public const string TexGlobalBg = "Textures/global_background";
        public const string TexFacelessPortrait = "Textures/faceless_portrait";
        public const string TexMagicCircleEmblem = "Textures/magic_circle_emblem";
        public const string TexMagicCircleRing = "Textures/magic_circle_ring";
        public const string TexMagicCircleOuterGlow = "Textures/magic_circle_outer_glow";
        public const string TexNodeBorder = "Textures/node_border";
        public const string TexPanelBorder = "Textures/panel_border";
        public const string TexEquipSlots = "Textures/equip_slots";
        public const string TexStarIconGreen = "Textures/star_icon_green";

        // ── 第二代暗黑哥特贴图 (8张新素材) ──
        public const string TexPanelBorderV2 = "Textures/ui_panel_border_gothic_gold_1x1";
        public const string TexPanelInnerV2 = "Textures/ui_panel_inner_darkstone_tile_1x1";
        public const string TexProgressBg = "Textures/ui_progressbar_bg_dark_gold_16x9";
        public const string TexProgressFill = "Textures/ui_progressbar_fill_green_gradient_16x9";
        public const string TexContractRunes = "Textures/ui_contract_runes_5set_green_1x1";
        public const string TexButtonMetal = "Textures/ui_button_dark_metal_gold_1x1";
        public const string TexCornerDeco = "Textures/ui_corners_gothic_vines_gold_1x1";
        public const string TexTitleBar = "Textures/ui_titlebar_dark_metal_rivets_16x9";

        // ── 装备槽名称 (与模型中的8个槽位一致) ──
        public static readonly string[] EquipmentSlotNames =
        {
            "头部", "肩部", "胸部", "护腕",
            "手套", "腰带", "腿部", "靴子"
        };

        /// <summary>契约节点中文名称（按顺序: 暗影血肉/邪能造物/虚空之智/灵魂羁绊/不灭意志）</summary>
        public static readonly string[] ContractNodeDisplayNames =
        {
            "暗影血肉", "邪能造物", "虚空之智", "灵魂羁绊", "不灭意志"
        };

        // ── 字体资源路径 ──
        public const string FontTitle = "Fonts/UnifrakturMaguntia";
        public const string FontBody = "Fonts/MedievalSharp";

        // ── 懒加载字体缓存 ──
        private static Font _titleFont;
        private static Font _bodyFont;

        public static Font TitleFont
        {
            get
            {
                if (_titleFont == null)
                    _titleFont = Resources.Load<Font>(FontTitle);
                return _titleFont;
            }
        }

        public static Font BodyFont
        {
            get
            {
                if (_bodyFont == null)
                    _bodyFont = Resources.Load<Font>(FontBody);
                return _bodyFont;
            }
        }

        // ── 帮助方法 ──
        /// <summary>创建 Text 组件 (使用正文字体 MedievalSharp)</summary>
        public static UnityEngine.UI.Text CreateText(string name, string text, Transform parent,
            Rect layout, int fontSize, Color color, TextAnchor alignment, bool bold)
        {
            return CreateTextWithFont(name, text, parent, layout, fontSize, color, alignment, bold, BodyFont);
        }

        /// <summary>创建标题 Text 组件 (使用哥特字体 UnifrakturMaguntia)</summary>
        public static UnityEngine.UI.Text CreateTitleText(string name, string text, Transform parent,
            Rect layout, int fontSize, Color color, TextAnchor alignment)
        {
            return CreateTextWithFont(name, text, parent, layout, fontSize, color, alignment, false, TitleFont);
        }

        /// <summary>创建 Text 组件的核心工厂</summary>
        private static UnityEngine.UI.Text CreateTextWithFont(string name, string text, Transform parent,
            Rect layout, int fontSize, Color color, TextAnchor alignment, bool bold, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(layout.x, -layout.y);
            rt.sizeDelta = new Vector2(layout.width, layout.height);

            var txt = go.GetComponent<UnityEngine.UI.Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = alignment;
            txt.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            txt.raycastTarget = false;
            txt.font = font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return txt;
        }

        /// <summary>创建 UI 元素的快捷工厂</summary>
        public static GameObject CreateUIElement(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>加载贴图并创建 Sprite</summary>
        public static Sprite LoadSprite(string resourcePath)
        {
            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex != null)
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            return null;
        }

        // ── 发光 Shader 工厂 ──
        private static Shader _glowShader;

        /// <summary>发光 Shader（懒加载，优先 Resources 加载，回退 Shader.Find）</summary>
        private static Shader GlowShader
        {
            get
            {
                if (_glowShader == null)
                    _glowShader = Resources.Load<Shader>("Shaders/UI_Glow");
                if (_glowShader == null)
                    _glowShader = Shader.Find("LifeRPG/UI/Glow");
                return _glowShader;
            }
        }

        /// <summary>
        /// 创建发光材质（赋给 Image.material 实现真 Shader 外发光 + 脉动动画）
        /// 注意：返回的 Material 需要由调用方在销毁时手动 Destroy
        /// </summary>
        public static Material CreateGlowMaterial(Color glowColor, float intensity = 1.2f,
            float width = 6f, float pulseSpeed = 2.5f, float pulseAmount = 0.35f)
        {
            var shader = GlowShader;
            if (shader == null)
            {
                Debug.LogWarning("[UIConstants] 未找到发光 Shader (LifeRPG/UI/Glow)，返回 null");
                return null;
            }
            var mat = new Material(shader);
            mat.SetColor("_GlowColor", glowColor);
            mat.SetFloat("_GlowIntensity", intensity);
            mat.SetFloat("_GlowWidth", width);
            mat.SetFloat("_PulseSpeed", pulseSpeed);
            mat.SetFloat("_PulseAmount", pulseAmount);
            return mat;
        }
    }
}
