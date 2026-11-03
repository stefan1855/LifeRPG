<script setup>
const saving = ref(false);
import { computed, onMounted, ref } from 'vue';
import { Check, Maximize2, Minus, Pin, Plus, RefreshCw } from 'lucide-vue-next';
import ExpBar from '../components/ExpBar.vue';
import ModalShell from '../components/ModalShell.vue';
import { api, ATTRIBUTE_META, QUADRANTS, attrMeta, quadrantMeta } from '../api';
import { notify, showLevelUp } from '../stores/toast';
import { sfx } from '../utils/audio';

const emit = defineEmits(['mode']);

const snap = ref(null);
const tasks = ref([]);
const loading = ref(true);
const isPinned = ref(false);

const showQuick = ref(false);
const quickText = ref('');

async function load() {
  loading.value = true;
  try {
    const [d, t, st] = await Promise.all([
      api.dashboard(),
      api.tasks(null, null),
      api.appState().catch(() => null),
    ]);
    snap.value = d;
    tasks.value = t.filter((x) => x.status !== 'Completed').sort((a,b) => (a.dueAt ? new Date(a.dueAt).getTime() : Infinity) - (b.dueAt ? new Date(b.dueAt).getTime() : Infinity)).slice(0, 5);
    if (st && typeof st.isAlwaysOnTop === 'boolean') {
      isPinned.value = st.isAlwaysOnTop;
    }
  } catch (e) {
    notify.error('加载失败：' + e.message);
  } finally {
    loading.value = false;
  }
}

async function handleTogglePin() {
  try {
    const next = !isPinned.value;
    await api.togglePin(next);
    isPinned.value = next;
    notify.info(next ? '挂件已置顶' : '已取消置顶');
  } catch (e) {
    notify.error('置顶切换失败：' + e.message);
  }
}

const level = computed(() => snap.value?.levelInfo || {});
const character = computed(() => snap.value?.character || {});
const attrs = computed(() => (snap.value?.attributes || []).slice(0, 6));
const today = computed(() => ({
  done: snap.value?.todayCompletedCount || 0,
  total: snap.value?.todayTotalCount || 0,
}));

async function complete(t) {
  try {
    const res = await api.completeTask(t.id);
    if (res.applied && !res.isDuplicate) {
      sfx.hammer();
      notify.success(`+${res.totalExperienceGranted} EXP`);
      if (res.leveledUp) showLevelUp(res.newLevel);
    }
    await load();
  } catch (e) {
    notify.error('结算失败：' + e.message);
  }
}

async function quickAdd() {
  const title = quickText.value.trim();
  if (!title) return;
  if (saving.value) return;
  saving.value = true;
  try {
    await api.createTask({
      title,
      content: '',
      category: '默认',
      quadrant: 'Q2',
      growthAttribute: 'mind',
      experience: 25,
      dueAt: null,
    });
    quickText.value = '';
    showQuick.value = false;
    notify.success('已加入契约');
    await load();
  } catch (e) {
    notify.error('添加失败：' + e.message);
  } finally { saving.value = false; }
}

onMounted(load);
</script>

