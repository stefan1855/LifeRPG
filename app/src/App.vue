<script setup>
import { computed, onMounted, ref } from 'vue';
import { getCurrentWindow } from '@tauri-apps/api/window';
import {
  Skull, Swords, ScrollText, Sparkles, Image as ImageIcon, Archive, Settings as SettingsIcon,
  Pin, Minus, Square, X, PanelTopClose, Shield, Zap, Globe,
} from 'lucide-vue-next';

import ToastHost from './components/ToastHost.vue';
import SetupView from './views/SetupView.vue';
import WidgetView from './views/WidgetView.vue';
import MiniBar from './views/MiniBar.vue';
import DashboardView from './views/DashboardView.vue';
import TasksView from './views/TasksView.vue';
import RecordsView from './views/RecordsView.vue';
import AttributesView from './views/AttributesView.vue';
import EquipmentView from './views/EquipmentView.vue';
import SkillsView from './views/SkillsView.vue';
import WorldView from './views/WorldView.vue';
import ImagesView from './views/ImagesView.vue';
import ArchiveView from './views/ArchiveView.vue';
import SettingsView from './views/SettingsView.vue';
import { api } from './api';
import { notify } from './stores/toast';

const NAV = [
  { key: 'dashboard', label: '角色仪表盘', icon: Skull, comp: DashboardView },
  { key: 'tasks', label: '行动任务', icon: Swords, comp: TasksView },
  { key: 'records', label: '行为记录', icon: ScrollText, comp: RecordsView },
  { key: 'attributes', label: '成长属性', icon: Sparkles, comp: AttributesView },
  { key: 'equipment', label: '遗物具装', icon: Shield, comp: EquipmentView },
  { key: 'skills', label: '天赋神殿', icon: Zap, comp: SkillsView },
  { key: 'world', label: '尘世投影', icon: Globe, comp: WorldView },
  { key: 'images', label: '真容画卷', icon: ImageIcon, comp: ImagesView },
  { key: 'archive', label: '复盘归档', icon: Archive, comp: ArchiveView },
  { key: 'settings', label: '封印核心', icon: SettingsIcon, comp: SettingsView },
];

const active = ref('dashboard');
const pinned = ref(false),navExpanded=ref(false);
async function toggleNav(){const previous=navExpanded.value;navExpanded.value=!previous;try{await api.configSave({navExpanded:navExpanded.value});}catch(e){navExpanded.value=previous;notify.error(e.message);}}
const current = computed(() => NAV.find((n) => n.key === active.value) || NAV[0]);
const win = getCurrentWindow();
function navigate(key) { if (NAV.some(item => item.key === key)) active.value = key; }

/** 首次启动引导：没有数据目录就先不让进主界面 */
const booting = ref(true);
const ready = ref(false);
/** main | widget | mini */
const viewMode = ref('main');

async function checkState() {
  try {
    const [st, cfg] = await Promise.all([api.appState(), api.configGet()]);
    ready.value = !!st?.dataDir;
    navExpanded.value=cfg?.navExpanded===true;
    viewMode.value = cfg?.viewMode || 'main';
    pinned.value = !!(st?.isAlwaysOnTop ?? cfg?.isAlwaysOnTop);
    const op = Math.max(60, Math.min(100, Number(cfg?.opacity) || 97));
    document.documentElement.style.setProperty('--app-opacity', (op / 100).toFixed(2));
    if (ready.value) {
      // 让后端窗口尺寸与前端形态保持一致
      await api.setViewMode(viewMode.value).catch(() => {});
    }
  } catch (e) {
    notify.error('读取配置失败：' + e.message);
    ready.value = false;
  } finally {
    booting.value = false;
  }
}

/** 三档形态切换：大窗口 / 挂件 / 胶囊 */
async function switchMode(mode) {
  const previous = viewMode.value;
  viewMode.value = mode;
  try {
    await api.setViewMode(mode);
  } catch (e) {
    viewMode.value = previous;
    notify.error('切换形态失败：' + e.message);
  }
}

function onSetupReady() {
  checkState();
}

onMounted(() => {
  window.__onHostViewMode = (mode) => {
    if (mode && ['main', 'widget', 'mini'].includes(mode)) {
      viewMode.value = mode;
    }
  };
  checkState();
});

async function minimize() { await win.minimize(); }
async function toggleMax() { await win.toggleMaximize(); }
async function closeApp() { await win.hide(); }

async function togglePin() {
  pinned.value = !pinned.value;
  try {
    await api.togglePin(pinned.value);
    notify.info(pinned.value ? '已置顶' : '已取消置顶');
  } catch (e) {
    notify.error('置顶失败：' + e.message);
  }
}
</script>

