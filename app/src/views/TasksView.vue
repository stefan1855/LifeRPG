<script setup>
const saving = ref(false);
import { computed, onMounted, onUnmounted, watch, reactive, ref } from 'vue';
import { Plus, Check, Undo2, Trash2, Pencil, Filter, CalendarDays, Sparkles, ChevronRight } from 'lucide-vue-next';
import ModalShell from '../components/ModalShell.vue';
import { api, ATTRIBUTE_META, QUADRANTS, attrMeta, fmtDate, quadrantMeta } from '../api';
import { notify, showLevelUp } from '../stores/toast';
import { sfx } from '../utils/audio';

const tasks = ref([]);
const loading = ref(true);
const filter = ref('pending');
const quadrantFilter = ref('');
const taskDefaults = { quadrant: 'Q2', growthAttribute: 'mind', experience: 25 };

const showEditor = ref(false);
const draftGenerated=ref(false);
const showTaskOptions = ref(false);
let editorRevision=0;
watch(showEditor,(open)=>{if(!open)editorRevision++;},{flush:"sync"});
onUnmounted(()=>editorRevision++);
const editingId = ref(null);
const form = reactive({
  title: '',
  content: '',
  category: '默认',
  ...taskDefaults,
  dueDate: '',
});

const showDelete = ref(false);
const deletingTask = ref(null);

const FILTERS = [
  { key: 'pending', label: '待办' },
  { key: 'completed', label: '已完成' },
  { key: 'all', label: '全部' },
];

async function load() {
  loading.value = true;
  try {
    tasks.value = await api.tasks(null, null);
  } catch (e) {
    notify.error('加载任务失败：' + e.message);
  } finally {
    loading.value = false;
  }
}

const filtered = computed(() => {
  let list = tasks.value;
  if (filter.value === 'pending') list = list.filter((t) => t.status !== 'Completed');
  if (filter.value === 'completed') list = list.filter((t) => t.status === 'Completed');
  if (quadrantFilter.value) list = list.filter((t) => t.quadrant === quadrantFilter.value);
  return list;
});

const grouped = computed(() =>
  QUADRANTS.map((q) => ({
    ...q,
    items: filtered.value.filter((t) => t.quadrant === q.key),
  }))
);

function openCreate(quadrant, title = '') {
  draftGenerated.value=false;
  showTaskOptions.value = false;
  editorRevision++;
  editingId.value = null;
  Object.assign(form, {
    title: title.trim(),
    content: '',
    category: '默认',
    quadrant: quadrant || taskDefaults.quadrant,
    growthAttribute: taskDefaults.growthAttribute,
    experience: quadrant ? quadrantMeta(quadrant).exp : taskDefaults.experience,
    dueDate: '',
  });
  showEditor.value = true;
}

function localDateTime(value) {
  const date = new Date(value);
  return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0,16);
}
function openEdit(task) {
  draftGenerated.value=false;
  showTaskOptions.value = true;
  editorRevision++;
  editingId.value = task.id;
  Object.assign(form, {
    title: task.title,
    content: task.content || '',
    category: task.category || '默认',
    quadrant: task.quadrant,
    growthAttribute: task.growthAttribute,
    experience: task.experience,
    dueDate: task.dueAt ? localDateTime(task.dueAt) : '',
  });
  showEditor.value = true;
}

function onQuadrantChange() {
  if(!settlementLocked.value)form.experience = quadrantMeta(form.quadrant).exp;
}
const settlementLocked = computed(()=>editingId.value && tasks.value.some(t=>t.id===editingId.value && t.status==='Completed'));

/** AI 军师：一句话 → 结构化任务，填入编辑弹窗供确认 */
const quickText = ref('');
const aiParsing = ref(false);
const quickAdding = ref(false);

function onQuickEnter(event) {
  if (event.isComposing || event.keyCode === 229) return;
  event.preventDefault();
  quickCreate();
}

async function quickCreate() {
  const title = quickText.value.trim();
  if (!title || quickAdding.value || aiParsing.value) return;
  quickAdding.value = true;
  try {
    await api.createTask({ title, ...taskDefaults });
    if (quickText.value.trim() === title) quickText.value = '';
    filter.value = 'pending';
    quadrantFilter.value = '';
    notify.success('任务已添加');
    await load();
  } catch (e) {
    notify.error('添加失败：' + e.message);
  } finally {
    quickAdding.value = false;
  }
}

