using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using LifeRPG;

namespace LifeRPG.UI
{
    /// <summary>
    /// 暗影契约技能环控制器 - 中下面板
    /// 中心圆形徽记 + 外环 + 5个节点围绕圆环正五边形分布
    /// </summary>
    public class ContractRingController : MonoBehaviour
    {
        [Header("UI 组件引用")]
        [SerializeField] private Text titleText;
        [SerializeField] private Image centerEmblem;
        [SerializeField] private Image outerRing;
        [SerializeField] private Image outerGlow;
        [SerializeField] private RectTransform ringContainer;
        [SerializeField] private List<ContractNodeUI> nodeUIs = new List<ContractNodeUI>();

        private const float RingRadius = 140f;
        private Vector2 ringCenter;

        // 发光材质（运行时创建，OnDestroy 时销毁）
        private Material _emblemGlowMat;
        private Material _ringGlowMat;

        /// <summary>初始化面板</summary>
        public void Initialize(Transform parent)
        {
            var panelRect = UIConstants.ContractRingRect;

            titleText = UIConstants.CreateTitleText("RingTitle", "暗 影 契 约", parent,
                new Rect(panelRect.x + 10, panelRect.y + 5, panelRect.width - 20, 24),
                UIConstants.FontSizeSubtitle, UIConstants.Gold, TextAnchor.UpperCenter);

            // 环形容器
            var containerGo = UIConstants.CreateUIElement("RingContainer", parent);
            ringContainer = containerGo.GetComponent<RectTransform>();
            ringContainer.anchorMin = ringContainer.anchorMax = new Vector2(0, 1);
            ringContainer.pivot = new Vector2(0, 1);
            ringContainer.anchoredPosition = new Vector2(panelRect.x, -(panelRect.y + 40));
            ringContainer.sizeDelta = new Vector2(panelRect.width, panelRect.height - 50);

            // 中心徽记
            var emblemGo = UIConstants.CreateUIElement("CenterEmblem", ringContainer);
            var emblemRt = emblemGo.GetComponent<RectTransform>();
            emblemRt.anchorMin = emblemRt.anchorMax = new Vector2(0.5f, 0.5f);
            emblemRt.sizeDelta = new Vector2(100, 100);
            emblemRt.anchoredPosition = Vector2.zero;
            centerEmblem = emblemGo.AddComponent<Image>();
            centerEmblem.raycastTarget = false;
            centerEmblem.sprite = UIConstants.LoadSprite(UIConstants.TexMagicCircleEmblem);
            _emblemGlowMat = UIConstants.CreateGlowMaterial(UIConstants.Green, 1.0f, 3f, 2.5f, 0.3f);
            if (_emblemGlowMat != null) centerEmblem.material = _emblemGlowMat;

            // 外环
            var ringGo = UIConstants.CreateUIElement("OuterRing", ringContainer);
            var ringRt = ringGo.GetComponent<RectTransform>();
            ringRt.anchorMin = ringRt.anchorMax = new Vector2(0.5f, 0.5f);
            ringRt.sizeDelta = new Vector2(320, 320);
            ringRt.anchoredPosition = Vector2.zero;
            outerRing = ringGo.AddComponent<Image>();
            outerRing.raycastTarget = false;
            outerRing.sprite = UIConstants.LoadSprite(UIConstants.TexMagicCircleRing);
            _ringGlowMat = UIConstants.CreateGlowMaterial(UIConstants.Green, 1.5f, 5f, 2.0f, 0.4f);
            if (_ringGlowMat != null) outerRing.material = _ringGlowMat;

            // 外环光晕
            var glowGo = UIConstants.CreateUIElement("OuterGlow", ringContainer);
            var glowRt = glowGo.GetComponent<RectTransform>();
            glowRt.anchorMin = glowRt.anchorMax = new Vector2(0.5f, 0.5f);
            glowRt.sizeDelta = new Vector2(360, 360);
            glowRt.anchoredPosition = Vector2.zero;
            outerGlow = glowGo.AddComponent<Image>();
            outerGlow.raycastTarget = false;
            outerGlow.color = new Color(UIConstants.Green.r, UIConstants.Green.g, UIConstants.Green.b, 0.3f);
            outerGlow.sprite = UIConstants.LoadSprite(UIConstants.TexMagicCircleOuterGlow);

            ringCenter = new Vector2(panelRect.width * 0.5f, (panelRect.height - 50) * 0.5f);

            // 预创建5个节点槽位
            CreateNodeSlots();
        }

