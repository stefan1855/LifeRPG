/** 格式化本地日期为 YYYY-MM-DD */
export function toDateStr(d) {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

/** 暗黑亡灵术士六维属性映射（暗影契约体系） */
export const ATTRIBUTE_META = {
  physique: {
    key: 'physique',
    name: '暗影血肉',
    originalName: '体魄',
    color: '#7CFF6B',
    glow: 'rgba(124, 255, 107, 0.45)',
    icon: 'Flame',
    symbol: '◈',
  },
  mind: {
    key: 'mind',
    name: '虚空意志',
    originalName: '心智',
    color: '#38bdf8',
    glow: 'rgba(56, 189, 248, 0.45)',
    icon: 'Brain',
    symbol: '◇',
  },
  discipline: {
    key: 'discipline',
    name: '诅咒束缚',
    originalName: '纪律',
    color: '#C8A45D',
    glow: 'rgba(200, 164, 93, 0.45)',
    icon: 'Shield',
    symbol: '⛓',
  },
  expression: {
    key: 'expression',
    name: '低语之舌',
    originalName: '表达',
    color: '#9A5CFF',
    glow: 'rgba(154, 92, 255, 0.45)',
    icon: 'MessageSquare',
    symbol: '✧',
  },
  creativity: {
    key: 'creativity',
    name: '邪能造物',
    originalName: '创造',
    color: '#7CFF6B',
    glow: 'rgba(124, 255, 107, 0.45)',
    icon: 'Sparkles',
    symbol: '⚚',
  },
  resources: {
    key: 'resources',
    name: '灵魂碎片',
    originalName: '资源',
    color: '#C8A45D',
    glow: 'rgba(200, 164, 93, 0.45)',
    icon: 'Coins',
    symbol: '✦',
  },
  general: {
    key: 'general',
    name: '通用暗影',
    originalName: '通用',
    color: '#B9A7D2',
    glow: 'rgba(185, 167, 210, 0.35)',
    icon: 'CircleDot',
    symbol: '※',
  },
};

export function getAttributeMeta(key) {
  return ATTRIBUTE_META[key] || {
    key: key || 'general',
    name: key || '未知契约',
    originalName: key || '未分类',
    color: '#7CFF6B',
    glow: 'rgba(124, 255, 107, 0.45)',
    icon: 'CircleDot',
    symbol: '※',
  };
}

/** 四象限黑暗契约定义 */
export const QUADRANT_META = {
  Q1: { label: '紧急深渊 (Q1)', short: 'Q1', exp: 50, color: '#E35D5D' },
  Q2: { label: '核心修炼 (Q2)', short: 'Q2', exp: 40, color: '#7CFF6B' },
  Q3: { label: '临时契约 (Q3)', short: 'Q3', exp: 20, color: '#C8A45D' },
  Q4: { label: '琐碎低语 (Q4)', short: 'Q4', exp: 10, color: '#7E7192' },
};

/** 日期解析与友好显示 */
export function formatTaskDate(dateStr) {
  if (!dateStr) return '';
  const datePart = dateStr.slice(0, 10);
  const m = datePart.match(/(\d{4})-(\d{2})-(\d{2})/);
  if (!m) return dateStr;

  const d = new Date(parseInt(m[1], 10), parseInt(m[2], 10) - 1, parseInt(m[3], 10));
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const tomorrow = new Date(today);
  tomorrow.setDate(tomorrow.getDate() + 1);

  const t = d.getTime();
  if (t < today.getTime()) return '已腐化(已截止)';
  if (t === today.getTime()) return '今日契约';
  if (t === tomorrow.getTime()) return '明日待行';

  const wd = ['周日', '周一', '周二', '周三', '周四', '周五', '周六'][d.getDay()];
  return `${parseInt(m[2], 10)}月${parseInt(m[3], 10)}日 ${wd}`;
}

/** 智能解析自然语言输入（日期、暗黑属性提示） */
export function parseNaturalTask(text) {
  if (!text) return {};
  const res = {};
  const today = new Date();
  today.setHours(0, 0, 0, 0);

  // 1. 属性提示识别
  if (/#暗影血肉|#血肉|#体魄|健身|跑步|锻炼|游泳|力量|深蹲/.test(text)) res.growthAttribute = 'physique';
  else if (/#虚空意志|#心智|#意志|学习|编程|代码|阅读|算法|读书|开发/.test(text)) res.growthAttribute = 'mind';
  else if (/#诅咒束缚|#束缚|#纪律|早起|打卡|禁食|专注|冥想/.test(text)) res.growthAttribute = 'discipline';
  else if (/#低语之舌|#低语|#表达|汇报|复盘|演讲|沟通|写作|视频|对话/.test(text)) res.growthAttribute = 'expression';
  else if (/#邪能造物|#造物|#创造|设计|画画|音乐|灵感|构思|工艺/.test(text)) res.growthAttribute = 'creativity';
  else if (/#灵魂碎片|#碎片|#资源|财务|理财|预算|省钱|投资|金币/.test(text)) res.growthAttribute = 'resources';

  // 2. 相对日期
  if (/今天|今日/.test(text)) {
    res.dueAt = toDateStr(today);
  } else if (/明天|明儿|明日/.test(text)) {
    const d = new Date(today);
    d.setDate(d.getDate() + 1);
    res.dueAt = toDateStr(d);
  } else if (/后天/.test(text)) {
    const d = new Date(today);
    d.setDate(d.getDate() + 2);
    res.dueAt = toDateStr(d);
  }

  // 3. 星期几
  const weekMatch = text.match(/(下周|下星期|这周|本周|周|星期|礼拜)([一二三四五六日天\d])/);
  if (weekMatch && !res.dueAt) {
    const prefix = weekMatch[1];
    const dayChar = weekMatch[2];
    const map = { '1': 1, '2': 2, '3': 3, '4': 4, '5': 5, '6': 6, '7': 0, '一': 1, '二': 2, '三': 3, '四': 4, '五': 5, '六': 6, '日': 0, '天': 0 };
    const targetDay = map[dayChar];
    if (targetDay !== undefined) {
      const currentDay = today.getDay();
      let diff = targetDay - currentDay;
      if (prefix.startsWith('下') || diff <= 0) diff += 7;
      const d = new Date(today);
      d.setDate(d.getDate() + diff);
      res.dueAt = toDateStr(d);
    }
  }

  return res;
}
