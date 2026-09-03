<script setup>
import { computed } from 'vue';
import { Archive, Calendar, CheckSquare } from 'lucide-vue-next';

const props = defineProps({
  character: {
    type: Object,
    required: true,
  },
  tasks: {
    type: Array,
    default: () => [],
  },
});

const completedTasks = computed(() => {
  return (props.tasks || []).filter((t) => t.status === 'Completed');
});

const completionRate = computed(() => {
  const total = (props.tasks || []).length;
  if (total === 0) return 0;
  return Math.round((completedTasks.value.length / total) * 100);
});
</script>

<template>
  <div class="archive-page-container">
    <div class="archive-header-bar gothic-frame-box">
      <div class="header-left">
        <span class="skull-sym">☠</span>
        <span class="page-title">成 长 复 盘 与 契 约 归 档</span>
        <span class="skull-sym">☠</span>
      </div>
      <div class="archive-date-tag">
        <Calendar :size="13" />
        <span>周期综合审计</span>
      </div>
    </div>

    <!-- 统计卡片网格 -->
    <div class="stats-overview-grid">
      <div class="stat-card gothic-frame-box">
        <div class="stat-label">总完成率</div>
        <div class="stat-value" style="color: var(--warlock-soul-green-bright);">{{ completionRate }}%</div>
        <div class="stat-sub">已达成 {{ completedTasks.length }} / 共 {{ tasks.length }} 桩契约</div>
      </div>

      <div class="stat-card gothic-frame-box">
        <div class="stat-label">总汲取灵魂能量</div>
        <div class="stat-value" style="color: var(--warlock-gold);">{{ Number(character.total_experience || 0).toLocaleString() }} EXP</div>
        <div class="stat-sub">当前境界 Lv.{{ character.level || 27 }}</div>
      </div>

      <div class="stat-card gothic-frame-box">
        <div class="stat-label">待行契约积压</div>
        <div class="stat-value" style="color: var(--warlock-arcane-purple);">{{ tasks.length - completedTasks.length }}</div>
        <div class="stat-sub">待汲取能量池</div>
      </div>
    </div>

    <!-- 归档历史列表 -->
    <div class="archive-list-card gothic-frame-box">
      <div class="archive-list-header">
        <span class="star-sym">※</span>
        <span>已 达 成 契 约 历 史 碑 铭</span>
        <span class="star-sym">※</span>
      </div>
      <div class="archive-list-body">
        <div v-if="completedTasks.length === 0" class="empty-archive">
          <span>暂无已归档契约</span>
        </div>
        <div
          v-for="task in completedTasks"
          :key="task.id"
          class="archive-item-row"
        >
          <div class="archive-item-left">
            <CheckSquare :size="15" style="color: var(--warlock-soul-green);" />
            <span class="archive-title">{{ task.title }}</span>
          </div>
          <div class="archive-item-right">
            <span class="archive-exp">+{{ task.experience || 25 }} EXP</span>
            <span class="archive-time">{{ task.completed_at ? task.completed_at.slice(0, 10) : '已达成' }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.archive-page-container {
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

.archive-header-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 16px;
  flex-shrink: 0;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 8px;
}

.page-title {
  font-size: 14px;
  font-weight: 800;
  letter-spacing: 2px;
  color: var(--warlock-gold);
  text-shadow: 0 0 10px var(--warlock-gold-glow), 0 1px 2px #000;
}

.archive-date-tag {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 11px;
  color: #cbd5e1;
}

.stats-overview-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 12px;
}

.stat-card {
  padding: 14px 16px;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.stat-label {
  font-size: 12px;
  color: #94a3b8;
  font-weight: 700;
}

.stat-value {
  font-size: 22px;
  font-weight: 900;
  font-family: var(--font-mono);
}

.stat-sub {
  font-size: 11px;
  color: #cbd5e1;
}

.archive-list-card {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.archive-list-header {
  padding: 10px 14px;
  background: linear-gradient(180deg, #241434 0%, #160c22 100%);
  border-bottom: 1px solid var(--warlock-border-gold);
  font-size: 13px;
  font-weight: 800;
  color: var(--warlock-gold);
  letter-spacing: 2px;
  display: flex;
  align-items: center;
  gap: 6px;
  text-shadow: 0 0 8px var(--warlock-gold-glow);
}

.archive-list-body {
  flex: 1;
  overflow-y: auto;
  padding: 10px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.empty-archive {
  padding: 40px;
  text-align: center;
  color: #64748b;
  font-size: 12px;
}

.archive-item-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 12px;
  background: rgba(14, 8, 20, 0.7);
  border: 1px solid rgba(120, 90, 150, 0.4);
  border-radius: 4px;
}

.archive-item-left {
  display: flex;
  align-items: center;
  gap: 8px;
}

.archive-title {
  font-size: 13px;
  font-weight: 700;
  color: #ffffff;
}

.archive-item-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.archive-exp {
  font-size: 11px;
  font-weight: 800;
  color: var(--warlock-soul-green-bright);
  font-family: var(--font-mono);
}

.archive-time {
  font-size: 10px;
  color: #94a3b8;
  font-family: var(--font-mono);
}
</style>
