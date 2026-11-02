<script setup>
const saving = ref(false);
import { computed, onMounted, reactive, ref } from 'vue';
import { Plus, Pencil, Trash2, MapPin, Users, Lock, Unlock, ThumbsUp, ThumbsDown } from 'lucide-vue-next';
import GothicPanel from '../components/GothicPanel.vue';
import ModalShell from '../components/ModalShell.vue';
import { api, relativeTime } from '../api';
import { notify } from '../stores/toast';

const npcs = ref([]);
const places = ref([]);
const loading = ref(true);

const showNpcEditor = ref(false);
const npcEditingId = ref(null);
const npcForm = reactive({ name: '', realIdentity: '', category: '普通', influenceType: '中性', linkedDomain: '' });

const showMapEditor = ref(false);
const mapEditingId = ref(null);
const mapForm = reactive({ name: '', locationType: '区域', description: '' });

const showDelete = ref(false);
const deleting = ref(null);
const deleteKind = ref('npc');

async function load() {
  loading.value = true;
  try {
    const [n, m] = await Promise.all([api.npcList(), api.mapList()]);
    npcs.value = n;
    places.value = m;
  } catch (e) {
    notify.error('读取世界数据失败：' + e.message);
  } finally {
    loading.value = false;
  }
}

const unlockedPlaces = computed(() => places.value.filter((p) => p.isUnlocked).length);

function relationTone(v) {
  if (v >= 60) return '#7cff1f';
  if (v >= 20) return '#a8ff4a';
  if (v > -20) return '#a89b75';
  if (v > -60) return '#d0703c';
  return '#e05a4a';
}

function relationLabel(v) {
  if (v >= 60) return '挚友';
  if (v >= 20) return '亲近';
  if (v > -20) return '平常';
  if (v > -60) return '疏远';
  return '敌对';
}

function openNpcCreate() {
  npcEditingId.value = null;
  Object.assign(npcForm, { name: '', realIdentity: '', category: '普通', influenceType: '中性', linkedDomain: '' });
  showNpcEditor.value = true;
}

function openNpcEdit(n) {
  npcEditingId.value = n.id;
  Object.assign(npcForm, {
    name: n.name,
    realIdentity: n.realIdentity || '',
    category: n.category || '普通',
    influenceType: n.influenceType || '中性',
    linkedDomain: n.linkedDomain || '',
  });
  showNpcEditor.value = true;
}

async function submitNpc() {
  if (!npcForm.name.trim()) {
    notify.warn('人物名不能为空');
    return;
  }
  if (saving.value) return;
  saving.value = true;
  try {
    await api.npcSave({ id: npcEditingId.value, ...npcForm });
    notify.success(npcEditingId.value ? '人物已更新' : '人物已登场');
    showNpcEditor.value = false;
    await load();
  } catch (e) {
    notify.error('保存失败：' + e.message);
  } finally { saving.value = false; }
}

async function adjust(n, delta) {
  try {
    const v = await api.npcAdjust(n.id, delta);
    notify.info(`「${n.name}」好感度 → ${v}`);
    await load();
  } catch (e) {
    notify.error('操作失败：' + e.message);
  }
}

function openMapCreate() {
  mapEditingId.value = null;
  Object.assign(mapForm, { name: '', locationType: '区域', description: '' });
  showMapEditor.value = true;
}

function openMapEdit(p) {
  mapEditingId.value = p.id;
  Object.assign(mapForm, {
    name: p.name,
    locationType: p.locationType || '区域',
    description: p.description || '',
  });
  showMapEditor.value = true;
}

async function submitMap() {
  if (!mapForm.name.trim()) {
    notify.warn('地点名不能为空');
    return;
  }
  if (saving.value) return;
  saving.value = true;
  try {
    await api.mapSave({ id: mapEditingId.value, ...mapForm });
    notify.success(mapEditingId.value ? '地点已更新' : '地点已标记');
    showMapEditor.value = false;
    await load();
  } catch (e) {
    notify.error('保存失败：' + e.message);
  } finally { saving.value = false; }
}

