<script setup>
import { Maximize2, X } from 'lucide-vue-next';
import { invoke } from '@tauri-apps/api/core';

defineProps({
  character: {
    type: Object,
    default: () => ({
      level: 1,
      name: '无面者',
      progress_percent: 0,
    }),
  },
  pendingCount: {
    type: Number,
    default: 0,
  },
});

defineEmits(['expand']);

async function handleExit() {
  try {
    await invoke('app_exit');
  } catch (e) {
    console.error('退出应用失败:', e);
  }
}
</script>

<template>
  <div
    class="mini-talisman"
    data-tauri-drag-region
    @dblclick="$emit('expand')"
    title="双击或点击 ⤢ 展开完整暗影 HUD 仪表盘"
  >
    <!-- 左侧徽章与角色 -->
    <div class="mini-indicator" data-tauri-drag-region>
      <span class="mini-lvl-badge">Lv.{{ character.level || 1 }}</span>
      <span class="mini-name-text">{{ character.name || '无面者' }}</span>

      <!-- 迷你灵魂能量槽 -->
      <div class="mini-energy-bar" title="灵魂能量进度">
        <div
          class="mini-energy-fill"
          :style="{ width: `${Math.min(100, Math.max(0, character.progress_percent || 0))}%` }"
        ></div>
      </div>

      <!-- 待行契约数 -->
      <span class="mini-pending-badge">
        {{ pendingCount }}
      </span>
    </div>

    <!-- 右侧操作按钮组 -->
    <div class="mini-actions">
      <button class="icon-btn mini-btn" @click.stop="$emit('expand')" title="展开完整 HUD">
        <Maximize2 :size="11" />
      </button>
      <button class="icon-btn mini-btn close-hover" @click.stop="handleExit" title="关闭暗影组件">
        <X :size="11" />
      </button>
    </div>
  </div>
</template>

<style scoped>
.mini-talisman {
  width: 100vw;
  height: 100vh;
  box-sizing: border-box;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 8px;
  background: rgba(14, 8, 22, 0.98);
  border: 1px solid var(--warlock-gold);
  border-radius: 8px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.9), inset 0 1px 0 rgba(200, 164, 93, 0.3);
  user-select: none;
  cursor: grab;
  overflow: hidden;
}
.mini-talisman:active {
  cursor: grabbing;
}
.mini-indicator {
  display: flex;
  align-items: center;
  gap: 6px;
  flex: 1;
  min-width: 0;
}
.mini-lvl-badge {
  font-family: var(--font-mono);
  font-size: 10px;
  font-weight: 700;
  color: var(--warlock-soul-green);
  background: rgba(124, 255, 107, 0.12);
  border: 1px solid rgba(124, 255, 107, 0.4);
  padding: 1px 4px;
  border-radius: 3px;
  letter-spacing: 0.5px;
}
.mini-name-text {
  font-family: var(--font-gothic);
  font-size: 11px;
  color: var(--warlock-gold);
  white-space: nowrap;
  font-weight: 600;
}
.mini-energy-bar {
  width: 36px;
  height: 5px;
  background: rgba(20, 10, 30, 0.9);
  border: 1px solid rgba(124, 255, 107, 0.3);
  border-radius: 3px;
  overflow: hidden;
}
.mini-energy-fill {
  height: 100%;
  background: linear-gradient(90deg, #166534, #7CFF6B);
  box-shadow: 0 0 6px var(--warlock-soul-green);
  transition: width 0.3s ease;
}
.mini-pending-badge {
  font-family: var(--font-mono);
  font-size: 10px;
  font-weight: 700;
  color: var(--warlock-soul-green);
  background: rgba(154, 92, 255, 0.15);
  border: 1px solid rgba(154, 92, 255, 0.4);
  padding: 1px 5px;
  border-radius: 10px;
}
.mini-actions {
  display: flex;
  align-items: center;
  gap: 3px;
}
.mini-btn {
  width: 20px;
  height: 20px;
  padding: 0;
}
.close-hover:hover {
  color: #ff5555 !important;
  background: rgba(255, 85, 85, 0.2) !important;
  border-color: rgba(255, 85, 85, 0.5) !important;
}
</style>
