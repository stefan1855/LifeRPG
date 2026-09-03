<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { invoke } from '@tauri-apps/api/core';
import { listen } from '@tauri-apps/api/event';
import {
  Pin,
  PinOff,
  Minimize2,
  Maximize2,
  RefreshCw,
  X,
  ChevronDown,
  Monitor,
  LayoutTemplate,
  Pill,
} from 'lucide-vue-next';

import globalBackground from './assets/theme/global_background.png';
import DashboardView from './views/DashboardView.vue';
import TasksView from './views/TasksView.vue';
import RecordsView from './views/RecordsView.vue';
import AttributesView from './views/AttributesView.vue';
import ImagesView from './views/ImagesView.vue';
import ArchiveView from './views/ArchiveView.vue';
import SettingsView from './views/SettingsView.vue';

import CharacterHud from './components/CharacterHud.vue';
import AttributesDrawer from './components/AttributesDrawer.vue';
import TaskItem from './components/TaskItem.vue';
import MiniBar from './components/MiniBar.vue';
import AddTaskModal from './components/AddTaskModal.vue';
import EditTaskModal from './components/EditTaskModal.vue';
import SettingsModal from './components/SettingsModal.vue';
import { toDateStr, parseNaturalTask } from './utils/timeParser';

// 视图模式：'main' (1460x920 大屏指挥中心), 'widget' (340x600 桌面 HUD), 'mini' (260x44 胶囊)
const viewMode = ref('main');
const activeTab = ref('dashboard'); // 'dashboard', 'tasks', 'records', 'attributes', 'images', 'archive', 'settings'
const isViewModeMenuOpen = ref(false);

// 核心数据
const character = ref({
  level: 1,
  name: '无面者',
  total_experience: 0,
  experience_into_current_level: 0,
  experience_required_for_current_level: 0,
  experience_required_for_next_level: 100,
  progress_percent: 0,
});
const attributes = ref([]);
const tasks = ref([]);
const isPinned = ref(false);
const winOpacity = ref(0.96);
const isDrawerExpanded = ref(false);
const widgetCategoryFilter = ref('all');

// 模态弹窗
const showAddModal = ref(false);
const showEditModal = ref(false);
const showSettingsModal = ref(false);
const editingTask = ref(null);

// 灵魂能量粒子
const particles = ref([]);
let particleId = 0;

const pendingTasksCount = computed(() => {
  return (tasks.value || []).filter((t) => t.status !== 'Completed').length;
});

const filteredWidgetTasks = computed(() => {
  const list = tasks.value || [];
  if (widgetCategoryFilter.value === 'all') return list;
  if (widgetCategoryFilter.value === 'today') {
    const today = toDateStr(new Date());
    return list.filter((t) => t.due_at && t.due_at.startsWith(today));
  }
  return list.filter((t) => t.growth_attribute === widgetCategoryFilter.value);
});

// 加载全量暗影数据快照
async function fetchSnapshot() {
  try {
    const snapshot = await invoke('rpg_get_dashboard');
    if (snapshot) {
      if (snapshot.character) character.value = snapshot.character;
      if (snapshot.attributes) attributes.value = snapshot.attributes;
      if (snapshot.tasks) tasks.value = snapshot.tasks;
    }
  } catch (err) {
    console.error('获取快照失败:', err);
  }
}

// 切换视图形态（大屏指挥中心 ⇄ 桌面 HUD ⇄ 胶囊护符）
async function switchViewMode(targetMode) {
  viewMode.value = targetMode;
  isViewModeMenuOpen.value = false;
  try {
    await invoke('window_set_view_mode', { mode: targetMode });
  } catch (e) {
    console.error('切换形态失败:', e);
  }
}

// 切换置顶
async function togglePin() {
  const nextPin = !isPinned.value;
  isPinned.value = nextPin;
  try {
    await invoke('window_toggle_pin', { pin: nextPin });
  } catch (e) {
    console.error('置顶切换异常:', e);
  }
}

