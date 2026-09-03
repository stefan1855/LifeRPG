using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using LifeRPG;

namespace LifeRPG.UI
{
    /// <summary>
    /// 装备面板控制器 - 右上面板左列
    /// 显示8行装备槽（头部~靴子）
    /// </summary>
    public class EquipmentPanelController : MonoBehaviour
    {
        [Header("UI 组件引用")]
        [SerializeField] private Text titleText;
        [SerializeField] private EquipmentSlotUI[] slotUIs;

        private const int SlotCount = 8;

        /// <summary>初始化面板</summary>
        public void Initialize(Transform parent)
        {
            var panelRect = UIConstants.RightTopPanelRect;
            float equipColumnX = panelRect.x + 10;
            float equipColumnW = 230;

            titleText = UIConstants.CreateTitleText("EquipTitle", "当 前 装 备", parent,
                new Rect(equipColumnX, panelRect.y + 5, equipColumnW, 24),
                UIConstants.FontSizeSubtitle, UIConstants.Gold, TextAnchor.UpperCenter);

            // 装备槽背景装饰
            var equipBgGo = UIConstants.CreateUIElement("EquipSlotBg", parent);
            var equipBgRt = equipBgGo.GetComponent<RectTransform>();
            equipBgRt.anchorMin = equipBgRt.anchorMax = new Vector2(0, 1);
            equipBgRt.pivot = new Vector2(0, 1);
            equipBgRt.anchoredPosition = new Vector2(equipColumnX + 10, -(panelRect.y + 40));
            equipBgRt.sizeDelta = new Vector2(equipColumnW - 20, SlotCount * 38 + 10);
            var equipBgImg = equipBgGo.AddComponent<Image>();
            equipBgImg.color = new Color(0.06f, 0.08f, 0.06f, 0.7f);
            equipBgImg.raycastTarget = false;
            equipBgImg.sprite = UIConstants.LoadSprite(UIConstants.TexEquipSlots);

            // 8行装备槽
            slotUIs = new EquipmentSlotUI[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                float yOffset = panelRect.y + 42 + i * 38;
                string slotName = (i < UIConstants.EquipmentSlotNames.Length)
                    ? UIConstants.EquipmentSlotNames[i] : $"槽位{i + 1}";
                slotUIs[i] = CreateSlotUI($"EquipSlot_{i}", parent,
                    new Rect(equipColumnX + 15, yOffset, equipColumnW - 30, 34), slotName);
            }
        }

        private EquipmentSlotUI CreateSlotUI(string name, Transform parent, Rect layout, string slotName)
        {
            var containerGo = UIConstants.CreateUIElement(name, parent);
            var containerRt = containerGo.GetComponent<RectTransform>();
            containerRt.anchorMin = containerRt.anchorMax = new Vector2(0, 1);
            containerRt.pivot = new Vector2(0, 1);
            containerRt.anchoredPosition = new Vector2(layout.x, -layout.y);
            containerRt.sizeDelta = new Vector2(layout.width, layout.height);

            // 槽位背景
            var bgGo = UIConstants.CreateUIElement("SlotBg", containerRt);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = bgRt.anchorMax = new Vector2(0, 0.5f);
            bgRt.pivot = new Vector2(0, 0.5f);
            bgRt.sizeDelta = new Vector2(28, 28);
            bgRt.anchoredPosition = new Vector2(4, 0);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.10f, 0.13f, 0.10f, 0.8f);
            bgImg.raycastTarget = false;

            var slotUI = new EquipmentSlotUI();

            // 槽位名称
            slotUI.SlotNameText = UIConstants.CreateText("SlotLabel", slotName, containerRt,
                new Rect(36, -8, 60, 20),
                UIConstants.FontSizeSmall, UIConstants.Dim, TextAnchor.MiddleLeft, false);

            // 装备名称
            slotUI.EquipNameText = UIConstants.CreateText("EquipName", "空", containerRt,
                new Rect(100, -8, layout.width - 105, 20),
                UIConstants.FontSizeSmall, UIConstants.Gray, TextAnchor.MiddleCenter, false);

            return slotUI;
        }

        /// <summary>更新装备面板</summary>
        public void UpdateDisplay(List<EquipmentSlot> slots)
        {
            if (slots == null) return;

            for (int i = 0; i < slotUIs.Length && i < slots.Count; i++)
            {
                var ui = slotUIs[i];
                var data = slots[i];

                if (ui.EquipNameText != null)
                {
                    bool hasEquip = data.IsEquipped;
                    ui.EquipNameText.text = hasEquip ? data.EquippedItemName : (data.IsLocked ? "🔒 锁定" : "空");
                    ui.EquipNameText.color = hasEquip ? UIConstants.White : UIConstants.Dim;
                }

                if (ui.SlotNameText != null)
                    ui.SlotNameText.text = data.SlotName;
            }
        }

        /// <summary>从 GameManager 加载并刷新</summary>
        public void RefreshFromGameManager()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("[EquipmentPanel] GameManager.Instance 为空");
                return;
            }
            UpdateDisplay(gm.EquipmentSlots);
        }
    }

    /// <summary>装备槽 UI 组件引用</summary>
    [System.Serializable]
    public class EquipmentSlotUI
    {
        public Text SlotNameText;
        public Text EquipNameText;
    }
}