<template>
  <div class="app-root">
    <!-- 启动中 -->
    <div v-if="booting" class="boot-splash">
      <div class="app-bg"></div>
      <span class="boot-sigil">☠</span>
    </div>

    <!-- 首次启动：数据目录引导 -->
    <SetupView v-else-if="!ready" @ready="onSetupReady" />

    <!-- 主界面 -->
    <template v-else>
      <div v-if="viewMode === 'main'" class="app-bg" :class="{ 'dashboard-background': active === 'dashboard' }"></div>
      <div v-if="viewMode === 'main'" class="app-mist"></div>

      <!-- 大窗口 -->
      <div v-if="viewMode === 'main'" class="shell" :class="{ immersive: active === 'dashboard' }">
      <!-- 自定义标题栏 -->
      <header class="titlebar" data-tauri-drag-region>
        <div class="brand" data-tauri-drag-region>
          <span class="sigil">☠</span>
          <span class="name">LIFE<span class="dot">·</span>RPG</span>
          <span class="tag">绿火毁灭术</span>
        </div>
        <div class="drag-fill" data-tauri-drag-region><span class="page-caption" data-tauri-drag-region>✧ {{ current.label }} ✧</span></div>
        <div class="win-actions">
          <button class="gothic-wbtn" title="收成桌面挂件" @click="switchMode('widget')">
            <PanelTopClose :size="12" />
          </button>
          <button class="gothic-wbtn" :class="{ on: pinned }" title="置顶" @click="togglePin">
            <Pin :size="12" />
          </button>
          <button class="gothic-wbtn" title="最小化" @click="minimize"><Minus :size="12" /></button>
          <button class="gothic-wbtn" title="最大化" @click="toggleMax"><Square :size="10" /></button>
          <button class="gothic-wbtn danger" title="隐藏到托盘" @click="closeApp"><X :size="12" /></button>
        </div>
      </header>

      <div class="body">
        <!-- 左侧导航 -->
        <nav class="sidebar" :class="{expanded:navExpanded}"><button class="nav-toggle" :aria-label="navExpanded?'收起导航':'展开导航'" :aria-expanded="navExpanded" @click="toggleNav">{{ navExpanded?'‹ 收起导航':'☰' }}</button>
          <div class="nav-list">
            <button
              v-for="item in NAV"
              :key="item.key"
              :title="item.label"
              :aria-label="item.label"
              class="nav-item"
              :class="{ active: active === item.key }"
              @click="active = item.key"
            >
              <component :is="item.icon" :size="20" class="ico" />
              <span>{{ item.label }}</span>
              <i class="glowbar"></i>
            </button>
          </div>
          <div class="sidebar-foot">
            <div class="rune"></div>
            <span>SOUL ARCHIVE</span>
          </div>
        </nav>

        <!-- 内容区 -->
        <main class="content">
          <transition name="view" mode="out-in">
            <component :is="current.comp" :key="current.key" @navigate="navigate" />
          </transition>
        </main>
      </div>
      </div>

      <!-- 挂件形态 -->
      <WidgetView v-else-if="viewMode === 'widget'" @mode="switchMode" />

      <!-- 胶囊形态 -->
      <MiniBar v-else @mode="switchMode" />
    </template>

    <ToastHost v-if="viewMode !== 'mini' || !ready" />
  </div>
</template>

<style scoped>
.boot-splash {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
}
.boot-sigil {
  position: relative;
  z-index: 1;
  font-size: 34px;
  color: var(--c-glow);
  text-shadow: var(--glow-soul-strong);
  animation: soulPulse 1.8s ease-in-out infinite;
}
.app-root {
  position: relative;
  height: 100%;
  overflow: hidden;
  opacity: var(--app-opacity, 1);
  transition: opacity 0.16s ease;
}
.shell {
  position: relative;
  z-index: 1;
  display: flex;
  flex-direction: column;
  height: 100%;
}

/* ---- 标题栏 ---- */
.titlebar {
  display: flex;
  align-items: center;
  height: var(--topbar-h);
  padding: 0 12px;
  border-bottom: 1px solid rgba(109, 91, 42, 0.45);
  background: linear-gradient(180deg, rgba(12, 18, 10, 0.96), rgba(5, 8, 5, 0.9));
  flex: 0 0 auto;
}
.brand {
  display: flex;
  align-items: center;
  gap: 8px;
}
.sigil {
  color: var(--c-gold-bright);
  font-size: 15px;
  text-shadow: var(--glow-gold);
}
.name {
  font-family: var(--f-title);
  font-size: 16px;
  font-weight: 900;
  letter-spacing: 0.16em;
  color: var(--t-title);
  text-shadow: 0 0 14px rgba(124, 255, 31, 0.3);
}
.dot {
  color: var(--c-soul);
}
.tag {
  margin-left: 6px;
  padding: 1px 7px;
  font-size: 12px;
  letter-spacing: 0.12em;
  color: var(--t-sub);
  border: 1px solid rgba(109, 91, 42, 0.6);
}
.drag-fill {
  flex: 1;
  height: 100%;
}
.win-actions {
  display: flex;
  align-items: center;
  gap: 10px;
  padding-right: 6px;
}
.gothic-wbtn.on {
  color: var(--c-soul);
  border-color: var(--c-soul);
  box-shadow: 0 0 10px rgba(124, 255, 31, 0.45);
}
.gothic-wbtn.danger:hover {
  color: #ffd9d3;
  border-color: var(--t-danger);
  box-shadow: 0 0 10px rgba(224, 90, 74, 0.45);
}

