<script setup>
const saving = ref(false);
import { computed, onMounted, onUnmounted, watch, reactive, ref } from 'vue';
import { Plus, Pencil, Trash2, ShieldCheck, ShieldOff, Sparkles } from 'lucide-vue-next';
import GothicPanel from '../components/GothicPanel.vue';
import ModalShell from '../components/ModalShell.vue';
import { api, ATTRIBUTE_META, EQUIP_SLOTS, attrMeta } from '../api';
import { notify } from '../stores/toast';
import { sfx } from '../utils/audio';

const conflicts=ref([]),showRepair=ref(false),repairing=ref(false);
async function repairConflicts(){if(repairing.value)return;repairing.value=true;try{const r=await api.equipRepair();notify.success(`已备份，卸下 ${r.removed} 件冲突装备`);showRepair.value=false;await load();}catch(e){notify.error(e.message);}finally{repairing.value=false;}}
const snapshot = ref(null);
const items = ref([]);
const loading = ref(true);

// 悬浮装备属性卡 (Warcraft/Dark Souls Tooltip) 状态
const hoveredItem = ref(null);
const tooltipVisible = ref(false);
const tooltipPos = reactive({ x: 0, y: 0 });

function showTip(item, e) {
  if (!item) return;
  hoveredItem.value = item;
  updateTipPos(e);
  tooltipVisible.value = true;
}

function moveTip(e) {
  if (tooltipVisible.value) {
    updateTipPos(e);
  }
}

function hideTip() {
  tooltipVisible.value = false;
  hoveredItem.value = null;
}

function updateTipPos(e) {
  const pad = 14;
  let x = e.clientX + pad;
  let y = e.clientY + pad;
  if (x + 290 > window.innerWidth) x = e.clientX - 300;
  if (y + 280 > window.innerHeight) y = e.clientY - 270;
  tooltipPos.x = Math.max(12, x);
  tooltipPos.y = Math.max(12, y);
}

function itemQuality(item) {
  const total = (item?.bonuses || []).reduce((sum, b) => sum + (b.bonusValue || 0), 0);
  if (total >= 18) return { label: '史诗遗物 · Epic', color: '#b366ff' };
  if (total >= 10) return { label: '精炼稀有 · Rare', color: '#4aa8ff' };
  if (total >= 5)  return { label: '符文契印 · Uncommon', color: '#7cff1f' };
  return { label: '凡世器物 · Common', color: '#cfea78' };
}

const SLOT_ICONS = {
  head: new URL('../assets/icons/slot_head.svg', import.meta.url).href,
  shoulder: new URL('../assets/icons/slot_shoulder.svg', import.meta.url).href,
  chest: new URL('../assets/icons/slot_chest.svg', import.meta.url).href,
  wrist: new URL('../assets/icons/slot_wrist.svg', import.meta.url).href,
  hands: new URL('../assets/icons/slot_hands.svg', import.meta.url).href,
  waist: new URL('../assets/icons/slot_waist.svg', import.meta.url).href,
  legs: new URL('../assets/icons/slot_legs.svg', import.meta.url).href,
  feet: new URL('../assets/icons/slot_feet.svg', import.meta.url).href,
  trinket1: new URL('../assets/icons/slot_trinket1.svg', import.meta.url).href,
  trinket2: new URL('../assets/icons/slot_trinket2.svg', import.meta.url).href,
  mainhand: new URL('../assets/icons/slot_mainhand.svg', import.meta.url).href,
  offhand: new URL('../assets/icons/slot_offhand.svg', import.meta.url).href,
};

const showEditor = ref(false);
let editorRevision=0;
watch(showEditor,(open)=>{if(!open)editorRevision++;},{flush:"sync"});
onUnmounted(()=>editorRevision++);
const pickingSlot = ref(null);
async function pickItem(item) { await toggle(item); pickingSlot.value = null; }
const editingId = ref(null);
const form = reactive({
  realItemName: '',
  gameItemName: '',
  category: 'chest',
  realEffect: '',
  bonuses: [],
});
const showDelete = ref(false);
const deleting = ref(null);