async function aiCreate() {
  if (aiParsing.value || quickAdding.value) return;
  const text = quickText.value.trim();
  if (!text) {
    notify.warn('先输入一句话，例如「明天下午健身一小时」');
    return;
  }
  const revision=editorRevision;
  aiParsing.value = true;
  try {
    const parsed = await api.aiParseTask(text);
    if(revision!==editorRevision)return;
    openCreate(parsed.quadrant);
    form.title = parsed.title || text;
    form.growthAttribute = parsed.growthAttribute || 'mind';
    form.experience = parsed.experience || quadrantMeta(parsed.quadrant).exp;
    form.dueDate = parsed.dueAt ? localDateTime(parsed.dueAt) : (parsed.dueDate ? parsed.dueDate + 'T23:59' : '');
    form.content = `（AI 解析自：「${text}」）`;
    draftGenerated.value=true;
    showTaskOptions.value = true;
    if (quickText.value.trim() === text) quickText.value = '';
    notify.success('AI 已解析，确认后铭刻');
  } catch (e) {
    notify.error('AI 解析失败：' + e.message);
  } finally {
    aiParsing.value = false;
  }
}

async function submit() {
  if (!form.title.trim()) {
    notify.warn('任务标题不能为空');
    return;
  }
  const payload = {
    title: form.title.trim(),
    content: form.content,
    category: form.category || '默认',
    quadrant: form.quadrant,
    growthAttribute: form.growthAttribute,
    experience: Number(form.experience) || 0,
    dueAt: form.dueDate ? new Date(form.dueDate).toISOString() : null,
  };
  if (saving.value) return;
  saving.value = true;
  try {
    if (editingId.value) {
      await api.updateTask({ id: editingId.value, ...payload });
      notify.success('任务已更新');
    } else {
      await api.createTask(payload);
      if (quickText.value.trim() === payload.title) quickText.value = '';
      filter.value = 'pending';
      quadrantFilter.value = '';
      notify.success('任务已添加');
    }
    showEditor.value = false;
    await load();
  } catch (e) {
    notify.error('保存失败：' + e.message);
  } finally { saving.value = false; }
}

async function complete(task) {
  try {
    const res = await api.completeTask(task.id);
    if (res.isDuplicate) {
      notify.warn('该任务经验已结算过');
    } else if (res.applied) {
      sfx.hammer();
      notify.success(`完成「${task.title}」，获得 +${res.totalExperienceGranted} EXP`);
      if (res.leveledUp) showLevelUp(res.newLevel);
    }
    await load();
  } catch (e) {
    notify.error('结算失败：' + e.message);
  }
}

async function uncomplete(task) {
  try {
    await api.uncompleteTask(task.id);
    notify.info('已撤销完成，经验已回滚');
    await load();
  } catch (e) {
    notify.error('撤销失败：' + e.message);
  }
}

function askDelete(task) {
  deletingTask.value = task;
  showDelete.value = true;
}

async function confirmDelete() {
  try {
    await api.deleteTask(deletingTask.value.id);
    notify.info('任务已抹除');
    showDelete.value = false;
    await load();
  } catch (e) {
    notify.error('删除失败：' + e.message);
  }
}

function isOverdue(t) {
  return t.status !== 'Completed' && t.dueAt && new Date(t.dueAt) < new Date();
}

onMounted(load);
</script>