/* ---- 主体 ---- */
.body {
  display: flex;
  flex: 1;
  min-height: 0;
}

.sidebar {
  width: var(--nav-w);
  flex: 0 0 auto;
  display: flex;
  flex-direction: column;
  border-right: 1px solid rgba(109, 91, 42, 0.45);
  background:
    url('./assets/textures/mat_metal_iron.webp') center / 300px,
    linear-gradient(180deg, rgba(9, 14, 9, 0.94), rgba(4, 6, 4, 0.97));
  background-blend-mode: soft-light, normal;
  box-shadow: inset -12px 0 24px rgba(0, 0, 0, 0.6);
}
.nav-list {
  flex: 1;
  padding: 14px 10px;
  display: flex;
  flex-direction: column;
  gap: 5px;
}
.nav-item {
  position: relative;
  display: flex;
  align-items: center;
  gap: 10px;
  height: 40px;
  padding: 0 12px;
  font-size: 13px;
  color: var(--t-sub);
  background: linear-gradient(180deg, rgba(26, 22, 10, 0.6), rgba(8, 10, 6, 0.7));
  border: 1px solid rgba(109, 91, 42, 0.28);
  cursor: pointer;
  transition: all 0.16s;
  text-align: left;
  box-shadow: inset 0 1px 0 rgba(192, 166, 76, 0.1);
}
.nav-item .ico {
  opacity: 0.75;
}
.nav-item:hover {
  color: var(--t-panel);
  background: rgba(124, 255, 31, 0.04);
  border-color: rgba(109, 91, 42, 0.45);
}
.nav-item.active {
  color: var(--c-glow);
  background: linear-gradient(90deg, rgba(124, 255, 31, 0.1), rgba(124, 255, 31, 0.01) 70%);
  border-color: rgba(124, 255, 31, 0.22);
  text-shadow: 0 0 12px rgba(124, 255, 31, 0.45);
}
.nav-item.active .ico {
  opacity: 1;
  filter: drop-shadow(0 0 6px rgba(124, 255, 31, 0.7));
}
.glowbar {
  position: absolute;
  left: -1px;
  top: 6px;
  bottom: 6px;
  width: 2px;
  background: transparent;
}
.nav-item.active .glowbar {
  background: var(--c-soul);
  box-shadow: var(--glow-soul);
}
.sidebar-foot {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px 16px;
  border-top: 1px solid rgba(109, 91, 42, 0.28);
  font-size: 12px;
  letter-spacing: 0.22em;
  color: var(--t-disabled);
}
.rune {
  width: 8px;
  height: 8px;
  border: 1px solid var(--c-gold);
  transform: rotate(45deg);
  animation: soulPulse 3s ease-in-out infinite;
}

.content {
  flex: 1;
  min-width: 0;
  padding: 16px;
  overflow: hidden;
  display: flex;
}

/* ---- 视图切换动画 ---- */
.view-enter-active,
.view-leave-active {
  transition: opacity 0.18s ease, transform 0.18s ease;
}
.view-enter-from {
  opacity: 0;
  transform: translateY(6px);
}
.view-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>




<style scoped>
@media(max-width:900px){
  .sidebar{width:64px}.nav-list{padding:12px 8px}.nav-item{justify-content:center;padding:0}
  .nav-item>span,.sidebar-foot,.tag{display:none}.content{padding:10px}
}
</style>

<style scoped>

.nav-list{min-height:0;overflow-y:auto}.nav-item{flex-shrink:0}
.dashboard-background{opacity:.075}
.sidebar{width:64px}.nav-list{padding:12px 8px;gap:7px}.nav-item{justify-content:center;padding:0;height:43px}.nav-item>span,.sidebar-foot,.tag{display:none}.drag-fill{display:flex;align-items:center;justify-content:center;min-width:0}.page-caption{font:600 23px var(--f-title);letter-spacing:.16em;color:#d8e995;text-shadow:0 0 16px #a0c53d35;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}.immersive .content{padding:12px}.immersive .titlebar{background:radial-gradient(ellipse at 50% 0,#26341588,transparent 70%),linear-gradient(#090e08,#030603)}.immersive::after{content:'';position:absolute;inset:0;pointer-events:none;border:1px solid #706032;box-shadow:inset 0 0 0 3px #040704,inset 0 0 0 4px #36321b;z-index:5}@media(max-width:900px){.page-caption{font-size:17px;letter-spacing:.04em}.name{font-size:12px}.win-actions{gap:5px}.immersive .content{padding:9px}}

</style>

<style scoped>
.nav-toggle{flex-shrink:0;min-height:36px;margin:8px 8px 0;background:#10170c;border:1px solid #66542b;color:#cbbb80;cursor:pointer;font:13px var(--f-body)}.sidebar.expanded{width:200px}.sidebar.expanded .nav-item{justify-content:flex-start;padding:0 12px}.sidebar.expanded .nav-item>span{display:inline}.sidebar.expanded .nav-list{padding:10px}.sidebar.expanded .nav-item{gap:10px}.sidebar.expanded .nav-toggle{text-align:left;padding-left:12px}
</style>
