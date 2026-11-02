<script setup>
import { ref, useId } from 'vue';

defineProps({
  title: { type: String, default: '' },
  subtitle: { type: String, default: '' },
  dense: { type: Boolean, default: false },
  glow: { type: Boolean, default: false },
  collapsible: { type: Boolean, default: false },
});
const expanded = ref(false);
const bodyId = useId();
</script>

<template>
  <section class="panel" :class="{ dense, glow }">
    <span class="orn orn-tl"></span>
    <span class="orn orn-tr"></span>
    <span class="orn orn-bl"></span>
    <span class="orn orn-br"></span>

    <header v-if="title" class="panel-head">
      <h2 class="panel-title">
        <button v-if="collapsible" class="panel-toggle" :aria-expanded="expanded" :aria-controls="bodyId" @click="expanded = !expanded">
          <span aria-hidden="true">{{ expanded ? '▾' : '▸' }}</span> {{ title }}
        </button>
        <template v-else>{{ title }}</template>
      </h2>
      <span v-if="subtitle" class="panel-sub">{{ subtitle }}</span>
      <div class="panel-head-line"></div>
      <slot v-if="!collapsible || expanded" name="actions" />
    </header>

    <div v-show="!collapsible || expanded" :id="bodyId" class="panel-body">
      <slot />
    </div>
  </section>
</template>

<style scoped>
.panel {
  position: relative;
  display: flex;
  flex-direction: column;
  background:
    radial-gradient(ellipse 120% 90% at 50% 0%, rgba(22, 32, 18, 0.55), transparent 68%),
    linear-gradient(180deg, #0b120b, #050805);
  border: 1px solid rgba(109, 91, 42, 0.45);
  border-radius: var(--radius);
  box-shadow: var(--sh-panel);
  min-height: 0;
}
/* 九宫格金属浮雕边框：不改布局，纯覆盖层 */
.panel::after {
  content: '';
  position: absolute;
  inset: 0;
  border: 10px solid transparent;
  border-image: url('../assets/textures/frame_panel.webp') 30 / 10px stretch;
  pointer-events: none;
  opacity: 0.65;
}
.panel::before {
  content: '';
  position: absolute;
  inset: 3px;
  border: 1px solid rgba(192, 166, 76, 0.08);
  pointer-events: none;
}
.panel.glow {
  box-shadow: var(--sh-panel), 0 0 22px rgba(124, 255, 31, 0.08);
}
.panel.dense .panel-body {
  padding: 10px 12px;
}

.orn {
  position: absolute;
  width: 22px;
  height: 22px;
  pointer-events: none;
  opacity: 0.92;
  background: url('../assets/textures/orn_corner.webp') no-repeat top left / contain;
  filter: drop-shadow(0 0 4px rgba(124, 255, 31, 0.2));
  z-index: 2;
}
.orn-tl { top: -1px; left: -1px; }
.orn-tr { top: -1px; right: -1px; transform: scaleX(-1); }
.orn-bl { bottom: -1px; left: -1px; transform: scaleY(-1); }
.orn-br { bottom: -1px; right: -1px; transform: scale(-1, -1); }

.panel-head {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 13px 20px 9px;
  border-bottom: 1px solid rgba(109, 91, 42, 0.3);
  flex: 0 0 auto;
  position: relative;
  z-index: 1;
}
.panel-title {
  font-family: var(--f-title);
  font-size: 15px;
  font-weight: 700;
  letter-spacing: 0.1em;
  color: var(--t-panel);
  text-shadow: 0 0 12px rgba(124, 255, 31, 0.25);
  white-space: nowrap;
}
.panel-toggle {
  color: inherit;
  font: inherit;
  letter-spacing: inherit;
  text-align: left;
  background: transparent;
  border: 0;
  cursor: pointer;
  padding: 4px 0;
}
.panel-toggle:focus-visible { outline: 1px solid var(--c-soul); outline-offset: 4px; }
.panel-sub {
  font-size: 12px;
  color: var(--t-time);
  letter-spacing: 0.04em;
}
.panel-head-line {
  flex: 1;
  height: 1px;
  background: linear-gradient(90deg, rgba(192, 166, 76, 0.45), transparent);
}
.panel-body {
  flex: 1;
  min-height: 0;
  padding: 14px 20px 18px;
  display: flex;
  flex-direction: column;
  gap: 10px;
  position: relative;
  z-index: 1;
}
/* 面板内部：极淡石纹（几乎不可见），只用来打散纯黑，不能抢内容 */
.panel-body::before {
  content: '';
  position: absolute;
  inset: 0;
  background: url('../assets/textures/mat_stone.webp') center / 520px;
  opacity: 0.025;
  mix-blend-mode: soft-light;
  pointer-events: none;
  z-index: 0;
}
.panel-body > :deep(*) {
  position: relative;
  z-index: 1;
}
</style>

<style scoped>
.panel-head{flex-wrap:wrap}.panel-head-line{min-width:8px}
</style>
