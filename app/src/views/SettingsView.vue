<script setup>
const saving = ref(false);
import { computed, onMounted, reactive, ref } from 'vue';
import { open } from '@tauri-apps/plugin-dialog';
import { Save, FolderOpen, RotateCcw, Download, Keyboard, Power, Info, HardDrive, Sparkles, FlaskConical, Volume2, VolumeX, Play, Plus, Trash2, Globe, Cpu, CheckCircle2, AlertCircle } from 'lucide-vue-next';
import GothicPanel from '../components/GothicPanel.vue';
import ModalShell from '../components/ModalShell.vue';
import ImageMaintenance from '../components/ImageMaintenance.vue';
import NotificationSettings from '../components/NotificationSettings.vue';
import { api, fmtDateTime } from '../api';
import { notify } from '../stores/toast';
import { sfx } from '../utils/audio';

const settings = reactive({ hotkey: 'CommandOrControl+Shift+L', notificationEnabled: true, opacity: 97 });
const sfxEnabled = ref(sfx.isEnabled());

function toggleSfx() {
  const state = sfx.toggle();
  sfxEnabled.value = state;
  notify.info(state ? '音效系统已开启' : '音效系统已静音');
}
const ai = reactive({ enabled: false, baseUrl: '', model: '', modelLight: '', apiKey: '' });
const aiTesting = ref(false);
const autostart = ref(false);
const backups = ref([]);
const dataDir = ref('');
const exeDir = ref('');
const runtimeDir = ref('');
const restoring = ref(null);
const busy = ref(false);
const migrateTarget = ref('');

// ============================ 外接神域 (MCP) ============================
const mcpServers = ref([]);
const mcpTesting = ref({});
const mcpTools = ref({});
const showMcpEditor = ref(false);
const editingMcp = reactive({
  id: '',
  name: '',
  command: 'python',
  argsStr: '',
  envStr: '',
  enabled: true,
});

function openCreateMcp() {
  Object.assign(editingMcp, {
    id: 'mcp-' + Date.now(),
    name: '',
    command: 'python',
    argsStr: '',
    envStr: '',
    enabled: true,
  });
  showMcpEditor.value = true;
}

function openEditMcp(s) {
  Object.assign(editingMcp, {
    id: s.id,
    name: s.name,
    command: s.command,
    argsStr: JSON.stringify(s.args || []),
    envStr: Object.entries(s.env || {}).map(([k, v]) => `${k}=${v}`).join('\n'),
    enabled: s.enabled,
  });
  showMcpEditor.value = true;
}

function splitArgsWithQuotes(raw) {
  if (raw.trim().startsWith('[')) {
    const values = JSON.parse(raw);
    if (!Array.isArray(values) || values.some(v => typeof v !== 'string')) throw new Error('参数必须是字符串数组');
    return values;
  }
  const out = [];
  let token = '', quote = null, started = false;
  for (let i = 0; i < raw.length; i++) {
    const ch = raw[i];
    if (quote) {
      if (ch === '\\' && raw[i + 1] === quote) { token += quote; i++; }
      else if (ch === quote) quote = null;
      else token += ch;
    } else if (ch === '"' || ch === "'") { quote = ch; started = true; }
    else if (/\s/.test(ch)) { if (started) { out.push(token); token = ''; started = false; } }
    else { token += ch; started = true; }
  }
  if (quote) throw new Error('引号没有闭合');
  if (started) out.push(token);
  return out;
}

async function submitMcp() {
  if (!editingMcp.name.trim() || !editingMcp.command.trim()) {
    notify.warn('名称与执行命令不能为空');
    return;
  }
  let args;
  if (saving.value) return;
  saving.value = true;
  try { args = splitArgsWithQuotes(editingMcp.argsStr.trim()); } catch { saving.value = false; notify.warn('参数引号或转义格式不正确'); return; }
  const env = {};
  for (const line of editingMcp.envStr.split('\n')) {
    const trimmed = line.trim();
    if (!trimmed || !trimmed.includes('=')) continue;
    const idx = trimmed.indexOf('=');
    const k = trimmed.slice(0, idx).trim();
    const v = trimmed.slice(idx + 1).trim();
    if (k) env[k] = v;
  }

  try {
    const updated = await api.mcpSaveServer({
      id: editingMcp.id,
      name: editingMcp.name.trim(),
      command: editingMcp.command.trim(),
      args,
      env,
      enabled: editingMcp.enabled,
    });
    mcpServers.value = updated;
    showMcpEditor.value = false;
    notify.success('MCP 契约已保存');
  } catch (e) {
    notify.error('保存失败：' + e.message);
  } finally { saving.value = false; }
}

