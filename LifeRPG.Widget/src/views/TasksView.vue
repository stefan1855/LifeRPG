<script setup>
import { ref, computed } from 'vue';
import { Plus, LayoutGrid, List, CheckCircle2, Circle, Send } from 'lucide-vue-next';
import { ATTRIBUTE_META, QUADRANT_META, parseNaturalTask } from '../utils/timeParser';

const props = defineProps({
  tasks: {
    type: Array,
    default: () => [],
  },
});

const emit = defineEmits(['toggle-task', 'open-add-task', 'open-edit-task', 'delete-task', 'quick-create']);

const viewMode = ref('matrix'); // 'matrix' or 'list'
const statusFilter = ref('all'); // 'all', 'pending', 'completed'
const attrFilter = ref('all');
const quickText = ref('');

const quadrants = [
  { key: 'Q1', title: '核心突击 · 紧急重要', exp: '+30 EXP', color: '#ef4444' },
  { key: 'Q2', title: '意志修行 · 重要非急', exp: '+25 EXP', color: '#48ff48' },
  { key: 'Q3', title: '琐碎杂务 · 紧迫次要', exp: '+15 EXP', color: '#ffd700' },
  { key: 'Q4', title: '灵魂休闲 · 闲暇调养', exp: '+5 EXP', color: '#b07eff' },
];

const filteredTasks = computed(() => {
  let list = props.tasks || [];
  if (statusFilter.value === 'pending') {
    list = list.filter((t) => t.status !== 'Completed');
  } else if (statusFilter.value === 'completed') {
    list = list.filter((t) => t.status === 'Completed');
  }
  if (attrFilter.value !== 'all') {
    list = list.filter((t) => t.growth_attribute === attrFilter.value);
  }
  return list;
});

function getQuadrantTasks(qKey) {
  return filteredTasks.value.filter((t) => (t.quadrant || t.priority) === qKey);
}

function handleQuickSubmit() {
  const text = quickText.value.trim();
  if (!text) return;
  emit('quick-create', text);
  quickText.value = '';
}
</script>

