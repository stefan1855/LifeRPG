<script setup>
import { ref, onMounted } from 'vue';
import { Settings, ShieldCheck, Zap, HardDrive, Keyboard } from 'lucide-vue-next';
import { invoke } from '@tauri-apps/api/core';
import { isEnabled, enable, disable } from '@tauri-apps/plugin-autostart';

const props = defineProps({
  opacity: {
    type: Number,
    default: 0.96,
  },
  alwaysOnTop: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(['update-opacity', 'toggle-pin']);

const currentOpacity = ref(props.opacity);
const autostartActive = ref(false);
const hotkeyText = ref('Alt+R');
const dbStatus = ref('已连接: %LOCALAPPDATA%\\LifeRPG.Native\\data\\liferpg.db');

onMounted(async () => {
  try {
    autostartActive.value = await isEnabled();
  } catch (e) {
    console.warn('Autostart check error:', e);
  }

  try {
    const cfg = await invoke('config_get');
    if (cfg && cfg.hotkey) {
      hotkeyText.value = cfg.hotkey;
    }
  } catch {}
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
    console.error('Toggle autostart error:', e);
  }
}
</script>

<template>
  <div class="settings-page-container">
    <div class="settings-header-bar gothic-frame-box">
      <div class="header-left">
        <span class="skull-sym">☠</span>
        <span class="page-title">暗 影 系 统 与 运 行 环 境 配 置</span>
        <span class="skull-sym">☠</span>
      </div>
    </div>

    <div class="settings-card-grid">
      <!-- 窗口与视觉 -->
      <div class="setting-group-box gothic-frame-box">
        <div class="group-title">
          <ShieldCheck :size="15" style="color: var(--warlock-gold);" />
          <span>视觉与置顶模式</span>
        </div>

        <div class="setting-item">
          <div class="item-info">
            <span class="item-name">虚空暗影透明度</span>
            <span class="item-desc">调节界面亚克力磨砂透光率</span>
          </div>
          <div style="display: flex; align-items: center; gap: 8px;">
            <input
              type="range"
              min="0.6"
              max="1.0"
              step="0.05"
              :value="currentOpacity"
              @input="handleOpacityChange"
              style="accent-color: var(--warlock-soul-green); cursor: pointer;"
            />
            <span style="font-family: var(--font-mono); color: var(--warlock-soul-green-bright); font-weight: 800; width: 40px;">
              {{ Math.round(currentOpacity * 100) }}%
            </span>
          </div>
        </div>

        <div class="setting-item">
          <div class="item-info">
            <span class="item-name">真实置顶浮窗</span>
            <span class="item-desc">始终悬浮在所有全屏窗口与软件上方</span>
          </div>
          <button
            class="warlock-toggle-pill"
            :class="{ active: alwaysOnTop }"
            @click="$emit('toggle-pin')"
          >
            {{ alwaysOnTop ? '已开启' : '已关闭' }}
          </button>
        </div>
      </div>

      <!-- 系统集成 -->
      <div class="setting-group-box gothic-frame-box">
        <div class="group-title">
          <Zap :size="15" style="color: var(--warlock-soul-green);" />
          <span>系统自启与快捷呼出</span>
        </div>

        <div class="setting-item">
          <div class="item-info">
            <span class="item-name">开机静默唤醒</span>
            <span class="item-desc">随 Windows 开机自动常驻在后台</span>
          </div>
          <button
            class="warlock-toggle-pill"
            :class="{ active: autostartActive }"
            @click="toggleAutostart"
          >
            {{ autostartActive ? '已开启' : '已关闭' }}
          </button>
        </div>

        <div class="setting-item">
          <div class="item-info">
            <span class="item-name">全局显隐热键</span>
            <span class="item-desc">随时从后台一键呼出或隐藏</span>
          </div>
          <div class="hotkey-badge">
            <Keyboard :size="13" />
            <span>{{ hotkeyText }}</span>
          </div>
        </div>
      </div>

      <!-- 存储与引擎 -->
      <div class="setting-group-box gothic-frame-box" style="grid-column: span 2;">
        <div class="group-title">
          <HardDrive :size="15" style="color: var(--warlock-gold);" />
          <span>底层数据与跨进程共享引擎</span>
        </div>

        <div class="setting-item">
          <div class="item-info">
            <span class="item-name">单一数据源连接状态</span>
            <span class="item-desc">与原 WPF 及小挂件共用同一份 SQLite 数据库</span>
          </div>
          <div class="db-status-pill">
            {{ dbStatus }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.settings-page-container {
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

.settings-header-bar {
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

.settings-card-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}

.setting-group-box {
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.group-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  font-weight: 800;
  color: var(--warlock-gold);
  border-bottom: 1px solid rgba(200, 164, 93, 0.35);
  padding-bottom: 8px;
  text-shadow: 0 0 8px var(--warlock-gold-glow);
}

.setting-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 0;
}

.item-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.item-name {
  font-size: 13px;
  font-weight: 700;
  color: #ffffff;
}

.item-desc {
  font-size: 11px;
  color: #94a3b8;
}

.warlock-toggle-pill {
  background: rgba(14, 8, 20, 0.85);
  border: 1px solid rgba(120, 90, 150, 0.6);
  color: #94a3b8;
  font-size: 12px;
  font-weight: 700;
  padding: 4px 12px;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.warlock-toggle-pill.active {
  background: rgba(72, 255, 72, 0.15);
  border-color: var(--warlock-soul-green);
  color: var(--warlock-soul-green-bright);
  box-shadow: 0 0 8px rgba(72, 255, 72, 0.3);
}

.hotkey-badge {
  display: flex;
  align-items: center;
  gap: 6px;
  background: rgba(14, 8, 20, 0.9);
  border: 1px solid var(--warlock-border-gold);
  border-radius: 4px;
  padding: 4px 10px;
  font-family: var(--font-mono);
  font-size: 12px;
  font-weight: 800;
  color: var(--warlock-gold-bright);
}

.db-status-pill {
  font-family: var(--font-mono);
  font-size: 11px;
  color: var(--warlock-soul-green-bright);
  background: rgba(10, 5, 16, 0.9);
  border: 1px solid rgba(72, 255, 72, 0.3);
  padding: 4px 10px;
  border-radius: 4px;
}
</style>