// 达成 / 撤回任务
async function handleToggleTask(task) {
  try {
    if (task.status === 'Completed') {
      await invoke('task_uncomplete', { id: task.id });
      await fetchSnapshot();
    } else {
      const res = await invoke('task_complete', { id: task.id });
      if (res && res.applied) {
        const pId = ++particleId;
        const mainGain = res.gains && res.gains.length > 0 ? ` (${res.gains[0].display_name || res.gains[0].attribute_name} +${res.gains[0].experience_delta || res.gains[0].experience_gained})` : '';
        particles.value.push({
          id: pId,
          text: `+${res.total_experience_granted} 灵魂能量${mainGain}`,
        });
        setTimeout(() => {
          particles.value = particles.value.filter((p) => p.id !== pId);
        }, 1400);

        if (res.leveled_up) {
          const lvId = ++particleId;
          particles.value.push({
            id: lvId,
            text: `💀 突破深渊境界 Lv.${res.new_level} 💀`,
          });
          setTimeout(() => {
            particles.value = particles.value.filter((p) => p.id !== lvId);
          }, 2500);
        }
      }
      await fetchSnapshot();
    }
  } catch (err) {
    console.error('任务状态切换失败:', err);
  }
}

// 极速创建任务
async function handleQuickCreate(text) {
  const parsed = parseNaturalTask(text);
  const payload = {
    title: parsed.cleanTitle || text,
    description: '',
    growth_attribute: parsed.growthAttribute || 'mind',
    quadrant: 'Q2',
    due_at: parsed.dueAt ? `${parsed.dueAt}T23:59:59` : null,
    experience: 25,
  };
  try {
    await invoke('task_create', { input: payload });
    await fetchSnapshot();
  } catch (err) {
    console.error('创建任务失败:', err);
  }
}

// 模态框创建任务
async function handleCreateTask(payload) {
  try {
    await invoke('task_create', { input: payload });
    showAddModal.value = false;
    await fetchSnapshot();
  } catch (err) {
    console.error('缔结契约失败:', err);
  }
}

// 编辑任务
function handleOpenEdit(task) {
  editingTask.value = task;
  showEditModal.value = true;
}

async function handleUpdateTask(payload) {
  try {
    await invoke('task_update', { input: payload });
    showEditModal.value = false;
    editingTask.value = null;
    await fetchSnapshot();
  } catch (err) {
    console.error('更新契约失败:', err);
  }
}

// 删除任务
async function handleDeleteTask(task) {
  try {
    await invoke('task_delete', { id: task.id });
    await fetchSnapshot();
  } catch (err) {
    console.error('删除契约失败:', err);
  }
}

// 退出应用
async function handleExit() {
  try {
    await invoke('app_exit');
  } catch (e) {
    console.error('退出应用失败:', e);
  }
}

function handleUpdateOpacity(val) {
  winOpacity.value = val;
  try {
    localStorage.setItem('liferpg_widget_opacity', String(val));
  } catch {}
}

function handleClickOutside(e) {
  const dropdown = document.querySelector('.view-mode-dropdown-wrapper');
  if (dropdown && !dropdown.contains(e.target)) {
    isViewModeMenuOpen.value = false;
  }
}

let unlistenShortcut = null;

onMounted(async () => {
  document.addEventListener('click', handleClickOutside);

  try {
    const savedOpacity = localStorage.getItem('liferpg_widget_opacity');
    if (savedOpacity) {
      winOpacity.value = parseFloat(savedOpacity);
    }
  } catch {}

  await fetchSnapshot();

  try {
    unlistenShortcut = await listen('global-shortcut-toggle', () => {
      if (viewMode.value === 'main') {
        switchViewMode('widget');
      } else if (viewMode.value === 'widget') {
        switchViewMode('mini');
      } else {
        switchViewMode('main');
      }
    });
  } catch (e) {}

  const timer = setInterval(() => {
    fetchSnapshot();
  }, 4000);

  onUnmounted(() => {
    document.removeEventListener('click', handleClickOutside);
    clearInterval(timer);
    if (unlistenShortcut) unlistenShortcut();
  });
});
</script>