<template>
  <div class="widget">
    <span class="w-corner wc-tl"></span>
    <span class="w-corner wc-tr"></span>
    <span class="w-corner wc-bl"></span>
    <span class="w-corner wc-br"></span>

    <!-- 头部 -->
    <header class="w-head" data-tauri-drag-region>
      <img src="../assets/textures/orn_skull.webp" class="w-skull" alt="" />
      <div class="w-title" data-tauri-drag-region>
        <span class="w-name">{{ character.name || '人生主角' }}</span>
        <span class="w-lv num">Lv.{{ level.level || 1 }}</span>
      </div>
      <div class="w-head-line"></div>
      <div class="w-actions">
        <button class="w-btn" title="刷新" @click="load"><RefreshCw :size="11" /></button>
        <button class="w-btn" title="胶囊" @click="emit('mode', 'mini')"><Minus :size="11" /></button>
        <button class="w-btn" title="展开大窗口" @click="emit('mode', 'main')"><Maximize2 :size="11" /></button>
      </div>
    </header>

    <!-- 经验 -->
    <div class="w-exp">
      <ExpBar
        :percent="level.progressPercent || 0"
        :value="`${(level.progressPercent || 0).toFixed(0)}%`"
        :height="18"
        :show-shine="false"
      />
      <div class="w-exp-foot">
        <span>{{ level.experienceIntoCurrentLevel || 0 }} / {{ (level.experienceRequiredForNextLevel - level.experienceRequiredForCurrentLevel) || 100 }} EXP</span>
        <span>今日完成 {{ today.done }}/{{ today.total }}</span>
      </div>
    </div>

    <!-- 六维 -->
    <div class="w-attrs">
      <div v-for="a in attrs" :key="a.id" class="w-attr">
        <span class="wa-sigil"></span>
        <span class="wa-name">{{ a.displayName }}</span>
        <span class="wa-bar">
          <i :style="{ width: (a.progressPercent || 0) + '%' }"></i>
        </span>
        <span class="wa-val num">{{ Math.round((a.progressPercent || 0)) }}%</span>
        <span class="wa-lv num">Lv.{{ a.level }}</span>
      </div>
    </div>

    <!-- 待办 -->
    <div class="w-tasks">
      <div class="w-sec-head">
        <span>待办契约</span>
        <div class="w-sec-line"></div>
        <button class="w-add" title="快速记录" @click="showQuick = true"><Plus :size="11" /></button>
      </div>
      <div class="w-list">
        <div v-if="!tasks.length" class="w-empty">暂无待办</div>
        <div v-for="t in tasks" :key="t.id" class="w-task">
          <button class="w-check" @click="complete(t)"><Check :size="9" /></button>
          <div class="w-task-body">
            <span class="w-task-title">{{ t.title }}</span>
            <span class="w-task-sub">{{ t.category || '契约' }} · {{ t.quadrant || 'Q2' }}</span>
          </div>
          <span class="w-task-exp num">+{{ t.experience }} EXP</span>
        </div>
      </div>
    </div>

    <!-- 底部数据条：把下半区空白收成信息 -->
    <div class="w-stats">
      <div class="w-stat">
        <span class="ws-num num">{{ today.done }}/{{ today.total }}</span>
        <span class="ws-label">今日</span>
      </div>
      <div class="w-stat">
        <span class="ws-num num">{{ character.totalExperience || 0 }}</span>
        <span class="ws-label">总经验</span>
      </div>
      <div class="w-stat">
        <span class="ws-num num">{{ attrs.length }}</span>
        <span class="ws-label">属性</span>
      </div>
    </div>

    <!-- 底部：把空白区收成功能条 -->
    <footer class="w-foot">
      <button class="w-foot-btn" @click="showQuick = true"><Plus :size="11" /> 记一笔</button>
      <button class="w-foot-btn" :class="{ active: isPinned }" @click="handleTogglePin">
        <Pin :size="11" /> {{ isPinned ? '已置顶' : '置顶' }}
      </button>
    </footer>

    <ModalShell
      v-if="showQuick"
      title="快速记录"
      subtitle="QUICK CAPTURE"
      :width="420"
      @close="showQuick = false"
    >
      <input
        v-model="quickText"
        class="input"
        placeholder="要做什么？回车即可加入"
        @keyup.enter="quickAdd"
      />
      <template #footer="{ close }">
        <button class="btn btn-ghost" @click="close">取消</button>
        <button class="btn btn-primary" @click="quickAdd">加入契约</button>
      </template>
    </ModalShell>
  </div>
</template>

