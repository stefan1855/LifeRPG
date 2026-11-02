import { invoke } from '@tauri-apps/api/core';

/** 统一封装 Tauri 命令调用，失败时抛出可读错误 */
async function call(cmd, args) {
  try {
    return await invoke(cmd, args);
  } catch (e) {
    const msg = typeof e === 'string' ? e : e?.message || String(e);
    throw new Error(msg);
  }
}

export const api = {
  // ---- 角色与仪表盘 ----
  dashboard: () => call('rpg_get_dashboard'),
  character: () => call('rpg_get_character'),
  updateCharacter: (name, description) => call('rpg_update_character', { name, description }),
  setAvatarFit: (mode)=>call('rpg_set_avatar_fit',{mode}),
  setAvatar: (path) => call('rpg_set_avatar', { path }),
  attributes: () => call('rpg_get_attributes'),
  logs: (limit = 50) => call('rpg_get_logs', { limit }),

  // ---- 行动任务 ----
  tasks: (status, category) => call('task_list', { status: status || null, category: category || null }),
  taskCategories: () => call('task_get_categories'),
  createTask: (input) => call('task_create', { input }),
  updateTask: (input) => call('task_update', { input }),
  deleteTask: (id) => call('task_delete', { id }),
  completeTask: (id) => call('task_complete', { id }),
  uncompleteTask: (id) => call('task_uncomplete', { id }),

  // ---- 行为记录 ----
  records: (keyword, category, limit, offset = 0) => call('record_list', { keyword: keyword || null, category: category || null, limit: limit || null, offset }),
  record: (id) => call('record_get', { id }),
  createRecord: (input) => call('record_create', { input }),
  updateRecord: (input) => call('record_update', { input }),
  deleteRecord: (id) => call('record_delete', { id }),
  recordCategories: () => call('record_categories'),
  imageGallery: (limit=50,offset=0)=>call('image_gallery',{limit,offset}),
  imageDataUrl: (path, thumbnail = false) => call('image_to_data_url', { path, thumbnail }),

  // ---- 统计 / 备份 / 导出 ----
  archiveStats: (days = 30) => call('archive_stats', { days }),
  backupCreate: (destDir) => call('backup_create', { destDir: destDir || null }),
  backupList: (destDir) => call('backup_list', { destDir: destDir || null }),
  backupRestore: (path) => call('backup_restore', { path }),
  exportMarkdown: (destDir) => call('export_markdown', { destDir }),
  dataDir: () => call('app_data_dir'),

  // ---- 设置 / 窗口 ----
  configGet: () => call('config_get'),
  configRecover: (source, dataDir) => call('config_recover', { source: source || null, dataDir: dataDir || null }),
  configSave: (settings) => call('config_save', { settings }),
  getAutostart: () => call('get_autostart'),
  setAutostart: (enabled) => call('set_autostart', { enabled }),
  setHotkey: (key) => call('set_hotkey', { key }),
  exit: () => call('app_exit'),
  setViewMode: (mode) => call('window_set_view_mode', { mode }),
  togglePin: (pin) => call('window_toggle_pin', { pin }),

  // ---- 便携配置 / 数据目录 ----
  appState: () => call('app_state'),
  dataDirInit: (path) => call('data_dir_init', { path }),
  dataDirMigrate: (path) => call('data_dir_migrate', { path }),
  dataDirCheck: (path) => call('data_dir_check', { path }),

  // ---- 装备 ----
  equipList: () => call('equip_list'),
  equipConflicts: () => call('equip_conflicts'),
  equipRepair: () => call('equip_repair'),
  equipSnapshot: () => call('equip_snapshot'),
  equipSave: (input) => call('equip_save', { input }),
  equipToggle: (id) => call('equip_toggle', { id }),
  equipDelete: (id) => call('equip_delete', { id }),

  // ---- 技能 ----
  skillList: () => call('skill_list'),
  skillSave: (input) => call('skill_save', { input }),
  skillUnlock: (id) => call('skill_unlock', { id }),
  skillRelock: (id) => call('skill_relock', { id }),
  skillAddExp: (id, amount) => call('skill_add_exp', { id, amount }),
  skillDelete: (id) => call('skill_delete', { id }),

  // ---- 世界 ----
  npcList: () => call('npc_list'),
  npcSave: (input) => call('npc_save', { input }),
  npcAdjust: (id, delta) => call('npc_adjust_relation', { id, delta }),
  npcDelete: (id) => call('npc_delete', { id }),
  mapList: () => call('map_list'),
  mapSave: (input) => call('map_save', { input }),
  mapToggle: (id) => call('map_toggle', { id }),
  mapDelete: (id) => call('map_delete', { id }),

  imageCleanupScan: () => call('image_cleanup_scan'),
  imageCleanupQuarantine: (root, files) => call('image_cleanup_quarantine', { root, files }),
  imageCleanupBatches: () => call('image_cleanup_batches'),
  imageCleanupRestore: (id) => call('image_cleanup_restore', { id }),
  imageCleanupPurge: (id, confirmation) => call('image_cleanup_purge', { id, confirmation }),
  notificationStatus: () => call('notification_status'),
  notificationTest: () => call('notification_test'),
  notificationReceipt: (tag) => call('notification_receipt', { tag }),

  // ---- AI 军师 ----
  aiConfigGet: () => call('ai_config_get'),
  aiTest: () => call('ai_test'),
  aiReview: (days) => call('ai_review', { days: days || null }),
  aiParseTask: (text) => call('ai_parse_task', { text }),
  aiNameEquipment: (realName, realEffect) =>
    call('ai_name_equipment', { realName, realEffect }),
  aiCoach: () => call('ai_coach'),

  // ---- 外接神域 (MCP) 契约 ----
  mcpGetServers: () => call('mcp_get_servers'),
  mcpSaveServer: (server) => call('mcp_save_server', { server }),
  mcpDeleteServer: (id) => call('mcp_delete_server', { id }),
  mcpTestServer: (server) => call('mcp_test_server', { server }),
  mcpCallTool: (serverId, toolName, args) =>
    call('mcp_call_tool', { serverId, toolName, arguments: args || {} }),
};