// MCP 工具交互执行台
const activeToolModal = ref(null);
const toolArgsJson = ref('{}');
const toolCalling = ref(false);
const toolFailed = ref(false);
const notificationRevision = ref(0);
const toolResult = ref('');

let toolRevision=0;
const resultContext=ref(null);
function closeToolRunner(){if(saving.value)return;toolRevision++;activeToolModal.value=null;toolCalling.value=false;resultContext.value=null;}
function openToolRunner(server, tool) {
  toolRevision++;toolCalling.value=false;resultContext.value=null;
  activeToolModal.value = { server, tool };
  toolArgsJson.value = '{}';
  toolResult.value = '';
  toolFailed.value = false;
}

async function executeMcpTool() {
  if (!activeToolModal.value) return;
  let parsedArgs = {};
  try {
    parsedArgs = toolArgsJson.value.trim() ? JSON.parse(toolArgsJson.value) : {};
    if (!parsedArgs || typeof parsedArgs !== 'object' || Array.isArray(parsedArgs)) throw new Error('请输入 JSON 对象');
  } catch (e) {
    notify.warn('参数格式不是有效 JSON：' + e.message);
    return;
  }
  if(toolCalling.value)return;
  const revision=++toolRevision;
  const context={server:{...activeToolModal.value.server},tool:{...activeToolModal.value.tool}};
  toolCalling.value = true;
  toolResult.value = '';
  toolFailed.value = false;
  try {
    const res = await api.mcpCallTool(
      context.server.id,
      context.tool.name,
      parsedArgs
    );
    if(revision!==toolRevision)return;
    resultContext.value=context;
    toolResult.value = res?.content || JSON.stringify(res, null, 2);
    toolFailed.value = !!res?.isError;
    if (res?.isError) {
      notify.warn('工具返回了错误状态');
    } else {
      sfx.hammer();
      notify.success('工具调用完成');
    }
  } catch (e) {
    if(revision!==toolRevision)return;
    toolFailed.value = true;
    toolResult.value = '调用失败：' + e.message;
    notify.error('调用失败：' + e.message);
  } finally {
    if(revision===toolRevision)toolCalling.value = false;
  }
}

async function saveToolResultAsRecord() {
  if (toolCalling.value || toolFailed.value || !toolResult.value.trim() || !resultContext.value || !activeToolModal.value) return;
  if (saving.value) return;
  saving.value = true;
  try {
    await api.createRecord({
      title: `神域同步 · ${resultContext.value.server.name} (${resultContext.value.tool.name})`,
      content: toolResult.value.trim(),
      category: '外接神域',
      experience: 20,
      imagePaths: [],
    });
    sfx.hammer();
    notify.success('已转存为行为记录，角色获得 +20 EXP！');
    activeToolModal.value = null;
  } catch (e) {
    notify.error('转存失败：' + e.message);
  } finally { saving.value = false; }
}

async function removeMcp(id) {
  try {
    const updated = await api.mcpDeleteServer(id);
    mcpServers.value = updated;
    notify.info('MCP 契约已解除');
  } catch (e) {
    notify.error('删除失败：' + e.message);
  }
}

async function testMcp(s) {
  mcpTesting.value[s.id] = true;
  try {
    const tools = await api.mcpTestServer(s);
    mcpTools.value[s.id] = tools;
    notify.success(`[${s.name}] 握手成功，探明 ${tools.length} 项神域工具！`);
  } catch (e) {
    notify.error(`[${s.name}] 握手失败：` + e.message);
  } finally {
    mcpTesting.value[s.id] = false;
  }
}

function humanSize(bytes) {
  if (!bytes) return '0 KB';
  const kb = bytes / 1024;
  if (kb < 1024) return `${kb.toFixed(0)} KB`;
  return `${(kb / 1024).toFixed(1)} MB`;
}