<template>
  <div class="tasks-page-container">
    <!-- 顶部过滤与视图切换工具栏 -->
    <div class="tasks-header-bar gothic-frame-box">
      <div class="filter-group">
        <div class="status-pills">
          <button
            class="pill-btn"
            :class="{ active: statusFilter === 'all' }"
            @click="statusFilter = 'all'"
          >
            全部 ({{ tasks.length }})
          </button>
          <button
            class="pill-btn"
            :class="{ active: statusFilter === 'pending' }"
            @click="statusFilter = 'pending'"
          >
            待行
          </button>
          <button
            class="pill-btn"
            :class="{ active: statusFilter === 'completed' }"
            @click="statusFilter = 'completed'"
          >
            已达成
          </button>
        </div>

        <div class="attr-pills">
          <button
            class="pill-btn-sm"
            :class="{ active: attrFilter === 'all' }"
            @click="attrFilter = 'all'"
          >
            全部属性
          </button>
          <button
            v-for="(meta, key) in ATTRIBUTE_META"
            :key="key"
            class="pill-btn-sm"
            :class="{ active: attrFilter === key }"
            :style="attrFilter === key ? { borderColor: meta.color, color: meta.color } : {}"
            @click="attrFilter = key"
          >
            {{ meta.symbol }} {{ meta.name }}
          </button>
        </div>
      </div>

      <div class="action-group">
        <div class="view-toggle">
          <button
            class="icon-btn-toggle"
            :class="{ active: viewMode === 'matrix' }"
            @click="viewMode = 'matrix'"
            title="四象限矩阵看板"
          >
            <LayoutGrid :size="15" />
          </button>
          <button
            class="icon-btn-toggle"
            :class="{ active: viewMode === 'list' }"
            @click="viewMode = 'list'"
            title="契约清单列表"
          >
            <List :size="15" />
          </button>
        </div>

        <button class="warlock-btn primary" @click="$emit('open-add-task')">
          <Plus :size="13" />
          <span>缔结新契约</span>
        </button>
      </div>
    </div>

    <!-- ═══ 视图 1：四象限暗黑矩阵看板 ═══ -->
    <div v-if="viewMode === 'matrix'" class="quadrant-matrix-grid">
      <div
        v-for="q in quadrants"
        :key="q.key"
        class="quad-column gothic-frame-box"
      >
        <div class="quad-col-header" :style="{ borderLeftColor: q.color }">
          <div class="quad-col-title">{{ q.title }}</div>
          <div class="quad-col-exp" :style="{ color: q.color }">{{ q.exp }}</div>
        </div>

        <div class="quad-col-body">
          <div v-if="getQuadrantTasks(q.key).length === 0" class="empty-quad">
            <span>暂无此象限契约</span>
          </div>

          <div
            v-for="task in getQuadrantTasks(q.key)"
            :key="task.id"
            class="matrix-task-card"
            :class="{ completed: task.status === 'Completed' }"
          >
            <button
              class="matrix-check-btn"
              :class="{ checked: task.status === 'Completed' }"
              @click="$emit('toggle-task', task)"
            >
              <component :is="task.status === 'Completed' ? CheckCircle2 : Circle" :size="18" />
            </button>

            <div class="matrix-task-content">
              <div class="matrix-task-title">{{ task.title }}</div>
              <div class="matrix-task-meta">
                <span class="matrix-attr-tag" :style="{ color: ATTRIBUTE_META[task.growth_attribute]?.color }">
                  {{ ATTRIBUTE_META[task.growth_attribute]?.symbol }} {{ ATTRIBUTE_META[task.growth_attribute]?.name }}
                </span>
                <span v-if="task.due_at" class="matrix-due-tag">
                  {{ task.due_at.slice(5, 10) }} 截止
                </span>
              </div>
            </div>

            <div class="matrix-task-actions">
              <button class="matrix-btn-action" @click="$emit('open-edit-task', task)">✎</button>
              <button class="matrix-btn-action del" @click="$emit('delete-task', task)">✕</button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ═══ 视图 2：全量列表模式 ═══ -->
    <div v-else class="list-view-container gothic-frame-box">
      <div class="list-scroll-area">
        <div v-if="filteredTasks.length === 0" class="empty-quad" style="padding: 60px;">
          <span>暂无符合条件的契约条目</span>
        </div>

        <div
          v-for="task in filteredTasks"
          :key="task.id"
          class="list-task-row"
          :class="{ completed: task.status === 'Completed' }"
        >
          <button
            class="matrix-check-btn"
            :class="{ checked: task.status === 'Completed' }"
            @click="$emit('toggle-task', task)"
          >
            <component :is="task.status === 'Completed' ? CheckCircle2 : Circle" :size="18" />
          </button>

          <div class="list-task-title-col">
            <span class="list-title-text">{{ task.title }}</span>
            <span v-if="task.description" class="list-desc-text">{{ task.description }}</span>
          </div>

          <div class="list-task-tags-col">
            <span class="matrix-attr-tag" :style="{ color: ATTRIBUTE_META[task.growth_attribute]?.color }">
              {{ ATTRIBUTE_META[task.growth_attribute]?.symbol }} {{ ATTRIBUTE_META[task.growth_attribute]?.name }}
            </span>
            <span class="quad-badge">
              {{ QUADRANT_META[task.quadrant || task.priority]?.label }}
            </span>
            <span v-if="task.due_at" class="matrix-due-tag">
              {{ task.due_at.slice(0, 10) }}
            </span>
          </div>

          <div class="list-task-exp-col">
            +{{ task.experience || 25 }} EXP
          </div>

          <div class="matrix-task-actions">
            <button class="matrix-btn-action" @click="$emit('open-edit-task', task)">✎</button>
            <button class="matrix-btn-action del" @click="$emit('delete-task', task)">✕</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.tasks-page-container {
  display: flex;
  flex-direction: column;
  gap: 12px;
  width: 100%;
  height: 100%;
  padding: 14px;
  box-sizing: border-box;
  overflow: hidden;
  background: radial-gradient(circle at 50% 20%, #120a1c 0%, #08040d 100%);
}

.gothic-frame-box {
  background: linear-gradient(180deg, rgba(26, 16, 38, 0.95) 0%, rgba(12, 7, 18, 0.98) 100%);
  border: 1px solid var(--warlock-border-gold);
  border-radius: 6px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.8), inset 0 1px 0 rgba(200, 164, 93, 0.35);
  position: relative;
  overflow: hidden;
}

.tasks-header-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  padding: 10px 16px;
  flex-shrink: 0;
}

