<script setup>
import { ref } from 'vue';
import { ATTRIBUTE_META, QUADRANT_META } from '../utils/timeParser';
import { X } from 'lucide-vue-next';

const props = defineProps({
  task: {
    type: Object,
    required: true,
  },
});

const emit = defineEmits(['save', 'close']);

const title = ref(props.task.title || '');
const description = ref(props.task.description || '');
const growthAttribute = ref(props.task.growth_attribute || 'mind');
const priority = ref(props.task.priority || 'Q2');
const dueAt = ref(props.task.due_at ? props.task.due_at.slice(0, 10) : '');
const experience = ref(props.task.experience || 40);

function handleSubmit() {
  if (!title.value.trim()) return;
  emit('save', {
    ...props.task,
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
          <span style="color: var(--warlock-gold);">※</span>
          重塑契约条目
        </h3>
        <button class="icon-btn" @click="$emit('close')">
          <X :size="13" />
        </button>
      </div>

      <div style="display: flex; flex-direction: column; gap: 4px;">
        <label>契约目标</label>
        <input
          v-model="title"
          type="text"
          placeholder="契约目标"
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
          placeholder="修行补充说明..."
        ></textarea>
      </div>

      <div class="modal-btns">
        <button class="btn-warlock" @click="$emit('close')">取消</button>
        <button class="btn-warlock primary" @click="handleSubmit">铭刻保存</button>
      </div>
    </div>
  </div>
</template>
