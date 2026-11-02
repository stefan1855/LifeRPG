<script setup>
const saving = ref(false);
import { computed, onMounted, onUnmounted, watch, ref } from 'vue';
import { open } from '@tauri-apps/plugin-dialog';
import { Download, Flame, Trophy, TrendingUp, CalendarCheck, Sparkles } from 'lucide-vue-next';
import GothicPanel from '../components/GothicPanel.vue';
import { api, quadrantMeta } from '../api';
import { notify } from '../stores/toast';
import ModalShell from '../components/ModalShell.vue';

const stats = ref(null);
const loading = ref(true);

async function load() {
  loading.value = true;
  try {
    stats.value = await api.archiveStats(30);
  } catch (e) {
    notify.error('加载统计失败：' + e.message);
  } finally {
    loading.value = false;
  }
}

const daily = computed(() => stats.value?.daily || []);
const maxDaily = computed(() => {
  let m = 1;
  for (const d of daily.value) {
    const v = (d.taskCount || 0) + (d.recordCount || 0);
    if (v > m) m = v;
  }
  return m;
});

const completionRate = computed(() => {
  const s = stats.value;
  if (!s || !s.totalTasks) return 0;
  return Math.round((s.completedTasks / s.totalTasks) * 100);
});

async function exportMd() {
  try {
    const dir = await open({ directory: true, title: '选择 Markdown 导出目录' });
    if (!dir) return;
    const path = await api.exportMarkdown(dir);
    notify.success('已导出：' + path);
  } catch (e) {
    notify.error('导出失败：' + e.message);
  }
}

/** AI 复盘：汇总数据 → 军师成文 → 可编辑 → 存为行为记录 */
const aiBusy = ref(false);
const aiText = ref('');
const showAiReview = ref(false);
let reviewRevision=0;
watch(showAiReview,(open)=>{if(!open)reviewRevision++;},{flush:"sync"});
onUnmounted(()=>reviewRevision++);

async function runAiReview() {
  if(aiBusy.value)return;
  const revision=++reviewRevision;
  aiBusy.value = true;
  showAiReview.value = true;
  aiText.value = '';
  try {
    const result=await api.aiReview(7);
    if(revision===reviewRevision)aiText.value=result;
  } catch (e) {
    notify.error('AI 复盘失败：' + e.message);
    if(revision===reviewRevision)showAiReview.value = false;
  } finally {
    aiBusy.value = false;
  }
}

