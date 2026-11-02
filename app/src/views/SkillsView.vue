<script setup>
const saving = ref(false);
import { computed, onMounted, reactive, ref } from 'vue';
import { Plus, Lock, Unlock, Pencil, Trash2, Zap, Sparkles } from 'lucide-vue-next';
import GothicPanel from '../components/GothicPanel.vue';
import ModalShell from '../components/ModalShell.vue';
import ExpBar from '../components/ExpBar.vue';
import { api } from '../api';
import { notify } from '../stores/toast';
import { sfx } from '../utils/audio';

const skills = ref([]);
const loading = ref(true);

const showEditor = ref(false);
const editingId = ref(null);
const form = reactive({ name: '', category: '通用', description: '' });
const showDelete = ref(false);
const deleting = ref(null);

async function load() {
  loading.value = true;
  try {
    skills.value = await api.skillList();
  } catch (e) {
    notify.error('读取技能失败：' + e.message);
  } finally {
    loading.value = false;
  }
}

/** 哥特暗黑三大天赋专精柱（Warcraft/Diablo 经典三系天赋树阵列） */
const pillars = computed(() => {
  const defaults = [
    {
      key: '通用',
      name: '意志修持 · 通用系',
      subtitle: 'DISCIPLINE & HABIT',
      glyph: '✦',
      desc: '基石自律、全域习惯与精力守恒',
      list: [],
    },
    {
      key: '专业',
      name: '深度造物 · 专业系',
      subtitle: 'CRAFT & MASTERY',
      glyph: '⚡',
      desc: '硬核技艺、专注工作与专业突破',
      list: [],
    },
    {
      key: '生活',
      name: '淬体调神 · 生活系',
      subtitle: 'VITALITY & SPIRIT',
      glyph: '◈',
      desc: '肉身锻造、生命体验与情智恢复',
      list: [],
    },
  ];

  const map = new Map();
  defaults.forEach((d) => map.set(d.key, { ...d, list: [] }));

  for (const s of skills.value) {
    const rawCat = (s.category || '通用').trim();
    let matchedKey = null;
    for (const d of defaults) {
      if (rawCat === d.key || rawCat.includes(d.key)) {
        matchedKey = d.key;
        break;
      }
    }
    if (matchedKey) {
      map.get(matchedKey).list.push(s);
    } else {
      if (!map.has(rawCat)) {
        map.set(rawCat, {
          key: rawCat,
          name: `${rawCat}天赋系`,
          subtitle: 'ARCANE BRANCH',
          glyph: '✵',
          desc: '独立外传秘法分支',
          list: [],
        });
      }
      map.get(rawCat).list.push(s);
    }
  }

  return [...map.values()];
});

const unlockedCount = computed(() => skills.value.filter((s) => s.isUnlocked).length);

function openCreate() {
  editingId.value = null;
  Object.assign(form, { name: '', category: '通用', description: '' });
  showEditor.value = true;
}

function openCreateFor(cat) {
  editingId.value = null;
  Object.assign(form, { name: '', category: cat || '通用', description: '' });
  showEditor.value = true;
}

function openEdit(s) {
  editingId.value = s.id;
  Object.assign(form, {
    name: s.name,
    category: s.category || '通用',
    description: s.description || '',
  });
  showEditor.value = true;
}

async function submit() {
  if (!form.name.trim()) {
    notify.warn('技能名不能为空');
    return;
  }
  if (saving.value) return;
  saving.value = true;
  try {
    await api.skillSave({
      id: editingId.value,
      name: form.name.trim(),
      category: form.category || '通用',
      description: form.description,
    });
    sfx.hammer();
    notify.success(editingId.value ? '技能已更新' : '技能已铭刻');
    showEditor.value = false;
    await load();
  } catch (e) {
    notify.error('保存失败：' + e.message);
  } finally { saving.value = false; }
}

async function toggleUnlock(s) {
  try {
    if (s.isUnlocked) {
      await api.skillRelock(s.id);
      sfx.hammer();
      notify.info(`「${s.name}」已重新封印`);
    } else {
      await api.skillUnlock(s.id);
      sfx.soulFlame();
      notify.success(`「${s.name}」突破封印，已解锁！`);
    }
    await load();
  } catch (e) {
    notify.error('操作失败：' + e.message);
  }
}