async function togglePlace(p) {
  try {
    const on = await api.mapToggle(p.id);
    notify.info(on ? `「${p.name}」已解锁` : `「${p.name}」已封锁`);
    await load();
  } catch (e) {
    notify.error('操作失败：' + e.message);
  }
}

function askDelete(kind, item) {
  deleteKind.value = kind;
  deleting.value = item;
  showDelete.value = true;
}

async function confirmDelete() {
  try {
    if (deleteKind.value === 'npc') {
      await api.npcDelete(deleting.value.id);
      notify.info('人物已退场');
    } else {
      await api.mapDelete(deleting.value.id);
      notify.info('地点已抹除');
    }
    showDelete.value = false;
    await load();
  } catch (e) {
    notify.error('删除失败：' + e.message);
  }
}

onMounted(load);
</script>

<template>
  <div class="world-view">
    <!-- 人物关系 -->
    <GothicPanel title="人物关系" subtitle="RELATIONS" class="npc-panel">
      <template #actions>
        <button class="btn btn-primary btn-sm" @click="openNpcCreate"><Plus :size="13" /> 新增人物</button>
      </template>

      <div v-if="!npcs.length && !loading" class="empty-state">
        <span>还没有人物登场</span>
        <span style="font-size: 12px">把影响你生活的人写进来，记录关系的温度</span>
      </div>

      <div class="npc-list scroll">
        <article v-for="n in npcs" :key="n.id" class="npc">
          <div class="npc-head">
            <span class="npc-avatar">{{ n.name.slice(0, 1) }}</span>
            <div class="npc-title">
              <h4>{{ n.name }}</h4>
              <span class="npc-real">{{ n.realIdentity || '—' }}</span>
            </div>
            <span class="npc-tag">{{ n.category }}</span>
            <span class="npc-rel num" :style="{ color: relationTone(n.relationshipValue) }">
              {{ n.relationshipValue > 0 ? '+' : '' }}{{ n.relationshipValue }}
              <em>{{ relationLabel(n.relationshipValue) }}</em>
            </span>
          </div>

          <div class="npc-bar">
            <i :style="{ width: ((n.relationshipValue + 100) / 2) + '%', background: relationTone(n.relationshipValue) }"></i>
          </div>

          <div class="npc-foot">
            <span class="npc-domain">{{ n.linkedDomain || '未关联领域' }}</span>
            <span class="npc-time">{{ n.lastInteractionAt ? relativeTime(n.lastInteractionAt) : '尚无互动' }}</span>
            <div class="npc-actions">
              <button class="ico-btn good" title="关系 +10" @click="adjust(n, 10)"><ThumbsUp :size="12" /></button>
              <button class="ico-btn bad" title="关系 -10" @click="adjust(n, -10)"><ThumbsDown :size="12" /></button>
              <button class="ico-btn" title="编辑" @click="openNpcEdit(n)"><Pencil :size="12" /></button>
              <button class="ico-btn" title="退场" @click="askDelete('npc', n)"><Trash2 :size="12" /></button>
            </div>
          </div>
        </article>
      </div>
    </GothicPanel>

    <!-- 地图 -->
    <GothicPanel title="世界地图" subtitle="MAP" class="map-panel">
      <template #actions>
        <div class="head-actions">
          <span class="hint">{{ unlockedPlaces }} / {{ places.length }} 已解锁</span>
          <button class="btn btn-primary btn-sm" @click="openMapCreate"><Plus :size="13" /> 新增地点</button>
        </div>
      </template>

      <div v-if="!places.length && !loading" class="empty-state">
        <span>地图还是一片迷雾</span>
        <span style="font-size: 12px">把你想去或已抵达的地方标上去</span>
      </div>

      <div class="map-grid scroll">
        <article
          v-for="p in places"
          :key="p.id"
          class="place"
          :class="{ unlocked: p.isUnlocked }"
        >
          <header class="pl-head">
            <MapPin :size="13" class="pl-ico" />
            <h4>{{ p.name }}</h4>
            <span class="pl-type">{{ p.locationType }}</span>
          </header>
          <p class="pl-desc">{{ p.description || '暂无描述' }}</p>
          <div class="pl-actions">
            <button class="btn btn-sm" :class="p.isUnlocked ? 'btn-ghost' : 'btn-primary'" @click="togglePlace(p)">
              <component :is="p.isUnlocked ? Lock : Unlock" :size="11" />
              {{ p.isUnlocked ? '封锁' : '解锁' }}
            </button>
            <button class="ico-btn" title="编辑" @click="openMapEdit(p)"><Pencil :size="12" /></button>
            <button class="ico-btn" title="删除" @click="askDelete('map', p)"><Trash2 :size="12" /></button>
          </div>
        </article>
      </div>
    </GothicPanel>

    <!-- 人物编辑 -->
    <ModalShell
      v-if="showNpcEditor" :state="npcForm" :busy="saving"
      :title="npcEditingId ? '编辑人物' : '人物登场'"
      subtitle="RELATION"
      :width="520"
      @close="showNpcEditor = false"
    >
      <div class="field-row">
        <label class="field">
          <span>游戏内称谓</span>
          <input v-model="npcForm.name" class="input" placeholder="例如：老周" />
        </label>
        <label class="field">
          <span>现实身份</span>
          <input v-model="npcForm.realIdentity" class="input" placeholder="例如：直属上级" />
        </label>
      </div>
      <div class="field-row">
        <label class="field">
          <span>分类</span>
          <input v-model="npcForm.category" class="input" placeholder="家人 / 同事 / 客户" />
        </label>
        <label class="field">
          <span>影响类型</span>
          <input v-model="npcForm.influenceType" class="input" placeholder="助力 / 中性 / 阻力" />
        </label>
      </div>
      <label class="field">
        <span>关联领域</span>
        <input v-model="npcForm.linkedDomain" class="input" placeholder="例如：工作、家庭" />
      </label>
      <template #footer="{ close }">
        <button class="btn btn-ghost" @click="close">取消</button>
        <button class="btn btn-primary" @click="submitNpc">{{ npcEditingId ? '保存' : '登场' }}</button>
      </template>
    </ModalShell>

    <!-- 地点编辑 -->
    <ModalShell
      v-if="showMapEditor" :state="mapForm" :busy="saving"
      :title="mapEditingId ? '编辑地点' : '标记地点'"
      subtitle="LOCATION"
      :width="500"
      @close="showMapEditor = false"
    >
      <div class="field-row">
        <label class="field">
          <span>地点名</span>
          <input v-model="mapForm.name" class="input" placeholder="例如：书房" />
        </label>
        <label class="field">
          <span>类型</span>
          <input v-model="mapForm.locationType" class="input" placeholder="区域 / 据点 / 秘境" />
        </label>
      </div>
      <label class="field">
        <span>描述</span>
        <textarea v-model="mapForm.description" class="textarea" rows="3" placeholder="这里发生过什么"></textarea>
      </label>
      <template #footer="{ close }">
        <button class="btn btn-ghost" @click="close">取消</button>
        <button class="btn btn-primary" @click="submitMap">{{ mapEditingId ? '保存' : '标记' }}</button>
      </template>
    </ModalShell>

    <ModalShell
      v-if="showDelete"
      :title="deleteKind === 'npc' ? '人物退场' : '抹除地点'"
      subtitle="此操作不可撤销"
      :width="420"
      danger
      @close="showDelete = false"
    >
      <p class="confirm-text">确定删除「<b>{{ deleting?.name }}</b>」？</p>
      <template #footer="{ close }">
        <button class="btn btn-ghost" @click="showDelete = false">取消</button>
        <button class="btn btn-danger" @click="confirmDelete">确认删除</button>
      </template>
    </ModalShell>
  </div>