<template>
  <div class="tasks-view">
    <!-- 工具栏 -->
    <div class="toolbar">
      <div class="tabs">
        <button
          v-for="f in FILTERS"
          :key="f.key"
          class="tab"
          :class="{ active: filter === f.key }"
          @click="filter = f.key"
        >{{ f.label }}</button>
      </div>
      <div class="q-filter">
        <Filter :size="12" class="dim" />
        <button
          class="q-chip"
          :class="{ active: !quadrantFilter }"
          @click="quadrantFilter = ''"
        >全部象限</button>
        <button
          v-for="q in QUADRANTS"
          :key="q.key"
          class="q-chip"
          :class="{ active: quadrantFilter === q.key }"
          :style="quadrantFilter === q.key ? { borderColor: q.color, color: q.color } : {}"
          @click="quadrantFilter = q.key"
        >{{ q.label }}</button>
      </div>
    </div>

    <!-- 默认直接添加，AI 解析由用户主动选择 -->
    <div class="ai-quick">
      <input
        v-model="quickText"
        class="ai-input"
        aria-label="新任务标题"
        placeholder="要做什么？输入后按回车添加"
        @keydown.enter="onQuickEnter"
      />
      <button class="btn btn-primary btn-sm" :disabled="quickAdding || aiParsing || !quickText.trim()" @click="quickCreate">
        <Plus :size="14" /> {{ quickAdding ? '添加中…' : '添加任务' }}
      </button>
      <button class="btn btn-ghost btn-sm" :disabled="aiParsing || quickAdding || !quickText.trim()" @click="aiCreate">
        <Sparkles :size="13" /> {{ aiParsing ? '解析中…' : 'AI 解析' }}
      </button>
      <button class="btn btn-ghost btn-sm" @click="openCreate(quadrantFilter || undefined, quickText)">
        详细新建
      </button>
      <span class="quick-defaults">默认：{{ quadrantMeta(taskDefaults.quadrant).label }} · {{ attrMeta(taskDefaults.growthAttribute).label }} +{{ taskDefaults.experience }} EXP</span>
    </div>

    <!-- 四象限矩阵 -->
    <div class="matrix">
      <div v-for="q in grouped" :key="q.key" class="quadrant">
        <header class="q-head" :style="{ borderColor: q.color + '66' }">
          <span class="q-name" :style="{ color: q.color }">{{ q.label }}</span>
          <span class="q-meta num">{{ q.items.length }} 项 · 基础 +{{ q.exp }}</span>
          <button class="q-add" @click="openCreate(q.key)"><Plus :size="12" /></button>
        </header>
        <div class="q-body scroll">
          <div v-if="!q.items.length" class="q-empty">
            <span class="q-empty-mark">◇</span>
            <span>暂无契约</span>
          </div>
          <article
            v-for="t in q.items"
            :key="t.id"
            class="task-card"
            :class="{ done: t.status === 'Completed', overdue: isOverdue(t) }"
          >
            <div class="tc-main">
              <button
                class="check"
                :class="{ on: t.status === 'Completed' }"
                @click="t.status === 'Completed' ? uncomplete(t) : complete(t)"
              >
                <Check v-if="t.status === 'Completed'" :size="11" />
              </button>
              <div class="tc-text">
                <h4>{{ t.title }}</h4>
                <p v-if="t.content" class="tc-content">{{ t.content }}</p>
                <div class="tc-meta">
                  <span class="attr-tag" :style="{ color: attrMeta(t.growthAttribute).color }">
                    {{ attrMeta(t.growthAttribute).sigil }} {{ attrMeta(t.growthAttribute).label }}
                  </span>
                  <span class="exp-tag num">+{{ t.experience }}</span>
                  <span v-if="t.dueAt" class="due-tag" :class="{ warn: isOverdue(t) }">
                    <CalendarDays :size="10" /> {{ fmtDate(t.dueAt) }}
                  </span>
                  <span class="cat-tag">{{ t.category }}</span>
                </div>
              </div>
            </div>
            <div class="tc-actions">
              <button class="ico-btn" title="编辑" @click="openEdit(t)"><Pencil :size="12" /></button>
              <button class="ico-btn" title="删除" @click="askDelete(t)"><Trash2 :size="12" /></button>
            </div>
          </article>
        </div>
      </div>
    </div>

    <!-- 编辑弹窗 -->
    <ModalShell
      v-if="showEditor" :state="form" :busy="saving" :initial-dirty="draftGenerated"
      :title="editingId ? '编辑行动契约' : '铭刻新的行动契约'"
      subtitle="TASK CONTRACT"
      :width="560"
      @close="showEditor = false"
    >
      <label class="field">
        <span>任务标题</span>
        <input v-model="form.title" class="input" placeholder="例如：完成 Rust 所有权章节精读" />
      </label>
      <label class="field">
        <span>截止时间（可选）</span>
        <input v-model="form.dueDate" type="datetime-local" class="input" />
      </label>
      <button class="options-toggle" :aria-expanded="showTaskOptions" aria-controls="task-options" @click="showTaskOptions = !showTaskOptions">
        <span>更多设置</span>
        <span class="options-value">{{ quadrantMeta(form.quadrant).label }} · {{ attrMeta(form.growthAttribute).label }} +{{ form.experience }} EXP</span>
        <ChevronRight :size="16" class="options-chevron" :class="{ expanded: showTaskOptions }" />
      </button>
      <div id="task-options" class="options-collapse" :class="{ expanded: showTaskOptions }" :inert="!showTaskOptions || undefined" :aria-hidden="!showTaskOptions">
        <div class="options-clip">
        <div class="task-options-body">
          <label class="field">
            <span>任务详情</span>
            <textarea v-model="form.content" class="textarea" placeholder="补充说明（可选）"></textarea>
          </label>
          <div class="field-row">
            <label class="field">
              <span>四象限</span>
              <select v-model="form.quadrant" class="select" @change="onQuadrantChange">
                <option v-for="q in QUADRANTS" :key="q.key" :value="q.key">{{ q.label }}（+{{ q.exp }}）</option>
              </select>
            </label>
            <label class="field">
              <span>成长属性</span>
              <select v-model="form.growthAttribute" class="select" :disabled="settlementLocked">
                <option v-for="(m, k) in ATTRIBUTE_META" :key="k" :value="k">{{ m.label }}</option>
              </select>
            </label>
          </div>
          <label class="field">
            <span>经验值</span>
            <input v-model.number="form.experience" type="number" class="input" min="0" step="5" :disabled="settlementLocked" />
            <small v-if="settlementLocked">已结算。先撤销完成后可调整经验和属性。</small>
          </label>
          <label class="field">
            <span>分类</span>
            <input v-model="form.category" class="input" placeholder="默认" />
          </label>
        </div>
        </div>
      </div>

      <template #footer="{ close }">
        <button class="btn btn-ghost" @click="close">取消</button>
        <button class="btn btn-primary" :disabled="saving" @click="submit">{{ saving ? '保存中…' : (editingId ? '保存' : '添加任务') }}</button>
      </template>
    </ModalShell>

    <!-- 删除确认 -->
    <ModalShell
      v-if="showDelete"
      title="抹除行动契约"
      subtitle="此操作不可撤销"
      :width="420"
      danger
      @close="showDelete = false"
    >
      <p class="confirm-text">
        确定要删除「<b>{{ deletingTask?.title }}</b>」吗？<br />
        已结算的角色与属性经验将一并回滚。
      </p>
      <template #footer="{ close }">
        <button class="btn btn-ghost" @click="showDelete = false">取消</button>
        <button class="btn btn-danger" @click="confirmDelete">确认抹除</button>
      </template>
    </ModalShell>
  </div>
