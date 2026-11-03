<script setup>
import { onMounted, ref, watch } from 'vue';
import { open } from '@tauri-apps/plugin-dialog';
import { getCurrentWindow } from '@tauri-apps/api/window';
const win = getCurrentWindow();
import { FolderOpen, HardDrive, ShieldCheck, Sparkles, Copy, CheckCircle2, AlertTriangle, Wand2 } from 'lucide-vue-next';
import { api } from '../api';
import { notify } from '../stores/toast';
import { copyText } from '../utils/clipboard';

const emit = defineEmits(['ready']);

const state = ref(null);
const chosen = ref('');
const busy = ref(false);

/** 预检结果：null=未检查，{ok:true}=可用，{ok:false}=不可用 */
const check = ref(null);
const checking = ref(false);
/** 初始化失败时的常驻错误（含完整文本，便于复制） */
const lastError = ref('');
const copied = ref(false);
const suggested = ref('');
async function recoverFile(){
  if(busy.value)return;
  busy.value=true;
  try{const source=await open({multiple:false,filters:[{name:'配置 JSON',extensions:['json']}]});if(!source)return;await api.configRecover(source,null);await load();if(state.value?.dataDir)emit('ready');}
  catch(e){lastError.value=e.message;}finally{busy.value=false;}
}

async function load() {
  try {
    state.value = await api.appState();
    if (state.value?.dataDir) chosen.value = state.value.dataDir;
  } catch (e) {
    notify.error('读取配置失败：' + e.message);
  }
}

async function pick() {
  try {
    const dir = await open({
      directory: true,
      title: '选择 LifeRPG 数据目录（建议放至非系统盘）',
    });
    if (dir) {
      chosen.value = typeof dir === 'string' ? dir : dir[0];
    }
  } catch (e) {
    notify.error('选择目录失败：' + (e?.message || String(e)));
  }
}

/** 预检：能否在该位置建目录并写入 */
async function runCheck() {
  const p = chosen.value.trim();
  if (!p) {
    check.value = null;
    return;
  }
  checking.value = true;
  try {
    const r = await api.dataDirCheck(p);
    check.value = r;
    if (r?.suggestedPath) suggested.value = r.suggestedPath;
  } catch (e) {
    check.value = { ok: false, message: e?.message || String(e) };
  } finally {
    checking.value = false;
  }
}

watch(chosen, () => {
  lastError.value = '';
  copied.value = false;
  runCheck();
});

/** 一键改用推荐目录（程序目录下的 LifeRPG-Data，必定可写） */
function useSuggested() {
  if (!suggested.value) return;
  chosen.value = suggested.value;
}

async function copyError() {
  const text = lastError.value;
  if (!text) return;
  const ok = await copyText(text);
  if (!ok) {
    notify.warn('复制失败，请手动选中文本复制');
    return;
  }
  copied.value = true;
  setTimeout(() => { copied.value = false; }, 2000);
}

