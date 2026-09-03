<script setup>
import { ref, onMounted } from 'vue';
import { invoke } from '@tauri-apps/api/core';
import { ATTRIBUTE_META } from '../utils/timeParser';
import { History, RefreshCw } from 'lucide-vue-next';

const logs = ref([]);
const loading = ref(false);

async function fetchLogs() {
  loading.value = true;
  try {
    const res = await invoke('rpg_get_logs', { limit: 100 });
    logs.value = res || [];
  } catch (e) {
    console.error('获取日志失败:', e);
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  fetchLogs();
});
</script>

<template>
  <div class="records-page-container">
    <div class="records-header-bar gothic-frame-box">
      <div class="header-title-flex">
        <span class="skull-sym">☠</span>
        <span class="page-title">灵 能 汲 取 与 行 为 流 水 审 计</span>
        <span class="skull-sym">☠</span>
      </div>
      <button class="warlock-btn" @click="fetchLogs" :class="{ disabled: loading }">
        <RefreshCw :size="13" :class="{ 'spin-icon': loading }" />
        <span>刷新流水</span>
      </button>
    </div>

    <div class="records-table-card gothic-frame-box">
      <div class="table-scroll-area">
        <table class="warlock-table">
          <thead>
            <tr>
              <th style="width: 170px;">时间</th>
              <th style="width: 140px;">契约属性</th>
              <th style="width: 120px;">灵能收益</th>
              <th>事由 / 契约来源</th>
              <th style="width: 120px;">来源类型</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="logs.length === 0">
              <td colspan="5" class="empty-cell">
                暂无灵魂能量流水记录
              </td>
            </tr>
            <tr v-for="log in logs" :key="log.id" class="table-row">
              <td class="font-mono time-col">{{ log.createdAt ? log.createdAt.replace('T', ' ').slice(0, 19) : '-' }}</td>
              <td>
                <span class="attr-pill" :style="{ color: ATTRIBUTE_META[log.attributeKey || log.attribute_key]?.color }">
                  {{ ATTRIBUTE_META[log.attributeKey || log.attribute_key]?.symbol }} {{ ATTRIBUTE_META[log.attributeKey || log.attribute_key]?.name || log.attributeKey }}
                </span>
              </td>
              <td class="font-mono exp-gain">+{{ log.experienceDelta || log.experience_delta || 0 }} EXP</td>
              <td class="log-reason">{{ log.reason || '-' }}</td>
              <td class="font-mono text-muted">{{ log.sourceType || log.source_type || 'Task' }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<style scoped>
.records-page-container {
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

.records-header-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 16px;
  flex-shrink: 0;
}

.header-title-flex {
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

.records-table-card {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.table-scroll-area {
  flex: 1;
  overflow-y: auto;
}

.warlock-table {
  width: 100%;
  border-collapse: collapse;
  text-align: left;
}

.warlock-table thead {
  position: sticky;
  top: 0;
  background: linear-gradient(180deg, #241434 0%, #160c22 100%);
  border-bottom: 1px solid var(--warlock-border-gold);
  z-index: 10;
}

.warlock-table th {
  padding: 10px 14px;
  font-size: 12px;
  font-weight: 800;
  color: var(--warlock-gold);
  letter-spacing: 1px;
}

.table-row {
  border-bottom: 1px solid rgba(120, 90, 150, 0.3);
  transition: background 0.2s ease;
}

.table-row:hover {
  background: rgba(40, 26, 56, 0.7);
}

.warlock-table td {
  padding: 10px 14px;
  font-size: 12px;
  color: #f1f5f9;
}

.empty-cell {
  text-align: center;
  color: #64748b;
  padding: 60px 0;
  font-size: 13px;
}

.time-col {
  color: #cbd5e1;
  font-size: 11px;
}

.attr-pill {
  font-weight: 700;
  font-size: 12px;
}

.exp-gain {
  color: var(--warlock-soul-green-bright);
  font-weight: 800;
  text-shadow: 0 0 6px var(--warlock-soul-green-glow);
}

.log-reason {
  color: #ffffff;
  font-weight: 600;
}

.text-muted {
  color: #94a3b8;
}

.font-mono {
  font-family: var(--font-mono);
}

.spin-icon {
  animation: spin 1s infinite linear;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}
</style>