async function load() {
  try {
    const [cfg, auto, list, state, aiCfg, mcps] = await Promise.all([
      api.configGet(),
      api.getAutostart(),
      api.backupList(null),
      api.appState(),
      api.aiConfigGet(),
      api.mcpGetServers(),
    ]);
    mcpServers.value = mcps || [];
    Object.assign(settings, cfg || {});
    autostart.value = !!auto;
    backups.value = list;
    dataDir.value = state?.dataDir || '';
    exeDir.value = state?.exeDir || '';
    runtimeDir.value = state?.runtimeDir || '';
    ai.enabled = !!aiCfg?.enabled;
    ai.baseUrl = aiCfg?.baseUrl || 'http://127.0.0.1:8317/v1';
    ai.model = aiCfg?.model || 'gemini-3.8-flash-high';
    ai.modelLight = aiCfg?.modelLight || 'gemini-3-flash';
    ai.apiKey = '';
  } catch (e) {
    notify.error('读取设置失败：' + e.message);
  }
}

/** 保存 AI 配置（密钥留空 = 不改） */
async function saveAi() {
  busy.value = true;
  try {
    await api.configSave({
      aiEnabled: ai.enabled,
      aiBaseUrl: ai.baseUrl,
      aiModel: ai.model,
      aiModelLight: ai.modelLight,
      ...(ai.apiKey.trim() ? { aiApiKey: ai.apiKey.trim() } : {}),
    });
    ai.apiKey = '';
    notify.success('AI 配置已保存');
    return true;
  } catch (e) {
    notify.error('保存失败：' + e.message);
    return false;
  } finally {
    busy.value = false;
  }
}

/** 连接测试：真实走一遍调用链 */
async function testAi() {
  aiTesting.value = true;
  try {
    if (!(await saveAi())) return;
    const reply = await api.aiTest();
    notify.success('AI 在线：' + (reply || '').slice(0, 20));
  } catch (e) {
    notify.error('AI 连不上：' + e.message);
  } finally {
    aiTesting.value = false;
  }
}

/** 选一个新目录并迁移过去 */
async function pickMigrate() {
  try {
    const dir = await open({ directory: true, title: '选择新的数据目录（建议放至非系统盘）' });
    if (!dir) return;
    migrateTarget.value = typeof dir === 'string' ? dir : dir[0];
  } catch (e) {
    notify.error('选择目录失败：' + e.message);
  }
}

async function confirmMigrate() {
  busy.value = true;
  try {
    const st = await api.dataDirMigrate(migrateTarget.value);
    dataDir.value = st?.dataDir || migrateTarget.value;
    migrateTarget.value = '';
    notify.success('数据已迁移，重启应用后生效');
    await load();
  } catch (e) {
    notify.error('迁移失败：' + e.message);
  } finally {
    busy.value = false;
  }
}

function previewOpacity() {
  const op = Math.max(60, Math.min(100, Number(settings.opacity) || 97));
  document.documentElement.style.setProperty('--app-opacity', (op / 100).toFixed(2));
}

async function saveNotificationEnabled() {
  const next = !!settings.notificationEnabled;
  try { await api.configSave({ notificationEnabled: next }); notificationRevision.value++; notify.success(next ? '到期提醒已开启' : '到期提醒已关闭'); }
  catch (e) { settings.notificationEnabled = !next; notify.error('保存提醒设置失败：' + (e.message || e)); }
}
async function saveSettings() {
  try {
    const op = Math.max(60, Math.min(100, Number(settings.opacity) || 97));
    settings.opacity = op;
    previewOpacity();
    await api.setHotkey(settings.hotkey);
    await api.configSave({
      notificationEnabled: settings.notificationEnabled,
      opacity: op,
    });
    notify.success('设置已保存');
  } catch (e) {
    notify.error('保存失败：' + e.message);
  }
}

async function toggleAutostart() {
  try {
    await api.setAutostart(!autostart.value);
    autostart.value = !autostart.value;
    notify.info(autostart.value ? '已开启开机自启' : '已关闭开机自启');
  } catch (e) {
    notify.error('切换自启失败：' + e.message);
  }
}

async function createBackup(toCustomDir = false) {
  busy.value = true;
  try {
    let dir = null;
    if (toCustomDir) {
      dir = await open({ directory: true, title: '选择备份目录' });
      if (!dir) { busy.value = false; return; }
    }
    const info = await api.backupCreate(dir);
    notify.success(`备份完成：${info.fileName}（${humanSize(info.sizeBytes)}）`);
    backups.value = await api.backupList(null);
  } catch (e) {
    notify.error('备份失败：' + e.message);
  } finally {
    busy.value = false;
  }
}

function askRestore(b) {
  restoring.value = b;
}

