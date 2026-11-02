<script setup>
const saving = ref(false);
import { computed, onMounted, reactive, ref } from 'vue';
import { open } from '@tauri-apps/plugin-dialog';
import { Plus, Search, Trash2, Pencil, ImagePlus, X } from 'lucide-vue-next';
import GothicPanel from '../components/GothicPanel.vue';
import ModalShell from '../components/ModalShell.vue';
import { api, fmtDateTime, relativeTime } from '../api';
import { notify } from '../stores/toast';

const records = ref([]);
const categories = ref([]);
const keyword = ref('');
const activeCat = ref('');
const loading = ref(true);
const hasMore = ref(false);

const showEditor = ref(false);
const editingId = ref(null);
const pendingImages = ref([]);
const existingImages = ref([]);
const previews = ref({});
const form = reactive({ title: '', content: '', category: '日常', experience: 0 });

const showDelete = ref(false);
const deleting = ref(null);

const lightboxImg = ref(null);
const lightboxUrl = ref('');

async function openLightbox(img, record) {
  lightboxImg.value = { ...img, recordTitle: record.title };
  lightboxUrl.value = '';
  try { const url = await api.imageDataUrl(img.filePath); if (lightboxImg.value?.id === img.id) lightboxUrl.value = url; } catch(e) { notify.error(e.message); }
}

async function load(append = false) {
  append = append === true;
  loading.value = true;
  try {
    const [list, cats] = await Promise.all([
      api.records(keyword.value, activeCat.value, 30, append ? records.value.length : 0),
      api.recordCategories(),
    ]);
    hasMore.value = list.length === 30;
    records.value = append ? [...records.value, ...list] : list;
    categories.value = cats;
    await loadPreviews(list);
  } catch (e) {
    notify.error('加载记录失败：' + e.message);
  } finally {
    loading.value = false;
  }
}

async function loadPreviews(list) {
  const map = { ...previews.value };
  for (const r of list) {
    for (const img of r.images || []) {
      if (map[img.id]) continue;
      try {
        map[img.id] = await api.imageDataUrl(img.filePath, true);
      } catch {
        map[img.id] = '';
      }
    }
  }
  previews.value = map;
}

function openCreate() {
  editingId.value = null;
  Object.assign(form, { title: '', content: '', category: '日常', experience: 0 });
  pendingImages.value = [];
  existingImages.value = [];
  showEditor.value = true;
}

function openEdit(rec) {
  editingId.value = rec.id;
  Object.assign(form, {
    title: rec.title,
    content: rec.content || '',
    category: rec.category || '日常',
    experience: rec.experience || 0,
  });
  pendingImages.value = [];
  existingImages.value = (rec.images || []).map((i) => ({ ...i }));
  showEditor.value = true;
}

async function pickImages() {
  try {
    const picked = await open({
      multiple: true,
      filters: [{ name: '图片', extensions: ['png', 'jpg', 'jpeg', 'webp', 'gif', 'bmp'] }],
    });
    if (!picked) return;
    const arr = Array.isArray(picked) ? picked : [picked];
    pendingImages.value.push(...arr);
  } catch (e) {
    notify.error('选择图片失败：' + e.message);
  }
}

function removeExisting(id) {
  existingImages.value = existingImages.value.filter((i) => i.id !== id);
}

async function submit() {
  if (!form.title.trim()) {
    notify.warn('记录标题不能为空');
    return;
  }
  if (saving.value) return;
  saving.value = true;
  try {
    if (editingId.value) {
      await api.updateRecord({
        id: editingId.value,
        title: form.title.trim(),
        content: form.content,
        category: form.category,
        experience: Number(form.experience) || 0,
        addImagePaths: pendingImages.value,
        removeImageIds: (records.value.find((r) => r.id === editingId.value)?.images || [])
          .filter((i) => !existingImages.value.some((e) => e.id === i.id))
          .map((i) => i.id),
      });
      notify.success('记录已更新');
    } else {
      await api.createRecord({
        title: form.title.trim(),
        content: form.content,
        category: form.category,
        experience: Number(form.experience) || 0,
        imagePaths: pendingImages.value,
      });
      notify.success('记录已铭刻');
    }
    showEditor.value = false;
    previews.value = {};
    await load();
  } catch (e) {
    notify.error('保存失败：' + e.message);
  } finally { saving.value = false; }
}

