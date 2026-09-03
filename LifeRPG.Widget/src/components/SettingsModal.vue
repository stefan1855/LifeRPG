<script setup>
import { ref, onMounted } from 'vue';
import { Settings, X, HardDrive, ShieldCheck, Zap } from 'lucide-vue-next';
import { invoke } from '@tauri-apps/api/core';
import { isEnabled, enable, disable } from '@tauri-apps/plugin-autostart';

const props = defineProps({
  opacity: {
    type: Number,
    default: 0.96,
  },
  alwaysOnTop: {
    type: Boolean,
    default: true,
  },
});

const emit = defineEmits(['update-opacity', 'toggle-pin', 'close']);

const currentOpacity = ref(props.opacity);
const autostartActive = ref(false);
const dbPath = ref('Loading...');

onMounted(async () => {
  try {
    autostartActive.value = await isEnabled();
  } catch (e) {
    console.warn('Autostart check error:', e);
  }

  try {
    const snapshot = await invoke('get_dashboard_snapshot');
    if (snapshot && snapshot.character) {
      dbPath.value = 'LifeRPG Native Core (已连接暗影数据库)';
    }
  } catch (e) {
    dbPath.value = '连接异常: ' + e;
  }
});

function handleOpacityChange(e) {
  const val = parseFloat(e.target.value);
  currentOpacity.value = val;
  emit('update-opacity', val);
}

async function toggleAutostart() {
  try {
    if (autostartActive.value) {
      await disable();
      autostartActive.value = false;
    } else {
      await enable();
      autostartActive.value = true;
    }
  } catch (e) {
    console.error('Toggle autostart failed:', e);
  }
}
</script>

<template>
  <div class="modal" @click.self="$emit('close')">
    <div class="modal-box-warlock">
      <div style="display: flex; justify-content: space-between; align-items: center;">
        <h3>
          <Settings :size="16" style="color: var(--warlock-gold);" />
          暗影 HUD 系统配置
        </h3>
        <button class="icon-btn" @click="$emit('close')">
          <X :size="13" />
        </button>
      </div>

      <!-- 磨砂透明度 -->
      <div style="display: flex; flex-direction: column; gap: 6px;">
        <label>
          <span>虚空暗影透明度</span>
          <span style="color: var(--warlock-soul-green);">{{ Math.round(currentOpacity * 100) }}%</span>
        </label>
        <input
          type="range"
          min="0.6"
          max="1.0"
          step="0.05"
          :value="currentOpacity"
          @input="handleOpacityChange"
          style="accent-color: var(--warlock-soul-green); cursor: pointer;"
        />
      </div>

      <!-- 窗口置顶 / 桌面挂载 -->
      <div style="display: flex; justify-content: space-between; align-items: center; padding: 6px 0;">
        <div style="display: flex; align-items: center; gap: 6px;">
          <ShieldCheck :size="14" style="color: var(--warlock-gold);" />
          <span style="font-size: 12px;">置顶暗影之眼</span>
        </div>
        <input
          type="checkbox"
          class="task-checkbox-warlock"
          :checked="alwaysOnTop"
          @change="$emit('toggle-pin')"
        />
      </div>

      <!-- 开机自启 -->
      <div style="display: flex; justify-content: space-between; align-items: center; padding: 6px 0;">
        <div style="display: flex; align-items: center; gap: 6px;">
          <Zap :size="14" style="color: var(--warlock-soul-green);" />
          <span style="font-size: 12px;">随 Windows 静默唤醒</span>
        </div>
        <input
          type="checkbox"
          class="task-checkbox-warlock"
          :checked="autostartActive"
          @change="toggleAutostart"
        />
      </div>

      <!-- 数据库状态 -->
      <div style="background: rgba(10, 5, 16, 0.7); padding: 10px; border-radius: 6px; border: 1px solid var(--warlock-border); display: flex; flex-direction: column; gap: 4px;">
        <div style="display: flex; align-items: center; gap: 6px; font-size: 11px; color: var(--warlock-text-secondary);">
          <HardDrive :size="12" style="color: var(--warlock-soul-green);" />
          <span>SQLite 跨进程共享引擎</span>
        </div>
        <div style="font-family: var(--font-mono); font-size: 10px; color: var(--warlock-text-muted); word-break: break-all;">
          {{ dbPath }}
        </div>
      </div>

      <div class="modal-btns">
        <button class="btn-warlock primary" style="width: 100%;" @click="$emit('close')">确认</button>
      </div>
    </div>
  </div>
</template>
