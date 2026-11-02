<script setup>
import { computed, onMounted, ref } from 'vue';
import { Maximize2 } from 'lucide-vue-next';
import { api } from '../api';

const emit = defineEmits(['mode']);

const snap = ref(null);

async function load() {
  try {
    snap.value = await api.dashboard();
  } catch {
    /* 胶囊形态静默失败，不打扰 */
  }
}

const level = computed(() => snap.value?.levelInfo || {});
const today = computed(() => ({
  done: snap.value?.todayCompletedCount || 0,
  total: snap.value?.todayTotalCount || 0,
}));

onMounted(load);
</script>

<template>
  <div class="mini" data-tauri-drag-region title="双击展开为桌面挂件" @dblclick="emit('mode', 'widget')">
    <span class="m-sigil">☠</span>
    <span class="m-lv num">Lv.{{ level.level || 1 }}</span>
    <span class="m-bar">
      <i :style="{ width: (level.progressPercent || 0) + '%' }"></i>
    </span>
    <span class="m-pct num">{{ (level.progressPercent || 0).toFixed(0) }}%</span>
    <span class="m-today num">{{ today.done }}/{{ today.total }}</span>
    <button class="m-btn" title="展开挂件" @click="emit('mode', 'widget')">
      <Maximize2 :size="10" />
    </button>
  </div>
</template>

<style scoped>
.mini {
  display: flex;
  align-items: center;
  gap: 7px;
  height: 100%;
  padding: 0 9px;
  background:
    linear-gradient(180deg, rgba(16, 24, 14, 0.98), rgba(5, 8, 5, 0.99));
  border: 1px solid rgba(109, 91, 42, 0.6);
  box-shadow: inset 0 1px 0 rgba(192, 166, 76, 0.2);
  cursor: default;
}
.m-sigil { color: var(--c-glow); font-size: 12px; text-shadow: var(--glow-soul); }
.m-lv { font-size: 12px; color: var(--c-soul); }
.m-bar {
  flex: 1;
  height: 7px;
  background: url('../assets/textures/bar_trough.webp') repeat-x center / auto 100%;
  border: 1px solid rgba(109, 91, 42, 0.7);
  overflow: hidden;
  box-shadow: inset 0 1px 3px rgba(0, 0, 0, 0.9);
}
.m-bar i {
  display: block;
  height: 100%;
  background: url('../assets/textures/bar_fill.webp') repeat-x center / auto 100%;
  box-shadow: 0 0 10px rgba(124, 255, 31, 0.8), inset 0 1px 0 rgba(255, 255, 255, 0.6);
  transition: width 0.5s ease;
}
.m-pct { font-size: 12px; color: var(--t-bar); }
.m-today { font-size: 12px; color: var(--t-time); }
.m-btn {
  width: 18px; height: 18px;
  display: flex; align-items: center; justify-content: center;
  color: var(--t-sub);
  background: transparent;
  border: 1px solid rgba(109, 91, 42, 0.45);
  cursor: pointer;
}
.m-btn:hover { color: var(--c-soul); border-color: var(--c-soul); }
</style>

<style scoped>

@media(max-width:220px){.mini{gap:4px;padding:0 6px}.m-pct{display:none}}

</style>
