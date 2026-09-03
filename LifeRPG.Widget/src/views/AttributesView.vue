<script setup>
import { ATTRIBUTE_META } from '../utils/timeParser';
import { Shield } from 'lucide-vue-next';

defineProps({
  attributes: {
    type: Array,
    default: () => [],
  },
  character: {
    type: Object,
    required: true,
  },
});
</script>

<template>
  <div class="attributes-page-container">
    <div class="attr-header-bar gothic-frame-box">
      <div class="header-left">
        <span class="skull-sym">☠</span>
        <span class="page-title">六 维 暗 影 契 约 属 性 进 阶</span>
        <span class="skull-sym">☠</span>
      </div>
      <div class="character-summary-tag">
        <span>当前境界: </span>
        <strong class="realm-name">Lv.{{ character.level || 27 }} 无面者</strong>
      </div>
    </div>

    <!-- 6 属性详细卡片网格 -->
    <div class="attr-cards-grid">
      <div
        v-for="attr in attributes"
        :key="attr.id"
        class="attr-detail-card gothic-frame-box"
      >
        <div class="card-head">
          <div class="sym-box" :style="{ color: ATTRIBUTE_META[attr.attribute_key]?.color || 'var(--warlock-soul-green)', borderColor: ATTRIBUTE_META[attr.attribute_key]?.color || 'var(--warlock-soul-green)' }">
            {{ ATTRIBUTE_META[attr.attribute_key]?.symbol || '◈' }}
          </div>
          <div class="head-info">
            <div class="attr-title-text">{{ ATTRIBUTE_META[attr.attribute_key]?.name || attr.display_name }}</div>
            <div class="attr-sub-text">映射原质: {{ ATTRIBUTE_META[attr.attribute_key]?.originalName || '原力' }}</div>
          </div>
          <div class="lvl-badge-big">
            <span class="lvl-sm">Lv.</span>
            <strong class="lvl-lg">{{ attr.level }}</strong>
          </div>
        </div>

        <div class="card-body">
          <div class="bar-row">
            <span>灵能积累进度</span>
            <span class="pct-text">{{ Math.round(attr.progress_percent || 0) }}%</span>
          </div>

          <div class="progress-track">
            <div
              class="progress-fill"
              :style="{
                width: `${Math.min(100, Math.max(0, attr.progress_percent || 0))}%`,
                background: ATTRIBUTE_META[attr.attribute_key]?.color || 'var(--warlock-soul-green)',
                boxShadow: `0 0 10px ${ATTRIBUTE_META[attr.attribute_key]?.color || 'var(--warlock-soul-green)'}`
              }"
            ></div>
          </div>

          <div class="meta-row">
            <span>累计经验: {{ Number(attr.experience).toLocaleString() }} EXP</span>
            <span>本级已蓄: {{ Number(attr.exp_into_current_level || 0).toLocaleString() }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.attributes-page-container {
  display: flex;
  flex-direction: column;
  gap: 12px;
  width: 100%;
  height: 100%;
  padding: 14px;
  box-sizing: border-box;
  overflow-y: auto;
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

.attr-header-bar {
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

.character-summary-tag {
  font-size: 12px;
  color: #cbd5e1;
}

.realm-name {
  color: var(--warlock-soul-green-bright);
  font-weight: 800;
  margin-left: 4px;
  text-shadow: 0 0 8px var(--warlock-soul-green-glow);
}

.attr-cards-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 12px;
}

.attr-detail-card {
  padding: 14px 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  transition: all 0.2s ease;
}

.attr-detail-card:hover {
  border-color: var(--warlock-gold);
  transform: translateY(-2px);
  box-shadow: 0 12px 28px rgba(0, 0, 0, 0.9), 0 0 12px rgba(200, 164, 93, 0.3);
}

.card-head {
  display: flex;
  align-items: center;
  gap: 12px;
}

.sym-box {
  width: 38px;
  height: 38px;
  border-radius: 6px;
  border: 1.5px solid;
  background: rgba(14, 8, 20, 0.9);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
  flex-shrink: 0;
  box-shadow: 0 0 10px rgba(0, 0, 0, 0.8);
}

.head-info {
  flex: 1;
}

.attr-title-text {
  font-size: 15px;
  font-weight: 800;
  color: #ffffff;
}

.attr-sub-text {
  font-size: 11px;
  color: #94a3b8;
}

.lvl-badge-big {
  background: rgba(10, 5, 16, 0.9);
  border: 1px solid var(--warlock-border-gold);
  border-radius: 4px;
  padding: 2px 8px;
  display: flex;
  align-items: baseline;
  gap: 3px;
}

.lvl-sm {
  font-size: 10px;
  color: var(--warlock-gold);
  font-weight: 700;
}

.lvl-lg {
  font-size: 16px;
  font-weight: 900;
  color: var(--warlock-soul-green-bright);
  font-family: var(--font-mono);
}

.card-body {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.bar-row {
  display: flex;
  justify-content: space-between;
  font-size: 12px;
  color: #cbd5e1;
  font-weight: 600;
}

.pct-text {
  font-family: var(--font-mono);
  font-weight: 800;
  color: var(--warlock-soul-green-bright);
}

.progress-track {
  width: 100%;
  height: 10px;
  background: rgba(10, 5, 16, 0.95);
  border: 1px solid rgba(200, 164, 93, 0.4);
  border-radius: 5px;
  overflow: hidden;
  box-shadow: inset 0 2px 4px rgba(0, 0, 0, 0.9);
}

.progress-fill {
  height: 100%;
  border-radius: 4px;
  transition: width 0.4s ease;
}

.meta-row {
  display: flex;
  justify-content: space-between;
  font-size: 11px;
  color: #94a3b8;
  font-family: var(--font-mono);
}
</style>