function askDelete(rec) {
  deleting.value = rec;
  showDelete.value = true;
}

async function confirmDelete() {
  try {
    await api.deleteRecord(deleting.value.id);
    notify.info('记录已抹除');
    showDelete.value = false;
    await load();
  } catch (e) {
    notify.error('删除失败：' + e.message);
  }
}

const totalExp = computed(() => records.value.reduce((s, r) => s + (r.experience || 0), 0));

onMounted(load);
</script>

<template>
  <div class="records-view">
    <GothicPanel title="行为记录" subtitle="LIFE RECORDS" class="list-panel">
      <template #actions>
        <div class="head-actions">
          <div class="search">
            <Search :size="12" class="dim" />
            <input
              v-model="keyword"
              class="search-input"
              placeholder="搜索标题或内容…"
              @keyup.enter="load"
            />
          </div>
          <button class="btn btn-ghost btn-sm" @click="load">搜索</button>
          <button class="btn btn-primary btn-sm" @click="openCreate"><Plus :size="13" /> 新建记录</button>
        </div>
      </template>

      <div class="cat-row">
        <button class="cat-chip" :class="{ active: !activeCat }" @click="activeCat = ''; load()">全部</button>
        <button
          v-for="c in categories"
          :key="c"
          class="cat-chip"
          :class="{ active: activeCat === c }"
          @click="activeCat = c; load()"
        >{{ c }}</button>
        <span class="sum">已加载 {{ records.length }} 条 · 累计 {{ totalExp }} EXP</span>
      </div>

      <div class="record-list scroll">
        <div v-if="!records.length && !loading" class="empty">
          还没有行为记录。每一次行动都值得被铭刻。
        </div>
        <article v-for="r in records" :key="r.id" class="record">
          <div class="r-line"></div>
          <div class="r-head">
            <h4>{{ r.title }}</h4>
            <span class="r-cat">{{ r.category }}</span>
            <span v-if="r.experience" class="r-exp num">+{{ r.experience }}</span>
            <span class="r-time">{{ relativeTime(r.createdAt) }}</span>
            <div class="r-actions">
              <button class="ico-btn" title="编辑" @click="openEdit(r)"><Pencil :size="12" /></button>
              <button class="ico-btn" title="删除" @click="askDelete(r)"><Trash2 :size="12" /></button>
            </div>
          </div>
          <p v-if="r.content" class="r-content">{{ r.content }}</p>
          <div v-if="r.images?.length" class="r-images">
            <div
              v-for="img in r.images"
              :key="img.id"
              class="thumb"
              title="点击查看大图"
              @click="openLightbox(img, r)"
            >
              <img v-if="previews[img.id]" :src="previews[img.id]" :alt="img.fileName" />
              <span v-else class="thumb-loading">…</span>
            </div>
          </div>
        </article>
        <button v-if="hasMore" class="btn btn-ghost" :disabled="loading" @click="load(true)">加载更多记录</button>
      </div>
    </GothicPanel>

    <!-- 编辑器 -->
    <ModalShell
      v-if="showEditor" :state="{form,pendingImages,existingImages}" :busy="saving"
      :title="editingId ? '编辑行为记录' : '铭刻行为记录'"
      subtitle="LIFE RECORD"
      :width="620"
      @close="showEditor = false"
    >
      <label class="field">
        <span>标题</span>
        <input v-model="form.title" class="input" placeholder="今天做了什么？" />
      </label>
      <label class="field">
        <span>内容</span>
        <textarea v-model="form.content" class="textarea" rows="6" placeholder="记录细节、感受、复盘…"></textarea>
      </label>
      <div class="field-row">
        <label class="field">
          <span>分类</span>
          <input v-model="form.category" class="input" list="cat-list" />
          <datalist id="cat-list">
            <option v-for="c in categories" :key="c" :value="c" />
          </datalist>
        </label>
        <label class="field">
          <span>经验值</span>
          <input v-model.number="form.experience" type="number" class="input" min="0" step="5" />
        </label>
      </div>

      <div class="field">
        <span>图片证据</span>
        <div class="img-grid">
          <div v-for="img in existingImages" :key="img.id" class="img-cell">
            <img v-if="previews[img.id]" :src="previews[img.id]" />
            <button class="img-del" @click="removeExisting(img.id)"><X :size="10" /></button>
          </div>
          <div v-for="(p, i) in pendingImages" :key="'p' + i" class="img-cell new">
            <span class="new-name">{{ p.split(/[\\/]/).pop() }}</span>
            <button class="img-del" @click="pendingImages.splice(i, 1)"><X :size="10" /></button>
          </div>
          <button class="img-add" @click="pickImages"><ImagePlus :size="16" /></button>
        </div>
      </div>

      <template #footer="{ close }">
        <button class="btn btn-ghost" @click="close">取消</button>
        <button class="btn btn-primary" @click="submit">{{ editingId ? '保存' : '铭刻' }}</button>
      </template>
    </ModalShell>

    <ModalShell
      v-if="showDelete"
      title="抹除记录"
      subtitle="此操作不可撤销"
      :width="420"
      danger
      @close="showDelete = false"
    >
      <p class="confirm-text">确定删除「<b>{{ deleting?.title }}</b>」？关联图片也会一并删除。</p>
      <template #footer="{ close }">
        <button class="btn btn-ghost" @click="showDelete = false">取消</button>
        <button class="btn btn-danger" @click="confirmDelete">确认抹除</button>
      </template>
    </ModalShell>

    <!-- 图片大图灯箱 -->
    <ModalShell
      v-if="lightboxImg"
      :title="lightboxImg.recordTitle || lightboxImg.fileName"
      subtitle="EVIDENCE LIGHTBOX"
      :width="760"
      @close="lightboxImg = null"
    >
      <div class="lightbox-frame">
        <img v-if="lightboxUrl" :src="lightboxUrl" :alt="lightboxImg.fileName" />
      </div>
      <p class="lightbox-name">{{ lightboxImg.fileName }}</p>
    </ModalShell>
  </div>
