<script setup>
import { computed } from 'vue';
import { ChevronDown, ChevronUp } from 'lucide-vue-next';
import facelessImg from '../assets/theme/faceless_portrait.png';

const props = defineProps({
  character: {
    type: Object,
    default: () => ({
      name: '无面者',
      level: 1,
      total_experience: 0,
      experience_into_current_level: 0,
      experience_required_for_current_level: 0,
      experience_required_for_next_level: 100,
      experience_to_next_level: 100,
      progress_percent: 0,
    }),
  },
  isExpanded: {
    type: Boolean,
    default: false,
  },
});

defineEmits(['toggle-drawer']);

const spanExp = computed(() => {
  return Math.max(1, (props.character.experience_required_for_next_level || 100) - (props.character.experience_required_for_current_level || 0));
});

const currentExp = computed(() => {
  return props.character.experience_into_current_level || 0;
});

const progress = computed(() => {
  return Math.min(100, Math.max(0, props.character.progress_percent || 0));
});

const formatNumber = (num) => {
  return (num || 0).toLocaleString();
};
</script>

<template>
  <div
    class="character-hud-warlock"
    @click="$emit('toggle-drawer')"
    title="点击展开/收起暗影契约法阵"
  >
    <div class="hud-char-row">
      <!-- 亡灵术士头像框 -->
      <div class="avatar-frame">
        <img :src="facelessImg" alt="Avatar" class="avatar-img" />
        <span class="avatar-level-tag">Lv.{{ character.level || 1 }}</span>
      </div>

      <!-- 角色名与暗影阶位 -->
      <div class="hud-info">
        <div class="hud-name-line">
          <span class="char-title-text">{{ character.name || '无面者' }}</span>
          <span class="char-class-badge">
            <span>💀</span>
            <span>暗影学徒</span>
            <component
              :is="isExpanded ? ChevronUp : ChevronDown"
              :size="14"
              style="margin-left: 2px; color: var(--warlock-soul-green);"
            />
          </span>
        </div>

        <div class="hud-exp-stats">
          <span class="soul-energy-val">
            {{ formatNumber(character.total_experience) }} 灵魂能量
          </span>
          <span class="soul-to-next">
            距下级 {{ formatNumber(character.experience_to_next_level) }} EXP ({{ progress.toFixed(1) }}%)
          </span>
        </div>
      </div>
    </div>

    <!-- 邪能液体雕花经验槽 -->
    <div class="warlock-exp-bar-track">
      <div class="warlock-exp-bar-fill" :style="{ width: `${progress}%` }"></div>
    </div>
  </div>
</template>
