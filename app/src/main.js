import { createApp } from 'vue';
import './styles/tokens.css';
import './styles/base.css';
import './styles/reference.css';
import App from './App.vue';
import { notify } from './stores/toast';

const app = createApp(App);

/** 兜底：单个组件抛错不应该整块白屏 */
app.config.errorHandler = (err, _instance, info) => {
  const msg = err?.message || String(err);
  console.error('[LifeRPG] 组件异常', info, err);
  try {
    notify.error('界面出现异常：' + msg);
  } catch {
    /* 提示本身失败也不能再抛 */
  }
};

window.addEventListener('unhandledrejection', (e) => {
  const msg = e?.reason?.message || String(e?.reason ?? '未知错误');
  console.error('[LifeRPG] 未处理的 Promise 异常', e.reason);
  try {
    notify.error('操作失败：' + msg);
  } catch {
    /* ignore */
  }
});

window.addEventListener('error', (e) => {
  console.error('[LifeRPG] 运行时异常', e.error || e.message);
});

try {
  app.mount('#app');
} catch (err) {
  // 最坏情况：挂载失败也要给用户一条活路，而不是白屏
  console.error('[LifeRPG] 应用挂载失败', err);
  const root = document.getElementById('app');
  if (root) {
    root.innerHTML =
      '<div style="height:100%;display:flex;flex-direction:column;align-items:center;justify-content:center;' +
      'gap:10px;color:#cfea78;font-family:system-ui,sans-serif;background:#050805">' +
      '<div style="font-size:28px">☠</div>' +
      '<div style="font-size:14px">界面启动失败</div>' +
      '<div style="font-size:12px;color:#8d855f;max-width:520px;text-align:center;line-height:1.8">' +
      String(err?.message || err) +
      '<br/>请尝试重启程序；若持续失败，把此提示截图反馈。</div>' +
      '</div>';
  }
}