async function addExp(s, amount) {
  try {
    const updated = await api.skillAddExp(s.id, amount);
    if (updated.level > s.level) {
      sfx.levelUp();
      notify.success(`「${s.name}」突破进阶！升至 Lv.${updated.level}`);
    } else {
      sfx.hammer();
      notify.success(`「${s.name}」+${amount} 熟练度`);
    }
    await load();
  } catch (e) {
    notify.error('操作失败：' + e.message);
  }
}

function askDelete(s) {
  deleting.value = s;
  showDelete.value = true;
}

async function confirmDelete() {
  try {
    await api.skillDelete(deleting.value.id);
    notify.info('技能已抹除');
    showDelete.value = false;
    await load();
  } catch (e) {
    notify.error('删除失败：' + e.message);
  }
}

onMounted(load);
</script>

<template>
  <div class="skills-view">
    <GothicPanel title="技能树 · 天赋神殿" subtitle="TALENT SANCTUARY" class="main-panel">
      <template #actions>
        <div class="head-actions">
          <span class="hint">{{ unlockedCount }} / {{ skills.length }} 秘技已参悟</span>
          <button class="btn btn-primary btn-sm" @click="openCreate"><Plus :size="13" /> 铭刻新秘技</button>
        </div>
      </template>

      <!-- 三大专精支柱网格 (3-Pillar Talent Grid) -->
      <div class="sanctuary-body scroll">
        <div class="pillars-grid">
          <section v-for="p in pillars" :key="p.key" class="pillar">
            <span class="pillar-corner plc-tl"></span>
            <span class="pillar-corner plc-tr"></span>
            <span class="pillar-corner plc-bl"></span>
            <span class="pillar-corner plc-br"></span>

            <!-- 支柱殿堂冠额 -->
            <header class="pillar-head">
              <span class="ph-corner phc-tl"></span>
              <span class="ph-corner phc-tr"></span>
              <div class="ph-top">
                <span class="ph-glyph">{{ p.glyph }}</span>
                <div class="ph-titles">
                  <h3 class="ph-name">{{ p.name }}</h3>
                  <span class="ph-sub">{{ p.subtitle }}</span>
                </div>
                <button class="ph-add-btn" :title="`在「${p.key}」系下铭刻技能`" @click="openCreateFor(p.key)">
                  <Plus :size="11" />
                </button>
              </div>
              <p class="ph-desc">{{ p.desc }}</p>
              <div class="ph-stat">
                <span class="ph-count">
                  {{ p.list.filter(x => x.isUnlocked).length }} / {{ p.list.length }} 阶已觉醒
                </span>
                <div class="ph-line"></div>
              </div>
            </header>

            <!-- 支柱内部阶梯与导轨 -->
            <div class="pillar-body">
              <!-- 左侧主干能量导轨 -->
              <div class="pillar-rail"></div>

              <!-- 节点卡片流 -->
              <div class="nodes-list">
                <article
                  v-for="(s, idx) in p.list"
                  :key="s.id"
                  class="talent-node"
                  :class="{ locked: !s.isUnlocked }"
                >
                  <!-- 导轨横向连接线 -->
                  <div class="node-connector">
                    <span class="nc-line"></span>
                    <span class="nc-pin"></span>
                  </div>

                  <!-- 节点卡片上部 -->
                  <div class="tn-top">
                    <div class="tn-badge-wrap">
                      <div class="tn-badge">
                        <span>{{ s.isUnlocked ? p.glyph : '🔒' }}</span>
                      </div>
                      <span class="tn-tier num">TIER {{ idx + 1 }}</span>
                    </div>

                    <div class="tn-info">
                      <div class="tn-name-row">
                        <h4 class="tn-name">{{ s.name }}</h4>
                        <span v-if="s.isUnlocked" class="tn-lv num">Lv.{{ s.level }}</span>
                        <span v-else class="tn-locked-tag">封印中</span>
                      </div>
                      <p class="tn-desc">{{ s.description || '暂无秘传心诀' }}</p>
                    </div>
                  </div>

                  <!-- 熟练度经验条 -->
                  <div class="tn-exp-row">
                    <template v-if="s.isUnlocked">
                      <div class="tn-exp-head">
                        <span>熟练度淬炼</span>
                        <span class="num">{{ s.experience }} / 100</span>
                      </div>
                      <ExpBar :percent="s.experience" :value="`${s.experience}%`" :height="11" :show-shine="false" />
                    </template>
                    <div v-else class="tn-sealed-hint">
                      <span>需解除封印方可积聚熟练度</span>
                    </div>
                  </div>

                  <!-- 操作按钮底栏 -->
                  <div class="tn-actions">
                    <button
                      class="btn btn-sm tn-btn"
                      :class="s.isUnlocked ? 'btn-ghost' : 'btn-primary'"
                      @click="toggleUnlock(s)"
                    >
                      <component :is="s.isUnlocked ? Lock : Unlock" :size="11" />
                      {{ s.isUnlocked ? '凝神封印' : '解除封印' }}
                    </button>
                    <button
                      v-if="s.isUnlocked"
                      class="btn btn-sm btn-ghost tn-btn tn-pump-btn"
                      title="注入 20 点熟练度"
                      @click="addExp(s, 20)"
                    >
                      <Zap :size="11" /> +20 淬炼
                    </button>
                    <div class="tn-tool-btns">
                      <button class="ico-btn" title="编辑秘技" @click="openEdit(s)"><Pencil :size="12" /></button>
                      <button class="ico-btn" title="抹除秘技" @click="askDelete(s)"><Trash2 :size="12" /></button>
                    </div>
                  </div>
                </article>

                <!-- 待启封的天赋卡槽 (保持三列严格对齐与纯粹神殿质感) -->
                <div
                  v-for="extraIdx in Math.max(1, 3 - p.list.length)"
                  :key="'extra-' + extraIdx"
                  class="talent-node placeholder"
                  title="点击在此专精下铭刻新秘技"
                  @click="openCreateFor(p.key)"
                >
                  <div class="node-connector locked">
                    <span class="nc-line"></span>
                    <span class="nc-pin"></span>
                  </div>
                  <div class="pl-center">
                    <div class="pl-badge">
                      <Plus :size="15" />
                    </div>
                    <div class="pl-meta">
                      <span class="pl-tier num">TIER {{ p.list.length + extraIdx }} · 待启封</span>
                      <span class="pl-desc">点击此槽位在此神殿参悟新秘技</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </section>
        </div>
      </div>
    </GothicPanel>

    <!-- 弹窗：编辑技能 -->
    <ModalShell
      v-if="showEditor" :state="form" :busy="saving"
      :title="editingId ? '编辑秘技' : '铭刻新秘技'"
      subtitle="TALENT INSCRIPTION"
      :width="520"
      @close="showEditor = false"
    >
      <label class="field">
        <span>秘技名</span>
        <input v-model="form.name" class="input" placeholder="例如：深度心流 / 力量深蹲 / 结构化复盘" />
      </label>
      <label class="field">
        <span>归属神殿分支</span>
        <input v-model="form.category" class="input" placeholder="通用 / 专业 / 生活" />
      </label>
      <label class="field">
        <span>心法秘诀（描述）</span>
        <textarea v-model="form.description" class="textarea" rows="3" placeholder="这项能力在现实中代表怎样的突破与心法"></textarea>
      </label>
      <template #footer="{ close }">
        <button class="btn btn-ghost" @click="close">取消</button>
        <button class="btn btn-primary" @click="submit">{{ editingId ? '保存修正' : '铭刻入树' }}</button>
      </template>
    </ModalShell>

    <!-- 弹窗：删除确认 -->
    <ModalShell
      v-if="showDelete"
      title="抹除秘技"
      subtitle="此操作将永久抹除其等级记忆"
      :width="420"
      danger
      @close="showDelete = false"
    >
      <p class="confirm-text">确定从神殿中永久抹除「<b>{{ deleting?.name }}</b>」？</p>
      <template #footer="{ close }">
        <button class="btn btn-ghost" @click="showDelete = false">取消</button>
        <button class="btn btn-danger" @click="confirmDelete">确认抹除</button>
      </template>
    </ModalShell>
  </div>
