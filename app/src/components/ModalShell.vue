<script setup>
import { ref, computed, nextTick, useId, onMounted, onUnmounted } from 'vue';

const props=defineProps({
  state: { default: undefined },
  busy: { type:Boolean, default:false },
  initialDirty: { type:Boolean, default:false },
  title: { type: String, default: '' },
  subtitle: { type: String, default: '' },
  width: { type: Number, default: 520 },
  danger: { type: Boolean, default: false },
});
const emit = defineEmits(['close']);

const panel = ref(null);
const dirty = ref(false);
const baseline=JSON.stringify(props.state);
const changed=computed(()=>props.initialDirty || (props.state===undefined?dirty.value:JSON.stringify(props.state)!==baseline));
const discarding = ref(false);
const headingId = useId();
let previousFocus;
function close() {
  if(props.busy)return;
  if (changed.value) discarding.value = true;
  else emit('close');
}
function onKey(e) {
  if (e.key === 'Escape') { e.stopPropagation(); close(); }
  if (e.key !== 'Tab') return;
  const nodes = [...(panel.value?.querySelectorAll('button:not(:disabled),input:not(:disabled),textarea:not(:disabled),select:not(:disabled),[tabindex="0"]') || [])].filter(n => n.getClientRects().length);
  const first = nodes[0], last = nodes.at(-1);
  if (!first) { e.preventDefault(); return; }
  if (e.shiftKey && (document.activeElement === first || !panel.value.contains(document.activeElement))) { e.preventDefault(); last.focus(); }
  else if (!e.shiftKey && (document.activeElement === last || !panel.value.contains(document.activeElement))) { e.preventDefault(); first.focus(); }
}
onMounted(async () => {
  previousFocus = document.activeElement;
  window.addEventListener('keydown', onKey);
  await nextTick();
  (panel.value?.querySelector('input,textarea,select') || panel.value)?.focus();
});
onUnmounted(() => { window.removeEventListener('keydown', onKey); if (previousFocus?.isConnected) previousFocus.focus(); });
</script>

<template>
  <Teleport to="body">
  <div class="overlay" @mousedown.self="close">
    <div ref="panel" role="dialog" aria-modal="true" :aria-labelledby="headingId" tabindex="-1" @input="dirty = true" @change="dirty = true" class="modal fade-up" :style="{ width: width + 'px' }" :class="{ danger }">
      <span class="corner tl"></span><span class="corner tr"></span>
      <span class="corner bl"></span><span class="corner br"></span>
      <header class="modal-head">
        <div>
          <h3 :id="headingId">{{ title }}</h3>
          <p v-if="subtitle">{{ subtitle }}</p>
        </div>
        <button class="close" aria-label="关闭弹窗" :disabled="busy" @click="close">✕</button>
      </header>
      <div v-if="discarding" class="discard-prompt" role="alert">
        <span>有尚未保存的内容，是否放弃？</span>
        <button class="btn btn-ghost" @click="discarding = false">继续编辑</button>
        <button class="btn btn-danger" @click="emit('close')">放弃修改</button>
      </div>
      <div class="modal-body" :inert="busy || undefined">
        <slot />
      </div>
      <footer v-if="$slots.footer" class="modal-foot">
        <slot name="footer" :close="close" />
      </footer>
    </div>
  </div>
  </Teleport>
</template>

<style scoped>
.overlay {
  position: fixed;
  inset: 0;
  z-index: 90;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(2, 4, 2, 0.72);
  backdrop-filter: blur(6px);
}
.modal {
  position: relative;
  max-width: calc(100vw - 24px);
  max-height: calc(100dvh - 24px);
  display: flex;
  flex-direction: column;
  background:
    radial-gradient(ellipse 100% 70% at 50% 0%, rgba(22, 34, 18, 0.9), transparent 70%),
    linear-gradient(180deg, #0b120b, #050805);
  border: 1px solid rgba(192, 166, 76, 0.45);
  box-shadow: 0 24px 70px rgba(0, 0, 0, 0.9), 0 0 40px rgba(124, 255, 31, 0.06);
}
.modal.danger {
  border-color: rgba(224, 90, 74, 0.5);
}
.corner {
  position: absolute;
  width: 16px;
  height: 16px;
  background: linear-gradient(135deg, var(--c-gold-bright), var(--c-bronze) 60%, transparent 61%);
  clip-path: polygon(0 0, 100% 0, 0 100%);
}
.tl { top: -1px; left: -1px; }
.tr { top: -1px; right: -1px; transform: scaleX(-1); }
.bl { bottom: -1px; left: -1px; transform: scaleY(-1); }
.br { bottom: -1px; right: -1px; transform: scale(-1, -1); }

.modal-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  padding: 16px 18px 12px;
  border-bottom: 1px solid rgba(109, 91, 42, 0.35);
}
.modal-head h3 {
  font-family: var(--f-title);
  font-size: 16px;
  letter-spacing: 0.08em;
  color: var(--t-title);
}
.modal-head p {
  margin-top: 3px;
  font-size: 12px;
  color: var(--t-time);
}
.close {
  width: 26px;
  height: 26px;
  color: var(--t-sub);
  background: transparent;
  border: 1px solid #2b2412;
  cursor: pointer;
}
.close:hover {
  color: var(--c-glow);
  border-color: var(--c-gold);
}
.modal-body {
  padding: 16px 18px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.modal-foot {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  padding: 12px 18px 16px;
  border-top: 1px solid rgba(109, 91, 42, 0.28);
}
</style>

<style scoped>
.discard-prompt { padding: 12px 18px; display: flex; flex-wrap: wrap; gap: 8px; border-bottom: 1px solid var(--t-warn); color: var(--t-warn); }
.modal-head,.modal-foot { flex-shrink: 0; }
.modal-body { min-height: 0; }
</style>