async function load() {
  loading.value = true;
  try {
    const [snap, list] = await Promise.all([api.equipSnapshot(), api.equipList()]);
    snapshot.value = snap;
    items.value = list;
    conflicts.value=await api.equipConflicts();
  } catch (e) {
    notify.error('读取装备失败：' + e.message);
  } finally {
    loading.value = false;
  }
}

const equippedCount = computed(() => (snapshot.value?.slots || []).filter((s) => s.item).length);

function openCreate(slotKey) {
  editorRevision++;
  editingId.value = null;
  Object.assign(form, {
    realItemName: '',
    gameItemName: '',
    category: slotKey || 'chest',
    realEffect: '',
    bonuses: [],
  });
  showEditor.value = true;
}

function openEdit(item) {
  editorRevision++;
  editingId.value = item.id;
  Object.assign(form, {
    realItemName: item.realItemName,
    gameItemName: item.gameItemName,
    category: item.category,
    realEffect: item.realEffect || '',
    bonuses: (item.bonuses || []).map((b) => ({ ...b })),
  });
  showEditor.value = true;
}

function addBonus() {
  form.bonuses.push({ attributeKey: 'mind', bonusValue: 5 });
}

/** AI 军师：现实物品 → 哥特游戏名 + 效果 + 加成 */
const aiNaming = ref(false);

async function aiName() {
  const name = form.realItemName.trim() || form.gameItemName.trim();
  if (!name) {
    notify.warn('先填现实物品名，AI 才知道要给什么起名');
    return;
  }
  const revision=editorRevision;
  aiNaming.value = true;
  try {
    const r = await api.aiNameEquipment(name, form.realEffect || '');
    if(revision!==editorRevision)return;
    if (r.gameItemName) form.gameItemName = r.gameItemName;
    if (r.realEffect) form.realEffect = r.realEffect;
    if (Array.isArray(r.bonuses) && r.bonuses.length) {
      form.bonuses = r.bonuses
        .filter((b) => b && b.attributeKey && ATTRIBUTE_META[b.attributeKey])
        .slice(0, 2)
        .map((b) => ({
          attributeKey: b.attributeKey,
          bonusValue: Math.max(1, Math.min(10, Math.round(b.bonusValue || 5))),
        }));
    }
    notify.success('军师已命名，可再手调');
  } catch (e) {
    notify.error('AI 起名失败：' + e.message);
  } finally {
    aiNaming.value = false;
  }
}

async function submit() {
  if (!form.realItemName.trim() && !form.gameItemName.trim()) {
    notify.warn('至少填一个物品名');
    return;
  }
  if (saving.value) return;
  saving.value = true;
  try {
    await api.equipSave({
      id: editingId.value,
      realItemName: form.realItemName.trim() || form.gameItemName.trim(),
      gameItemName: form.gameItemName.trim() || form.realItemName.trim(),
      category: form.category,
      realEffect: form.realEffect,
      bonuses: form.bonuses,
    });
    notify.success(editingId.value ? '装备已更新' : '装备已入库');
    showEditor.value = false;
    await load();
  } catch (e) {
    notify.error('保存失败：' + e.message);
  } finally { saving.value = false; }
}

async function toggle(item) {
  try {
    const on = await api.equipToggle(item.id);
    sfx.equip();
    notify.info(on ? `已装备「${item.gameItemName}」` : `已卸下「${item.gameItemName}」`);
    await load();
  } catch (e) {
    notify.error('操作失败：' + e.message);
  }
}

function askDelete(item) {
  deleting.value = item;
  showDelete.value = true;
}

async function confirmDelete() {
  try {
    await api.equipDelete(deleting.value.id);
    notify.info('装备已销毁');
    showDelete.value = false;
    await load();
  } catch (e) {
    notify.error('删除失败：' + e.message);
  }
}

onMounted(load);
</script>

