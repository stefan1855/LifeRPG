<script setup>
import { ref, watch } from 'vue';
import { ATTRIBUTE_META, QUADRANT_META, parseNaturalTask, toDateStr } from '../utils/timeParser';
import { X } from 'lucide-vue-next';

const emit = defineEmits(['save', 'close']);

const title = ref('');
const description = ref('');
const growthAttribute = ref('mind');
const priority = ref('Q2');
const dueAt = ref(toDateStr(new Date()));
const experience = ref(40);

// 监听标题输入，实时提取自然语言提示
watch(title, (val) => {
  const parsed = parseNaturalTask(val);
  if (parsed.growthAttribute) {
    growthAttribute.value = parsed.growthAttribute;
  }
  if (parsed.dueAt) {
    dueAt.value = parsed.dueAt;
  }
});

// 监听四象限变化，自动适配推荐经验值
watch(priority, (val) => {
  const meta = QUADRANT_META[val];
  if (meta) {
    experience.value = meta.exp;
  }
});

function handleSubmit() {
  if (!title.value.trim()) return;
  emit('save', {
    title: title.value.trim(),
    description: description.value.trim(),
    growth_attribute: growthAttribute.value,
    priority: priority.value,
    due_at: dueAt.value ? `${dueAt.value}T23:59:59` : null,
    experience: parseInt(experience.value, 10) || 10,
  });
}
</script>

<template>
  <div class="modal" @click.self="$emit('close')">
    <div class="modal-box-warlock">
      <div style="display: flex; justify-content: space-between; align-items: center;">
        <h3>
          <span style="color: var(--warlock-soul-green);">※</span>
          缔结暗影契约
        </h3>
        <button class="icon-btn" @click="$emit('close')">
          <X :size="13" />
        </button>
      </div>

      <div style="display: flex; flex-direction: column; gap: 4px;">
        <label>契约目标 (支持 "明天 学习算法 #虚空意志")</label>
        <input
          v-model="title"
          type="text"
          placeholder="例如：完成 Rust 异步引擎重构"
          autofocus
          @keyup.enter="handleSubmit"
        />
      </div>

      <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 8px;">
        <div style="display: flex; flex-direction: column; gap: 4px;">
          <label>暗影契约属性</label>
          <select v-model="growthAttribute">
            <option
              v-for="(meta, key) in ATTRIBUTE_META"
              :key="key"
              :value="key"
            >
              {{ meta.symbol }} {{ meta.name }} ({{ meta.originalName }})
            </option>
          </select>
        </div>

        <div style="display: flex; flex-direction: column; gap: 4px;">
          <label>契约象限</label>
          <select v-model="priority">
            <option
              v-for="(meta, key) in QUADRANT_META"
              :key="key"
              :value="key"
            >
              {{ meta.label }}
            </option>
          </select>
        </div>
      </div>

      <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 8px;">
        <div style="display: flex; flex-direction: column; gap: 4px;">
          <label>契约截止日期</label>
          <input v-model="dueAt" type="date" />
        </div>

        <div style="display: flex; flex-direction: column; gap: 4px;">
          <label>灵魂能量收益 (+EXP)</label>
          <input v-model.number="experience" type="number" min="5" max="500" step="5" />
        </div>
      </div>

      <div style="display: flex; flex-direction: column; gap: 4px;">
        <label>契约低语 (备注)</label>
        <textarea
          v-model="description"
          rows="2"
          placeholder="记录修行目标或验收契约条目..."
        ></textarea>
      </div>

      <div class="modal-btns">
        <button class="btn-warlock" @click="$emit('close')">取消</button>
        <button class="btn-warlock primary" @click="handleSubmit">铭刻契约</button>
      </div>
    </div>
  </div>
</template>