</template>

<style scoped>
.world-view {
  flex: 1;
  min-width: 0;
  display: grid;
  grid-template-columns: 1.15fr 0.85fr;
  gap: 14px;
}
.npc-panel, .map-panel { min-height: 0; }
/* 地图面板铺羊皮纸底纹，和人物面板区分开 */
.map-panel {
  background:
    url('../assets/textures/mat_parchment.webp') center / 340px,
    radial-gradient(ellipse 100% 60% at 50% 0%, rgba(46, 36, 16, 0.5), transparent 70%),
    linear-gradient(180deg, rgba(14, 18, 10, 0.95), rgba(6, 8, 4, 0.98));
  background-blend-mode: soft-light, normal, normal;
}
.head-actions { display: flex; align-items: center; gap: 10px; }
.hint { font-size: 12px; color: var(--t-time); }

.npc-list, .map-grid {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  gap: 9px;
  padding-right: 4px;
}
.npc {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 10px 12px;
  background: linear-gradient(180deg, rgba(16, 22, 13, 0.9), rgba(7, 10, 6, 0.95));
  border: 1px solid rgba(109, 91, 42, 0.38);
}
.npc:hover { border-color: rgba(192, 166, 76, 0.55); }
.npc-head { display: flex; align-items: center; gap: 9px; }
.npc-avatar {
  width: 26px; height: 26px;
  display: flex; align-items: center; justify-content: center;
  font-size: 13px;
  color: var(--t-title);
  background: radial-gradient(circle, rgba(124, 255, 31, 0.14), rgba(0, 0, 0, 0.9));
  border: 1px solid rgba(192, 166, 76, 0.5);
  transform: rotate(45deg);
}
.npc-avatar::first-letter { transform: rotate(-45deg); }
.npc-title { flex: 1; min-width: 0; }
.npc-title h4 { font-size: 13px; color: var(--t-title); }
.npc-real { font-size: 12px; color: var(--t-time); }
.npc-tag {
  padding: 0 6px;
  font-size: 12px;
  color: var(--t-sub);
  border: 1px solid rgba(109, 91, 42, 0.45);
}
.npc-rel { font-size: 12px; font-weight: 700; }
.npc-rel em { font-style: normal; font-size: 12px; margin-left: 4px; opacity: 0.8; }
.npc-bar {
  height: 5px;
  background: rgba(0, 0, 0, 0.7);
  border: 1px solid rgba(109, 91, 42, 0.35);
  overflow: hidden;
}
.npc-bar i { display: block; height: 100%; transition: width 0.4s ease; box-shadow: 0 0 8px currentColor; }
.npc-foot { display: flex; align-items: center; gap: 10px; font-size: 12px; color: var(--t-time); }
.npc-domain { flex: 1; }
.npc-actions { display: flex; gap: 2px; }
.ico-btn {
  width: 20px; height: 20px;
  display: flex; align-items: center; justify-content: center;
  color: var(--t-sub); background: transparent; border: 1px solid transparent; cursor: pointer;
}
.ico-btn:hover { color: var(--c-glow); border-color: rgba(109, 91, 42, 0.6); }
.ico-btn.good:hover { color: var(--c-soul); }
.ico-btn.bad:hover { color: var(--t-danger); }

.map-grid { display: grid; grid-template-columns: 1fr; align-content: start; }
.place {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 10px 12px;
  background: linear-gradient(180deg, rgba(14, 20, 12, 0.9), rgba(6, 9, 5, 0.95));
  border: 1px solid rgba(109, 91, 42, 0.35);
  opacity: 0.7;
  transition: all 0.16s;
}
.place.unlocked {
  opacity: 1;
  border-color: rgba(124, 255, 31, 0.4);
  box-shadow: inset 0 0 16px rgba(124, 255, 31, 0.08);
}
.pl-head { display: flex; align-items: center; gap: 7px; }
.pl-ico { color: var(--c-gold); }
.pl-head h4 { flex: 1; font-size: 13px; color: var(--t-title); }
.pl-type { font-size: 12px; color: var(--t-time); letter-spacing: 0.1em; }
.pl-desc { font-size: 12px; color: var(--t-time); line-height: 1.7; }
.pl-actions { display: flex; align-items: center; gap: 5px; }

.field { display: flex; flex-direction: column; gap: 5px; }
.field > span { font-size: 12px; color: var(--t-sub); }
.field-row { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.confirm-text { font-size: 13px; line-height: 1.9; }
.confirm-text b { color: var(--t-title); }
</style>
