<script setup>
defineProps({ title: String, eyebrow: String, crest: { type: String, default: '✦' } });
</script>
<template>
  <section class="relic-panel">
    <i class="relic-corner tl" aria-hidden="true"></i><i class="relic-corner tr" aria-hidden="true"></i>
    <i class="relic-corner bl" aria-hidden="true"></i><i class="relic-corner br" aria-hidden="true"></i>
    <header v-if="title" class="relic-head"><span class="relic-crest" aria-hidden="true">{{ crest }}</span><h2>{{ title }}</h2><span v-if="eyebrow" class="relic-eyebrow">{{ eyebrow }}</span><span class="relic-rule" aria-hidden="true"></span><slot name="actions" /></header>
    <div class="relic-body"><slot /></div>
  </section>
</template>
<style scoped>
.relic-panel{position:relative;isolation:isolate;min-width:0;min-height:0;display:flex;flex-direction:column;border:1px solid #69532b;background:radial-gradient(ellipse at 50% 0,rgba(41,60,23,.22),transparent 65%),linear-gradient(150deg,#0b100bea,#030603f5);box-shadow:inset 0 0 0 3px #030603,inset 0 0 0 4px #302b18,0 5px 18px #0008}
.relic-panel::before{content:'';position:absolute;inset:3px;z-index:3;border:1px solid #8a733842;box-shadow:0 0 0 1px #010401,inset 0 1px 0 #d7bb5533;pointer-events:none}.relic-panel::after{content:'';position:absolute;inset:6px;z-index:-1;background:url('../assets/textures/mat_stone.webp') center/450px;opacity:.035;pointer-events:none}
.relic-corner{position:absolute;width:30px;height:30px;z-index:4;pointer-events:none;background:url('../assets/textures/orn_corner.webp') center/contain no-repeat;mix-blend-mode:screen;opacity:.85}.tl{top:-3px;left:-3px}.tr{top:-3px;right:-3px;transform:scaleX(-1)}.bl{bottom:-3px;left:-3px;transform:scaleY(-1)}.br{bottom:-3px;right:-3px;transform:scale(-1)}
.relic-head{display:flex;align-items:center;flex-wrap:wrap;gap:8px;min-height:44px;flex-shrink:0;padding:10px 16px 8px;border-bottom:1px solid #5a4d254f;position:relative;z-index:2}.relic-head h2{font:600 16px var(--f-title);letter-spacing:.1em;color:#e2d58c;line-height:1.45}.relic-crest{color:#c8d46c;font-size:15px}.relic-eyebrow{font:9px var(--f-title);letter-spacing:.15em;color:#93805a}.relic-rule{height:1px;flex:1;min-width:8px;background:linear-gradient(90deg,#8e743863,transparent)}.relic-body{padding:14px;position:relative;z-index:1;display:flex;flex-direction:column;gap:10px;flex:1;min-width:0;min-height:0}
</style>
<style scoped>
.relic-head h2{margin:0}.relic-head{min-height:42px}.relic-corner{opacity:.65;filter:sepia(.7) brightness(.72)}
</style>