.filter-group {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.status-pills, .attr-pills {
  display: flex;
  gap: 4px;
}

.pill-btn, .pill-btn-sm {
  background: rgba(14, 8, 20, 0.85);
  border: 1px solid rgba(120, 90, 150, 0.6);
  color: #cbd5e1;
  border-radius: 4px;
  padding: 5px 12px;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s ease;
}

.pill-btn-sm {
  padding: 4px 8px;
  font-size: 11px;
}

.pill-btn:hover, .pill-btn-sm:hover {
  background: rgba(36, 22, 50, 0.9);
  color: #ffffff;
  border-color: var(--warlock-gold);
}

.pill-btn.active, .pill-btn-sm.active {
  background: rgba(72, 255, 72, 0.15);
  border-color: var(--warlock-soul-green);
  color: var(--warlock-soul-green-bright);
  box-shadow: 0 0 10px rgba(72, 255, 72, 0.3);
}

.action-group {
  display: flex;
  align-items: center;
  gap: 8px;
}

.view-toggle {
  display: flex;
  background: rgba(10, 5, 16, 0.9);
  border: 1px solid var(--warlock-border-gold);
  border-radius: 4px;
  overflow: hidden;
}

.icon-btn-toggle {
  background: transparent;
  border: none;
  color: #94a3b8;
  padding: 6px 10px;
  cursor: pointer;
  display: flex;
  align-items: center;
  transition: all 0.2s ease;
}

.icon-btn-toggle:hover {
  color: #ffffff;
  background: rgba(40, 26, 56, 0.7);
}

.icon-btn-toggle.active {
  background: rgba(72, 255, 72, 0.2);
  color: var(--warlock-soul-green-bright);
}

/* 四象限网格 */
.quadrant-matrix-grid {
  flex: 1;
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  grid-template-rows: repeat(2, 1fr);
  gap: 12px;
  overflow: hidden;
}

.quad-column {
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.quad-col-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 14px;
  background: linear-gradient(180deg, rgba(30, 18, 44, 0.95) 0%, rgba(14, 8, 20, 0.95) 100%);
  border-left: 4px solid var(--warlock-border-gold);
  border-bottom: 1px solid rgba(200, 164, 93, 0.35);
  flex-shrink: 0;
}

.quad-col-title {
  font-size: 13px;
  font-weight: 800;
  color: #ffffff;
  letter-spacing: 1px;
}

.quad-col-exp {
  font-size: 12px;
  font-weight: 800;
  font-family: var(--font-mono);
}

.quad-col-body {
  flex: 1;
  overflow-y: auto;
  padding: 8px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.empty-quad {
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #64748b;
  font-size: 12px;
}

.matrix-task-card {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 10px;
  background: rgba(14, 8, 20, 0.7);
  border: 1px solid rgba(120, 90, 150, 0.4);
  border-radius: 4px;
  transition: all 0.2s ease;
}

.matrix-task-card:hover {
  background: rgba(36, 22, 50, 0.85);
  border-color: var(--warlock-border-gold);
  transform: translateY(-1px);
}

.matrix-task-card.completed {
  opacity: 0.55;
  background: rgba(8, 4, 12, 0.6);
}

.matrix-check-btn {
  background: transparent;
  border: none;
  color: #64748b;
  cursor: pointer;
  padding: 0;
  display: flex;
  align-items: center;
  transition: all 0.2s ease;
}

.matrix-check-btn:hover {
  color: var(--warlock-soul-green);
}

.matrix-check-btn.checked {
  color: var(--warlock-soul-green-bright);
}

.matrix-task-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.matrix-task-title {
  font-size: 13px;
  font-weight: 700;
  color: #ffffff;
}

.matrix-task-card.completed .matrix-task-title {
  text-decoration: line-through;
  color: #94a3b8;
}

.matrix-task-meta {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 11px;
}

.matrix-attr-tag {
  font-weight: 700;
}

.matrix-due-tag {
  color: #94a3b8;
  font-size: 10px;
  font-family: var(--font-mono);
}

.matrix-task-actions {
  display: flex;
  gap: 4px;
}

.matrix-btn-action {
  width: 22px;
  height: 22px;
  border-radius: 3px;
  background: rgba(10, 5, 16, 0.6);
  border: 1px solid rgba(120, 90, 150, 0.4);
  color: #cbd5e1;
  font-size: 11px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
}

.matrix-btn-action:hover {
  border-color: var(--warlock-gold);
  color: var(--warlock-gold-bright);
}

.matrix-btn-action.del:hover {
  border-color: #ef4444;
  color: #ef4444;
}

/* 列表视图 */
.list-view-container {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.list-scroll-area {
  flex: 1;
  overflow-y: auto;
  padding: 10px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.list-task-row {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 14px;
  background: rgba(14, 8, 20, 0.75);
  border: 1px solid rgba(120, 90, 150, 0.4);
  border-radius: 4px;
  transition: all 0.2s ease;
}

.list-task-row:hover {
  background: rgba(36, 22, 50, 0.85);
  border-color: var(--warlock-border-gold);
}

.list-task-row.completed {
  opacity: 0.55;
  background: rgba(8, 4, 12, 0.6);
}

.list-task-title-col {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.list-title-text {
  font-size: 13px;
  font-weight: 700;
  color: #ffffff;
}

.list-desc-text {
  font-size: 11px;
  color: #94a3b8;
}

.list-task-tags-col {
  display: flex;
  align-items: center;
  gap: 8px;
}

.quad-badge {
  background: rgba(200, 164, 93, 0.15);
  border: 1px solid rgba(200, 164, 93, 0.4);
  color: var(--warlock-gold-bright);
  font-size: 10px;
  font-weight: 700;
  padding: 2px 6px;
  border-radius: 3px;
}

.list-task-exp-col {
  font-size: 12px;
  font-weight: 800;
  color: var(--warlock-soul-green-bright);
  font-family: var(--font-mono);
  width: 90px;
  text-align: right;
}
</style>