        private void CreateNodeSlots()
        {
            nodeUIs.Clear();
            for (int i = 0; i < 5; i++)
            {
                float angleDeg = -90f + i * (360f / 5f);
                float angleRad = angleDeg * Mathf.Deg2Rad;
                float x = ringCenter.x + RingRadius * Mathf.Cos(angleRad);
                float y = ringCenter.y + RingRadius * Mathf.Sin(angleRad);
                nodeUIs.Add(CreateNodeUI($"ContractNode_{i}", ringContainer, new Vector2(x, y)));
            }
        }

        private ContractNodeUI CreateNodeUI(string name, Transform parent, Vector2 position)
        {
            var containerGo = UIConstants.CreateUIElement(name, parent);
            var containerRt = containerGo.GetComponent<RectTransform>();
            containerRt.anchorMin = containerRt.anchorMax = new Vector2(0, 1);
            containerRt.pivot = new Vector2(0.5f, 0.5f);
            containerRt.anchoredPosition = position;
            containerRt.sizeDelta = new Vector2(80, 100);

            var nodeUI = new ContractNodeUI();

            // 节点边框 (九宫格)
            var borderGo = UIConstants.CreateUIElement("Border", containerRt);
            var borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.anchorMin = borderRt.anchorMax = new Vector2(0.5f, 0.5f);
            borderRt.sizeDelta = new Vector2(54, 54);
            borderRt.anchoredPosition = new Vector2(0, 10);
            nodeUI.BorderImage = borderGo.AddComponent<Image>();
            nodeUI.BorderImage.type = Image.Type.Sliced;
            nodeUI.BorderImage.raycastTarget = false;
            nodeUI.BorderImage.sprite = UIConstants.LoadSprite(UIConstants.TexNodeBorder);

            // 图标
            var iconGo = UIConstants.CreateUIElement("Icon", containerRt);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(30, 30);
            iconRt.anchoredPosition = new Vector2(0, 10);
            nodeUI.IconImage = iconGo.AddComponent<Image>();
            nodeUI.IconImage.raycastTarget = false;
            nodeUI.IconImage.sprite = UIConstants.LoadSprite(UIConstants.TexContractRunes);

            // 节点名称
            nodeUI.NameText = UIConstants.CreateText("NodeName", "", containerRt,
                new Rect(-39, -40, 78, 16),
                UIConstants.FontSizeTiny, UIConstants.White, TextAnchor.MiddleCenter, false);

            // 等级+EXP
            nodeUI.LevelExpText = UIConstants.CreateText("NodeExp", "", containerRt,
                new Rect(-39, -54, 78, 14),
                9, UIConstants.Dim, TextAnchor.MiddleCenter, false);

            return nodeUI;
        }

        /// <summary>更新技能环显示</summary>
        public void UpdateDisplay(List<ContractNode> nodes)
        {
            if (nodes == null) return;

            for (int i = 0; i < nodeUIs.Count && i < nodes.Count; i++)
            {
                var ui = nodeUIs[i];
                var data = nodes[i];

                if (ui.NameText != null)
                    ui.NameText.text = data.NodeName;

                if (ui.LevelExpText != null)
                    ui.LevelExpText.text = $"Lv.{data.Level}  {data.Exp}/{data.MaxExp}";

                if (ui.BorderImage != null)
                {
                    float intensity = Mathf.Clamp01(data.Level / 10f);
                    ui.BorderImage.color = Color.Lerp(UIConstants.Dim, UIConstants.Green, intensity);
                }
            }
        }

        /// <summary>从 GameManager 加载并刷新</summary>
        public void RefreshFromGameManager()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("[ContractRing] GameManager.Instance 为空");
                return;
            }
            UpdateDisplay(gm.Nodes);
        }

        private void OnDestroy()
        {
            if (_emblemGlowMat != null) Destroy(_emblemGlowMat);
            if (_ringGlowMat != null) Destroy(_ringGlowMat);
        }
    }

    /// <summary>契约节点 UI 组件引用</summary>
    [System.Serializable]
    public class ContractNodeUI
    {
        public Image BorderImage;
        public Image IconImage;
        public Text NameText;
        public Text LevelExpText;
    }
}