</template>

<style scoped>
.tasks-view {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  flex: 0 0 auto;
  padding: 8px 12px;
  background:
    url('../assets/textures/mat_metal_iron.webp') center / 300px,
    linear-gradient(180deg, rgba(12, 18, 10, 0.96), rgba(5, 8, 5, 0.98));
  background-blend-mode: soft-light, normal;
  border: 1px solid rgba(109, 91, 42, 0.5);
  box-shadow: var(--sh-inset);
}
.tabs {
  display: flex;
  border: 1px solid rgba(109, 91, 42, 0.5);
}
.tab {
  padding: 0 16px;
  height: 30px;
  font-size: 12.5px;
  color: var(--t-sub);
  background: rgba(0, 0, 0, 0.4);
  border: none;
  cursor: pointer;
  transition: all 0.15s;
}
.tab.active {
  color: var(--c-glow);
  background: rgba(124, 255, 31, 0.12);
  text-shadow: 0 0 8px rgba(124, 255, 31, 0.5);
}
.q-filter {
  display: flex;
  align-items: center;
  gap: 6px;
}
.q-chip {
  padding: 0 9px;
  height: 24px;
  font-size: 12px;
  color: var(--t-time);
  background: transparent;
  border: 1px solid rgba(109, 91, 42, 0.35);
  cursor: pointer;
}
.q-chip.active {
  color: var(--c-glow);
  border-color: var(--c-soul);
}
.spacer { flex: 1; }

