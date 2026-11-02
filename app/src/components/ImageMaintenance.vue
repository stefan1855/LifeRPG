<script setup>
import { computed, onMounted, ref } from 'vue';
import GothicPanel from './GothicPanel.vue';
import ModalShell from './ModalShell.vue';
import { api } from '../api';
import { notify } from '../stores/toast';
const busy = ref(false), scan = ref(null), selected = ref([]), batches = ref([]), showConfirm = ref(false), purging = ref(null), confirmation = ref('');
const visibleCount = ref(100);
const selectedFiles = computed(() => (scan.value?.candidates || []).filter(f => selected.value.includes(f.name)));
const size = bytes => bytes >= 1048576 ? `${(bytes / 1048576).toFixed(1)} MB` : `${(bytes / 1024).toFixed(1)} KB`;
const bytes = files => files.reduce((sum, file) => sum + file.bytes, 0);
async function reloadBatches() { batches.value = await api.imageCleanupBatches(); }
async function run(action) {
  if (busy.value) return;
  busy.value = true;
  try { await action(); } catch (e) { notify.error('图片维护失败：' + (e.message || e)); } finally { busy.value = false; }
}
function scanImages() { return run(async () => { scan.value = await api.imageCleanupScan(); selected.value = []; visibleCount.value = 100; await reloadBatches(); }); }
function quarantine() { return run(async () => {
  await api.imageCleanupQuarantine(scan.value.root, selectedFiles.value);
  showConfirm.value = false; scan.value = null; selected.value = []; await reloadBatches();
  notify.success('已移入隔离区，可恢复；永久删除后才会释放磁盘空间');
}); }
function restore(batch) { return run(async () => { const count = await api.imageCleanupRestore(batch.id); await reloadBatches(); scan.value = null; notify.success(`已恢复 ${count} 张图片`); }); }
function purge() { return run(async () => { const count = await api.imageCleanupPurge(purging.value.id, confirmation.value); purging.value = null; confirmation.value = ''; await reloadBatches(); notify.success(`已永久删除 ${count} 张隔离图片`); }); }
onMounted(() => run(reloadBatches));
</script>
<template>
  <GothicPanel title="图片整理" subtitle="IMAGE CARE">
    <p class="note">仅列出超过 24 小时、未被记录、任务或头像引用的图片。先隔离，可恢复；确认永久删除后释放磁盘空间。</p>
    <button class="btn btn-ghost" :disabled="busy" @click="scanImages">{{ busy ? '处理中…' : '扫描未引用图片' }}</button>
    <template v-if="scan">
      <p class="summary">发现 {{ scan.candidates.length }} 张 · {{ size(scan.bytes) }}；近期文件 {{ scan.recent }} 个暂不处理</p>
      <p v-if="scan.missing.length" class="missing">另有 {{ scan.missing.length }} 个引用文件缺失，请先核对备份与原始图片。<span :title="scan.missing.join('\n')">{{ scan.missing.slice(0, 3).join('、') }}</span></p>
      <template v-if="scan.candidates.length">
        <div class="actions">
          <button class="btn btn-ghost btn-sm" :disabled="busy" @click="selected = scan.candidates.map(f => f.name)">选择全部</button>
          <button class="btn btn-ghost btn-sm" :disabled="busy" @click="selected = []">取消选择</button>
          <span>已选 {{ selectedFiles.length }} 张 · {{ size(bytes(selectedFiles)) }}</span>
        </div>
        <div class="file-list scroll">
          <label v-for="file in scan.candidates.slice(0, visibleCount)" :key="file.name" class="file-row">
            <input v-model="selected" type="checkbox" :value="file.name" :disabled="busy" />
            <span :title="file.name">{{ file.name }}</span><small>{{ size(file.bytes) }}</small>
          </label>
          <button v-if="visibleCount < scan.candidates.length" class="btn btn-ghost btn-sm" @click="visibleCount += 100">显示更多</button>
        </div>
        <button class="btn btn-primary" :disabled="busy || !selected.length" @click="showConfirm = true">将所选图片移入隔离区</button>
      </template>
      <p v-else class="note">当前没有可整理的未引用图片。</p>
    </template>
    <details v-if="batches.length" class="quarantine">
      <summary>隔离区 · {{ batches.reduce((n, b) => n + b.files.length, 0) }} 张 · {{ size(batches.reduce((n, b) => n + bytes(b.files), 0)) }}</summary>
      <div v-for="batch in batches" :key="batch.id" class="batch">
        <span>{{ new Date(batch.createdAt).toLocaleString() }} · {{ batch.files.length }} 张 · {{ size(bytes(batch.files)) }}</span>
        <details><summary>查看文件清单</summary><div class="file-list scroll"><div v-for="f in batch.files" :key="f.name" class="file-name">{{ f.name }}</div></div></details>
        <div class="actions">
          <button class="btn btn-ghost btn-sm" :disabled="busy" @click="restore(batch)">恢复此批图片</button>
          <button class="btn btn-danger btn-sm" :disabled="busy" @click="purging = batch; confirmation = ''">永久删除此批</button>
        </div>
      </div>
    </details>
  </GothicPanel>
  <ModalShell v-if="showConfirm" title="移入图片隔离区" @close="!busy && (showConfirm = false)">
    <p>将隔离 {{ selectedFiles.length }} 张未引用图片（{{ size(bytes(selectedFiles)) }}）。隔离区不计入后续备份，可以在这里恢复。</p>
    <template #footer><button class="btn btn-ghost" :disabled="busy" @click="showConfirm = false">取消</button><button class="btn btn-primary" :disabled="busy" @click="quarantine">确认隔离</button></template>
  </ModalShell>
  <ModalShell v-if="purging" title="永久删除隔离图片" danger @close="!busy && (purging = null)">
    <p>将永久删除此批 {{ purging.files.length }} 张图片（{{ size(bytes(purging.files)) }}），无法从隔离区恢复。</p>
    <label class="confirm-label">输入“永久删除”确认<input v-model="confirmation" class="input" placeholder="永久删除" :disabled="busy" /></label>
    <template #footer><button class="btn btn-ghost" :disabled="busy" @click="purging = null">取消</button><button class="btn btn-danger" :disabled="busy || confirmation !== '永久删除'" @click="purge">确认永久删除</button></template>
  </ModalShell>
</template>
<style scoped>
.note,.summary,.actions,.batch{font-size:12px;line-height:1.8;color:var(--t-sub)}.missing{font-size:12px;color:var(--t-warn);overflow-wrap:anywhere}.missing span{display:block}.actions{display:flex;flex-wrap:wrap;align-items:center;gap:8px}.file-list{max-height:220px;min-width:0}.file-row{display:flex;align-items:center;gap:8px;padding:6px 0;border-bottom:1px solid #2b2412;font-size:12px}.file-row span{min-width:0;flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.file-row small{flex-shrink:0}.batch{padding:10px 0;border-top:1px solid #2b2412;display:flex;flex-direction:column;gap:8px}.file-name{overflow-wrap:anywhere;font-size: 12px}.quarantine summary{cursor:pointer;color:var(--t-sub);font-size:12px;padding:5px 0}.confirm-label{display:flex;flex-direction:column;gap:8px;font-size:12px}
</style>