async function saveAiReview() {
  if (!aiText.value.trim()) return;
  if (saving.value) return;
  saving.value = true;
  try {
    const today = new Date();
    const stamp = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`;
    await api.createRecord({
      title: `AI 周复盘 ${stamp}`,
      content: aiText.value.trim(),
      category: '复盘',
      experience: 0,
      imagePaths: [],
    });
    notify.success('复盘已存入行为记录');
    showAiReview.value = false;
  } catch (e) {
    notify.error('保存失败：' + e.message);
  } finally { saving.value = false; }
}

onMounted(load);
</script>

<template>
  <div class="archive-view">
    <div class="kpi-row">
      <div class="kpi">
        <Trophy :size="15" class="k-ico" />
        <span class="k-num num">{{ completionRate }}%</span>
        <span class="k-label">任务完成率</span>
        <span class="k-sub">{{ stats?.completedTasks || 0 }} / {{ stats?.totalTasks || 0 }}</span>
      </div>
      <div class="kpi">
        <Flame :size="15" class="k-ico" />
        <span class="k-num num">{{ stats?.streakDays || 0 }}</span>
        <span class="k-label">连续活跃天数</span>
        <span class="k-sub">保持不断，灵魂不熄</span>
      </div>
      <div class="kpi">
        <TrendingUp :size="15" class="k-ico" />
        <span class="k-num num">{{ stats?.totalExperience || 0 }}</span>
        <span class="k-label">累计总经验</span>
        <span class="k-sub">来自全部已结算事件</span>
      </div>
      <div class="kpi">
        <CalendarCheck :size="15" class="k-ico" />
        <span class="k-num num">{{ stats?.totalRecords || 0 }}</span>
        <span class="k-label">行为记录</span>
        <span class="k-sub">{{ stats?.totalImages || 0 }} 张图片证据</span>
      </div>
    </div>

    <div class="lower">
      <GothicPanel title="近 30 天活跃度" subtitle="ACTIVITY" class="chart-panel">
        <template #actions>
          <button class="btn btn-primary btn-sm" :disabled="aiBusy" @click="runAiReview">
            <Sparkles :size="12" /> {{ aiBusy ? '军师推演中…' : 'AI 复盘' }}
          </button>
          <button class="btn btn-ghost btn-sm" @click="exportMd"><Download :size="12" /> 导出 Markdown</button>
        </template>
        <div class="chart">
          <div v-for="d in daily" :key="d.date" class="bar-col" :title="`${d.date} · 任务${d.taskCount} · 记录${d.recordCount}`">
            <div class="bar-stack">
              <div
                class="bar task"
                :style="{ height: ((d.taskCount || 0) / maxDaily) * 100 + '%' }"
              ></div>
              <div
                class="bar record"
                :style="{ height: ((d.recordCount || 0) / maxDaily) * 100 + '%' }"
              ></div>
            </div>
            <span class="bar-date">{{ d.date.slice(8) }}</span>
          </div>
        </div>
        <div class="legend">
          <span><i class="dot task"></i>完成任务</span>
          <span><i class="dot record"></i>行为记录</span>
          <span class="overdue">逾期未完成：{{ stats?.overdueTasks || 0 }}</span>
        </div>
      </GothicPanel>

      <GothicPanel title="分类分布" subtitle="CATEGORY" class="cat-panel">
        <div v-if="!stats?.categoryBreakdown?.length" class="empty">还没有任务分类数据</div>
        <div v-for="c in stats?.categoryBreakdown || []" :key="c.name" class="cat-row">
          <span class="cat-name">{{ c.name }}</span>
          <div class="cat-bar">
            <i :style="{ width: Math.min(100, (c.count / (stats.totalTasks || 1)) * 100) + '%' }"></i>
          </div>
          <span class="cat-count num">{{ c.count }}</span>
          <span class="cat-exp num">+{{ c.experience }}</span>
        </div>
      </GothicPanel>
    </div>

    <!-- AI 复盘结果 -->
    <ModalShell
      v-if="showAiReview" :state="aiText" :initial-dirty="!!aiText" :busy="saving"
      title="AI 军师 · 本周复盘"
      subtitle="WEEKLY REVIEW"
      :width="640"
      @close="showAiReview = false"
    >
      <div v-if="aiBusy" class="ai-loading">
        <span class="sigil">☠</span>
        <span>军师正在翻阅你的灵魂卷轴…</span>
      </div>
      <textarea v-else v-model="aiText" class="textarea ai-review-text" rows="16"></textarea>
      <template #footer="{ close }">
        <button class="btn btn-ghost" @click="close">关闭</button>
        <button class="btn btn-primary" :disabled="aiBusy || !aiText.trim()" @click="saveAiReview">
          存入行为记录
        </button>
      </template>
    </ModalShell>
  </div>
</template>

<style scoped>
.ai-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
  padding: 40px 0;
  color: var(--t-time);
  font-size: 12.5px;
}
.ai-loading .sigil {
  font-size: 24px;
  color: var(--c-glow);
  text-shadow: var(--glow-soul-strong);
  animation: soulPulse 1.6s ease-in-out infinite;
}
.ai-review-text {
  font-size: 12.5px;
  line-height: 1.9;
  min-height: 320px;
}
.archive-view {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 14px;
}
.kpi-row {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 12px;
  flex: 0 0 auto;
}
.kpi {
  display: flex;
  flex-direction: column;
  gap: 1px;
  padding: 12px 14px;
  background: linear-gradient(180deg, rgba(14, 20, 12, 0.95), rgba(5, 8, 5, 0.98));
  border: 1px solid rgba(109, 91, 42, 0.45);
  box-shadow: var(--sh-inset);
}
.k-ico { color: var(--c-gold); margin-bottom: 2px; }
.k-num { font-size: 24px; font-weight: 700; color: var(--t-num); }
.k-label { font-size: 12px; color: var(--t-sub); letter-spacing: 0.06em; }
.k-sub { font-size: 12px; color: var(--t-disabled); }

.lower {
  flex: 1;
  min-height: 0;
  display: grid;
  grid-template-columns: 1.6fr 1fr;
  gap: 14px;
}
.chart-panel, .cat-panel { min-height: 0; }
.chart {
  flex: 1;
  display: flex;
  align-items: flex-end;
  gap: 3px;
  min-height: 160px;
  padding: 8px 0 0;
  border-bottom: 1px solid rgba(109, 91, 42, 0.3);
}
.bar-col {
  flex: 1;
  height: 100%;
  display: flex;
  flex-direction: column;
  justify-content: flex-end;
  align-items: center;
  gap: 3px;
}
.bar-stack {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  justify-content: flex-end;
  gap: 1px;
}
.bar {
  width: 100%;
  min-height: 0;
  transition: height 0.5s ease;
}
.bar.task { background: linear-gradient(180deg, #7cff1f, #3f9c0c); box-shadow: 0 0 6px rgba(124,255,31,0.35); }
.bar.record { background: linear-gradient(180deg, #c0a64c, #6d5b2a); }
.bar-date { font-size: 12px; color: var(--t-disabled); font-family: var(--f-mono); }
.legend {
  display: flex;
  align-items: center;
  gap: 14px;
  font-size: 12px;
  color: var(--t-time);
  padding-top: 6px;
}
.dot { display: inline-block; width: 7px; height: 7px; margin-right: 4px; }
.dot.task { background: var(--c-soul); }
.dot.record { background: var(--c-gold); }
.overdue { margin-left: auto; color: var(--t-warn); }

.cat-row {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  padding: 4px 0;
}
.cat-name { width: 70px; color: var(--t-body); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.cat-bar {
  flex: 1;
  height: 6px;
  background: rgba(0, 0, 0, 0.6);
  border: 1px solid rgba(109, 91, 42, 0.3);
}
.cat-bar i { display: block; height: 100%; background: linear-gradient(90deg, var(--c-bronze), var(--c-gold-bright)); }
.cat-count { width: 28px; text-align: right; color: var(--t-sub); }
.cat-exp { width: 46px; text-align: right; color: var(--c-soul); font-size: 12px; }
.empty { padding: 30px 0; text-align: center; color: var(--t-disabled); font-size: 12px; }
</style>
