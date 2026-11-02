import { reactive } from 'vue';
import { sfx } from '../utils/audio';

/** 全局提示（灵魂低语） */
export const toastState = reactive({ items: [] });

let seq = 0;
/** 计时器与剩余时间放在响应式状态之外，避免被 Proxy 包裹 */
const timers = new Map();
const remaining = new Map();

function schedule(id, ms) {
  const t = setTimeout(() => removeToast(id), ms);
  timers.set(id, t);
  remaining.set(id, ms);
}

/** 手动关闭某条提示 */
export function removeToast(id) {
  const t = timers.get(id);
  if (t) clearTimeout(t);
  timers.delete(id);
  remaining.delete(id);
  const i = toastState.items.findIndex((x) => x.id === id);
  if (i >= 0) toastState.items.splice(i, 1);
}

/** 鼠标悬停时暂停倒计时，方便用户看清/复制 */
export function pauseToast(id) {
  const t = timers.get(id);
  if (t) {
    clearTimeout(t);
    timers.delete(id);
  }
  const left = remaining.get(id);
  if (typeof left === 'number') {
    remaining.set(id, Math.max(800, left));
  }
}

/** 移开鼠标后按剩余时间继续倒计时 */
export function resumeToast(id) {
  if (!toastState.items.some((x) => x.id === id)) return;
  if (timers.has(id)) return;
  schedule(id, remaining.get(id) ?? 2600);
}

/**
 * @param {string} message 提示文本
 * @param {'info'|'success'|'warn'|'error'} type 类型
 * @param {number} duration 停留毫秒
 * @param {boolean} copyable 是否显示复制按钮
 */
export function toast(message, type = 'info', duration = 2600, copyable = false) {
  const id = ++seq;
  toastState.items.push({ id, message, type, copyable });
  schedule(id, duration);
  return id;
}

export const notify = {
  info: (m) => toast(m, 'info', 2600, false),
  success: (m) => toast(m, 'success', 2600, false),
  warn: (m) => toast(m, 'warn', 6000, true),
  error: (m) => toast(m, 'error', 10000, true),
};

/** 升级飘字特效状态 */
export const levelUpState = reactive({ visible: false, level: 1 });

export function showLevelUp(level) {
  levelUpState.level = level;
  levelUpState.visible = true;
  sfx.levelUp();
  setTimeout(() => {
    levelUpState.visible = false;
  }, 2600);
}