<template>
  <div
    class="app-root"
    :class="[`mode-${viewMode}`]"
    :style="{ '--win-opacity': winOpacity }"
  >
    <!-- ══════════════════════════════════════════════════════════
         形态 1：大屏指挥中心 (1460x920)
         ══════════════════════════════════════════════════════════ -->
    <template v-if="viewMode === 'main'">
      <div class="main-dashboard-window">
        <!-- 顶部主标题栏 -->
        <div class="main-topbar" data-tauri-drag-region>
          <div class="topbar-left" data-tauri-drag-region>
            <span class="occult-skull">※</span>
            <span class="topbar-title">角色仪表盘</span>
            <span class="occult-skull">※</span>
          </div>

          <div class="topbar-actions">
            <!-- 刷新数据 -->
            <button class="warlock-btn" @click="fetchSnapshot" title="萃取最新灵魂数据">
              <RefreshCw :size="13" />
              <span>刷新数据</span>
            </button>

            <!-- 需求 1：大屏形态下的自选下拉窗口 -->
            <div class="view-mode-dropdown-wrapper">
              <button
                class="warlock-dropdown-trigger"
                :class="{ active: isViewModeMenuOpen }"
                @click.stop="isViewModeMenuOpen = !isViewModeMenuOpen"
                title="选择窗口展现形态"
              >
                <Monitor :size="13" style="color: var(--warlock-gold);" />
                <span class="current-mode-label">形态: 大屏指挥中心</span>
                <ChevronDown :size="12" class="arrow-icon" :class="{ open: isViewModeMenuOpen }" />
              </button>

              <!-- 下拉浮窗 -->
              <div v-if="isViewModeMenuOpen" class="warlock-dropdown-menu" @click.stop>
                <div
                  class="dropdown-option"
                  :class="{ selected: viewMode === 'main' }"
                  @click="switchViewMode('main')"
                >
                  <div class="option-icon-box">
                    <Monitor :size="16" />
                  </div>
                  <div class="option-text">
                    <div class="option-title">大屏指挥中心 (1460×920)</div>
                    <div class="option-desc">全景展开 7 大导航系统与高精三栏仪表盘</div>
                  </div>
                  <span v-if="viewMode === 'main'" class="option-check">✓</span>
                </div>

                <div
                  class="dropdown-option"
                  :class="{ selected: viewMode === 'widget' }"
                  @click="switchViewMode('widget')"
                >
                  <div class="option-icon-box">
                    <LayoutTemplate :size="16" />
                  </div>
                  <div class="option-text">
                    <div class="option-title">桌面常驻 HUD (340×600)</div>
                    <div class="option-desc">悬浮小挂件，暗影法阵、立绘与快捷勾选</div>
                  </div>
                  <span v-if="viewMode === 'widget'" class="option-check">✓</span>
                </div>

                <div
                  class="dropdown-option"
                  :class="{ selected: viewMode === 'mini' }"
                  @click="switchViewMode('mini')"
                >
                  <div class="option-icon-box">
                    <Pill :size="16" />
                  </div>
                  <div class="option-text">
                    <div class="option-title">极简灵能胶囊 (260×44)</div>
                    <div class="option-desc">原生物理收缩至桌角，零遮挡桌面操作</div>
                  </div>
                  <span v-if="viewMode === 'mini'" class="option-check">✓</span>
                </div>
              </div>
            </div>

            <!-- 置顶锁定 -->
            <button
              class="icon-btn"
              :class="{ pinned: isPinned }"
              @click="togglePin"
              :title="isPinned ? '取消置顶' : '固定置顶浮窗'"
            >
              <component :is="isPinned ? Pin : PinOff" :size="13" />
            </button>

            <!-- 优雅关闭 -->
            <button class="icon-btn close-btn" @click="handleExit" title="退出应用">
              <X :size="14" />
            </button>
          </div>
        </div>

        <!-- 主体结构：100% 聚焦于 1:1 游戏角色大面板 (后续可根据需要在特定区域扩展导航) -->
        <div class="main-body-container">
          <DashboardView
            :character="character"
            :attributes="attributes"
            :tasks="tasks"
            @toggle-task="handleToggleTask"
            @open-add-task="showAddModal = true"
            @open-edit-task="handleOpenEdit"
            @delete-task="handleDeleteTask"
            @quick-create="handleQuickCreate"
            @switch-tab="(tab) => activeTab = tab"
          />
        </div>
      </div>
    </template>

    <!-- ══════════════════════════════════════════════════════════
         形态 2：桌面常驻 HUD 挂件 (340x600)
         ══════════════════════════════════════════════════════════ -->
    <template v-else-if="viewMode === 'widget'">
      <div class="panel">
        <div class="titlebar" data-tauri-drag-region>
          <div class="titlebar-brand" data-tauri-drag-region>
            <span class="occult-skull">※</span>
            <span>角色仪表盘</span>
            <span class="occult-skull">※</span>
          </div>

          <div class="titlebar-btns">
            <button class="icon-btn" @click="switchViewMode('main')" title="展开为大屏指挥中心">
              <Maximize2 :size="12" />
            </button>
            <button class="icon-btn" @click="fetchSnapshot" title="刷新">
              <RefreshCw :size="12" />
            </button>
            <button
              class="icon-btn"
              :class="{ pinned: isPinned }"
              @click="togglePin"
              :title="isPinned ? '取消置顶' : '固定置顶'"
            >
              <component :is="isPinned ? Pin : PinOff" :size="12" />
            </button>
            <button class="icon-btn" @click="switchViewMode('mini')" title="折叠为胶囊护符">
              <Minimize2 :size="12" />
            </button>
            <button class="icon-btn close-btn" @click="handleExit" title="退出">
              <X :size="12" />
            </button>
          </div>
        </div>

        <CharacterHud
          :character="character"
          :is-expanded="isDrawerExpanded"
          @toggle-drawer="isDrawerExpanded = !isDrawerExpanded"
        />

        <AttributesDrawer
          v-if="isDrawerExpanded"
          :attributes="attributes"
        />

        <div class="content">
          <div class="task-list">
            <TaskItem
              v-for="item in filteredWidgetTasks"
              :key="item.id"
              :task="item"
              @toggle="handleToggleTask"
              @edit="handleOpenEdit"
              @delete="handleDeleteTask"
            />
          </div>
        </div>
      </div>
    </template>

    <!-- ══════════════════════════════════════════════════════════
         形态 3：极简灵能胶囊护符 (260x44)
         ══════════════════════════════════════════════════════════ -->
    <template v-else-if="viewMode === 'mini'">
      <MiniBar
        :character="character"
        :pending-count="pendingTasksCount"
        @expand="switchViewMode('widget')"
      />
    </template>

    <!-- 浮动灵魂能量粒子 -->
    <div
      v-for="p in particles"
      :key="p.id"
      class="soul-particle"
    >
      {{ p.text }}
    </div>

    <!-- 模态框 -->
    <AddTaskModal
      v-if="showAddModal"
      @save="handleCreateTask"
      @close="showAddModal = false"
    />

    <EditTaskModal
      v-if="showEditModal && editingTask"
      :task="editingTask"
      @save="handleUpdateTask"
      @close="showEditModal = false"
    />

    <SettingsModal
      v-if="showSettingsModal"
      :opacity="winOpacity"
      :always-on-top="isPinned"
      @update-opacity="handleUpdateOpacity"
      @toggle-pin="togglePin"
      @close="showSettingsModal = false"
    />
  </div>
