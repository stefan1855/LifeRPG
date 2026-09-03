<script setup>
import { computed } from 'vue';
import { getAttributeMeta, formatTaskDate } from '../utils/timeParser';
import {
  Flame,
  Brain,
  Shield,
  MessageSquare,
  Sparkles,
  Coins,
  CircleDot,
  Edit3,
  Trash2,
} from 'lucide-vue-next';

const iconMap = {
  Flame,
  Brain,
  Shield,
  MessageSquare,
  Sparkles,
  Coins,
  CircleDot,
};

const props = defineProps({
  task: {
    type: Object,
    required: true,
  },
});

defineEmits(['toggle', 'edit', 'delete']);

const isCompleted = computed(() => props.task.status === 'Completed');

const attrMeta = computed(() => {
  return getAttributeMeta(props.task.growth_attribute);
});

const iconComp = computed(() => {
  return iconMap[attrMeta.value.icon] || CircleDot;
});

const formattedDue = computed(() => {
  return formatTaskDate(props.task.due_at);
});

const isOverdue = computed(() => {
  return formattedDue.value.startsWith('已腐化') && !isCompleted.value;
});
</script>

<template>
  <div class="task-card-warlock" :class="{ completed: isCompleted }">
    <!-- 哥特邪能复选框 -->
    <input
      type="checkbox"
      class="task-checkbox-warlock"
      :checked="isCompleted"
      @change="$emit('toggle', task)"
      title="标记契约达成 / 重启"
    />

    <div class="task-body">
      <div class="task-title-row">
        <span
          class="task-title"
          @click="$emit('edit', task)"
          :title="task.description ? `${task.title}\n${task.description}` : task.title"
        >
          {{ task.title }}
        </span>
      </div>

      <div class="task-meta-row">
        <!-- 暗影契约属性标签 -->
        <span
          v-if="task.growth_attribute && task.growth_attribute !== 'general'"
          class="attr-tag-warlock"
          :style="{
            color: attrMeta.color,
            borderColor: attrMeta.glow,
          }"
        >
          <component :is="iconComp" :size="10" />
          <span>{{ attrMeta.name }}</span>
        </span>

        <!-- 灵魂能量收益 -->
        <span class="exp-badge-warlock">
          +{{ task.experience || 10 }} EXP
        </span>

        <!-- 契约时限 -->
        <span
          v-if="formattedDue"
          class="task-due"
          :style="{ color: isOverdue ? 'var(--warlock-danger)' : 'var(--warlock-text-muted)' }"
        >
          {{ formattedDue }}
        </span>
      </div>
    </div>

    <!-- 暗铁悬停快捷操作 -->
    <div style="display: flex; gap: 3px; opacity: 0.75; align-self: center;">
      <button
        class="icon-btn"
        style="width: 22px; height: 22px;"
        @click.stop="$emit('edit', task)"
        title="编辑契约"
      >
        <Edit3 :size="11" />
      </button>
      <button
        class="icon-btn"
        style="width: 22px; height: 22px;"
        @click.stop="$emit('delete', task)"
        title="销毁契约"
      >
        <Trash2 :size="11" />
      </button>
    </div>
  </div>
</template>