/** 12 个装备槽位 */
export const EQUIP_SLOTS = [
  { key: 'head', label: '头部', glyph: '⛑' },
  { key: 'shoulder', label: '肩部', glyph: '⛰' },
  { key: 'chest', label: '胸甲', glyph: '🛡' },
  { key: 'hands', label: '手套', glyph: '✋' },
  { key: 'waist', label: '腰带', glyph: '➰' },
  { key: 'legs', label: '腿部', glyph: '🦵' },
  { key: 'feet', label: '战靴', glyph: '👣' },
  { key: 'wrist', label: '护腕', glyph: '⛓' },
  { key: 'mainhand', label: '主手', glyph: '⚔' },
  { key: 'offhand', label: '副手', glyph: '🛡' },
  { key: 'trinket1', label: '饰品一', glyph: '◈' },
  { key: 'trinket2', label: '饰品二', glyph: '◈' },
];

/** 六大属性 key → 展示名与配色（统一在暗金-魂火绿谱系内，避免杂色破坏主题） */
export const ATTRIBUTE_META = {
  physique: { label: '体魄', sigil: '⚔', color: '#c08a3c' },
  mind: { label: '心智', sigil: '✦', color: '#a8ff4a' },
  discipline: { label: '纪律', sigil: '⬢', color: '#c0a64c' },
  expression: { label: '表达', sigil: '✧', color: '#7cff1f' },
  creativity: { label: '创造', sigil: '❋', color: '#d4ff6b' },
  resources: { label: '资源', sigil: '◈', color: '#8b7430' },
};

/** 四象限元数据（配色统一收敛到暗金-魂火-锈红谱系） */
export const QUADRANTS = [
  { key: 'Q1', label: '重要且紧急', exp: 30, color: '#d0703c' },
  { key: 'Q2', label: '重要不紧急', exp: 25, color: '#c0a64c' },
  { key: 'Q3', label: '不重要紧急', exp: 15, color: '#8b7430' },
  { key: 'Q4', label: '不重要不紧急', exp: 5, color: '#7d7458' },
];

export function quadrantMeta(key) {
  return QUADRANTS.find((q) => q.key === key) || QUADRANTS[1];
}

export function attrMeta(key) {
  return ATTRIBUTE_META[key] || { label: key || '未设置', sigil: '◇', color: '#a89b75' };
}

/** 时间格式化 */
export function fmtDate(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso.slice(0, 10);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

export function fmtDateTime(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return `${fmtDate(iso)} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
}

export function relativeTime(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  const diff = Date.now() - d.getTime();
  const min = Math.floor(diff / 60000);
  if (min < 1) return '刚刚';
  if (min < 60) return `${min} 分钟前`;
  const hour = Math.floor(min / 60);
  if (hour < 24) return `${hour} 小时前`;
  const day = Math.floor(hour / 24);
  if (day < 30) return `${day} 天前`;
  return fmtDate(iso);
}
