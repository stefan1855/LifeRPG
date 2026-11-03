<script setup>
import { computed } from 'vue';
import { attrMeta } from '../api';
import { themeAttribute, themeAsset } from '../utils/referenceTheme';
const props=defineProps({attributes:{type:Array,default:()=>[]},level:{type:Number,default:1}});
const emit=defineEmits(['navigate']);
const positions=[[32,17],[68,17],[21,50],[79,50],[32,83],[68,83]];
const nodes=computed(()=>['physique','mind','discipline','expression','creativity','resources'].map((key,i)=>{
 const a=props.attributes.find(a=>a.attributeKey===key)||{};
 return {...a,key,meta:attrMeta(key),theme:themeAttribute(key),x:positions[i][0],y:positions[i][1],side:i%2?'right':'left',middle:i===2||i===3,pct:Math.max(0,Math.min(100,a.progressPercent||0))};
}));
</script>
<template>
 <div class="contract-wheel" aria-label="六维暗影契约">
  <img class="wheel-orbit" :src="themeAsset('contract-orbit')" alt="" aria-hidden="true"/>
  <img class="wheel-floor" :src="themeAsset('ritual-pedestal')" alt="" aria-hidden="true"/>
  <img class="wheel-core" :src="themeAsset('contract-core-emblem')" alt="" aria-hidden="true"/>
  <button v-for="n in nodes" :key="n.key" class="contract-node" :class="[n.side,{middle:n.middle}]" :style="{left:n.x+'%',top:n.y+'%'}" :aria-label="`${n.theme.title} · ${n.meta.label}，等级 ${n.level||1}，查看成长属性`" @click="emit('navigate','attributes')">
   <span class="rune-setting"><img class="node-frame" :src="themeAsset('node-normal')" alt=""/><img class="node-frame hover-frame" :src="themeAsset('node-hover')" alt=""/><img class="node-glyph" :src="n.theme.rune" alt=""/></span>
   <span class="node-copy"><span class="node-title">{{ n.theme.title }}</span><span class="node-level">Lv. {{ n.level||1 }} <small>{{ n.meta.label }}</small></span><span class="node-exp num">{{ (n.expIntoCurrentLevel||0).toLocaleString() }} / {{ (n.expSpan||100).toLocaleString() }} EXP</span><span class="node-bar" :aria-label="`成长进度 ${n.pct.toFixed(1)}%`"><i :style="{width:n.pct+'%'}"></i></span></span>
  </button>
 </div>
</template>
<style scoped>
.contract-wheel{position:relative;width:100%;aspect-ratio:1/.92;max-width:570px;margin:auto;isolation:isolate;container:contract/inline-size}
.wheel-orbit{position:absolute;left:17%;top:10%;width:66%;height:78%;object-fit:fill;pointer-events:none;opacity:.8}
.wheel-floor{position:absolute;left:0;bottom:-9%;width:100%;height:28%;object-fit:fill;pointer-events:none;opacity:.8}
.wheel-core{position:absolute;left:31%;top:31%;width:38%;height:38%;object-fit:contain;pointer-events:none}
.contract-node{position:absolute;transform:translate(-50%,-50%);width:20%;aspect-ratio:1;background:none;border:0;padding:0;color:#d8cc8e;cursor:pointer;font-family:var(--f-body)}
.rune-setting{position:absolute;inset:0;display:grid;place-items:center}.node-frame{position:absolute;inset:-12%;width:124%;height:124%;object-fit:contain}.hover-frame{opacity:0;transition:opacity .18s}.contract-node:hover .hover-frame,.contract-node:focus-visible .hover-frame{opacity:1}.node-glyph{position:relative;width:43%;height:43%;object-fit:contain}
.node-copy{position:absolute;top:5%;width:24cqi;display:flex;flex-direction:column;align-items:center;gap:2px;pointer-events:none}.left .node-copy{right:91%}.right .node-copy{left:91%}.node-title{font-size:clamp(13px,3.15cqi,17px);font-weight:600;white-space:nowrap;line-height:1.5;color:#d9ce83}.node-level{font:700 clamp(17px,4cqi,22px)/1.2 var(--f-mono);color:#b8ea54;white-space:nowrap}.node-level small{font:400 11px var(--f-body);color:#a9a178}.node-exp{font-size:clamp(10px,2.4cqi,12px);color:#bfb186;white-space:nowrap;line-height:1.5}.node-bar{display:block;width:90%;height:9px;padding:2px;background:url('../assets/reference/stat-trough.webp') center/100% 100% no-repeat}.node-bar i{display:block;height:100%;background:url('../assets/reference/experience-fill.webp') left center/140px 100%;transition:width .5s}
.middle .node-copy{top:85%;width:24cqi}.middle.left .node-copy{right:20%}.middle.right .node-copy{left:20%}
.contract-node:focus-visible{outline:1px solid #badb65;outline-offset:4px}
@container contract (max-width:360px){.node-level small{display:none}.node-title{font-size:12px}.node-copy{width:26cqi}.node-exp{font-size:10px}.middle .node-copy{width:30cqi;top:78%}}
@media(prefers-reduced-motion:reduce){.hover-frame,.node-bar i{transition:none}}
</style>