</template>

<style scoped>
.skills-view {
  flex: 1;
  min-width: 0;
  display: flex;
}
.main-panel {
  flex: 1;
}
.head-actions {
  display: flex;
  align-items: center;
  gap: 12px;
}
.head-actions .btn-primary {
  background:
    url('../assets/textures/btn_plate.webp') center / 100% 100%,
    linear-gradient(180deg, #1d2c10 0%, #0a1206 100%);
  border: 1px solid rgba(192, 166, 76, 0.85);
  box-shadow:
    inset 0 1px 0 rgba(255, 235, 160, 0.45),
    0 0 14px rgba(124, 255, 31, 0.35),
    0 2px 6px rgba(0, 0, 0, 0.8);
  color: #eaffc0;
  font-size: 12px;
  letter-spacing: 0.08em;
}
.hint {
  font-size: 12px;
  color: var(--t-time);
  letter-spacing: 0.04em;
}

/* 神殿主滚动容器 */
.sanctuary-body {
  flex: 1;
  min-height: 0;
  padding: 6px 4px 10px 0;
}

/* 三列并列天赋神殿格架 (3-Pillar Talent Grid) */
.pillars-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
  gap: 18px;
  align-items: start;
}

/* 单个专精支柱神殿 */
.pillar {
  position: relative;
  display: flex;
  flex-direction: column;
  background:
    url('../assets/textures/mat_stone.webp') center / 300px,
    linear-gradient(180deg, rgba(16, 24, 14, 0.96), rgba(6, 9, 6, 0.98));
  background-blend-mode: soft-light, normal;
  border: 1px solid #4a3c1b;
  box-shadow:
    inset 1px 1px 0 rgba(192, 166, 76, 0.35),
    inset -1px -1px 0 rgba(0, 0, 0, 0.9),
    0 4px 14px rgba(0, 0, 0, 0.7);
  padding: 12px 14px 16px;
}