async function confirm() {
  if (!chosen.value.trim()) {
    notify.warn('请先选择一个数据目录');
    return;
  }
  lastError.value = '';
  busy.value = true;
  try {
    if(state.value?.configError) await api.configRecover(null,chosen.value.trim());
    const st = await api.dataDirInit(chosen.value.trim());
    notify.success('数据目录已就绪');
    emit('ready', st);
  } catch (e) {
    const msg = e?.message || String(e);
    lastError.value = msg;
    notify.error('初始化失败：' + msg);
    // 失败后顺手刷新一次预检，把最新诊断展示出来
    runCheck();
  } finally {
    busy.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div class="setup">
    <div class="app-bg"></div>
    <div class="app-mist"></div>

    <div class="card fade-up">
      <span class="corner tl"></span><span class="corner tr"></span>
      <span class="corner bl"></span><span class="corner br"></span>

      <header class="head" data-tauri-drag-region>
        <button class="setup-close" aria-label="隐藏到托盘" title="隐藏到托盘" @click="win.hide()">✕</button>
        <div class="sigil">☠</div>
        <h1 class="title-gothic" data-tauri-drag-region>初 次 唤 醒</h1>
        <p class="sub">SOUL CONTAINER INITIALIZATION</p>
      </header>

      <div class="body">
        <div v-if="state?.configError" class="err-panel" role="alert"><b>配置需要恢复</b><p>{{ state.configError }}</p><code>{{ state.configPath }}</code><p>可导入有效配置，或选择数据目录重新设置。恢复前会保留原配置文件。</p><button class="btn" :disabled="busy" @click="recoverFile">导入配置文件</button></div>
        <p class="lead">
          在你开始记录之前，先指定一处<b>灵魂容器</b>——数据库、图片证据与备份都会存放在这里。
        </p>

        <div class="facts">
          <div class="fact">
            <HardDrive :size="14" />
            <div>
              <span class="f-title">程序本体</span>
              <code>{{ state?.exeDir || '—' }}</code>
            </div>
          </div>
          <div class="fact">
            <ShieldCheck :size="14" />
            <div>
              <span class="f-title">运行时缓存</span>
              <code>{{ state?.runtimeDir || '—' }}</code>
            </div>
          </div>
          <div class="fact">
            <Sparkles :size="14" />
            <div>
              <span class="f-title">便携配置</span>
              <code>{{ state?.configPath || '—' }}</code>
            </div>
          </div>
        </div>

        <div class="pick">
          <div class="pick-label">数据目录</div>
          <div class="pick-row">
            <input
              v-model="chosen"
              class="input path-input"
              placeholder="点击右侧按钮选择，或直接输入路径（如 D:\LifeRPG-Data）"
            />
            <button class="btn btn-ghost btn-sm" @click="pick">
              <FolderOpen :size="13" /> 选择目录
            </button>
          </div>
          <p class="hint">
            目录内会自动生成 <code>data\</code>、<code>images\</code>、<code>backups\</code> 三个子目录。支持点击按钮弹出窗口挑选，也可以直接手动粘贴或修改路径。
          </p>

          <!-- 预检状态 -->
          <div v-if="checking" class="check-line checking">正在检查目录可用性…</div>
          <div v-else-if="check?.ok" class="check-line ok">
            <CheckCircle2 :size="12" /> 目录可用，可以写入
          </div>
          <div v-else-if="check && !check.ok" class="check-line bad">
            <AlertTriangle :size="12" /> 该目录当前不可用
          </div>
        </div>

        <!-- 失败时的常驻错误面板：完整文本 + 复制 + 改用推荐目录 -->
        <div v-if="lastError" class="err-panel">
          <div class="err-head">
            <AlertTriangle :size="13" />
            <span>初始化失败</span>
            <button class="err-copy" :title="copied ? '已复制' : '复制错误信息'" @click="copyError">
              <component :is="copied ? CheckCircle2 : Copy" :size="12" />
              {{ copied ? '已复制' : '复制' }}
            </button>
          </div>
          <pre class="err-body">{{ lastError }}</pre>
          <div class="err-actions">
            <button v-if="suggested" class="btn btn-primary btn-sm" @click="useSuggested">
              <Wand2 :size="12" /> 改用推荐目录
            </button>
            <button class="btn btn-ghost btn-sm" @click="pick">
              <FolderOpen :size="12" /> 重新选择
            </button>
          </div>
          <p v-if="suggested" class="err-suggest">推荐目录：<code>{{ suggested }}</code></p>
        </div>
      </div>

      <footer class="foot">
        <span class="privacy">全部数据保存在本地</span>
        <button class="btn btn-primary" :disabled="busy || !chosen" @click="confirm">
          {{ busy ? '正在初始化…' : '开始使用' }}
        </button>
      </footer>
    </div>
  </div>
</template>

<style scoped>
.setup {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  overflow: hidden;
}
/* 深渊暗角：压暗四周，把视线收进卡片 */
.setup::after {
  content: '';
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  background:
    radial-gradient(ellipse 42% 38% at 50% 42%, rgba(124, 255, 31, 0.12), transparent 70%),
    radial-gradient(ellipse 100% 85% at 50% 50%, transparent 18%, rgba(0, 0, 0, 0.96) 100%);
}
.card {
  position: relative;
  z-index: 1;
  width: 640px;
  background:
    radial-gradient(ellipse 100% 60% at 50% 0%, rgba(24, 34, 18, 0.75), transparent 70%),
    linear-gradient(180deg, rgba(11, 18, 11, 0.98), rgba(5, 8, 5, 0.99));
  border: 1px solid rgba(192, 166, 76, 0.45);
  box-shadow: 0 30px 80px rgba(0, 0, 0, 0.9), 0 0 50px rgba(124, 255, 31, 0.07);
}
.card::after {
  content: '';
  position: absolute;
  inset: 0;
  border: 13px solid transparent;
  border-image: url('../assets/reference/panel-frame-clean.webp') 180 / 24px stretch;
  pointer-events: none;
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

.head {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  padding: 26px 0 16px;
}
.sigil {
  font-size: 26px;
  color: var(--c-glow);
  text-shadow: var(--glow-soul-strong);
  animation: soulPulse 3.2s ease-in-out infinite;
}
.title {
  font-size: 24px;
  font-weight: 900;
  letter-spacing: 0.2em;
}
.sub {
  font-size: 12px;
  letter-spacing: 0.32em;
  color: var(--t-disabled);
}

.body {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 0 28px 8px;
}
.lead {
  font-size: 12.5px;
  line-height: 1.9;
  color: var(--t-body);
}
.lead b { color: var(--t-title); }

.facts {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 12px 14px;
  background: rgba(0, 0, 0, 0.4);
  border: 1px solid rgba(109, 91, 42, 0.32);
}
.fact {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  color: var(--c-gold);
}
.f-title {
  display: block;
  font-size: 12px;
  color: var(--t-sub);
  letter-spacing: 0.06em;
}
.fact code {
  display: block;
  margin-top: 2px;
  font-family: var(--f-mono);
  font-size: 12px;
  color: var(--t-time);
  word-break: break-all;
  user-select: text;
}

.pick { display: flex; flex-direction: column; gap: 7px; }
.pick-label {
  font-size: 12px;
  color: var(--t-sub);
  letter-spacing: 0.1em;
}
.pick-row { display: flex; align-items: center; gap: 10px; }
.path-input {
  flex: 1;
  font-family: var(--f-mono);
  font-size: 12px;
  color: var(--t-title);
  background: rgba(0, 0, 0, 0.55);
  border: 1px solid #2b2412;
}
.hint {
  font-size: 12px;
  line-height: 1.8;
  color: var(--t-time);
}
.hint b { color: var(--t-panel); }
.hint code {
  font-family: var(--f-mono);
  color: var(--t-sub);
}

/* 预检状态行 */
.check-line {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  line-height: 1.6;
}
.check-line.ok { color: var(--c-glow); }
.check-line.bad { color: var(--t-warn); }
.check-line.checking { color: var(--t-time); }

/* 失败错误面板 */
.err-panel {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 12px 14px;
  background: rgba(38, 12, 8, 0.55);
  border: 1px solid rgba(224, 90, 74, 0.55);
  box-shadow: inset 0 1px 0 rgba(224, 90, 74, 0.18);
}
.err-head {
  display: flex;
  align-items: center;
  gap: 7px;
  font-size: 12px;
  font-weight: 700;
  color: #ffb3a8;
  letter-spacing: 0.06em;
}
.err-copy {
  margin-left: auto;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 2px 8px;
  font-size: 12px;
  color: #ffd9d3;
  background: rgba(0, 0, 0, 0.45);
  border: 1px solid rgba(224, 90, 74, 0.5);
  cursor: pointer;
  transition: all 0.14s;
}
.err-copy:hover {
  color: #fff;
  border-color: #ff8a76;
  box-shadow: 0 0 8px rgba(224, 90, 74, 0.45);
}
.err-body {
  max-height: 190px;
  overflow: auto;
  margin: 0;
  padding: 8px 10px;
  font-family: var(--f-mono);
  font-size: 12px;
  line-height: 1.75;
  color: #e8d9d2;
  background: rgba(0, 0, 0, 0.55);
  border: 1px solid rgba(109, 91, 42, 0.35);
  white-space: pre-wrap;
  word-break: break-all;
  user-select: text;
}
.err-actions { display: flex; gap: 8px; }
.err-suggest {
  font-size: 12px;
  color: var(--t-time);
  word-break: break-all;
}
.err-suggest code {
  font-family: var(--f-mono);
  color: var(--t-sub);
}

.foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 28px 24px;
}
.privacy {
  font-size: 12px;
  letter-spacing: 0.12em;
  color: var(--t-disabled);
}
</style>

<style scoped>
.setup{padding:12px;overflow:auto}.card{max-width:100%;max-height:calc(100dvh - 24px);overflow-y:auto;flex-shrink:0}
.head{position:relative}.setup-close{position:absolute;right:18px;top:18px;width:28px;height:28px;color:var(--t-sub);background:transparent;border:1px solid var(--c-bronze);cursor:pointer}
</style>