</template>

<style scoped>
.app-root {
  width: 100vw;
  height: 100vh;
  box-sizing: border-box;
  overflow: hidden;
  background: transparent;
}

/* ═══ 大屏指挥中心视图书写 ═══ */
.main-dashboard-window {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  background-color: #08040d;
  background-image: url('./assets/theme/global_background.png');
  background-size: cover;
  background-position: center;
  background-repeat: no-repeat;
  border: 1.5px solid var(--warlock-border-gold);
  border-radius: 8px;
  overflow: hidden;
  box-shadow: var(--gothic-shadow);
}

.main-topbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 6px 14px;
  background: linear-gradient(180deg, rgba(28, 16, 40, 0.95) 0%, rgba(12, 6, 18, 0.98) 100%);
  border-bottom: 1px solid rgba(200, 164, 93, 0.45);
  flex-shrink: 0;
  z-index: 50;
}

.topbar-left {
  display: flex;
  align-items: center;
  gap: 8px;
}

.topbar-title {
  font-size: 16px;
  font-weight: 800;
  letter-spacing: 3px;
  color: var(--warlock-gold);
  text-shadow: 0 0 10px var(--warlock-gold-glow), 0 1px 2px #000;
}

.topbar-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

/* ═══ 下拉窗口选择器 ═══ */
.view-mode-dropdown-wrapper {
  position: relative;
  z-index: 100;
}

