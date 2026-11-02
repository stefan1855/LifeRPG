<script setup>
import { ref } from 'vue';
import { Copy, Check, X } from 'lucide-vue-next';
import {
  toastState,
  levelUpState,
  pauseToast,
  resumeToast,
  removeToast,
} from '../stores/toast';
import { copyText } from '../utils/clipboard';

const copiedId = ref(0);

async function onCopy(t) {
  const ok = await copyText(t.message);
  if (!ok) return;
  copiedId.value = t.id;
  setTimeout(() => {
    if (copiedId.value === t.id) copiedId.value = 0;
  }, 1800);
}
</script>

<template>
  <div class="toast-host">
    <transition-group name="toast">
      <div
        v-for="t in toastState.items"
        :key="t.id"
        class="toast"
        :class="t.type"
        @mouseenter="pauseToast(t.id)"
        @mouseleave="resumeToast(t.id)"
      >
        <span class="mark"></span>
        <span class="msg">{{ t.message }}</span>
        <button
          v-if="t.copyable"
          class="toast-act"
          :title="copiedId === t.id ? '已复制' : '复制错误信息'"
          @click="onCopy(t)"
        >
          <component :is="copiedId === t.id ? Check : Copy" :size="12" />
        </button>
        <button v-if="t.copyable" class="toast-act close" title="关闭" @click="removeToast(t.id)">
          <X :size="12" />
        </button>
      </div>
    </transition-group>
  </div>

  <transition name="lvup">
    <div v-if="levelUpState.visible" class="levelup">
      <div class="burst"></div>
      <div class="txt">
        <span class="small">SOUL ASCENDED</span>
        <span class="big num">Lv.{{ levelUpState.level }}</span>
        <span class="small">灵魂升华</span>
      </div>
    </div>
  </transition>
</template>

<style scoped>
.toast-host {
  position: fixed;
  right: 18px;
  bottom: 18px;
  z-index: 120;
  display: flex;
  flex-direction: column;
  gap: 8px;
  pointer-events: none;
}
.toast {
  display: flex;
  align-items: flex-start;
  gap: 9px;
  min-width: 200px;
  max-width: 420px;
  padding: 9px 12px;
  font-size: 12.5px;
  color: var(--t-title);
  background: linear-gradient(180deg, rgba(16, 24, 14, 0.97), rgba(5, 8, 5, 0.98));
  border: 1px solid rgba(109, 91, 42, 0.6);
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.8);
  /* 只有可交互的提示才接收鼠标，避免挡住下层 UI */
  pointer-events: none;
}
.toast.error,
.toast.warn {
  pointer-events: auto;
}
.mark {
  width: 6px;
  height: 6px;
  margin-top: 6px;
  background: var(--c-gold);
  transform: rotate(45deg);
  flex: 0 0 auto;
}
.msg {
  flex: 1;
  min-width: 0;
  line-height: 1.6;
  white-space: pre-wrap;
  word-break: break-word;
  user-select: text;
}
.toast-act {
  flex: 0 0 auto;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 20px;
  color: var(--t-sub);
  background: rgba(0, 0, 0, 0.4);
  border: 1px solid rgba(109, 91, 42, 0.55);
  cursor: pointer;
  transition: all 0.14s;
}
.toast-act:hover {
  color: var(--c-glow);
  border-color: var(--c-soul);
  box-shadow: 0 0 8px rgba(124, 255, 31, 0.35);
}
.toast-act.close:hover {
  color: #ffd9d3;
  border-color: rgba(224, 90, 74, 0.7);
  box-shadow: 0 0 8px rgba(224, 90, 74, 0.4);
}
.toast.success { border-color: rgba(124, 255, 31, 0.55); }
.toast.success .mark { background: var(--c-soul); box-shadow: var(--glow-soul); }
.toast.warn { border-color: rgba(224, 169, 74, 0.55); }
.toast.warn .mark { background: var(--t-warn); }
.toast.error { border-color: rgba(224, 90, 74, 0.6); }
.toast.error .mark { background: var(--t-danger); }

.toast-enter-active,
.toast-leave-active { transition: all 0.26s ease; }
.toast-enter-from { opacity: 0; transform: translateX(24px); }
.toast-leave-to { opacity: 0; transform: translateX(24px); }

.levelup {
  position: fixed;
  inset: 0;
  z-index: 130;
  display: flex;
  align-items: center;
  justify-content: center;
  pointer-events: none;
}
.burst {
  position: absolute;
  width: 520px;
  height: 520px;
  border-radius: 50%;
  background: radial-gradient(circle, rgba(124, 255, 31, 0.35) 0%, rgba(124, 255, 31, 0.08) 40%, transparent 70%);
  animation: burst 2.4s ease-out forwards;
}
@keyframes burst {
  0% { transform: scale(0.2); opacity: 0; }
  25% { transform: scale(1); opacity: 1; }
  100% { transform: scale(1.5); opacity: 0; }
}
.txt {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  animation: rise 2.4s ease-out forwards;
}
@keyframes rise {
  0% { transform: translateY(24px) scale(0.85); opacity: 0; }
  25% { transform: translateY(0) scale(1); opacity: 1; }
  80% { opacity: 1; }
  100% { transform: translateY(-34px); opacity: 0; }
}
.small {
  font-size: 12px;
  letter-spacing: 5px;
  color: var(--c-acid);
}
.big {
  font-family: var(--f-title);
  font-size: 56px;
  font-weight: 900;
  color: #eaffc0;
  text-shadow: var(--glow-soul-strong);
}
.lvup-enter-active, .lvup-leave-active { transition: opacity 0.3s; }
.lvup-enter-from, .lvup-leave-to { opacity: 0; }
</style>

<style scoped>
.toast-host{right:12px;bottom:12px;width:min(420px,calc(100vw - 24px))}
.toast{min-width:0;max-width:100%;max-height:calc(100dvh - 24px);overflow-y:auto}
</style>