</template>

<style scoped>
.records-view { flex: 1; min-width: 0; display: flex; }
.list-panel { flex: 1; }
.head-actions { display: flex; align-items: center; gap: 8px; }
.search {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 0 9px;
  height: 26px;
  background: rgba(0, 0, 0, 0.5);
  border: 1px solid #2b2412;
}
.search-input {
  width: 180px;
  font-size: 12px;
  color: var(--t-title);
  background: transparent;
  border: none;
  outline: none;
  user-select: text;
}
.cat-row {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
  padding-bottom: 4px;
  border-bottom: 1px solid rgba(109, 91, 42, 0.2);
}
.cat-chip {
  padding: 2px 10px;
  font-size: 12px;
  color: var(--t-time);
  background: transparent;
  border: 1px solid rgba(109, 91, 42, 0.35);
  cursor: pointer;
}
.cat-chip.active { color: var(--c-glow); border-color: var(--c-soul); background: rgba(124, 255, 31, 0.08); }
.sum { margin-left: auto; font-size: 12px; color: var(--t-disabled); }

.record-list {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding-right: 4px;
}
.record {
  position: relative;
  padding: 10px 12px 10px 16px;
  background: rgba(13, 19, 12, 0.8);
  border: 1px solid rgba(109, 91, 42, 0.32);
}
.r-line {
  position: absolute;
  left: 0;
  top: 8px;
  bottom: 8px;
  width: 2px;
  background: linear-gradient(180deg, var(--c-soul), transparent);
  opacity: 0.5;
}
.r-head { display: flex; align-items: center; gap: 10px; }
.r-head h4 { font-size: 13.5px; color: var(--t-title); font-weight: 600; }
.r-cat {
  padding: 0 6px;
  font-size: 12px;
  color: var(--t-sub);
  border: 1px solid rgba(109, 91, 42, 0.45);
}
.r-exp { font-size: 12px; color: var(--c-soul); }
.r-time { font-size: 12px; color: var(--t-time); margin-left: auto; }
.r-actions { display: flex; gap: 2px; opacity: 0; transition: opacity 0.15s; }
.record:hover .r-actions { opacity: 1; }
.r-content {
  margin-top: 6px;
  font-size: 12.5px;
  color: var(--t-body);
  line-height: 1.75;
  white-space: pre-wrap;
  user-select: text;
}
.r-images { display: flex; gap: 6px; margin-top: 8px; flex-wrap: wrap; }
.thumb {
  width: 64px;
  height: 64px;
  border: 1px solid rgba(109, 91, 42, 0.5);
  overflow: hidden;
  background: #000;
  cursor: pointer;
  transition: all 0.16s;
}
.thumb:hover {
  border-color: var(--c-soul);
  box-shadow: 0 0 10px rgba(124, 255, 31, 0.4);
  transform: scale(1.04);
}
.thumb img { width: 100%; height: 100%; object-fit: cover; display: block; }
.thumb-loading { display: block; text-align: center; line-height: 62px; color: var(--t-disabled); }