async function confirmRestore() {
  busy.value = true;
  try {
    const restored = await api.backupRestore(restoring.value.filePath);
    if (restored?.previouslyMissingImages) notify.warn(`恢复前缺少 ${restored.previouslyMissingImages} 张图片，可读旧状态已保存在 ${restored.protectionPath}`);
    notify.success('恢复完成，请重启应用以加载数据');
    restoring.value = null;
    await load();
  } catch (e) {
    notify.error('恢复失败：' + e.message);
  } finally {
    busy.value = false;
  }
}

async function exportMd() {
  try {
    const dir = await open({ directory: true, title: '选择 Markdown 导出目录' });
    if (!dir) return;
    const path = await api.exportMarkdown(dir);
    notify.success('已导出：' + path);
  } catch (e) {
    notify.error('导出失败：' + e.message);
  }
}

async function openDataDir() {
  try {
    const { openPath } = await import('@tauri-apps/plugin-opener');
    await openPath(dataDir.value);
  } catch (e) {
    notify.error('打开目录失败：' + e.message);
  }
}

const hotkeyHint = computed(() => (settings.hotkey || '').replace('CommandOrControl', 'Ctrl') || '未设置');

onMounted(load);
</script>

<template>
  <div class="settings-view scroll">
    <div class="cols">
      <div class="col">
        <GothicPanel title="窗口与快捷" subtitle="WINDOW">
          <label class="field">
            <span><Keyboard :size="11" /> 呼出快捷键</span>
            <input v-model="settings.hotkey" class="input" placeholder="CommandOrControl+Shift+L" />
            <em class="hint">当前：{{ hotkeyHint }}</em>
          </label>
          <label class="field">
            <span>界面亮度（{{ settings.opacity }}%）</span>
            <input
              v-model.number="settings.opacity"
              type="range"
              min="60"
              max="100"
              class="range"
              @input="previewOpacity"
            />
          </label>
          <label class="check-row">
            <input v-model="settings.notificationEnabled" type="checkbox" @change="saveNotificationEnabled" />
            <span>启用到期提醒通知</span>
          </label>
          <NotificationSettings :key="notificationRevision" />
          <label class="check-row">
            <input :checked="autostart" type="checkbox" @change="toggleAutostart" />
            <span><Power :size="11" /> 开机自动启动</span>
          </label>
          <button class="btn btn-primary btn-sm" @click="saveSettings"><Save :size="12" /> 保存设置</button>
        </GothicPanel>

        <GothicPanel title="音效" :subtitle="sfxEnabled ? '已开启' : '已关闭'" collapsible>
          <label class="check-row">
            <input :checked="sfxEnabled" type="checkbox" @change="toggleSfx" />
            <span>
              <component :is="sfxEnabled ? Volume2 : VolumeX" :size="12" />
              启用音效
            </span>
          </label>
          <div class="sfx-preview-grid">
            <button class="sfx-btn" title="任务完成与熟练度淬炼" @click="sfx.hammer()">
              <Play :size="10" /> 铁砧锻造（打铁声）
            </button>
            <button class="sfx-btn" title="装备穿戴与卸下" @click="sfx.equip()">
              <Play :size="10" /> 锁扣重装（重甲咬合）
            </button>
            <button class="sfx-btn" title="突破封印与秘技点亮" @click="sfx.soulFlame()">
              <Play :size="10" /> 翡翠魂火（呼啸声）
            </button>
            <button class="sfx-btn" title="角色等级进阶" @click="sfx.levelUp()">
              <Play :size="10" /> 钟鸣大和弦（升级声）
            </button>
          </div>
        </GothicPanel>

        <GothicPanel title="数据位置" subtitle="STORAGE">
          <div class="path-row">
            <span class="path-label">数据目录</span>
            <code>{{ dataDir || '—' }}</code>
            <button class="btn btn-ghost btn-sm" @click="openDataDir"><FolderOpen :size="12" /> 打开</button>
          </div>
          <div class="path-row">
            <span class="path-label">程序目录</span>
            <code>{{ exeDir || '—' }}</code>
          </div>
          <div class="path-row">
            <span class="path-label">运行时缓存</span>
            <code>{{ runtimeDir || '—' }}</code>
          </div>
          <div class="actions-row">
            <button class="btn btn-ghost btn-sm" @click="pickMigrate"><HardDrive :size="12" /> 迁移数据目录</button>
            <button class="btn btn-ghost btn-sm" @click="exportMd"><Download :size="12" /> 导出 Markdown</button>
          </div>
        </GothicPanel>

        <GothicPanel title="AI 军师" :subtitle="ai.enabled ? '已启用' : '未启用'" collapsible>
          <label class="check-row">
            <input v-model="ai.enabled" type="checkbox" />
            <span><Sparkles :size="11" /> 启用 AI 军师（复盘 / 建任务 / 装备命名 / 教练）</span>
          </label>
          <label class="field">
            <span>网关地址（OpenAI 兼容）</span>
            <input v-model="ai.baseUrl" class="input" placeholder="http://127.0.0.1:8317/v1" />
          </label>
          <div class="field-row">
            <label class="field">
              <span>主力模型（复盘）</span>
              <input v-model="ai.model" class="input" placeholder="gemini-3.8-flash-high" />
            </label>
            <label class="field">
              <span>轻量模型（建任务等）</span>
              <input v-model="ai.modelLight" class="input" placeholder="gemini-3-flash" />
            </label>
          </div>
          <label class="field">
            <span>密钥（留空 = 沿用已保存的）</span>
            <input v-model="ai.apiKey" type="password" class="input" placeholder="sk-..." />
          </label>
          <div class="actions-row">
            <button class="btn btn-primary btn-sm" :disabled="busy" @click="saveAi"><Save :size="12" /> 保存</button>
            <button class="btn btn-ghost btn-sm" :disabled="aiTesting" @click="testAi">
              <FlaskConical :size="12" /> {{ aiTesting ? '测试中…' : '测试连接' }}
            </button>
          </div>
          <p class="note">
            AI 仅在你主动点击对应按钮时调用，关掉它不影响任何核心功能；断网或网关离线时按钮会报错但应用照常运行。
          </p>
        </GothicPanel>
      </div>

      <div class="col">
        <GothicPanel title="外接工具 · MCP" :subtitle="`${mcpServers.length} 个服务`" class="mcp-panel" collapsible>
          <template #actions>
            <button class="btn btn-primary btn-sm" @click="openCreateMcp"><Plus :size="12" /> 新增 MCP 契约</button>
          </template>

          <p class="note" style="margin-top: -2px;">
            支持通过标准 Stdio 协议接入腾讯会议、抖音或自定义工具。在这里执行工具并将结果存为行为记录后，AI 复盘可读取已保存的内容。
          </p>

          <div v-if="!mcpServers.length" class="empty">
            尚未订立任何外接 MCP 契约，点击右上角自主添加
          </div>

          <div v-for="s in mcpServers" :key="s.id" class="mcp-card" :class="{ disabled: !s.enabled }">
            <div class="mcp-card-head">
              <div class="mcp-title-row">
                <Globe :size="13" class="mcp-ico" />
                <h4 class="mcp-name">{{ s.name }}</h4>
                <span class="mcp-status" :class="s.enabled ? 'on' : 'off'">{{ s.enabled ? '已启用' : '已封印' }}</span>
              </div>
              <div class="mcp-actions">
                <button
                  class="btn btn-ghost btn-sm"
                  :disabled="mcpTesting[s.id]"
                  title="启动子进程并测试握手探测工具"
                  @click="testMcp(s)"
                >
                  <FlaskConical :size="11" /> {{ mcpTesting[s.id] ? '握手中…' : '测试握手' }}
                </button>
                <button class="ico-btn" title="编辑" @click="openEditMcp(s)"><FolderOpen :size="12" /></button>
                <button class="ico-btn" title="解约删除" @click="removeMcp(s.id)"><Trash2 :size="12" /></button>
              </div>
            </div>

            <div class="mcp-cmd-box">
              <code>{{ s.command }} {{ (s.args || []).join(' ') }}</code>
            </div>

            <!-- 探测出来的工具列表 -->
            <div v-if="mcpTools[s.id]?.length" class="mcp-tools-list">
              <div class="mcp-tl-head">
                <CheckCircle2 :size="11" class="ok-ico" /> 探明 {{ mcpTools[s.id].length }} 项神域工具（点击可直接调用）：
              </div>
              <div class="mcp-tool-chips">
                <button
                  v-for="t in mcpTools[s.id]"
                  :key="t.name"
                  class="mcp-tool-chip"
                  :title="(t.description || t.name) + ' · 点击执行'"
                  @click="openToolRunner(s, t)"
                >
                  <Cpu :size="9" /> {{ t.name }}
                </button>
              </div>
            </div>
          </div>
        </GothicPanel>

        <GothicPanel title="备份与恢复" subtitle="BACKUP" class="backup-panel">
          <template #actions>
            <div class="bk-actions">
              <button class="btn btn-primary btn-sm" :disabled="busy" @click="createBackup(false)">立即备份</button>
              <button class="btn btn-ghost btn-sm" :disabled="busy" @click="createBackup(true)">备份到…</button>
            </div>
          </template>

          <div v-if="!backups.length" class="empty">还没有备份文件</div>
          <div v-for="b in backups" :key="b.filePath" class="backup-row">
            <div class="bk-info">
              <span class="bk-name">{{ b.fileName }}</span>
              <span class="bk-meta">{{ fmtDateTime(b.createdAt) }} · {{ humanSize(b.sizeBytes) }}</span>
            </div>
            <button class="btn btn-ghost btn-sm" @click="askRestore(b)"><RotateCcw :size="11" /> 恢复</button>
          </div>
        </GothicPanel>

        <ImageMaintenance :key="dataDir" />

        <GothicPanel title="关于" subtitle="ABOUT">
          <div class="about">
            <div class="about-row"><Info :size="12" /><span>LifeRPG · 绿火毁灭术</span></div>
            <div class="about-row"><span class="k">版本</span><span class="v num">2.3.0-rc.6</span></div>
            <div class="about-row"><span class="k">架构</span><span class="v">Tauri v2 · Vue 3 · Rust · SQLite</span></div>
            <div class="about-row"><span class="k">数据</span><span class="v">本地优先，零联网</span></div>
          </div>
        </GothicPanel>
      </div>
    </div>

    <ModalShell
      v-if="migrateTarget"
      title="迁移数据目录"
      subtitle="复制到新位置后切换指针"
      :width="500"
      danger
      @close="migrateTarget = ''"
    >
      <p class="confirm-text">
        将把当前数据复制到：<br />
        <b>{{ migrateTarget }}</b><br /><br />
        原目录的数据不会被删除，确认无误后可自行清理。迁移完成后需要重启应用。
      </p>
      <template #footer="{ close }">
        <button class="btn btn-ghost" @click="migrateTarget = ''">取消</button>
        <button class="btn btn-danger" :disabled="busy" @click="confirmMigrate">确认迁移</button>
      </template>
    </ModalShell>

    <ModalShell
      v-if="restoring"
      title="从备份恢复"
      subtitle="当前数据会先自动备份"
      :width="460"
      danger
      @close="restoring = null"
    >
      <p class="confirm-text">
        将用「<b>{{ restoring.fileName }}</b>」覆盖当前数据库与图片。<br />
        恢复前会自动生成一份当前状态的备份，恢复完成后需要重启应用。
      </p>
      <template #footer="{ close }">
        <button class="btn btn-ghost" @click="restoring = null">取消</button>
        <button class="btn btn-danger" :disabled="busy" @click="confirmRestore">确认恢复</button>
      </template>
    </ModalShell>

    <!-- MCP 契约编辑弹窗 -->
    <ModalShell
      v-if="showMcpEditor" :state="editingMcp" :busy="saving"
      :title="editingMcp.name ? `编辑 MCP 契约 · ${editingMcp.name}` : '订立新 MCP 契约'"
      subtitle="MODEL CONTEXT PROTOCOL"
      :width="540"
      @close="showMcpEditor = false"
    >
      <label class="field">
        <span>契约名称（如 tencent-meeting 或 douyin）</span>
        <input v-model="editingMcp.name" class="input" placeholder="例如：腾讯会议考勤 / 抖音运营数据" />
      </label>

      <div class="field-row">
        <label class="field">
          <span>启动程序（Command）</span>
          <input v-model="editingMcp.command" class="input" placeholder="python 或 node 或 npx" />
        </label>
        <label class="check-row" style="margin-top: 22px;">
          <input v-model="editingMcp.enabled" type="checkbox" />
          <span>启用此 MCP 契约</span>
        </label>
      </div>

      <label class="field">
        <span>执行参数（推荐 JSON 数组）</span>
        <input
          v-model="editingMcp.argsStr"
          class="input"
          placeholder="例如：D:\Projects\mcp-servers\tencent-meeting-mcp\server.py"
        />
        <em class="hint">推荐填写字符串数组，例如 ["D:\\tools\\server.py", "--port", "8000"]；兼容旧的空格分隔格式。</em>
      </label>

      <label class="field">
        <span>环境变量（ENV，每行一个 KEY=VALUE，可选）</span>
        <textarea
          v-model="editingMcp.envStr"
          class="textarea"
          rows="3"
          placeholder="例如：&#10;TENCENT_MEETING_USERID=alex&#10;PYTHONUNBUFFERED=1"
        ></textarea>
      </label>

      <template #footer="{ close }">
        <button class="btn btn-ghost" @click="close">取消</button>
        <button class="btn btn-primary" @click="submitMcp">铭刻契约</button>
      </template>
    </ModalShell>

    <!-- MCP 工具调用与经验转存弹窗 -->
    <ModalShell
      v-if="activeToolModal" :busy="saving"
      :title="`调用神域工具 · ${activeToolModal.tool.name}`"
      :subtitle="activeToolModal.server.name"
      :width="560"
      @close="closeToolRunner"
    >
      <p v-if="activeToolModal.tool.description" class="note" style="margin-bottom: 6px;">
        {{ activeToolModal.tool.description }}
      </p>
      <label class="field">
        <span>调用参数（JSON 对象）</span>
        <textarea
          v-model="toolArgsJson"
          class="textarea"
          rows="3"
          placeholder='例如：{} 或 {"url": "https://..."}'
          style="font-family: var(--f-mono); font-size: 12px;"
        ></textarea>
      </label>

      <div v-if="toolResult" class="mcp-result-box">
        <div class="mrb-head">{{ toolFailed ? '调用未成功' : '返回数据' }}</div>
        <pre class="mrb-pre">{{ toolResult }}</pre>
      </div>

      <template #footer="{ close }">
        <button class="btn btn-ghost" @click="closeToolRunner">关闭</button>
        <button
          v-if="toolResult && !toolFailed"
          class="btn btn-ghost"
          :disabled="saving || toolCalling"
          @click="saveToolResultAsRecord"
        >
          存为行为记录 (+20 EXP)
        </button>
        <button class="btn btn-primary" :disabled="toolCalling" @click="executeMcpTool">
          <Play :size="11" /> {{ toolCalling ? '神域响应中…' : '执行调用' }}
        </button>
      </template>
    </ModalShell>
  </div>
