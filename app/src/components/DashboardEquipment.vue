<script setup>
import { computed } from 'vue';
import { EQUIP_SLOTS, attrMeta } from '../api';
const props = defineProps({ snapshot: Object });
const emit = defineEmits(['navigate']);
const slots = computed(() => EQUIP_SLOTS.map(slot => ({...slot,item:props.snapshot?.slots?.find(s=>s.key===slot.key)?.item,icon:new URL(`../assets/icons/slot_${slot.key}.svg`,import.meta.url).href})));
const count = computed(() => slots.value.filter(s=>s.item).length);
const bonuses = computed(() => props.snapshot?.totalBonuses || []);
</script>
<template>
  <div class="equipment-preview">
    <div class="equipment-main">
      <div class="equipment-slots"><button v-for="slot in slots" :key="slot.key" class="equipment-slot" :class="{equipped:slot.item}" :title="slot.item ? `${slot.label} · ${slot.item.gameItemName}，查看装备` : `${slot.label} · 未装备，打开装备库`" :aria-label="slot.item ? `${slot.label}，${slot.item.gameItemName}` : `${slot.label}，未装备`" @click="emit('navigate','equipment')"><img :src="slot.icon" alt="" /><span>{{ slot.label }}</span><i v-if="slot.item"></i></button></div>
      <button class="equipment-effigy" :aria-label="`已装备 ${count} 件，打开遗物具装`" @click="emit('navigate','equipment')"><img src="../assets/dashboard/faceless-cutout-v1.png" alt="" /><span class="effigy-caption"><strong>{{ count ? `${count} 件遗物` : '暂无装备' }}</strong><small>{{ count ? '查看当前穿戴与加成' : '进入装备库，装配现实之力' }}</small></span></button>
    </div>
    <div class="equipment-bonuses"><span>装备加成</span><b v-if="!bonuses.length">—</b><b v-for="bonus in bonuses" :key="bonus.attributeKey">{{ attrMeta(bonus.attributeKey).label }} +{{ bonus.bonusValue }}</b></div>
  </div>
</template>
<style scoped>
.equipment-preview{display:flex;flex-direction:column;gap:10px;flex:1;min-height:0}.equipment-main{display:flex;gap:8px;flex:1;min-height:0}.equipment-slots{width:70px;display:flex;flex-direction:column;gap:3px;flex-shrink:0;overflow-y:auto;min-height:0;padding:2px}.equipment-slot{position:relative;min-height:28px;flex:1 0 28px;max-height:34px;display:flex;align-items:center;gap:5px;padding:2px 3px;border:1px solid #514627;background:linear-gradient(135deg,#202319,#050805);color:#b4a16b;cursor:pointer;font:11px var(--f-body);box-shadow:inset 0 0 0 1px #020402}.equipment-slot img{width:22px;height:22px;object-fit:contain;opacity:.65}.equipment-slot.equipped{border-color:#71863e;color:#dceba3;background:linear-gradient(135deg,#2b391a,#0b1207)}.equipment-slot.equipped img{opacity:1}.equipment-slot i{position:absolute;right:1px;top:1px;width:3px;height:3px;background:#b6ee46;box-shadow:0 0 4px #8ad027}.equipment-slot:hover,.equipment-slot:focus-visible{border-color:#c2cf70;color:#ebf1bd}.equipment-effigy{position:relative;isolation:isolate;flex:1;min-width:0;min-height:280px;overflow:hidden;border:1px solid #465123;background:radial-gradient(ellipse at 50% 25%,#284e0c80,transparent 70%),url('../assets/theme/bg_void.png') center/cover;cursor:pointer;color:#b5bd82}.equipment-effigy img{position:absolute;inset:0;width:100%;height:100%;object-fit:cover;object-position:center top;opacity:.17;filter:brightness(.5) sepia(.8) hue-rotate(25deg)}.equipment-effigy::after{content:'';position:absolute;inset:0;background:linear-gradient(transparent,#02070275 65%,#020502)}.effigy-caption{position:absolute;left:8px;right:8px;top:49%;z-index:1;display:flex;flex-direction:column;gap:7px;align-items:center;text-align:center}.effigy-caption strong{font:600 15px var(--f-title);color:#c6c98c}.effigy-caption small{font:11px/1.8 var(--f-body);color:#9e9c70}.equipment-bonuses{display:flex;flex-wrap:wrap;gap:4px 8px;padding-top:8px;border-top:1px solid #50452966;font-size: 12px;color:#95865e}.equipment-bonuses b{font-weight:400;color:#aecd68}
</style>
<style scoped>
.equipment-slot{font-size:12px;min-height:29px;flex-basis:29px}.equipment-bonuses,.effigy-caption small{font-size:12px}
</style>
<style scoped>
.equipment-slots{width:146px;display:grid;grid-template-columns:repeat(2,minmax(0,1fr));align-content:start;gap:4px;overflow-y:auto}.equipment-slot{min-width:0;min-height:32px;max-height:none;gap:3px}.equipment-slot img{width:20px;height:20px}.equipment-slot span{white-space:nowrap}.equipment-effigy{min-height:280px}.equipment-bonuses{line-height:1.6}
</style>