<template>
  <div class="equip-view">
    <ModalShell v-if="showRepair" title="修复穿戴冲突" :busy="repairing" @close="showRepair=false"><p>先创建备份，再保留各槽最近穿戴的一件，卸下其余装备。</p><p v-for="c in conflicts" :key="c.id">{{ EQUIP_SLOTS.find(s=>s.key===c.slot)?.label }} · {{ c.name }}：{{ c.remove?'卸下':'保留' }}</p><template #footer><button class="btn btn-primary" :disabled="repairing" @click="repairConflicts">{{ repairing?'正在备份与修复…':'备份并修复' }}</button></template></ModalShell>
    <GothicPanel title="装备栏" subtitle="EQUIPMENT" class="slots-panel">
      <template #actions>
        <button v-if="conflicts.length" class="btn btn-sm" title="查看穿戴冲突修复预览" @click="showRepair=true">修复冲突</button>
        <span class="hint">{{ equippedCount }} / 12 已装备</span>
      </template>

      <div class="doll">
        <!-- 中央：角色徽记 + 术士立绘底座 + 总加成 -->
        <div class="doll-center">
          <div class="doll-pedestal"></div>
          <div class="doll-emblem">
            <span class="doll-sigil">☠</span>
          </div>
          <div class="doll-count">
            <span class="dc-num num">{{ equippedCount }}</span>
            <span class="dc-label">/ 12 具装已鸣</span>
          </div>
          <div v-if="snapshot?.totalBonuses?.length" class="doll-bonus">
            <span
              v-for="b in snapshot.totalBonuses"
              :key="b.attributeKey"
              class="db-item"
              :style="{ color: attrMeta(b.attributeKey).color }"
            >{{ attrMeta(b.attributeKey).label }} +{{ b.bonusValue }}</span>
          </div>
          <span v-else class="db-empty">全副武装以启灵力</span>
        </div>

        <button
          v-for="slot in snapshot?.slots || []"
          :key="slot.key"
          class="slot"
          :class="{ filled: slot.item }"
          :style="{ gridArea: slot.key }"
          @click="slot.item ? toggle(slot.item) : pickingSlot = slot.key"
          @mouseenter="slot.item && showTip(slot.item, $event)"
          @mousemove="slot.item && moveTip($event)"
          @mouseleave="hideTip"
        >
          <img v-if="SLOT_ICONS[slot.key]" :src="SLOT_ICONS[slot.key]" class="slot-icon-img" alt="" />
          <span class="slot-label">{{ slot.label }}</span>
          <span class="slot-body">
            <template v-if="slot.item">
              <span class="slot-name">{{ slot.item.gameItemName }}</span>
              <span class="slot-bonus">
                <span v-for="b in slot.item.bonuses" :key="b.attributeKey" :style="{ color: attrMeta(b.attributeKey).color }">
                  {{ attrMeta(b.attributeKey).label }} +{{ b.bonusValue }}
                </span>
              </span>
            </template>
            <span v-else class="slot-empty">点击嵌合装备</span>
          </span>
        </button>
      </div>
    </GothicPanel>

    <GothicPanel title="装备库" subtitle="INVENTORY" class="bag-panel">
      <template #actions>
        <button class="btn btn-primary btn-sm bag-add-btn" @click="openCreate('')"><Plus :size="13" /> 新增装备</button>
      </template>

      <div v-if="!items.length && !loading" class="empty-inventory">
        <div class="ei-desc">
          <span>暂无未装配的遗物</span>
          <span class="ei-sub">点击上方「新增装备」，把现实里的物品赋予暗黑魔力</span>
        </div>
        <!-- 4x3 幽灵行囊卡槽网格，消除大片死黑 -->
        <div class="ghost-grid">
          <div v-for="n in 12" :key="n" class="ghost-slot" title="点击铭刻新装备" @click="openCreate('')">
            <span class="gs-num num">{{ n }}</span>
            <span class="gs-plus">+</span>
          </div>
        </div>
      </div>

      <div class="bag-list scroll">
        <article
          v-for="it in items"
          :key="it.id"
          class="bag-item"
          :class="{ on: it.equipped }"
          @mouseenter="showTip(it, $event)"
          @mousemove="moveTip($event)"
          @mouseleave="hideTip"
        >
          <div class="bi-main">
            <span class="bi-slot">{{ EQUIP_SLOTS.find((s) => s.key === it.category)?.label || it.category }}</span>
            <h4>{{ it.gameItemName }}</h4>
            <p v-if="it.realItemName && it.realItemName !== it.gameItemName" class="bi-real">
              现实：{{ it.realItemName }}
            </p>
            <p v-if="it.realEffect" class="bi-effect">{{ it.realEffect }}</p>
            <div class="bi-bonuses">
              <span v-for="b in it.bonuses" :key="b.attributeKey" :style="{ color: attrMeta(b.attributeKey).color }">
                {{ attrMeta(b.attributeKey).sigil }} {{ attrMeta(b.attributeKey).label }} +{{ b.bonusValue }}
              </span>
            </div>
          </div>
          <div class="bi-actions">
            <button class="btn btn-sm" :class="it.equipped ? 'btn-ghost' : 'btn-primary'" @click="toggle(it)">
              <component :is="it.equipped ? ShieldOff : ShieldCheck" :size="11" />
              {{ it.equipped ? '卸下' : '装备' }}
            </button>
            <button class="ico-btn" title="编辑" @click="openEdit(it)"><Pencil :size="12" /></button>
            <button class="ico-btn" title="销毁" @click="askDelete(it)"><Trash2 :size="12" /></button>
          </div>
        </article>
      </div>
    </GothicPanel>

    <ModalShell v-if="pickingSlot" title="选择槽位装备" :width="480" @close="pickingSlot = null">
      <button v-for="it in items.filter(i => i.category === pickingSlot)" :key="it.id" class="btn btn-ghost" @click="pickItem(it)">{{ it.gameItemName }} {{ it.equipped ? '（已装备）' : '' }}</button>
      <p v-if="!items.some(i => i.category === pickingSlot)">这个槽位还没有装备。</p>
      <template #footer="{ close }"><button class="btn btn-primary" @click="openCreate(pickingSlot); pickingSlot = null">新增装备</button></template>
    </ModalShell>
    <ModalShell
      v-if="showEditor" :state="form" :busy="saving"
      :title="editingId ? '编辑装备' : '铸造装备'"
      subtitle="EQUIPMENT"
      :width="560"
      @close="showEditor = false"
    >
      <div class="field-row">
        <label class="field">
          <span>游戏内名称</span>
          <input v-model="form.gameItemName" class="input" placeholder="例如：深渊凝视者之眼" />
        </label>
        <label class="field">
          <span>对应现实物品</span>
          <input v-model="form.realItemName" class="input" placeholder="例如：那副降噪耳机" />
        </label>
      </div>
      <div class="ai-name-row">
        <button class="btn btn-ghost btn-sm" :disabled="aiNaming" @click="aiName">
          <Sparkles :size="12" /> {{ aiNaming ? '军师铸造中…' : 'AI 起名 + 加成' }}
        </button>
        <span class="ai-name-hint">填好现实物品后点这里，军师帮你起哥特名并建议属性加成</span>
      </div>
      <label class="field">
        <span>槽位</span>
        <select v-model="form.category" class="select">
          <option v-for="s in EQUIP_SLOTS" :key="s.key" :value="s.key">{{ s.label }}</option>
        </select>
      </label>
      <label class="field">
        <span>现实效果</span>
        <textarea v-model="form.realEffect" class="textarea" rows="2" placeholder="它实际帮你做到了什么"></textarea>
      </label>

      <div class="field">
        <span>属性加成</span>
        <div v-for="(b, i) in form.bonuses" :key="i" class="bonus-row">
          <select v-model="b.attributeKey" class="select">
            <option v-for="(m, k) in ATTRIBUTE_META" :key="k" :value="k">{{ m.label }}</option>
          </select>
          <input v-model.number="b.bonusValue" type="number" class="input" step="1" />
          <button class="ico-btn" @click="form.bonuses.splice(i, 1)">✕</button>
        </div>
        <button class="btn btn-ghost btn-sm" @click="addBonus"><Plus :size="11" /> 加一条</button>
      </div>

      <template #footer="{ close }">
        <button class="btn btn-ghost" @click="close">取消</button>
        <button class="btn btn-primary" @click="submit">{{ editingId ? '保存' : '铸造' }}</button>
      </template>
    </ModalShell>

    <ModalShell
      v-if="showDelete"
      title="销毁装备"
      subtitle="此操作不可撤销"
      :width="420"
      danger
      @close="showDelete = false"
    >
      <p class="confirm-text">确定销毁「<b>{{ deleting?.gameItemName }}</b>」？</p>
      <template #footer="{ close }">
        <button class="btn btn-ghost" @click="showDelete = false">取消</button>
        <button class="btn btn-danger" @click="confirmDelete">确认销毁</button>
      </template>
    </ModalShell>

    <!-- 悬浮装备属性卡 (Warcraft/Dark Souls Tooltip) -->
    <Teleport to="body">
      <div
        v-if="tooltipVisible && hoveredItem"
        class="wow-tooltip"
        :style="{ left: tooltipPos.x + 'px', top: tooltipPos.y + 'px' }"
      >
        <span class="wt-corner wtc-tl"></span>
        <span class="wt-corner wtc-tr"></span>
        <span class="wt-corner wtc-bl"></span>
        <span class="wt-corner wtc-br"></span>

        <div class="wt-head">
          <h3 class="wt-title" :style="{ color: itemQuality(hoveredItem).color }">{{ hoveredItem.gameItemName }}</h3>
          <span class="wt-quality" :style="{ color: itemQuality(hoveredItem).color }">{{ itemQuality(hoveredItem).label }}</span>
        </div>
        <div class="wt-meta">
          <span>{{ EQUIP_SLOTS.find(s => s.key === hoveredItem.category)?.label || '装备' }}</span>
          <span class="wt-bind">灵魂绑定 · SOULBOUND</span>
        </div>
        <div class="wt-line"></div>
        <div v-if="hoveredItem.realItemName && hoveredItem.realItemName !== hoveredItem.gameItemName" class="wt-real">
          <span class="wt-label">现实映射：</span>
          <span class="wt-val">{{ hoveredItem.realItemName }}</span>
        </div>
        <div v-if="hoveredItem.bonuses?.length" class="wt-bonuses">
          <div v-for="b in hoveredItem.bonuses" :key="b.attributeKey" class="wt-bonus-row">
            <span class="wt-dot" :style="{ background: attrMeta(b.attributeKey).color }"></span>
            <span class="wt-bname">{{ attrMeta(b.attributeKey).label }}</span>
            <span class="wt-bval num" :style="{ color: attrMeta(b.attributeKey).color }">+{{ b.bonusValue }}</span>
          </div>
        </div>
        <div v-if="hoveredItem.realEffect" class="wt-lore">
          “{{ hoveredItem.realEffect }}”
        </div>
        <div class="wt-foot">
          <span v-if="hoveredItem.equipped" class="wt-action unequip">左键点击 · 卸下装备</span>
          <span v-else class="wt-action equip">左键点击 · 装备上身</span>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.equip-view {
  flex: 1;
  min-width: 0;
  display: grid;
  grid-template-columns: minmax(420px, 0.95fr) minmax(460px, 1.05fr);
  gap: 14px;
}
.slots-panel, .bag-panel { min-height: 0; }
.hint { font-size: 12px; color: var(--t-time); }

