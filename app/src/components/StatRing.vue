<script setup>
import { computed } from 'vue';
import { attrMeta } from '../api';
const props = defineProps({ attributes: { type: Array, default: () => [] }, level: { type: Number, default: 1 } });
const emit = defineEmits(['navigate']);
const icons = {
  physique: new URL('../assets/icons/rune_body.svg', import.meta.url).href,
  mind: new URL('../assets/icons/rune_mind.svg', import.meta.url).href,
  discipline: new URL('../assets/icons/rune_discipline.svg', import.meta.url).href,
  expression: new URL('../assets/icons/rune_social.svg', import.meta.url).href,
  creativity: new URL('../assets/icons/rune_craft.svg', import.meta.url).href,
  resources: new URL('../assets/icons/rune_wealth.svg', import.meta.url).href,
};
const positions = [[25,18],[75,18],[19,50],[81,50],[25,82],[75,82]];
const nodes = computed(() => ['physique','mind','discipline','expression','creativity','resources'].map((key,i) => {
  const a = props.attributes.find(a => a.attributeKey === key) || {};
  return { ...a, key, meta: attrMeta(key), icon: icons[key], x: positions[i][0], y: positions[i][1], pct: Math.max(0,Math.min(100,a.progressPercent || 0)) };
}));
</script>
<template>
  <div class="contract-wheel" aria-label="六维暗影契约">
    <div class="wheel-runes" aria-hidden="true"></div>
    <svg class="wheel-links" viewBox="0 0 100 100" aria-hidden="true"><path d="M25 18 Q50 4 75 18 Q96 31 81 50 Q96 69 75 82 Q50 96 25 82 Q4 69 19 50 Q4 31 25 18Z" /></svg>
    <div class="wheel-core" aria-hidden="true"><img src="../assets/textures/orn_emblem.webp" alt="" /><span>SOUL CONTRACT</span><strong>Lv.{{ level }}</strong></div>
    <button v-for="n in nodes" :key="n.key" class="contract-node" :style="{left:n.x+'%',top:n.y+'%'}" :aria-label="`${n.meta.label}，等级 ${n.level || 1}，查看成长属性`" @click="emit('navigate','attributes')">
      <span class="rune-setting"><span class="rune-diamond"></span><img :src="n.icon" alt="" /></span>
      <span class="node-title">{{ n.meta.label }}</span>
      <span class="node-level">Lv.{{ n.level || 1 }} <small v-if="n.baseValue > 0">+{{ n.baseValue }}</small></span>
      <span class="node-exp num">{{ n.expIntoCurrentLevel || 0 }} / {{ n.expSpan || 100 }} EXP</span>
      <span class="node-bar"><i :style="{width:n.pct+'%'}"></i></span>
    </button>
    <span class="wheel-sigil" aria-hidden="true">✧</span>
  </div>
</template>
<style scoped>
.contract-wheel{position:relative;width:100%;max-width:520px;aspect-ratio:1/1.08;margin:auto;flex-shrink:0;isolation:isolate}.wheel-runes{position:absolute;inset:11% 4%;background:url('../assets/textures/orn_magic_circle.webp') center/contain no-repeat;opacity:.34;mix-blend-mode:screen;animation:ritual-turn 180s linear infinite;pointer-events:none}.wheel-links{position:absolute;inset:0;width:100%;height:100%;pointer-events:none;overflow:visible}.wheel-links path{fill:none;stroke:#9cae3a;stroke-width:.35;opacity:.5;stroke-dasharray:1 1.5}.wheel-core{position:absolute;left:29%;top:31%;width:42%;height:38%;display:flex;flex-direction:column;align-items:center;justify-content:center;pointer-events:none}.wheel-core img{width:190%;max-width:330px;mix-blend-mode:screen;filter:drop-shadow(0 0 13px #5baa192a)}.wheel-core span{font:8px var(--f-title);letter-spacing:.16em;color:#8c8e4a;margin-top:-5px}.wheel-core strong{font:500 23px var(--f-title);color:#d1ea80;letter-spacing:.04em}.contract-node{position:absolute;transform:translate(-50%,-50%);width:37%;display:flex;flex-direction:column;align-items:center;gap:2px;padding:3px 0;color:#e2d89e;background:none;border:0;cursor:pointer;font-family:var(--f-body);min-width:0}.rune-setting{position:relative;width:54px;height:62px;display:grid;place-items:center;margin-bottom:3px;filter:drop-shadow(0 0 6px #8ace2738)}.rune-diamond{position:absolute;width:43px;height:43px;transform:rotate(45deg);border:2px solid #9a9250;outline:1px solid #1c2b11;outline-offset:4px;background:linear-gradient(135deg,#7b7131 0,#182313 12%,#030704 50%,#172011 84%,#b1a05b);box-shadow:inset 0 0 0 3px #070b05,inset 0 0 0 4px #73713a,0 0 12px #81bc1b42}.rune-diamond::after{content:'';position:absolute;inset:-7px;border:1px solid #799923;clip-path:polygon(0 0,40% 0,0 40%,0 60%,40% 100%,0 100%,0 0,100% 0,100% 40%,60% 0,40% 0,0 40%,0 100%,100% 100%,100% 60%,60% 100%,100% 100%,100% 0)}.rune-setting img{position:relative;width:27px;height:27px;object-fit:contain;filter:sepia(.4) saturate(.7) brightness(1.45)}.node-title{font:600 15px var(--f-title);letter-spacing:.06em;line-height:1.4}.node-level{font:600 17px var(--f-title);color:#bde958;line-height:1.3}.node-level small{font:10px var(--f-mono);color:#c7a95c}.node-exp{font-size: 12px;color:#b2a374;white-space:nowrap}.node-bar{position:relative;width:74%;height:6px;margin-top:2px;border:1px solid #645528;background:#020402;box-shadow:inset 0 1px 3px #000}.node-bar i{display:block;height:100%;background:linear-gradient(#e0f892,#75b818 60%,#344b09);box-shadow:0 0 5px #a1e43065;transition:width .7s}.contract-node:hover .rune-setting,.contract-node:focus-visible .rune-setting{filter:drop-shadow(0 0 10px #a4ff3c8c)}.contract-node:focus-visible{outline:1px solid #b8e45b;outline-offset:2px}.wheel-sigil{position:absolute;bottom:-1%;left:50%;transform:translateX(-50%);font-size:27px;color:#9fbf3e;text-shadow:0 0 10px #6ebd24}@keyframes ritual-turn{to{transform:rotate(360deg)}}@media(prefers-reduced-motion:reduce){.wheel-runes{animation:none}.node-bar i{transition:none}}
</style>
<style scoped>
.contract-wheel{max-width:470px;aspect-ratio:1/1.02}.node-exp{font-size: 12px}.node-title{font-size:16px}.wheel-core span{font-size: 12px}
</style>
<style scoped>
.node-exp{font-size:12px}.wheel-core span{font-size: 12px}.node-level small{font-size:12px}
</style>