<style scoped>
.widget {
  position: relative;
  display: flex;
  flex-direction: column;
  height: 100%;
  gap: 10px;
  padding: 12px 14px 14px;
  background:
    url('../assets/textures/mat_metal_iron.webp') center / 320px,
    radial-gradient(ellipse 120% 90% at 50% 0%, rgba(28, 42, 20, 0.75), transparent 70%),
    linear-gradient(180deg, rgba(12, 18, 11, 0.98), rgba(5, 8, 5, 0.99));
  background-blend-mode: soft-light, normal, normal;
  border: 1px solid rgba(109, 91, 42, 0.55);
  box-shadow: var(--sh-panel);
  overflow: hidden;
}
.widget::before {
  content: '';
  position: absolute;
  inset: 0;
  background: url('../assets/textures/mat_stone.webp') center / 480px;
  opacity: 0.10;
  mix-blend-mode: soft-light;
  pointer-events: none;
  z-index: 0;
}
.widget > * {
  position: relative;
  z-index: 1;
}
.widget::after {
  content: '';
  position: absolute;
  inset: 0;
  border: 10px solid transparent;
  /* 挂件专用小尺寸边框：72px 源图 + 18px 切片，避免通用 120px 图被压扁 */
  border-image: url('../assets/reference/panel-frame-clean.webp') 180 / 18px stretch;
  pointer-events: none;
  z-index: 2;
}

.w-corner {
  position: absolute;
  width: 20px;
  height: 20px;
  pointer-events: none;
  opacity: 0.95;
  background: url('../assets/textures/orn_corner.webp') no-repeat top left / contain;
  filter: drop-shadow(0 0 4px rgba(124, 255, 31, 0.25));
  z-index: 3;
}
.wc-tl { top: -1px; left: -1px; }
.wc-tr { top: -1px; right: -1px; transform: scaleX(-1); }
.wc-bl { bottom: -1px; left: -1px; transform: scaleY(-1); }
.wc-br { bottom: -1px; right: -1px; transform: scale(-1, -1); }

