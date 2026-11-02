<script setup>
import { onMounted, ref } from 'vue';
import GothicPanel from '../components/GothicPanel.vue';
import ModalShell from '../components/ModalShell.vue';
import { api, fmtDateTime } from '../api';
import { notify } from '../stores/toast';

const items=ref([]),loading=ref(true),hasMore=ref(false),loadError=ref('');
const preview=ref(null),previewUrl=ref('');
const urls = ref({});

async function load(append = false) {
  append = append === true;
  loading.value = true;
  try {
    loadError.value='';
    const page=await api.imageGallery(50,append?items.value.length:0);
    hasMore.value=page.hasMore;
    items.value=append?[...items.value,...page.items]:page.items;
    const map = {};
    for (const it of items.value) {
      if (urls.value[it.id]) {
        map[it.id] = urls.value[it.id];
        continue;
      }
      try {
        map[it.id] = await api.imageDataUrl(it.filePath, true);
      } catch {
        map[it.id] = '';
      }
    }
    urls.value = map;
  } catch (e) {
    loadError.value=e.message;
    notify.error('加载图片失败：' + e.message);
  } finally {
    loading.value = false;
  }
}

async function openPreview(item) {
  preview.value = item;
  previewUrl.value = '';
  try { const url = await api.imageDataUrl(item.filePath); if (preview.value?.id === item.id) previewUrl.value = url; } catch(e) { notify.error(e.message); }
}

onMounted(load);
</script>

<template>
  <div class="images-view">
    <GothicPanel title="图片证据" subtitle="EVIDENCE" class="main-panel">
      <template #actions>
        <span class="count">已加载 {{ items.length }} 张</span>
      </template>

      <div v-if="!items.length && !loading && !loadError" class="empty">
        还没有图片证据。在「行为记录」里添加图片，它们会汇聚到这里。
      </div>

      <p v-if="loadError" role="alert">{{ loadError }} <button class="btn" :disabled="loading" @click="load(false)">重试</button></p>
      <div class="grid scroll">
        <figure
          v-for="it in items"
          :key="it.id"
          class="cell"
          @click="openPreview(it)"
        >
          <div class="frame">
            <img v-if="urls[it.id]" :src="urls[it.id]" :alt="it.fileName" loading="lazy" />
            <span v-else class="ph">…</span>
          </div>
          <figcaption>
            <span class="cap-title">{{ it.recordTitle }}</span>
            <span class="cap-time">{{ fmtDateTime(it.createdAt) }}</span>
          </figcaption>
        </figure>
      </div>
      <button v-if="hasMore" class="btn btn-ghost" :disabled="loading" @click="load(true)">加载更多历史图片</button>
    </GothicPanel>

    <ModalShell
      v-if="preview"
      :title="preview.recordTitle"
      :subtitle="fmtDateTime(preview.createdAt)"
      :width="760"
      @close="preview = null"
    >
      <div class="big-frame">
        <img v-if="previewUrl" :src="previewUrl" :alt="preview.fileName" />
      </div>
      <p class="path">{{ preview.fileName }}</p>
    </ModalShell>
  </div>
</template>

<style scoped>
.images-view { flex: 1; min-width: 0; display: flex; }
.main-panel { flex: 1; }
.count { font-size: 12px; color: var(--t-time); }
.grid {
  flex: 1;
  min-height: 0;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(168px, 1fr));
  gap: 12px;
  align-content: start;
  padding-right: 4px;
}
.cell { cursor: pointer; }
.frame {
  position: relative;
  aspect-ratio: 4 / 3;
  background: #000;
  border: 1px solid rgba(109, 91, 42, 0.45);
  overflow: hidden;
  transition: all 0.18s;
}
.frame img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
  transition: transform 0.35s ease, filter 0.2s;
  filter: saturate(0.9) brightness(0.92);
}
.cell:hover .frame {
  border-color: var(--c-soul);
  box-shadow: 0 0 16px rgba(124, 255, 31, 0.25);
}
.cell:hover .frame img {
  transform: scale(1.05);
  filter: saturate(1.05) brightness(1);
}
.ph { display: block; text-align: center; line-height: 120px; color: var(--t-disabled); }
figcaption {
  display: flex;
  flex-direction: column;
  gap: 1px;
  margin-top: 5px;
}
.cap-title {
  font-size: 12px;
  color: var(--t-body);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.cap-time { font-size: 12px; color: var(--t-disabled); }
.big-frame {
  display: flex;
  align-items: center;
  justify-content: center;
  background: #000;
  border: 1px solid rgba(109, 91, 42, 0.5);
  max-height: 62vh;
  overflow: hidden;
}
.big-frame img { max-width: 100%; max-height: 62vh; object-fit: contain; display: block; }
.path { font-size: 12px; color: var(--t-disabled); word-break: break-all; }
.empty { padding: 40px 0; text-align: center; color: var(--t-disabled); font-size: 12.5px; }
</style>
