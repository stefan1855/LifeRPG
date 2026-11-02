<script setup>
import { computed, onMounted, ref } from 'vue';
import GothicPanel from '../components/GothicPanel.vue';
import ExpBar from '../components/ExpBar.vue';
import { api, attrMeta } from '../api';
import { notify } from '../stores/toast';

const attrs = ref([]);
const loading = ref(true);

const DESCRIPTIONS = {
  physique: '身体是承载灵魂的容器。运动、睡眠、饮食与耐力。',
  mind: '认知与思考的深度。学习、阅读、专注与决策质量。',
  discipline: '对抗熵增的意志。作息、执行、坚持与自我约束。',
  expression: '向外传递的能力。表达、写作、沟通与影响力。',
  creativity: '从无到有的构建。创作、设计、洞察与想象力。',
  resources: '支撑行动的底气。财务、人脉、工具与时间余量。',
};

async function load() {
  loading.value = true;
  try {
    attrs.value = await api.attributes();
  } catch (e) {
    notify.error('加载属性失败：' + e.message);
  } finally {
    loading.value = false;
  }
}

const total = computed(() => attrs.value.reduce((s, a) => s + (a.experience || 0), 0));
const strongest = computed(() => {
  if (!attrs.value.length) return null;
  return attrs.value.reduce((a, b) => (a.experience > b.experience ? a : b));
});
const weakest = computed(() => {
  if (!attrs.value.length) return null;
  return attrs.value.reduce((a, b) => (a.experience < b.experience ? a : b));
});

onMounted(load);
</script>

<template>
  <div class="attrs-view">
    <GothicPanel title="成长属性" subtitle="ATTRIBUTES" class="main-panel">
      <div class="summary">
        <div class="sum-cell">
          <span class="sum-label">属性总经验</span>
          <span class="sum-value num">{{ total }}</span>
        </div>
        <div class="sum-cell">
          <span class="sum-label">最强契约</span>
          <span class="sum-value" :style="{ color: strongest ? attrMeta(strongest.attributeKey).color : '' }">
            {{ strongest ? attrMeta(strongest.attributeKey).label : '—' }}
          </span>
        </div>
        <div class="sum-cell">
          <span class="sum-label">最需滋养</span>
          <span class="sum-value" :style="{ color: weakest ? attrMeta(weakest.attributeKey).color : '' }">
            {{ weakest ? attrMeta(weakest.attributeKey).label : '—' }}
          </span>
        </div>
      </div>

      <div class="attr-grid scroll">
        <article
          v-for="a in attrs"
          :key="a.id"
          class="attr-card"
          :style="{ '--accent': attrMeta(a.attributeKey).color }"
        >
          <div class="ac-head">
            <span class="ac-sigil">{{ attrMeta(a.attributeKey).sigil }}</span>
            <div class="ac-title">
              <h3>{{ a.displayName }}</h3>
              <span class="ac-key">{{ a.attributeKey }}</span>
            </div>
            <span v-if="a.baseValue > 0" class="ac-bonus num">装备 +{{ a.baseValue }}</span>
            <span class="ac-level num">Lv.{{ a.level }}</span>
          </div>

          <p class="ac-desc">{{ DESCRIPTIONS[a.attributeKey] || '自定义属性' }}</p>

          <div class="ac-exp">
            <ExpBar
              :percent="a.progressPercent || 0"
              :label="`${a.expIntoCurrentLevel || 0} / ${a.expSpan || 100}`"
              :height="14"
            />
          </div>

          <div class="ac-foot">
            <span>累计 <b class="num">{{ a.experience }}</b> EXP</span>
            <span>升级还需 <b class="num">{{ a.expToNextLevel || 0 }}</b></span>
          </div>
        </article>
      </div>
    </GothicPanel>
  </div>
</template>

<style scoped>
.attrs-view { flex: 1; min-width: 0; display: flex; }
.main-panel { flex: 1; }
.summary {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 10px;
  flex: 0 0 auto;
}
.sum-cell {
  display: flex;
  flex-direction: column;
  gap: 2px;
  padding: 10px 14px;
  background: rgba(0, 0, 0, 0.35);
  border: 1px solid rgba(109, 91, 42, 0.35);
}
.sum-label { font-size: 12px; color: var(--t-time); letter-spacing: 0.1em; }
.sum-value { font-size: 20px; font-weight: 700; color: var(--t-num); font-family: var(--f-title); }

.attr-grid {
  flex: 1;
  min-height: 0;
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 12px;
  align-content: start;
  padding-right: 4px;
}
.attr-card {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 14px;
  background:
    radial-gradient(ellipse 100% 80% at 0% 0%, color-mix(in srgb, var(--accent) 8%, transparent), transparent 70%),
    linear-gradient(180deg, rgba(14, 20, 12, 0.9), rgba(5, 8, 5, 0.96));
  border: 1px solid rgba(109, 91, 42, 0.4);
  border-left: 2px solid var(--accent);
  transition: all 0.18s;
}
.attr-card:hover {
  border-color: color-mix(in srgb, var(--accent) 55%, transparent);
  box-shadow: 0 0 18px color-mix(in srgb, var(--accent) 18%, transparent);
}
.ac-head { display: flex; align-items: center; gap: 10px; }
.ac-sigil {
  font-size: 20px;
  color: var(--accent);
  text-shadow: 0 0 12px color-mix(in srgb, var(--accent) 60%, transparent);
}
.ac-title { flex: 1; }
.ac-title h3 {
  font-family: var(--f-title);
  font-size: 15px;
  letter-spacing: 0.1em;
  color: var(--t-title);
}
.ac-key { font-size: 12px; color: var(--t-disabled); letter-spacing: 0.14em; text-transform: uppercase; }
.ac-bonus {
  font-size: 12px;
  font-weight: 700;
  color: var(--c-glow);
  padding: 2px 7px;
  background: rgba(124, 255, 31, 0.1);
  border: 1px solid rgba(124, 255, 31, 0.4);
  text-shadow: 0 0 8px rgba(124, 255, 31, 0.5);
}
.ac-level { font-size: 13px; color: var(--accent); }
.ac-desc { font-size: 12px; color: var(--t-time); line-height: 1.7; min-height: 38px; }
.ac-foot {
  display: flex;
  justify-content: space-between;
  font-size: 12px;
  color: var(--t-time);
}
.ac-foot b { color: var(--t-bar); }
</style>