.w-head {
  display: flex;
  align-items: center;
  gap: 8px;
  padding-bottom: 6px;
  border-bottom: 1px solid rgba(109, 91, 42, 0.35);
  position: relative;
  z-index: 1;
}
.w-head-line {
  flex: 1;
  height: 1px;
  background: linear-gradient(90deg, rgba(192, 166, 76, 0.45), transparent);
}
.w-sec-line {
  flex: 1;
  height: 1px;
  background: linear-gradient(90deg, rgba(192, 166, 76, 0.4), transparent);
  margin: 0 8px;
}
.w-skull {
  width: 16px;
  height: 16px;
  object-fit: contain;
  filter: drop-shadow(0 0 6px rgba(124, 255, 31, 0.6));
}
.w-title { flex: 1; display: flex; align-items: baseline; gap: 6px; min-width: 0; }
.w-name {
  font-family: var(--f-title);
  font-size: 13.5px;
  letter-spacing: 0.1em;
  color: #cfea78;
  text-shadow: 0 0 10px rgba(207, 234, 120, 0.4);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.w-lv { font-size: 12px; color: var(--c-soul); text-shadow: 0 0 8px rgba(124, 255, 31, 0.6); }
.w-actions { display: flex; gap: 4px; }
.w-btn {
  width: 22px; height: 22px;
  display: flex; align-items: center; justify-content: center;
  color: #c0a64c;
  background:
    url('../assets/textures/btn_plate.webp') center / 100% 100%,
    linear-gradient(180deg, rgba(22, 28, 16, 0.95), rgba(8, 11, 6, 0.98));
  border: 1px solid rgba(192, 166, 76, 0.55);
  box-shadow: inset 0 1px 0 rgba(255, 235, 160, 0.2);
  cursor: pointer;
}
.w-btn:hover { color: #fff; border-color: var(--c-soul); box-shadow: 0 0 8px rgba(124, 255, 31, 0.4); }

.w-exp { display: flex; flex-direction: column; gap: 3px; }
.w-exp-foot {
  display: flex;
  justify-content: space-between;
  font-size: 12px;
  color: var(--t-time);
  font-family: var(--f-mono);
}

.w-attrs {
  display: flex;
  flex-direction: column;
  gap: 5px;
  padding: 9px 11px;
  background:
    url('../assets/textures/mat_stone.webp') center / 280px,
    linear-gradient(180deg, rgba(14, 20, 12, 0.95), rgba(6, 9, 5, 0.98));
  background-blend-mode: soft-light, normal;
  border: 1px solid #4a3c1b;
  box-shadow: inset 1px 1px 0 rgba(192, 166, 76, 0.35), inset -1px -1px 0 rgba(0, 0, 0, 0.9), 0 3px 8px rgba(0, 0, 0, 0.7);
}
.w-attr { display: flex; align-items: center; gap: 8px; font-size: 12px; }
.wa-sigil {
  width: 7px;
  height: 7px;
  flex: 0 0 auto;
  transform: rotate(45deg);
  background: var(--c-soul);
  box-shadow: 0 0 6px var(--c-soul), 0 0 2px #fff;
}
.wa-name { width: 34px; color: #d6d0b6; font-weight: 600; font-size: 12px; }
.wa-val { width: 36px; text-align: right; font-size: 12px; color: #a08e63; font-family: var(--f-mono); }
.wa-bar {
  flex: 1;
  height: 13px;
  background: url('../assets/textures/bar_trough.webp') repeat-x center / auto 100%;
  border: 1px solid #2b2412;
  overflow: hidden;
  box-shadow:
    inset 0 2px 5px rgba(0, 0, 0, 0.95),
    0 1px 0 rgba(192, 166, 76, 0.25);
}
.wa-bar i {
  display: block;
  height: 100%;
  min-width: 4px;
  background: url('../assets/textures/bar_fill.webp') repeat-x center / auto 100%;
  transition: width 0.5s ease;
  box-shadow: 0 0 10px rgba(124, 255, 31, 0.75), inset 0 1px 0 rgba(255, 255, 255, 0.65);
}
.wa-lv { width: 18px; text-align: right; color: var(--c-glow); font-size: 12px; font-weight: 700; text-shadow: 0 0 6px rgba(124, 255, 31, 0.6); }

/* 底部数据条 */
.w-stats {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 6px;
  flex: 0 0 auto;
}
.w-stat {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1px;
  padding: 6px 3px;
  background:
    url('../assets/textures/mat_metal_iron.webp') center / 200px,
    linear-gradient(180deg, rgba(22, 28, 16, 0.92), rgba(8, 11, 6, 0.96));
  background-blend-mode: soft-light, normal;
  border: 1px solid #4a3c1b;
  box-shadow: inset 1px 1px 0 rgba(192, 166, 76, 0.3), inset -1px -1px 0 rgba(0, 0, 0, 0.9), 0 2px 6px rgba(0, 0, 0, 0.7);
}
.ws-num {
  font-size: 14px;
  font-weight: 700;
  color: var(--c-glow);
  text-shadow: 0 0 10px rgba(124, 255, 31, 0.45);
}
.ws-label {
  font-size: 12px;
  letter-spacing: 0.16em;
  color: #b5a982;
}

.w-tasks { display: flex; flex-direction: column; gap: 6px; }
.w-sec-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.16em;
  color: #cfea78;
  text-shadow: 0 0 10px rgba(207, 234, 120, 0.35);
}
.w-add {
  width: 20px; height: 20px;
  display: flex; align-items: center; justify-content: center;
  color: #dcd6be;
  background: rgba(0, 0, 0, 0.4);
  border: 1px solid rgba(192, 166, 76, 0.55);
  cursor: pointer;
}
.w-add:hover { color: var(--c-soul); border-color: var(--c-soul); box-shadow: 0 0 8px rgba(124, 255, 31, 0.4); }
.w-list {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding-bottom: 2px;
}
/* 挂件里用极细暗轨，避免原生滚动条破坏沉浸感 */
.w-list::-webkit-scrollbar { width: 0; }
.w-list { scrollbar-width: none; }
.w-empty { padding: 18px 0; text-align: center; font-size: 12px; color: var(--t-disabled); }
.w-task {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 12px;
  background:
    url('../assets/textures/mat_metal_iron.webp') center / 220px,
    linear-gradient(180deg, rgba(26, 34, 20, 0.95), rgba(10, 14, 8, 0.98));
  background-blend-mode: soft-light, normal;
  border: 1px solid #4a3c1b;
  box-shadow: inset 1px 1px 0 rgba(192, 166, 76, 0.3), inset -1px -1px 0 rgba(0, 0, 0, 0.85), 0 3px 6px rgba(0, 0, 0, 0.7);
  min-height: 44px;
}
.w-task:hover {
  border-color: rgba(192, 166, 76, 0.75);
  box-shadow: inset 1px 1px 0 rgba(255, 235, 160, 0.25), 0 0 10px rgba(124, 255, 31, 0.2);
}
.w-check {
  width: 15px; height: 15px;
  flex: 0 0 auto;
  display: flex; align-items: center; justify-content: center;
  color: transparent;
  background: radial-gradient(circle at 40% 30%, rgba(60, 52, 24, 0.9), rgba(8, 8, 4, 0.95));
  border: 1px solid rgba(192, 166, 76, 0.85);
  transform: rotate(45deg);
  box-shadow: inset 0 0 5px rgba(0, 0, 0, 0.8);
  cursor: pointer;
}
.w-check > svg { transform: rotate(-45deg); }
.w-check:hover {
  color: #05120a;
  background: var(--c-soul);
  border-color: #d4ff6b;
  box-shadow: 0 0 10px rgba(124, 255, 31, 0.8);
}
.w-task-body {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}
.w-task-title {
  color: #e2dbbe;
  font-size: 12px;
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.w-task-sub {
  font-size: 12px;
  color: #9e916a;
  letter-spacing: 0.04em;
}
.w-task-exp {
  font-size: 12px;
  font-weight: 700;
  color: #c0a64c;
  text-shadow: 0 0 6px rgba(192, 166, 76, 0.4);
}

.w-foot {
  display: flex;
  gap: 8px;
  padding-top: 8px;
  border-top: 1px solid rgba(192, 166, 76, 0.35);
}
.w-foot-btn {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 5px;
  height: 30px;
  font-size: 12px;
  font-weight: 600;
  color: #cfea78;
  background:
    url('../assets/textures/btn_plate.webp') center / 100% 100%,
    linear-gradient(180deg, rgba(32, 44, 20, 0.95), rgba(12, 16, 9, 0.98));
  border: 1px solid rgba(192, 166, 76, 0.65);
  box-shadow: inset 0 1px 0 rgba(255, 235, 160, 0.25), 0 2px 6px rgba(0, 0, 0, 0.7);
  cursor: pointer;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.8);
}
.w-foot-btn:hover {
  color: #fff;
  border-color: var(--c-soul);
  box-shadow: inset 0 1px 0 rgba(255, 235, 160, 0.35), 0 0 10px rgba(124, 255, 31, 0.35);
}
.w-foot-btn.active {
  color: var(--c-glow);
  border-color: rgba(124, 255, 31, 0.75);
  background:
    url('../assets/textures/btn_plate.webp') center / 100% 100%,
    radial-gradient(circle at 50% 50%, rgba(35, 55, 24, 0.95), rgba(12, 18, 9, 0.98));
  box-shadow: 0 0 12px rgba(124, 255, 31, 0.35);
  text-shadow: 0 0 8px rgba(124, 255, 31, 0.6);
}
.w-foot-btn.primary {
  color: var(--c-glow);
  border-color: rgba(124, 255, 31, 0.5);
  box-shadow: inset 0 0 12px rgba(124, 255, 31, 0.12);
  text-shadow: 0 0 8px rgba(124, 255, 31, 0.5);
}
</style>

<style scoped>
 .w-tasks{flex:1;min-height:0}.w-list{overflow-y:auto;min-height:0}.w-head,.w-exp,.w-attrs,.w-stats,.w-foot{flex-shrink:0}.w-check{width:26px;height:26px}.wa-bar i{min-width:0}.w-name{font-size:14px}
</style>

<style scoped>

.w-exp-foot{flex-wrap:wrap}.w-foot-btn{min-width:0}.w-actions{flex-shrink:0}
@media(max-height:480px){.widget{gap:6px;padding:10px}.w-stats{display:none}}
@media(max-height:380px){.w-attrs{display:none}}
@media(max-width:300px){.w-head-line{display:none}.w-actions{gap:3px}.w-name{font-size:12px;letter-spacing:0}}
@media(max-width:230px){.w-skull{display:none}.w-head{gap:4px}}

</style>
