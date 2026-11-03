<script setup>
defineProps({ title: String, eyebrow: String, crest: { type: String, default: '✦' } });
</script>
<template>
  <section class="relic-panel reference-panel">
    <i class="relic-corner tl" aria-hidden="true"></i><i class="relic-corner tr" aria-hidden="true"></i>
    <i class="relic-corner bl" aria-hidden="true"></i><i class="relic-corner br" aria-hidden="true"></i>
    <header v-if="title" class="relic-head"><span class="relic-crest" aria-hidden="true">{{ crest }}</span><h2>{{ title }}</h2><span class="relic-rule" aria-hidden="true"></span><slot name="actions" /></header>
    <div class="relic-body"><slot /></div>
  </section>
</template>


<style scoped>
.relic-panel{position:relative;min-width:0;min-height:0;display:flex;flex-direction:column}.relic-head{display:flex;align-items:center;flex-wrap:wrap;gap:7px;min-height:47px;flex-shrink:0;padding:10px 15px 8px;border-bottom:1px solid #6d5b2a55;position:relative;z-index:2}.relic-head h2{font:600 20px/1.5 var(--f-title);letter-spacing:.08em;color:#d5d080;margin:0}.relic-crest{color:#d0c17b;font-size:17px}.relic-rule{height:1px;flex:1;min-width:8px;background:linear-gradient(90deg,#8b743066,transparent)}.relic-body{padding:13px 15px;position:relative;z-index:1;display:flex;flex-direction:column;gap:9px;flex:1;min-width:0;min-height:0}
</style>
