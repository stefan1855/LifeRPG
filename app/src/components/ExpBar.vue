<script setup>
import { computed } from 'vue';

const props = defineProps({
  percent: { type: Number, default: 0 },
  label: { type: String, default: '' },
  value: { type: String, default: '' },
  height: { type: Number, default: 18 },
  showShine: { type: Boolean, default: false },
});

const clamped = computed(() => Math.max(0, Math.min(100, props.percent)));
const display = computed(() => props.value || `${clamped.value.toFixed(1)}%`);
</script>

<template>
  <div class="expbar" :style="{ height: height + 'px' }">
    <div class="trough">
      <div class="fill" :style="{ width: clamped + '%' }">
        <span v-if="showShine" class="shine"></span>
      </div>
      <span class="ticks"></span>
    </div>
    <span v-if="label" class="label">{{ label }}</span>
    <span class="value num">{{ display }}</span>
  </div>
</template>

<style scoped>
.expbar {
  position: relative;
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
}
.trough {
  position: relative;
  flex: 1;
  height: 100%;
  background: url('../assets/reference/stat-trough.webp') no-repeat center / 100% 100%;
  border: 2px solid #2b2412;
  border-radius: 2px;
  overflow: hidden;
  box-shadow:
    inset 0 3px 8px rgba(0, 0, 0, 0.95),
    0 1px 0 rgba(192, 166, 76, 0.3),
    0 0 0 1px rgba(10, 8, 3, 0.9);
}
.fill {
  position: absolute;
  inset: 1px auto 1px 1px;
  background: url('../assets/reference/experience-fill.webp') no-repeat center / 100% 100%;
  box-shadow: 0 0 14px rgba(124, 255, 31, 0.75), inset 0 1px 0 rgba(255, 255, 255, 0.55);
  transition: width 0.7s cubic-bezier(0.22, 1, 0.36, 1);
}
.shine {
  position: absolute;
  top: 0;
  bottom: 0;
  width: 26px;
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.55), transparent);
  animation: sweep 2.8s linear infinite;
}
@keyframes sweep {
  from { left: -30px; }
  to   { left: 100%; }
}
.ticks {
  position: absolute;
  inset: 0;
  background:
    repeating-linear-gradient(90deg, transparent 0 9.4%, rgba(192, 166, 76, 0.55) 9.4% 9.7%),
    linear-gradient(180deg, rgba(0, 0, 0, 0.55), transparent 40%, rgba(0, 0, 0, 0.45));
  pointer-events: none;
}
.trough::after {
  content: '';
  position: absolute;
  inset: 0;
  box-shadow: inset 0 3px 6px rgba(0, 0, 0, 0.9), inset 0 -1px 0 rgba(192, 166, 76, 0.25);
  pointer-events: none;
}
.label {
  font-size: 12px;
  color: var(--t-time);
  white-space: nowrap;
  letter-spacing: 0.05em;
}
.value {
  font-size: 12px;
  color: var(--t-bar);
  white-space: nowrap;
  min-width: 44px;
  text-align: right;
}
</style>
