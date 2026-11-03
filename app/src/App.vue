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
        <div class="drag-fill" data-tauri-drag-region><span class="page-caption" data-tauri-drag-region>{{ current.label }}</span></div>
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
.app-root{position:relative;height:100%;overflow:hidden;opacity:var(--app-opacity,1)}.boot-splash{position:relative;display:grid;place-items:center;height:100%}.boot-sigil{z-index:1;font-size:34px;color:var(--c-glow)}.shell{position:relative;z-index:1;display:flex;flex-direction:column;height:100%;padding:0 22px 19px 35px;isolation:isolate}.shell::after{content:'';position:absolute;inset:0;pointer-events:none;border:0 solid transparent;border-image:url('./assets/reference/outer-chrome-frame.webp') 150 250 130 / 77px 125px 65px stretch;z-index:5}.titlebar{position:relative;display:flex;align-items:center;height:var(--topbar-h);flex:none;padding:0 56px 6px;z-index:6}.brand{display:flex;align-items:center;gap:6px;width:155px;flex:none}.sigil{font-size:15px;color:#aa9e54}.name{font:600 13px var(--f-latin);letter-spacing:.1em;color:#b3ba79}.dot{color:#9cc64e}.tag{display:none}.drag-fill{display:flex;align-items:center;justify-content:center;flex:1;min-width:0;height:100%}.page-caption{font:700 clamp(25px,2.65vw,41px)/1.3 var(--f-title);letter-spacing:.09em;color:#cfea78;text-shadow:0 0 10px #a8ff4a20;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}.win-actions{display:flex;align-items:center;gap:9px;width:155px;justify-content:flex-end;flex:none}.gothic-wbtn{width:24px;height:24px}.gothic-wbtn.on{color:#c6f284;border-color:#8cb34c}.gothic-wbtn.danger:hover{color:#f1a88f;border-color:#b97150}.body{display:flex;flex:1;min-height:0;position:relative;z-index:2;gap:8px}.sidebar{width:50px;flex:none;display:flex;flex-direction:column;padding-top:4px;border-right:1px solid #5b4e293d;background:#050a0575;min-height:0}.nav-toggle{flex:none;min-height:31px;margin:0 4px 7px;background:#111709;border:1px solid #62542c;color:#b8ac75;cursor:pointer;font:13px var(--f-body)}.nav-list{display:flex;flex-direction:column;gap:7px;flex:1;min-height:0;overflow-y:auto;padding:0 4px 12px}.nav-item{position:relative;display:flex;align-items:center;justify-content:center;gap:10px;height:41px;flex:none;padding:0;color:#a99a6e;background:#080d07;border:1px solid #4e422655;cursor:pointer}.nav-item>span{display:none}.nav-item:hover{color:#d3d996;border-color:#9b914a}.nav-item.active{color:#d0ec91;border-color:#7d993f;background:linear-gradient(90deg,#81a92c22,transparent)}.nav-item.active .ico{filter:drop-shadow(0 0 4px #91cd3b50)}.glowbar{position:absolute;left:0;top:7px;bottom:7px;width:2px}.nav-item.active .glowbar{background:#b8e16a}.sidebar.expanded{width:185px}.sidebar.expanded .nav-item{justify-content:flex-start;padding:0 10px;font:14px var(--f-body)}.sidebar.expanded .nav-item>span{display:inline}.sidebar.expanded .nav-toggle{text-align:left;padding-left:10px}.sidebar-foot{display:none}.content{display:flex;flex:1;min-width:0;overflow:hidden;padding:4px 3px 8px}.view-enter-active,.view-leave-active{transition:opacity .2s ease}.view-enter-from,.view-leave-to{opacity:0}
@media(max-width:1100px){.shell{padding:0 13px 16px}.shell::after{border-image-width:65px 85px 45px}.titlebar{padding:0 15px 8px;height:66px}.brand{width:100px}.name{font-size:11px}.win-actions{width:140px;gap:6px}.page-caption{font-size:29px}.sidebar.expanded{width:170px}}
@media(max-width:760px){.shell{padding:0 9px 12px}.shell::after{border-image-width:55px 60px 35px}.titlebar{height:61px;padding:0 6px 8px}.brand{display:none}.drag-fill{justify-content:flex-start;padding-left:13px}.page-caption{font-size:25px;letter-spacing:.03em}.win-actions{width:136px;gap:5px}.body{gap:6px}.sidebar{width:43px}.sidebar.expanded{width:155px}.nav-item{height:38px}.content{padding-right:0}}
@media(prefers-reduced-motion:reduce){.view-enter-active,.view-leave-active{transition:none}}
</style>
