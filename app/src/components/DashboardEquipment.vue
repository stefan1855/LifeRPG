<script setup>
import { computed } from 'vue';
import { EQUIP_SLOTS,attrMeta } from '../api';
import { themeAsset } from '../utils/referenceTheme';
const props=defineProps({snapshot:Object});
const emit=defineEmits(['navigate']);
const slotOrder=['head','shoulder','chest','wrist','hands','waist','legs','feet','trinket1','trinket2','mainhand','offhand'];
const textures={shoulder:'shoulders',wrist:'bracers',trinket1:'accessory-1',trinket2:'accessory-2'};
const slots=computed(()=>slotOrder.map(key=>{const slot=EQUIP_SLOTS.find(s=>s.key===key);return {...slot,item:props.snapshot?.slots?.find(s=>s.key===key)?.item,icon:themeAsset('equipment-'+(textures[key]||key))};}));
const count=computed(()=>slots.value.filter(s=>s.item).length);
const bonuses=computed(()=>props.snapshot?.totalBonuses||[]);
</script>
<template>
 <div class="equipment-preview"><div class="equipment-main">
  <div class="equipment-slots"><button v-for="slot in slots" :key="slot.key" class="equipment-slot" :class="{equipped:slot.item}" :title="slot.item?`${slot.label} · ${slot.item.gameItemName}，查看装备`:`${slot.label} · 未装备，打开装备库`" :aria-label="slot.item?`${slot.label}，${slot.item.gameItemName}`:`${slot.label}，未装备`" @click="emit('navigate','equipment')"><span class="slot-art"><img :src="slot.icon" alt=""/></span><span>{{ slot.label }}</span><i v-if="slot.item"></i></button></div>
  <button class="equipment-effigy" :aria-label="`已装备 ${count} 件，打开遗物具装`" @click="emit('navigate','equipment')"><span class="effigy-caption"><strong>{{ count?`${count} 件遗物`:'暂无装备' }}</strong><small>{{ count?'查看当前穿戴与加成':'去寻找强大的装备吧，提升你的力量。' }}</small></span></button>
 </div><div v-if="bonuses.length" class="equipment-bonuses"><span>装备加成</span><b v-for="bonus in bonuses" :key="bonus.attributeKey">{{ attrMeta(bonus.attributeKey).label }} +{{ bonus.bonusValue }}</b></div></div>
</template>
<style scoped>
.equipment-preview{display:flex;flex-direction:column;gap:8px;flex:1;min-height:0}.equipment-main{display:grid;grid-template-columns:86px minmax(0,1fr);gap:8px;flex:1;min-height:0}.equipment-slots{display:grid;grid-template-columns:minmax(0,1fr);grid-template-rows:repeat(12,minmax(32px,1fr));gap:3px;min-width:0}.equipment-slot{display:flex;align-items:center;gap:5px;position:relative;min-width:0;padding:0;border:0;background:transparent;color:#b9a66d;cursor:pointer;font:13px var(--f-body);white-space:nowrap}.slot-art{display:grid;place-items:center;width:35px;height:35px;flex-shrink:0;background:url('../assets/reference/equipment-empty-slot.webp') center/100% 100% no-repeat}.slot-art img{width:28px;height:28px;object-fit:contain;opacity:.66}.equipment-slot.equipped img{opacity:1;filter:drop-shadow(0 0 3px #8bcc3570)}.equipment-slot.equipped{color:#d4e994}.equipment-slot i{position:absolute;left:29px;top:3px;width:4px;height:4px;background:#c2ef64}.equipment-slot:hover{color:#e1e6ac}.equipment-effigy{position:relative;min-width:0;min-height:430px;border:0;cursor:pointer;background:#050805 url('../assets/reference/equipment-empty-scene.webp') center/100% 100% no-repeat;color:#bab78b}.effigy-caption{position:absolute;left:8px;right:8px;top:45%;display:flex;flex-direction:column;align-items:center;gap:8px;text-align:center}.effigy-caption strong{font-size:16px;font-weight:500;color:#d0c99b}.effigy-caption small{font-size:12px;line-height:1.9;color:#b5ad87}.equipment-bonuses{display:flex;flex-wrap:wrap;gap:4px 8px;color:#aba075;font-size:12px}.equipment-bonuses b{font-weight:400;color:#b8d777}
@media(max-width:1100px){.equipment-main{grid-template-columns:90px minmax(0,1fr)}}
</style>
