<script setup>
import { computed } from 'vue';
import { getAttributeMeta } from '../utils/timeParser';
import magicCircleImg from '../assets/theme/magic_circle_emblem.png';
import {
  Flame,
  Brain,
  Shield,
  MessageSquare,
  Sparkles,
  Coins,
  CircleDot,
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
  attributes: {
    type: Array,
    default: () => [],
  },
});

const enrichedAttributes = computed(() => {
  return (props.attributes || []).map((attr) => {
    const meta = getAttributeMeta(attr.key);
    const span = Math.max(1, (attr.experience_required_for_next_level || 100) - (attr.experience_required_for_current_level || 0));
    return {
      ...attr,
      meta,
      span,
      iconComponent: iconMap[meta.icon] || CircleDot,
    };
  });
});
</script>

<template>
  <div class="rune-contract-drawer">
    <div class="drawer-header-title">
      ※ 暗 影 契 约 ※
    </div>

    <!-- 邪能星盘微缩发光法阵 -->
    <div class="astrolabe-center-wrap">
      <div class="magic-circle-rotating">
        <img :src="magicCircleImg" alt="Magic Circle" class="magic-circle-img" />
      </div>
    </div>

    <!-- 六维暗影属性矩阵 -->
    <div class="attributes-grid-warlock">
      <div
        v-for="attr in enrichedAttributes"
        :key="attr.key"
        class="attr-rune-card"
      >
        <div class="attr-card-top">
          <div class="attr-rune-name" :style="{ color: attr.meta.color }">
            <span style="font-size: 11px;">{{ attr.meta.symbol }}</span>
            <span>{{ attr.meta.name }}</span>
          </div>
          <span class="attr-rune-lvl">Lv.{{ attr.level || 1 }}</span>
        </div>

        <div class="attr-liquid-track">
          <div
            class="attr-liquid-fill"
            :style="{
              width: `${Math.min(100, Math.max(0, attr.progress_percent || 0))}%`,
              background: attr.meta.color,
              boxShadow: `0 0 8px ${attr.meta.glow}`,
            }"
          ></div>
        </div>

        <div class="attr-exp-stat-line">
          <span>{{ attr.experience_into_current_level || 0 }} EXP</span>
          <span>目标 {{ attr.span }}</span>
        </div>
      </div>
    </div>
  </div>
</template>