.lightbox-frame {
  max-height: 65vh;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #000;
  border: 1px solid rgba(109, 91, 42, 0.6);
  box-shadow: inset 0 0 24px rgba(0, 0, 0, 0.9);
}
.lightbox-frame img {
  max-width: 100%;
  max-height: 65vh;
  object-fit: contain;
  display: block;
}
.lightbox-name {
  margin-top: 8px;
  font-size: 12px;
  color: var(--t-time);
  word-break: break-all;
  font-family: var(--f-mono);
}

.ico-btn {
  width: 20px; height: 20px;
  display: flex; align-items: center; justify-content: center;
  color: var(--t-sub); background: transparent; border: 1px solid transparent; cursor: pointer;
}
.ico-btn:hover { color: var(--c-glow); border-color: rgba(109, 91, 42, 0.6); }

.field { display: flex; flex-direction: column; gap: 5px; }
.field > span { font-size: 12px; color: var(--t-sub); }
.field-row { display: grid; grid-template-columns: 2fr 1fr; gap: 12px; }

.img-grid { display: flex; flex-wrap: wrap; gap: 8px; }
.img-cell {
  position: relative;
  width: 72px; height: 72px;
  border: 1px solid rgba(109, 91, 42, 0.5);
  overflow: hidden;
  background: #000;
}
.img-cell img { width: 100%; height: 100%; object-fit: cover; }
.img-cell.new { display: flex; align-items: center; justify-content: center; padding: 4px; }
.new-name { font-size: 12px; color: var(--t-sub); word-break: break-all; text-align: center; }
.img-del {
  position: absolute; top: 1px; right: 1px;
  width: 16px; height: 16px;
  display: flex; align-items: center; justify-content: center;
  color: #fff; background: rgba(224, 90, 74, 0.85); border: none; cursor: pointer;
}
.img-add {
  width: 72px; height: 72px;
  display: flex; align-items: center; justify-content: center;
  color: var(--t-sub);
  background: rgba(0, 0, 0, 0.4);
  border: 1px dashed rgba(109, 91, 42, 0.6);
  cursor: pointer;
}
.img-add:hover { color: var(--c-soul); border-color: var(--c-soul); }
.empty { padding: 40px 0; text-align: center; color: var(--t-disabled); font-size: 12.5px; }
.confirm-text { font-size: 13px; line-height: 1.9; }
.confirm-text b { color: var(--t-title); }
</style>