/* AI 一句话建任务 */
.ai-quick {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 9px;
  flex: 0 0 auto;
  padding: 8px 12px;
  background:
    url('../assets/textures/mat_metal_iron.webp') center / 300px,
    linear-gradient(180deg, rgba(16, 22, 14, 0.9), rgba(7, 10, 6, 0.95));
  background-blend-mode: soft-light, normal;
  border: 1px solid rgba(109, 91, 42, 0.5);
  box-shadow: var(--sh-inset);
}
.ai-ico {
  color: var(--c-gold);
  flex: 0 0 auto;
  filter: drop-shadow(0 0 6px rgba(192, 166, 76, 0.4));
}
.ai-input {
  flex: 1;
  min-width: 180px;
  padding: 6px 10px;
  font-family: var(--f-body);
  font-size: 12.5px;
  color: var(--t-title);
  background: rgba(0, 0, 0, 0.55);
  border: 1px solid #2b2412;
  border-radius: var(--radius);
  outline: none;
  user-select: text;
}
.ai-input:focus {
  border-color: var(--c-gold);
  box-shadow: 0 0 0 1px rgba(192, 166, 76, 0.25);
}
.ai-input::placeholder { color: var(--t-disabled); }
.quick-defaults { flex-basis: 100%; color: var(--t-time); font-size: 12px; }
.options-toggle { display: flex; align-items: center; gap: 8px; padding: 9px 10px; color: var(--t-sub); font: inherit; font-size: 13px; text-align: left; background: rgba(0,0,0,.3); border: 1px solid rgba(109,91,42,.45); cursor: pointer; transition: background .18s, border-color .18s; }
.options-toggle:hover { background: rgba(124,255,31,.06); border-color: var(--c-bronze); }
.options-toggle:focus-visible { outline: 1px solid var(--c-soul); outline-offset: 2px; }
.options-value { color: var(--t-time); font-size: 12px; }
.options-chevron { margin-left: auto; flex-shrink: 0; color: var(--c-soul); transition: transform .22s ease-out; }
.options-chevron.expanded { transform: rotate(90deg); }
.options-collapse { display: grid; grid-template-rows: 0fr; visibility: hidden; opacity: 0; transition: grid-template-rows .22s ease-out, opacity .18s, visibility 0s .22s; }
.options-collapse.expanded { grid-template-rows: 1fr; visibility: visible; opacity: 1; transition-delay: 0s; }
.options-clip { min-height: 0; overflow: hidden; }
.task-options-body { display: flex; flex-direction: column; gap: 12px; padding-top: 8px; }
@media (prefers-reduced-motion: reduce) { .options-chevron,.options-collapse { transition: none; } }

.matrix {
  flex: 1;
  min-height: 0;
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  grid-template-rows: repeat(2, 1fr);
  gap: 12px;
}
.quadrant {
  position: relative;
  display: flex;
  flex-direction: column;
  min-height: 0;
  background:
    radial-gradient(ellipse 90% 60% at 50% 0%, rgba(22, 30, 18, 0.55), transparent 70%),
    linear-gradient(180deg, rgba(11, 18, 11, 0.96), rgba(5, 8, 5, 0.985));
  border: 1px solid rgba(109, 91, 42, 0.45);
  box-shadow: var(--sh-inset);
}
.quadrant::after {
  content: '';
  position: absolute;
  inset: 0;
  border: 9px solid transparent;
  border-image: url('../assets/reference/panel-frame-clean.webp') 180 / 20px stretch;
  pointer-events: none;
  opacity: 0.9;
}
.q-head {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 10px;
  border-bottom: 1px solid rgba(109, 91, 42, 0.3);
  border-left: 2px solid;
}
.q-name {
  font-family: var(--f-title);
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 0.08em;
}
.q-meta { font-size: 12px; color: var(--t-time); }
.q-add {
  margin-left: auto;
  width: 20px;
  height: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--t-sub);
  background: transparent;
  border: 1px solid rgba(109, 91, 42, 0.5);
  cursor: pointer;
}
.q-add:hover { color: var(--c-soul); border-color: var(--c-soul); }
.q-body {
  flex: 1;
  min-height: 0;
  padding: 8px;
  display: flex;
  flex-direction: column;
  gap: 7px;
}
.q-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 22px 0;
  font-size: 12px;
  letter-spacing: 0.28em;
  color: var(--t-disabled);
}
.q-empty-mark {
  font-size: 16px;
  color: rgba(192, 166, 76, 0.45);
  animation: soulPulse 4s ease-in-out infinite;
}