/* 支柱冠额卡 */
.pillar-head {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 5px;
  padding: 10px 12px;
  background:
    url('../assets/textures/mat_metal_iron.webp') center / 220px,
    linear-gradient(180deg, rgba(28, 38, 22, 0.96), rgba(12, 16, 10, 0.98));
  background-blend-mode: soft-light, normal;
  border: 1px solid #6d5b2a;
  box-shadow:
    inset 0 1px 0 rgba(255, 235, 160, 0.25),
    0 2px 8px rgba(0, 0, 0, 0.8);
  margin-bottom: 12px;
}
.ph-corner {
  position: absolute;
  width: 14px;
  height: 14px;
  pointer-events: none;
  opacity: 0.9;
  background: url('../assets/textures/orn_corner.webp') no-repeat top left / contain;
  filter: drop-shadow(0 0 3px rgba(124, 255, 31, 0.3));
}
.phc-tl { top: -1px; left: -1px; }
.phc-tr { top: -1px; right: -1px; transform: scaleX(-1); }

.ph-top {
  display: flex;
  align-items: center;
  gap: 8px;
}
.ph-glyph {
  font-size: 16px;
  color: var(--c-glow);
  text-shadow: 0 0 10px rgba(124, 255, 31, 0.6);
  flex: 0 0 auto;
}
.ph-titles {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
}
.ph-name {
  font-family: var(--f-title);
  font-size: 13.5px;
  font-weight: 700;
  color: #cfea78;
  letter-spacing: 0.12em;
  text-shadow: 0 0 8px rgba(207, 234, 120, 0.35);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.ph-sub {
  font-size: 12px;
  color: #a08e63;
  letter-spacing: 0.16em;
  font-family: var(--f-mono);
}
.ph-add-btn {
  width: 20px;
  height: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #dcd6be;
  background: rgba(0, 0, 0, 0.5);
  border: 1px solid rgba(192, 166, 76, 0.6);
  cursor: pointer;
  transition: all 0.16s;
}
.ph-add-btn:hover {
  color: #fff;
  border-color: var(--c-soul);
  box-shadow: 0 0 8px rgba(124, 255, 31, 0.5);
}
.ph-desc {
  font-size: 12px;
  color: #b5a982;
  line-height: 1.4;
  margin-top: 2px;
}
.ph-stat {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 4px;
}
.ph-count {
  font-size: 12px;
  font-weight: 600;
  color: var(--c-glow);
  letter-spacing: 0.08em;
  white-space: nowrap;
}
.ph-line {
  flex: 1;
  height: 1px;
  background: linear-gradient(90deg, rgba(192, 166, 76, 0.45), transparent);
}

/* 支柱内部主体与导轨系统 */
.pillar-body {
  position: relative;
  display: flex;
  flex-direction: column;
  padding-left: 22px;
  min-height: 440px;
}

/* 纵向能量导轨 */
.pillar-rail {
  position: absolute;
  left: 7px;
  top: 10px;
  bottom: 20px;
  width: 4px;
  background: linear-gradient(180deg, rgba(124, 255, 31, 0.95) 0%, rgba(192, 166, 76, 0.75) 45%, rgba(124, 255, 31, 0.4) 85%, transparent);
  box-shadow: 0 0 14px rgba(124, 255, 31, 0.75), 0 0 3px #fff;
}
.pillar-rail::before {
  content: '';
  position: absolute;
  top: -5px;
  left: -4px;
  width: 12px;
  height: 12px;
  background: var(--c-soul);
  transform: rotate(45deg);
  box-shadow: 0 0 14px rgba(124, 255, 31, 0.95), 0 0 3px #fff;
}

/* 支柱空状态 */
.pillar-empty {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 40px 10px;
  text-align: center;
  background: rgba(0, 0, 0, 0.3);
  border: 1px dashed rgba(109, 91, 42, 0.4);
}
.pe-circle {
  width: 70px;
  height: 70px;
  background: url('../assets/textures/rune_watermark.webp') center / contain no-repeat;
  opacity: 0.25;
}
.pe-title {
  font-size: 12px;
  font-weight: 600;
  color: #cfea78;
  letter-spacing: 0.1em;
}
.pe-sub {
  font-size: 12px;
  color: var(--t-disabled);
}
.pe-btn {
  margin-top: 6px;
}

/* 节点卡片流 */
.nodes-list {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

/* 天赋卡片 */
.talent-node {
  position: relative;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  height: 128px;
  box-sizing: border-box;
  padding: 10px 12px 9px;
  background:
    url('../assets/textures/mat_metal_iron.webp') center / 240px,
    linear-gradient(180deg, rgba(24, 32, 20, 0.95), rgba(9, 13, 8, 0.98));
  background-blend-mode: soft-light, normal;
  border: 1px solid #4a3c1b;
  box-shadow:
    inset 1px 1px 0 rgba(192, 166, 76, 0.35),
    inset -1px -1px 0 rgba(0, 0, 0, 0.85),
    0 3px 8px rgba(0, 0, 0, 0.7);
  transition: all 0.16s ease;
}
.talent-node:hover {
  border-color: rgba(192, 166, 76, 0.85);
  box-shadow:
    inset 1px 1px 0 rgba(255, 235, 160, 0.3),
    0 0 14px rgba(124, 255, 31, 0.25);
}
.talent-node.locked {
  opacity: 0.75;
  filter: grayscale(0.2);
}

/* 导轨横向连接线 */
.node-connector {
  position: absolute;
  left: -22px;
  top: 24px;
  pointer-events: none;
}
.nc-line {
  position: absolute;
  left: 0;
  top: 0;
  width: 22px;
  height: 3px;
  background: linear-gradient(90deg, #7cff1f 0%, #c0a64c 100%);
  box-shadow: 0 0 8px rgba(124, 255, 31, 0.6);
}
.nc-pin {
  position: absolute;
  left: -5px;
  top: -3.5px;
  width: 10px;
  height: 10px;
  background: var(--c-soul);
  transform: rotate(45deg);
  box-shadow: 0 0 10px rgba(124, 255, 31, 0.9), 0 0 2px #fff;
}
.talent-node.locked .nc-line {
  background: linear-gradient(90deg, rgba(109, 91, 42, 0.6), rgba(109, 91, 42, 0.2));
  box-shadow: none;
}
.talent-node.locked .nc-pin {
  background: #736746;
  box-shadow: none;
}

/* 卡片头部 */
.tn-top {
  display: flex;
  gap: 10px;
  align-items: flex-start;
}
.tn-badge-wrap {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 3px;
  flex: 0 0 auto;
}
.tn-badge {
  width: 42px;
  height: 42px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  background: url('../assets/textures/plate_skill.webp') no-repeat center / contain;
  filter: drop-shadow(0 2px 5px rgba(0, 0, 0, 0.85));
}
.talent-node:not(.locked) .tn-badge {
  color: var(--c-glow);
  text-shadow: 0 0 10px rgba(124, 255, 31, 0.75);
  filter: drop-shadow(0 0 8px rgba(124, 255, 31, 0.4));
}
.talent-node.locked .tn-badge {
  color: var(--t-disabled);
  opacity: 0.6;
}
.tn-tier {
  font-size: 12px;
  color: #a08e63;
  letter-spacing: 0.12em;
  font-weight: 600;
}

.tn-info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.tn-name-row {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 6px;
}
.tn-name {
  font-family: var(--f-title);
  font-size: 13.5px;
  font-weight: 700;
  color: #cfea78;
  letter-spacing: 0.06em;
  text-shadow: 0 0 8px rgba(207, 234, 120, 0.3);
}
.tn-lv {
  font-size: 12px;
  font-weight: 700;
  color: var(--c-glow);
  text-shadow: 0 0 8px rgba(124, 255, 31, 0.6);
  white-space: nowrap;
}
.tn-locked-tag {
  font-size: 12px;
  color: #a09575;
  background: rgba(0, 0, 0, 0.4);
  border: 1px solid rgba(109, 91, 42, 0.45);
  padding: 1px 6px;
  letter-spacing: 0.08em;
}
.tn-desc {
  font-size: 12px;
  color: #c9bea0;
  line-height: 1.45;
  margin-top: 2px;
}

/* 熟练度经验条 */
.tn-exp-row {
  display: flex;
  flex-direction: column;
  gap: 3px;
  padding: 3px 0 1px;
}
.tn-exp-head {
  display: flex;
  justify-content: space-between;
  font-size: 12px;
  color: #bfae82;
  letter-spacing: 0.06em;
}
.tn-sealed-hint {
  font-size: 12px;
  color: #a39570;
  font-style: italic;
  padding: 2px 0;
}

/* 节点操作底栏 */
.tn-actions {
  display: flex;
  align-items: center;
  gap: 6px;
  padding-top: 6px;
  border-top: 1px solid rgba(109, 91, 42, 0.3);
  margin-top: 2px;
}
.tn-pump-btn {
  color: var(--c-glow);
  border-color: rgba(124, 255, 31, 0.45);
}
.tn-pump-btn:hover {
  border-color: var(--c-soul);
  box-shadow: 0 0 8px rgba(124, 255, 31, 0.4);
}
.tn-tool-btns {
  display: flex;
  gap: 4px;
  margin-left: auto;
}
.ico-btn {
  width: 22px;
  height: 22px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #a08e63;
  background:
    url('../assets/textures/btn_plate.webp') center / 100% 100%,
    linear-gradient(180deg, rgba(22, 28, 16, 0.92), rgba(8, 10, 6, 0.96));
  border: 1px solid rgba(192, 166, 76, 0.45);
  box-shadow: inset 0 1px 0 rgba(255, 235, 160, 0.15);
  cursor: pointer;
  transition: all 0.14s;
}
.ico-btn:hover {
  color: var(--c-glow);
  border-color: var(--c-soul);
  box-shadow: 0 0 8px rgba(124, 255, 31, 0.4);
}

.field {
  display: flex;
  flex-direction: column;
  gap: 5px;
}
.field > span {
  font-size: 12px;
  color: var(--t-sub);
}
.confirm-text {
  font-size: 13px;
  line-height: 1.9;
}
.confirm-text b {
  color: var(--t-title);
}

/* 支柱四角护角铸件 */
.pillar-corner {
  position: absolute;
  width: 16px;
  height: 16px;
  pointer-events: none;
  opacity: 0.92;
  background: url('../assets/textures/orn_corner.webp') no-repeat top left / contain;
  filter: drop-shadow(0 0 3px rgba(124, 255, 31, 0.25));
  z-index: 2;
}
.plc-tl { top: -1px; left: -1px; }
.plc-tr { top: -1px; right: -1px; transform: scaleX(-1); }
.plc-bl { bottom: -1px; left: -1px; transform: scaleY(-1); }
.plc-br { bottom: -1px; right: -1px; transform: scale(-1, -1); }

/* 占位卡槽样式 (Talent Socket Placeholder) */
.talent-node.placeholder {
  height: 128px;
  box-sizing: border-box;
  display: flex;
  align-items: center;
  justify-content: center;
  background:
    url('../assets/textures/mat_stone.webp') center / 240px,
    linear-gradient(180deg, rgba(14, 18, 12, 0.78), rgba(7, 10, 6, 0.88));
  background-blend-mode: soft-light, normal;
  border: 1px dashed rgba(192, 166, 76, 0.35);
  box-shadow: inset 0 2px 6px rgba(0, 0, 0, 0.9);
  cursor: pointer;
  opacity: 0.65;
  padding: 12px;
}
.talent-node.placeholder:hover {
  opacity: 0.98;
  border-color: rgba(124, 255, 31, 0.75);
  background:
    url('../assets/textures/mat_stone.webp') center / 240px,
    linear-gradient(180deg, rgba(22, 32, 18, 0.88), rgba(10, 14, 8, 0.94));
  box-shadow: 0 0 16px rgba(124, 255, 31, 0.28);
}
.pl-center {
  display: flex;
  align-items: center;
  gap: 12px;
}
.pl-badge {
  width: 38px;
  height: 38px;
  flex: 0 0 auto;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #a08e63;
  border: 1px dashed rgba(192, 166, 76, 0.5);
  background: rgba(0, 0, 0, 0.4);
  transform: rotate(45deg);
  transition: all 0.16s;
}
.pl-badge > * { transform: rotate(-45deg); }
.talent-node.placeholder:hover .pl-badge {
  color: #05120a;
  background: var(--c-soul);
  border-color: #d4ff6b;
  box-shadow: 0 0 10px rgba(124, 255, 31, 0.8);
}
.pl-meta {
  display: flex;
  flex-direction: column;
  gap: 2px;
  text-align: left;
}
.pl-tier {
  font-family: var(--f-title);
  font-size: 12.5px;
  font-weight: 700;
  color: #d8cfac;
  letter-spacing: 0.08em;
}
.talent-node.placeholder:hover .pl-tier {
  color: #cfea78;
  text-shadow: 0 0 8px rgba(207, 234, 120, 0.4);
}
.pl-desc {
  font-size: 12px;
  color: #8c8266;
}
.talent-node.placeholder:hover .pl-desc {
  color: #b5a982;
}

/* 按钮微调质感 */
.tn-btn {
  background:
    url('../assets/textures/btn_plate.webp') center / 100% 100%,
    linear-gradient(180deg, rgba(24, 32, 18, 0.95), rgba(10, 14, 8, 0.98));
  border: 1px solid rgba(192, 166, 76, 0.55);
  box-shadow: inset 0 1px 0 rgba(255, 235, 160, 0.2);
}
.tn-btn:hover {
  border-color: var(--c-soul);
  box-shadow: 0 0 10px rgba(124, 255, 31, 0.35);
}
</style>

<style scoped>
 .talent-node.placeholder{opacity:.38}.talent-node.locked{opacity:.9}.tn-desc{font-size:12px}.tn-actions{flex-wrap:wrap}.talent-node{height:auto;min-height:144px}.ph-sub{letter-spacing:.06em}
@media(max-width:800px){.pillars-grid{grid-template-columns:1fr}}
</style>