.warlock-dropdown-trigger {
  display: flex;
  align-items: center;
  gap: 7px;
  padding: 6px 12px;
  background: linear-gradient(180deg, #2a1b38 0%, #160d20 100%);
  border: 1px solid var(--warlock-border-gold);
  border-radius: 4px;
  color: #ffffff;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  box-shadow: var(--gold-emboss);
  transition: all 0.2s ease;
}

.warlock-dropdown-trigger:hover, .warlock-dropdown-trigger.active {
  background: linear-gradient(180deg, #3d2652 0%, #20132e 100%);
  border-color: var(--warlock-gold);
  color: var(--warlock-gold-bright);
  box-shadow: 0 0 10px var(--warlock-gold-glow);
}

.current-mode-label {
  letter-spacing: 0.5px;
}

.arrow-icon {
  transition: transform 0.25s ease;
  color: var(--warlock-gold);
}

.arrow-icon.open {
  transform: rotate(180deg);
}

.warlock-dropdown-menu {
  position: absolute;
  top: calc(100% + 6px);
  right: 0;
  width: 280px;
  background: radial-gradient(circle at 50% 10%, rgba(36, 22, 50, 0.98) 0%, rgba(14, 7, 20, 0.99) 100%);
  border: 1px solid var(--warlock-border-gold);
  border-radius: 6px;
  box-shadow: 0 12px 32px rgba(0, 0, 0, 0.9), 0 0 14px rgba(200, 164, 93, 0.3);
  padding: 6px;
  display: flex;
  flex-direction: column;
  gap: 4px;
  backdrop-filter: blur(10px);
}

.dropdown-option {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 10px;
  border-radius: 4px;
  cursor: pointer;
  border: 1px solid transparent;
  transition: all 0.2s ease;
}

.dropdown-option:hover {
  background: rgba(50, 32, 68, 0.8);
  border-color: rgba(200, 164, 93, 0.5);
}

.dropdown-option.selected {
  background: rgba(72, 255, 72, 0.12);
  border-color: var(--warlock-soul-green);
}

.option-icon-box {
  width: 30px;
  height: 30px;
  border-radius: 4px;
  background: rgba(10, 5, 16, 0.7);
  border: 1px solid rgba(200, 164, 93, 0.3);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--warlock-gold);
  flex-shrink: 0;
}

.dropdown-option.selected .option-icon-box {
  border-color: var(--warlock-soul-green);
  color: var(--warlock-soul-green-bright);
}

.option-text {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.option-title {
  font-size: 12px;
  font-weight: 700;
  color: #ffffff;
}

.dropdown-option.selected .option-title {
  color: var(--warlock-soul-green-bright);
}

.option-desc {
  font-size: 10px;
  color: var(--warlock-text-muted);
  line-height: 1.3;
}

.option-check {
  font-size: 14px;
  font-weight: 900;
  color: var(--warlock-soul-green-bright);
  text-shadow: 0 0 8px var(--warlock-soul-green-glow);
}

.main-body-container {
  flex: 1;
  display: flex;
  overflow: hidden;
  width: 100%;
  height: 100%;
}
</style>