.task-card {
  position: relative;
  display: flex;
  align-items: flex-start;
  gap: 8px;
  padding: 9px 10px;
  background:
    linear-gradient(180deg, rgba(20, 28, 16, 0.92), rgba(9, 13, 8, 0.95));
  border: 1px solid rgba(109, 91, 42, 0.4);
  box-shadow: inset 0 1px 0 rgba(192, 166, 76, 0.12), inset 0 -6px 12px rgba(0, 0, 0, 0.45);
  transition: all 0.16s;
}
.task-card:hover {
  border-color: rgba(192, 166, 76, 0.6);
  background: rgba(20, 28, 16, 0.95);
}
.task-card.done { opacity: 0.55; }
.task-card.done h4 { text-decoration: line-through; }
.task-card.overdue { border-left: 2px solid var(--t-danger); }

.tc-main { display: flex; gap: 8px; flex: 1; min-width: 0; }
.check {
  width: 15px;
  height: 15px;
  flex: 0 0 auto;
  margin-top: 2px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #05120a;
  background: radial-gradient(circle at 40% 30%, rgba(60, 52, 24, 0.9), rgba(10, 10, 5, 0.95));
  border: 1px solid rgba(192, 166, 76, 0.75);
  transform: rotate(45deg);
  cursor: pointer;
  transition: all 0.15s;
  box-shadow: inset 0 0 6px rgba(0, 0, 0, 0.8);
}
.check > svg { transform: rotate(-45deg); }
.check:hover {
  border-color: var(--c-soul);
  box-shadow: 0 0 10px rgba(124, 255, 31, 0.5), inset 0 0 6px rgba(0, 0, 0, 0.6);
}
.check.on {
  background: radial-gradient(circle at 40% 30%, #d4ff6b, #7cff1f 60%, #3f9c0c);
  border-color: #d4ff6b;
  box-shadow: 0 0 14px rgba(124, 255, 31, 0.75);
}
.tc-text { flex: 1; min-width: 0; }
.tc-text h4 {
  font-size: 13px;
  font-weight: 600;
  color: var(--t-title);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.tc-content {
  margin-top: 2px;
  font-size: 12px;
  color: var(--t-time);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.tc-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 4px;
  font-size: 12px;
}
.attr-tag { letter-spacing: 0.04em; }
.exp-tag { color: var(--c-soul); }
.due-tag { display: inline-flex; align-items: center; gap: 3px; color: var(--t-time); }
.due-tag.warn { color: var(--t-danger); }
.cat-tag { color: var(--t-disabled); }
.tc-actions { display: flex; gap: 3px; opacity: 0; transition: opacity 0.15s; }
.task-card:hover .tc-actions { opacity: 1; }
.ico-btn {
  width: 20px;
  height: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--t-sub);
  background: transparent;
  border: 1px solid transparent;
  cursor: pointer;
}
.ico-btn:hover { color: var(--c-glow); border-color: rgba(109, 91, 42, 0.6); }

.field { display: flex; flex-direction: column; gap: 5px; }
.field > span { font-size: 12px; color: var(--t-sub); letter-spacing: 0.06em; }
.field-row { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.confirm-text { font-size: 13px; color: var(--t-body); line-height: 1.9; }
.confirm-text b { color: var(--t-title); }
</style>

<style scoped>

.toolbar,.q-filter,.ai-quick,.q-head{flex-wrap:wrap}.ai-input{min-width:0;flex:1 1 220px}.q-add{flex-shrink:0}.matrix{grid-template-columns:repeat(2,minmax(0,1fr))}.quadrant{min-width:0}.task-card:focus-within .tc-actions{opacity:1}
@media(max-width:1000px){.tasks-view{overflow-y:auto;padding-right:4px}.matrix{flex:0 0 auto;grid-template-columns:minmax(0,1fr);grid-template-rows:none;grid-auto-rows:minmax(180px,auto)}.q-body{max-height:320px}}

</style>