</template>

<style scoped>
.settings-view {
  flex: 1;
  min-width: 0;
  padding-right: 4px;
}
.cols {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
  align-items: start;
}
.col { min-width: 0; display: flex; flex-direction: column; gap: 14px; }
.field { display: flex; flex-direction: column; gap: 5px; }
.field > span {
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 12px;
  color: var(--t-sub);
}
.hint { font-size: 12px; color: var(--t-disabled); font-style: normal; }
.range { width: 100%; accent-color: #7cff1f; }
.check-row {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12.5px;
  color: var(--t-body);
  cursor: pointer;
}
.check-row input { accent-color: #7cff1f; }
.check-row span { display: flex; align-items: center; gap: 5px; }

.path-row { display: flex; align-items: center; gap: 8px; }
.path-label {
  width: 62px;
  flex: 0 0 auto;
  font-size: 12px;
  color: var(--t-time);
}
.path-row code {
  min-width: 0;
  flex: 1;
  padding: 6px 9px;
  font-family: var(--f-mono);
  font-size: 12px;
  color: var(--t-sub);
  background: rgba(0, 0, 0, 0.5);
  border: 1px solid #2b2412;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  user-select: text;
}
.actions-row { display: flex; flex-wrap: wrap; gap: 8px; }
.note { font-size: 12px; color: var(--t-disabled); line-height: 1.8; }

.sfx-preview-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px;
  margin: 6px 0 4px;
}
.sfx-btn {
  display: flex;
  align-items: center;
  justify-content: flex-start;
  gap: 6px;
  padding: 8px 10px;
  font-size: 12px;
  color: #cfea78;
  background:
    url('../assets/textures/mat_metal_iron.webp') center / 180px,
    linear-gradient(180deg, rgba(24, 32, 18, 0.95), rgba(9, 13, 8, 0.98));
  background-blend-mode: soft-light, normal;
  border: 1px solid #4a3c1b;
  box-shadow: inset 1px 1px 0 rgba(192, 166, 76, 0.3), inset -1px -1px 0 rgba(0, 0, 0, 0.85);
  cursor: pointer;
  transition: all 0.14s;
}
.sfx-btn:hover {
  border-color: var(--c-soul);
  color: #fff;
  box-shadow: inset 0 1px 0 rgba(255, 235, 160, 0.3), 0 0 10px rgba(124, 255, 31, 0.3);
}

.bk-actions { display: flex; gap: 6px; }
.backup-row {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 7px 4px;
  border-bottom: 1px dashed rgba(109, 91, 42, 0.22);
}
.bk-info { flex: 1; display: flex; flex-direction: column; }
.bk-name { font-size: 12px; color: var(--t-title); font-family: var(--f-mono); }
.bk-meta { font-size: 12px; color: var(--t-time); }

.about { display: flex; flex-direction: column; gap: 6px; }
.about-row {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: var(--t-body);
}
.about-row .k { width: 48px; color: var(--t-time); }
.about-row .v { color: var(--t-sub); }
.empty { padding: 20px 0; text-align: center; color: var(--t-disabled); font-size: 12px; }
.confirm-text { font-size: 13px; line-height: 1.9; }
.confirm-text b { color: var(--t-title); }

/* MCP 样式 */
.mcp-card {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 10px 12px;
  background:
    url('../assets/textures/mat_metal_iron.webp') center / 220px,
    linear-gradient(180deg, rgba(20, 28, 16, 0.94), rgba(8, 11, 7, 0.98));
  background-blend-mode: soft-light, normal;
  border: 1px solid #4a3c1b;
  box-shadow: inset 1px 1px 0 rgba(192, 166, 76, 0.3), inset -1px -1px 0 rgba(0, 0, 0, 0.85);
  margin-bottom: 8px;
}
.mcp-card.disabled {
  opacity: 0.6;
}
.mcp-card-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}
.mcp-title-row {
  display: flex;
  align-items: center;
  gap: 6px;
  flex: 1;
  min-width: 0;
}
.mcp-ico { color: var(--c-glow); }
.mcp-name {
  font-family: var(--f-title);
  font-size: 13px;
  color: #cfea78;
  letter-spacing: 0.06em;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.mcp-status {
  font-size: 12px;
  padding: 1px 5px;
  border: 1px solid transparent;
}
.mcp-status.on {
  color: var(--c-glow);
  border-color: rgba(124, 255, 31, 0.4);
  background: rgba(124, 255, 31, 0.1);
}
.mcp-status.off {
  color: var(--t-disabled);
  border-color: rgba(109, 91, 42, 0.4);
}
.mcp-actions {
  display: flex;
  align-items: center;
  gap: 4px;
}
.mcp-cmd-box code {
  display: block;
  padding: 4px 8px;
  background: rgba(0, 0, 0, 0.6);
  border: 1px solid #2b2412;
  font-family: var(--f-mono);
  font-size: 12px;
  color: #b5a982;
  word-break: break-all;
}
.mcp-tools-list {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding-top: 4px;
  border-top: 1px dashed rgba(109, 91, 42, 0.3);
}
.mcp-tl-head {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  color: #a08e63;
}
.ok-ico { color: var(--c-glow); }
.mcp-tool-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
}
.mcp-tool-chip {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  padding: 2px 6px;
  font-size: 12px;
  font-family: var(--f-mono);
  color: #cfea78;
  background: rgba(124, 255, 31, 0.08);
  border: 1px solid rgba(124, 255, 31, 0.3);
  cursor: pointer;
  transition: all 0.14s;
}
.mcp-tool-chip:hover {
  color: #fff;
  border-color: var(--c-soul);
  background: rgba(124, 255, 31, 0.2);
  box-shadow: 0 0 8px rgba(124, 255, 31, 0.35);
}
.mcp-result-box {
  display: flex;
  flex-direction: column;
  gap: 4px;
  margin-top: 6px;
}
.mrb-head {
  font-size: 12px;
  color: #a08e63;
}
.mrb-pre {
  max-height: 200px;
  overflow: auto;
  margin: 0;
  padding: 8px 10px;
  background: rgba(0, 0, 0, 0.65);
  border: 1px solid rgba(109, 91, 42, 0.45);
  font-family: var(--f-mono);
  font-size: 12px;
  line-height: 1.65;
  color: #dcd6be;
  white-space: pre-wrap;
  word-break: break-all;
  user-select: text;
}
</style>

<style scoped>
@media(max-width:1280px){.cols{grid-template-columns:minmax(0,1fr)}} .note{font-size:12px}.path-row{flex-wrap:wrap}.path-row code{overflow-wrap:anywhere}
</style>