/* 纸娃娃布局：12 槽严格左右对称，中央立绘贯穿全高 */
.doll {
  flex: 1;
  min-height: 0;
  display: grid;
  grid-template-columns: 1fr 1.05fr 1fr;
  grid-template-rows: repeat(6, minmax(72px, 1fr));
  grid-template-areas:
    "head      center  shoulder"
    "chest     center  hands"
    "waist     center  legs"
    "wrist     center  feet"
    "mainhand  center  offhand"
    "trinket1  center  trinket2";
  gap: 9px;
  align-content: start;
}
.doll-center {
  grid-area: center;
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 10px 8px;
  background:
    radial-gradient(ellipse 80% 60% at 50% 40%, rgba(124, 255, 31, 0.08), transparent 70%),
    linear-gradient(180deg, rgba(10, 15, 9, 0.9), rgba(4, 6, 3, 0.96));
  border: 1px solid rgba(109, 91, 42, 0.35);
  box-shadow: inset 0 0 24px rgba(0, 0, 0, 0.9);
  overflow: hidden;
}
.doll-pedestal {
  position: absolute;
  inset: 6px;
  background: url('../assets/textures/faceless_portrait.webp') center 40% / 150px no-repeat;
  opacity: 0.18;
  mix-blend-mode: screen;
  pointer-events: none;
  -webkit-mask-image: radial-gradient(circle at 50% 50%, #000 40%, transparent 80%);
  mask-image: radial-gradient(circle at 50% 50%, #000 40%, transparent 80%);
}
.doll-emblem {
  position: relative;
  width: 140px;
  height: 140px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: url('../assets/textures/orn_emblem.webp') center / contain no-repeat;
  opacity: 1;
  filter: drop-shadow(0 0 22px rgba(124, 255, 31, 0.45)) drop-shadow(0 0 6px rgba(192, 166, 76, 0.5));
  animation: soulPulse 4.6s ease-in-out infinite;
}
.doll-sigil {
  font-size: 26px;
  color: var(--c-glow);
  text-shadow: var(--glow-soul-strong);
}
.doll-count { display: flex; align-items: baseline; gap: 4px; }
.dc-num { font-size: 22px; font-weight: 700; color: var(--t-num); text-shadow: 0 0 12px rgba(124, 255, 31, 0.3); }
.dc-label { font-size: 12px; color: var(--t-time); letter-spacing: 0.1em; }
.doll-bonus {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 6px;
  font-size: 12px;
}
.db-item { font-weight: 600; }
.db-empty { font-size: 12px; color: var(--t-disabled); letter-spacing: 0.1em; }
.slot {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 3px;
  min-height: 92px;
  padding: 9px 10px 8px;
  text-align: left;
  background:
    radial-gradient(ellipse 90% 70% at 50% 0%, rgba(24, 34, 18, 0.5), transparent 70%),
    linear-gradient(180deg, rgba(14, 20, 12, 0.95), rgba(5, 8, 5, 0.98));
  border: 1px solid rgba(109, 91, 42, 0.45);
  box-shadow:
    inset 0 2px 6px rgba(0, 0, 0, 0.9),
    inset 0 -1px 0 rgba(192, 166, 76, 0.12),
    0 1px 0 rgba(0, 0, 0, 0.8);
  cursor: pointer;
  transition: all 0.16s;
}
.slot::after {
  content: '';
  position: absolute;
  inset: 0;
  border: 6px solid transparent;
  border-image: url('../assets/textures/frame_panel.webp') 30 / 6px stretch;
  pointer-events: none;
  opacity: 0.8;
}
.slot:hover { border-color: rgba(192, 166, 76, 0.75); }
.slot.filled {
  border-color: rgba(124, 255, 31, 0.5);
  box-shadow:
    inset 0 0 18px rgba(124, 255, 31, 0.12),
    inset 0 2px 6px rgba(0, 0, 0, 0.85),
    0 0 12px rgba(124, 255, 31, 0.18);
}
.slot-icon-img {
  position: absolute;
  right: 6px;
  top: 6px;
  width: 28px;
  height: 28px;
  opacity: 0.35;
  filter: grayscale(0.8);
  pointer-events: none;
  transition: all 0.2s;
}
.slot.filled .slot-icon-img {
  opacity: 0.88;
  filter: drop-shadow(0 0 6px rgba(124, 255, 31, 0.55));
}
.slot-label {
  font-size: 12px;
  letter-spacing: 0.18em;
  color: var(--t-time);
  position: relative;
  z-index: 1;
}
.slot-body { flex: 1; display: flex; flex-direction: column; gap: 2px; position: relative; z-index: 1; }
.slot-name {
  font-size: 12px;
  color: var(--t-title);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.slot-bonus { display: flex; flex-wrap: wrap; gap: 5px; font-size: 12px; }
.slot-empty { font-size: 12px; color: var(--t-disabled); }

.bonus-total {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
  padding-top: 8px;
  border-top: 1px solid rgba(109, 91, 42, 0.25);
  font-size: 12px;
}
.bt-label { color: var(--t-time); letter-spacing: 0.1em; }
.bt-item { font-weight: 600; }

.bag-list {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding-right: 4px;
}
.bag-item {
  display: flex;
  gap: 10px;
  padding: 10px 12px;
  background: linear-gradient(180deg, rgba(16, 22, 13, 0.9), rgba(7, 10, 6, 0.95));
  border: 1px solid rgba(109, 91, 42, 0.35);
}
.bag-item.on { border-color: rgba(124, 255, 31, 0.4); }
.bi-main { flex: 1; min-width: 0; }
.bi-slot {
  display: inline-block;
  padding: 0 6px;
  font-size: 12px;
  letter-spacing: 0.12em;
  color: var(--t-time);
  border: 1px solid rgba(109, 91, 42, 0.45);
}
.bi-main h4 { margin-top: 4px; font-size: 13px; color: var(--t-title); }
.bi-real { margin-top: 2px; font-size: 12px; color: var(--t-time); }
.bi-effect { margin-top: 3px; font-size: 12px; color: var(--t-body); }
.bi-bonuses { display: flex; gap: 8px; margin-top: 5px; font-size: 12px; }
.bi-actions { display: flex; flex-direction: column; gap: 4px; align-items: flex-end; }
.ico-btn {
  width: 20px; height: 20px;
  display: flex; align-items: center; justify-content: center;
  color: var(--t-sub); background: transparent; border: 1px solid transparent; cursor: pointer;
}
.ico-btn:hover { color: var(--c-glow); border-color: rgba(109, 91, 42, 0.6); }

.field { display: flex; flex-direction: column; gap: 5px; }
.field > span { font-size: 12px; color: var(--t-sub); }
.field-row { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.ai-name-row {
  display: flex;
  align-items: center;
  gap: 10px;
}
.ai-name-hint { font-size: 12px; color: var(--t-disabled); }
.bonus-row { display: grid; grid-template-columns: 1fr 90px 24px; gap: 8px; align-items: center; }
.confirm-text { font-size: 13px; line-height: 1.9; }
.confirm-text b { color: var(--t-title); }

/* 魔兽/暗黑风格装备悬浮属性卡 (WoW Tooltip) */
.wow-tooltip {
  position: fixed;
  z-index: 99999;
  width: 280px;
  padding: 12px 14px 10px;
  background:
    url('../assets/textures/mat_metal_iron.webp') center / 240px,
    linear-gradient(180deg, rgba(14, 20, 12, 0.98), rgba(5, 8, 5, 0.99));
  background-blend-mode: soft-light, normal;
  border: 1px solid #6d5b2a;
  box-shadow:
    inset 0 1px 0 rgba(255, 235, 160, 0.25),
    inset 0 0 16px rgba(0, 0, 0, 0.9),
    0 12px 32px rgba(0, 0, 0, 0.95),
    0 0 16px rgba(124, 255, 31, 0.2);
  pointer-events: none;
  display: flex;
  flex-direction: column;
  gap: 6px;
  animation: tooltipFade 0.12s ease-out;
}
@keyframes tooltipFade {
  from { opacity: 0; transform: scale(0.97); }
  to { opacity: 1; transform: scale(1); }
}
.wt-corner {
  position: absolute;
  width: 14px;
  height: 14px;
  pointer-events: none;
  opacity: 0.92;
  background: url('../assets/textures/orn_corner.webp') no-repeat top left / contain;
  filter: drop-shadow(0 0 3px rgba(124, 255, 31, 0.3));
}
.wtc-tl { top: -1px; left: -1px; }
.wtc-tr { top: -1px; right: -1px; transform: scaleX(-1); }
.wtc-bl { bottom: -1px; left: -1px; transform: scaleY(-1); }
.wtc-br { bottom: -1px; right: -1px; transform: scale(-1, -1); }

.wt-head {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 6px;
}
.wt-title {
  font-family: var(--f-title);
  font-size: 14px;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-shadow: 0 0 10px currentColor;
}
.wt-quality {
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.08em;
  white-space: nowrap;
}
.wt-meta {
  display: flex;
  justify-content: space-between;
  font-size: 12px;
  color: var(--t-time);
}
.wt-bind {
  color: #a08e63;
  letter-spacing: 0.08em;
}
.wt-line {
  height: 1px;
  background: linear-gradient(90deg, rgba(192, 166, 76, 0.5), rgba(124, 255, 31, 0.3) 50%, transparent);
  margin: 2px 0;
}
.wt-real {
  font-size: 12px;
  display: flex;
  gap: 4px;
}
.wt-label { color: #8a8064; }
.wt-val { color: #d6d0b6; }
.wt-bonuses {
  display: flex;
  flex-direction: column;
  gap: 3px;
  padding: 4px 0;
}
.wt-bonus-row {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
}
.wt-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  box-shadow: 0 0 6px currentColor;
}
.wt-bname { color: #e2dbbe; }
.wt-bval { font-weight: 700; }
.wt-lore {
  font-size: 12px;
  font-style: italic;
  color: #9c9274;
  line-height: 1.5;
  padding-top: 4px;
  border-top: 1px dashed rgba(109, 91, 42, 0.35);
}
.wt-foot {
  padding-top: 5px;
  border-top: 1px solid rgba(109, 91, 42, 0.35);
  font-size: 12px;
  text-align: right;
}
.wt-action {
  letter-spacing: 0.08em;
  font-weight: 600;
}
.wt-action.equip { color: var(--c-glow); text-shadow: 0 0 6px rgba(124, 255, 31, 0.5); }
.wt-action.unequip { color: #e07050; text-shadow: 0 0 6px rgba(224, 90, 74, 0.5); }

/* 行囊空状态：4x3 幽灵行囊卡槽网格 */
.empty-inventory {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 6px 0;
}
.ei-desc {
  display: flex;
  flex-direction: column;
  gap: 3px;
  font-size: 12px;
  color: #cfea78;
  font-weight: 600;
}
.ei-sub {
  font-size: 12px;
  color: var(--t-disabled);
  font-weight: normal;
}
.ghost-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 8px;
}
.ghost-slot {
  position: relative;
  height: 66px;
  display: flex;
  align-items: center;
  justify-content: center;
  background:
    url('../assets/textures/mat_metal_iron.webp') center / 180px,
    linear-gradient(180deg, rgba(16, 22, 14, 0.9), rgba(7, 10, 6, 0.96));
  background-blend-mode: soft-light, normal;
  border: 1px solid #4a3c1b;
  box-shadow:
    inset 1px 1px 0 rgba(192, 166, 76, 0.25),
    inset -1px -1px 0 rgba(0, 0, 0, 0.9),
    0 2px 5px rgba(0, 0, 0, 0.7);
  cursor: pointer;
  transition: all 0.16s;
}
.ghost-slot:hover {
  border-color: rgba(192, 166, 76, 0.85);
  background:
    url('../assets/textures/mat_metal_iron.webp') center / 180px,
    linear-gradient(180deg, rgba(24, 34, 18, 0.95), rgba(9, 14, 8, 0.98));
  box-shadow:
    inset 1px 1px 0 rgba(255, 235, 160, 0.35),
    0 0 14px rgba(124, 255, 31, 0.25);
}
.gs-num {
  position: absolute;
  top: 3px;
  left: 6px;
  font-size: 12px;
  color: #6a624a;
}
.gs-plus {
  font-size: 18px;
  color: #736746;
  transition: all 0.16s;
}
.ghost-slot:hover .gs-plus {
  color: var(--c-glow);
  text-shadow: 0 0 8px rgba(124, 255, 31, 0.8);
}
.bag-add-btn {
  background:
    url('../assets/textures/btn_plate.webp') center / 100% 100%,
    linear-gradient(180deg, #1d2c10 0%, #0a1206 100%);
  border: 1px solid rgba(192, 166, 76, 0.8);
  box-shadow: inset 0 1px 0 rgba(255, 235, 160, 0.35), 0 0 12px rgba(124, 255, 31, 0.3);
}
</style>

<style scoped>
@media(max-width:1200px){.equip-view{grid-template-columns:1fr;overflow-y:auto}.slots-panel{min-height:640px}.bag-panel{min-height:360px}}
.doll-center{min-width:0}.doll-emblem{max-width:100%}.slot{min-width:0}.bag-item{align-items:center}
</style>
