<script setup>
import { ref, computed, onMounted } from 'vue';
import { invoke } from '@tauri-apps/api/core';
import {
  RefreshCw,
  ChevronDown,
  ChevronUp,
  ChevronRight,
  Flame,
  Zap,
  Skull,
  Crown,
} from 'lucide-vue-next';
import facelessPortrait from '../assets/theme/faceless_portrait.png';
import magicCircleEmblem from '../assets/theme/magic_circle_emblem.png';
import magicCircleOuterGlow from '../assets/theme/magic_circle_outer_glow.png';
import bannerDarkIron from '../assets/theme/banner_dark_iron.png';
import cornerSkull from '../assets/theme/corner_skull.png';
import equipSlotsWatermark from '../assets/theme/equip_slots.png';
import panelInnerTexture from '../assets/theme/panel_inner_texture.png';

const props = defineProps({
  character: {
    type: Object,
    required: true,
  },
  attributes: {
    type: Array,
    default: () => [],
  },
  tasks: {
    type: Array,
    default: () => [],
  },
});

const emit = defineEmits(['toggle-task', 'open-add-task', 'open-edit-task', 'delete-task', 'quick-create', 'switch-tab']);

const isDrawerOpen = ref(true);
const recentLogs = ref([]);
const isRefreshing = ref(false);

// 6 维暗影契约属性定义
const defaultAttributesMap = {
  body: { key: 'body', name: '暗影血肉', orig: '体魄', icon: '💀', defaultLvl: 19, defaultExp: 12480, maxExp: 20000 },
  mind: { key: 'mind', name: '虚空意志', orig: '心智', icon: '👁', defaultLvl: 23, defaultExp: 18760, maxExp: 28000 },
  discipline: { key: 'discipline', name: '诅咒束缚', orig: '自律', icon: '⛓', defaultLvl: 16, defaultExp: 8240, maxExp: 18000 },
  social: { key: 'social', name: '低语之舌', orig: '表达', icon: '✦', defaultLvl: 15, defaultExp: 6840, maxExp: 16000 },
  craft: { key: 'craft', name: '邪能造物', orig: '创造', icon: '🧪', defaultLvl: 21, defaultExp: 15300, maxExp: 24000 },
  wealth: { key: 'wealth', name: '灵魂碎片', orig: '财富', icon: '💎', defaultLvl: 17, defaultExp: 9620, maxExp: 19000 },
};

// 12 个装备槽位
const equipSlotsLeft = [
  { id: 'head', name: '头部', icon: '🪖' },
  { id: 'shoulder', name: '肩部', icon: '🛡' },
  { id: 'chest', name: '胸部', icon: '🥋' },
  { id: 'wrist', name: '护腕', icon: '🦾' },
  { id: 'hands', name: '手套', icon: '🧤' },
  { id: 'waist', name: '腰带', icon: '🎗' },
];

const equipSlotsRight = [
  { id: 'legs', name: '腿部', icon: '👖' },
  { id: 'feet', name: '靴子', icon: '👢' },
  { id: 'trinket1', name: '饰品1', icon: '💍' },
  { id: 'trinket2', name: '饰品2', icon: '🧿' },
  { id: 'mainhand', name: '主手', icon: '🗡' },
  { id: 'offhand', name: '副手', icon: '🛡' },
];

function getAttrData(key) {
  const found = (props.attributes || []).find((a) => a.attribute_key === key);
  const def = defaultAttributesMap[key];
  if (found) {
    const max = found.experience_to_next || (found.level * 1000) || def.maxExp;
    const cur = found.experience || def.defaultExp;
    const pct = Math.min(100, Math.max(0, Math.round((cur / max) * 100)));
    return {
      name: found.display_name || def.name,
      level: found.level || def.defaultLvl,
      currentExp: cur,
      maxExp: max,
      percent: pct,
      icon: def.icon,
    };
  }
  return {
    name: def.name,
    level: def.defaultLvl,
    currentExp: def.defaultExp,
    maxExp: def.maxExp,
    percent: Math.round((def.defaultExp / def.maxExp) * 100),
    icon: def.icon,
  };
}

const totalExp = computed(() => {
  return props.character?.total_experience || 125760;
});

const currentLevelExp = computed(() => {
  return props.character?.experience_into_current_level || 125760;
});

const nextLevelExp = computed(() => {
  return props.character?.experience_required_for_next_level || 150000;
});

const expToNext = computed(() => {
  const req = nextLevelExp.value;
  const cur = currentLevelExp.value;
  return Math.max(0, req - cur) || 24240;
});

const expPercent = computed(() => {
  if (props.character?.progress_percent) {
    return Number(props.character.progress_percent).toFixed(1);
  }
  return (83.7).toFixed(1);
});

async function fetchRecentLogs() {
  try {
    const logs = await invoke('rpg_get_logs', { limit: 6 });
    if (logs && logs.length > 0) {
      recentLogs.value = logs;
    } else {
      recentLogs.value = [
        { id: '1', attribute_name: '邪能造物', experience_gained: 300, source_title: '创造恶魔工艺品', time_str: '3小时前' },
        { id: '2', attribute_name: '虚空意志', experience_gained: 180, source_title: '完成冥想修炼', time_str: '5小时前' },
        { id: '3', attribute_name: '暗影血肉', experience_gained: 240, source_title: '击败暗影狱王', time_str: '5小时前' },
        { id: '4', attribute_name: '诅咒束缚', experience_gained: 120, source_title: '抵抗心灵诱惑', time_str: '1小时前' },
        { id: '5', attribute_name: '低语之舌', experience_gained: 90, source_title: '与NPC对话', time_str: '2小时前' },
        { id: '6', attribute_name: '灵魂碎片', experience_gained: 150, source_title: '收集灵魂碎片', time_str: '5小时前' },
      ];
    }
  } catch (e) {
    console.error('获取日志异常:', e);
  }
}

function handleRefresh() {
  isRefreshing.value = true;
  fetchRecentLogs();
  setTimeout(() => {
    isRefreshing.value = false;
  }, 600);
}

onMounted(() => {
  fetchRecentLogs();
});
</script>

<template>
  <div class="wow-dashboard-container">
    <!-- ══════════════════════════════════════════════════════════
         左栏：角色卡片与无面者肖像 (Character Portrait Panel)
         ══════════════════════════════════════════════════════════ -->
    <div class="col-character gothic-frame-box">
      <!-- 装饰四角颅骨 -->
      <img :src="cornerSkull" class="corner-skull-ornament top-left" alt="" />
      <img :src="cornerSkull" class="corner-skull-ornament top-right" alt="" />
      <img :src="cornerSkull" class="corner-skull-ornament bottom-left" alt="" />
      <img :src="cornerSkull" class="corner-skull-ornament bottom-right" alt="" />

      <!-- 顶部等级浮标盾徽 -->
      <div class="lvl-badge-shield">
        <span class="lvl-txt-label">Lv.</span>
        <span class="lvl-txt-num">{{ character.level || 27 }}</span>
      </div>

      <!-- 无面者立绘展示区 -->
      <div class="portrait-display-area">
        <div class="portrait-glow-halo"></div>
        <img :src="facelessPortrait" alt="无面者" class="portrait-main-img" />
        <div class="fel-ground-rune"></div>
      </div>

      <!-- 角色核心命名与称号 -->
      <div class="char-identity-block">
        <h1 class="char-name-gold">{{ character.name || '无面者' }}</h1>
        <div class="char-title-fel">
          <span class="skull-sym">☠</span>
          <span>暗 影 学 徒</span>
          <span class="skull-sym">☠</span>
        </div>
        <p class="char-flavor-quote">
          “在阴影中行走，聆听灵魂的低语。<br />
          追寻知识，探索未知的深渊，直至成为暗影本身。”
        </p>
      </div>

      <!-- 操作按钮群 -->
      <div class="char-actions-grid">
        <div class="btn-half-row">
          <button class="gothic-btn" @click="$emit('switch-tab', 'settings')">选择头像</button>
          <button class="gothic-btn" @click="$emit('switch-tab', 'settings')">清除头像</button>
        </div>
        <button class="gothic-btn btn-full" @click="$emit('switch-tab', 'settings')">
          编辑角色资料
        </button>
      </div>
    </div>

    <!-- ══════════════════════════════════════════════════════════
         中栏：主核心区 (经验区域 + 六维暗影契约星盘)
         ══════════════════════════════════════════════════════════ -->
    <div class="col-center-core">
      <!-- 上半区：☠ 经验区域 ☠ -->
      <div class="exp-subpanel gothic-frame-box">
        <img :src="cornerSkull" class="corner-skull-ornament top-left" alt="" />
        <img :src="cornerSkull" class="corner-skull-ornament top-right" alt="" />
        <img :src="cornerSkull" class="corner-skull-ornament bottom-left" alt="" />
        <img :src="cornerSkull" class="corner-skull-ornament bottom-right" alt="" />

        <!-- 纹理条幅标题栏 -->
        <div class="gothic-banner-header" :style="{ backgroundImage: `url(${bannerDarkIron})` }">
          <div class="header-title-text">
            <span class="skull-sym">☠</span>
            <span>经 验 区 域</span>
            <span class="skull-sym">☠</span>
          </div>
        </div>

        <!-- 四列数据展示 -->
        <div class="exp-stats-four-cols">
          <div class="stat-col col-energy">
            <div class="stat-label">灵魂能量</div>
            <div class="stat-val-huge">{{ Number(totalExp).toLocaleString() }}</div>
          </div>
          <div class="stat-col">
            <div class="stat-label">距离下一级</div>
            <div class="stat-val-mid">{{ Number(expToNext).toLocaleString() }} EXP</div>
          </div>
          <div class="stat-col">
            <div class="stat-label">契约之日</div>
            <div class="stat-val-date">2024-01-15</div>
          </div>
          <div class="stat-col">
            <div class="stat-label">最近成长</div>
            <div class="stat-val-date">2024-05-25 22:47</div>
          </div>
        </div>

        <!-- 经典暗黑双翼宝石经验进度槽 -->
        <div class="exp-progress-wrapper">
          <div class="exp-wing-claw left-claw"></div>
          <div class="exp-track-gold-frame">
            <div
              class="exp-fill-green"
              :style="{ width: `${Math.min(100, Math.max(0, expPercent))}%` }"
            ></div>
            <div class="exp-center-pill">
              <span class="pct-text">{{ expPercent }}%</span>
            </div>
          </div>
          <div class="exp-wing-claw right-claw"></div>
        </div>

        <div class="exp-bottom-numbers">
          {{ Number(totalExp).toLocaleString() }} / {{ Number(nextLevelExp).toLocaleString() }} EXP
        </div>
      </div>

      <!-- 下半区：※ 暗影契约 ※ 六维星盘 -->
      <div class="contract-subpanel gothic-frame-box">
        <img :src="cornerSkull" class="corner-skull-ornament top-left" alt="" />
        <img :src="cornerSkull" class="corner-skull-ornament top-right" alt="" />
        <img :src="cornerSkull" class="corner-skull-ornament bottom-left" alt="" />
        <img :src="cornerSkull" class="corner-skull-ornament bottom-right" alt="" />

        <div class="gothic-banner-header" :style="{ backgroundImage: `url(${bannerDarkIron})` }">
          <div class="header-title-text">
            <span class="star-sym">※</span>
            <span>暗 影 契 约</span>
            <span class="star-sym">※</span>
          </div>
        </div>

        <!-- 六维星盘主舞台 -->
        <div class="astrolabe-stage">
          <!-- 背景旋转魔法阵与外圈光晕 -->
          <div class="center-magic-ring-wrapper">
            <img :src="magicCircleOuterGlow" alt="" class="outer-glow-bg" />
            <img :src="magicCircleEmblem" alt="法阵" class="rotating-magic-ring" />
            <div class="center-fel-flame">
              <Flame :size="42" class="flame-svg" />
            </div>
          </div>

          <!-- 六维属性节点布局 (环形绝对定位) -->
          <!-- 1. 暗影血肉 (左上) -->
          <div class="attr-node-box node-top-left" @click="$emit('switch-tab', 'attributes')">
            <div class="attr-rune-diamond">
              <span class="diamond-icon">💀</span>
            </div>
            <div class="attr-node-content">
              <div class="node-title-row">
                <span class="node-name">暗影血肉</span>
                <span class="node-lvl">Lv. {{ getAttrData('body').level }}</span>
              </div>
              <div class="node-exp-line">
                {{ Number(getAttrData('body').currentExp).toLocaleString() }} / {{ Number(getAttrData('body').maxExp).toLocaleString() }} EXP
              </div>
              <div class="node-mini-bar-track">
                <div class="node-mini-bar-fill" :style="{ width: `${getAttrData('body').percent}%` }"></div>
              </div>
            </div>
          </div>

          <!-- 2. 虚空意志 (右上) -->
          <div class="attr-node-box node-top-right" @click="$emit('switch-tab', 'attributes')">
            <div class="attr-rune-diamond">
              <span class="diamond-icon">👁</span>
            </div>
            <div class="attr-node-content">
              <div class="node-title-row">
                <span class="node-name">虚空意志</span>
                <span class="node-lvl">Lv. {{ getAttrData('mind').level }}</span>
              </div>
              <div class="node-exp-line">
                {{ Number(getAttrData('mind').currentExp).toLocaleString() }} / {{ Number(getAttrData('mind').maxExp).toLocaleString() }} EXP
              </div>
              <div class="node-mini-bar-track">
                <div class="node-mini-bar-fill" :style="{ width: `${getAttrData('mind').percent}%` }"></div>
              </div>
            </div>
          </div>

          <!-- 3. 诅咒束缚 (左中) -->
          <div class="attr-node-box node-mid-left" @click="$emit('switch-tab', 'attributes')">
            <div class="attr-rune-diamond">
              <span class="diamond-icon">⛓</span>
            </div>
            <div class="attr-node-content">
              <div class="node-title-row">
                <span class="node-name">诅咒束缚</span>
                <span class="node-lvl">Lv. {{ getAttrData('discipline').level }}</span>
              </div>
              <div class="node-exp-line">
                {{ Number(getAttrData('discipline').currentExp).toLocaleString() }} / {{ Number(getAttrData('discipline').maxExp).toLocaleString() }} EXP
              </div>
              <div class="node-mini-bar-track">
                <div class="node-mini-bar-fill" :style="{ width: `${getAttrData('discipline').percent}%` }"></div>
              </div>
            </div>
          </div>

          <!-- 4. 低语之舌 (右中) -->
          <div class="attr-node-box node-mid-right" @click="$emit('switch-tab', 'attributes')">
            <div class="attr-rune-diamond">
              <span class="diamond-icon">✦</span>
            </div>
            <div class="attr-node-content">
              <div class="node-title-row">
                <span class="node-name">低语之舌</span>
                <span class="node-lvl">Lv. {{ getAttrData('social').level }}</span>
              </div>
              <div class="node-exp-line">
                {{ Number(getAttrData('social').currentExp).toLocaleString() }} / {{ Number(getAttrData('social').maxExp).toLocaleString() }} EXP
              </div>
              <div class="node-mini-bar-track">
                <div class="node-mini-bar-fill" :style="{ width: `${getAttrData('social').percent}%` }"></div>
              </div>
            </div>
          </div>

          <!-- 5. 邪能造物 (左下) -->
          <div class="attr-node-box node-bottom-left" @click="$emit('switch-tab', 'attributes')">
            <div class="attr-rune-diamond">
              <span class="diamond-icon">🧪</span>
            </div>
            <div class="attr-node-content">
              <div class="node-title-row">
                <span class="node-name">邪能造物</span>
                <span class="node-lvl">Lv. {{ getAttrData('craft').level }}</span>
              </div>
              <div class="node-exp-line">
                {{ Number(getAttrData('craft').currentExp).toLocaleString() }} / {{ Number(getAttrData('craft').maxExp).toLocaleString() }} EXP
              </div>
              <div class="node-mini-bar-track">
                <div class="node-mini-bar-fill" :style="{ width: `${getAttrData('craft').percent}%` }"></div>
              </div>
            </div>
          </div>

          <!-- 6. 灵魂碎片 (右下) -->
          <div class="attr-node-box node-bottom-right" @click="$emit('switch-tab', 'attributes')">
            <div class="attr-rune-diamond">
              <span class="diamond-icon">💎</span>
            </div>
            <div class="attr-node-content">
              <div class="node-title-row">
                <span class="node-name">灵魂碎片</span>
                <span class="node-lvl">Lv. {{ getAttrData('wealth').level }}</span>
              </div>
              <div class="node-exp-line">
                {{ Number(getAttrData('wealth').currentExp).toLocaleString() }} / {{ Number(getAttrData('wealth').maxExp).toLocaleString() }} EXP
              </div>
              <div class="node-mini-bar-track">
                <div class="node-mini-bar-fill" :style="{ width: `${getAttrData('wealth').percent}%` }"></div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ══════════════════════════════════════════════════════════
         右栏：装备栏、最近变化与技能折叠抽屉 (Equipment & Logs)
         ══════════════════════════════════════════════════════════ -->
    <div class="col-right-side">
      <!-- 上半区：当前装备与最近变化双分栏 -->
      <div class="top-side-split">
        <!-- 装备栏 (※ 当前装备 ※) -->
        <div class="equip-subpanel gothic-frame-box">
          <img :src="cornerSkull" class="corner-skull-ornament top-left" alt="" />
          <img :src="cornerSkull" class="corner-skull-ornament top-right" alt="" />
          <img :src="cornerSkull" class="corner-skull-ornament bottom-left" alt="" />
          <img :src="cornerSkull" class="corner-skull-ornament bottom-right" alt="" />

          <div class="gothic-banner-header" :style="{ backgroundImage: `url(${bannerDarkIron})` }">
            <div class="header-title-text">
              <span class="star-sym">※</span>
              <span>当 前 装 备</span>
              <span class="star-sym">※</span>
            </div>
          </div>

          <div class="equip-slots-layout">
            <!-- 左侧 6 槽位 -->
            <div class="slots-col">
              <div v-for="slot in equipSlotsLeft" :key="slot.id" class="equip-slot-item" :title="slot.name">
                <div class="slot-icon-box">{{ slot.icon }}</div>
                <span class="slot-label">{{ slot.name }}</span>
              </div>
            </div>

            <!-- 中央恶魔暗影水印与无装备提示 -->
            <div class="equip-center-watermark">
              <img :src="equipSlotsWatermark" alt="" class="demon-silhouette-img" />
              <div class="watermark-title">暂 无 装 备</div>
              <div class="watermark-hint">去寻找强大的装备吧，提升你的力量。</div>
            </div>

            <!-- 右侧 6 槽位 -->
            <div class="slots-col">
              <div v-for="slot in equipSlotsRight" :key="slot.id" class="equip-slot-item" :title="slot.name">
                <div class="slot-icon-box">{{ slot.icon }}</div>
                <span class="slot-label">{{ slot.name }}</span>
              </div>
            </div>
          </div>
        </div>

        <!-- 最近变化 (☠ 最近变化) -->
        <div class="logs-subpanel gothic-frame-box">
          <img :src="cornerSkull" class="corner-skull-ornament top-left" alt="" />
          <img :src="cornerSkull" class="corner-skull-ornament top-right" alt="" />
          <img :src="cornerSkull" class="corner-skull-ornament bottom-left" alt="" />
          <img :src="cornerSkull" class="corner-skull-ornament bottom-right" alt="" />

          <div class="gothic-banner-header logs-title-banner" :style="{ backgroundImage: `url(${bannerDarkIron})` }">
            <div class="header-title-text">
              <span class="skull-sym">☠</span>
              <span>最 近 变 化</span>
            </div>
            <button class="refresh-tiny-btn" :class="{ spinning: isRefreshing }" @click="handleRefresh" title="刷新变化日志">
              <RefreshCw :size="12" />
            </button>
          </div>

          <div class="logs-scroll-area">
            <div v-for="item in recentLogs" :key="item.id" class="log-entry-row">
              <div class="log-diamond-symbol">✦</div>
              <div class="log-info-text">
                <div class="log-top-line">
                  <span class="log-attr-name">{{ item.attribute_name }}</span>
                  <span class="log-exp-gain">+{{ item.experience_gained }} EXP</span>
                </div>
                <div class="log-bottom-line">
                  <span class="log-source">{{ item.source_title }}</span>
                  <span class="log-time">{{ item.time_str || '刚刚' }}</span>
                </div>
              </div>
            </div>
          </div>

          <button class="view-all-logs-btn" @click="$emit('switch-tab', 'records')">
            查看全部日志
          </button>
        </div>
      </div>

      <!-- 下半区：折叠抽屉 (☠ 展开技能与 NPC/地图 ︿) -->
      <div class="drawer-subpanel gothic-frame-box">
        <img :src="cornerSkull" class="corner-skull-ornament top-left" alt="" />
        <img :src="cornerSkull" class="corner-skull-ornament top-right" alt="" />
        <img :src="cornerSkull" class="corner-skull-ornament bottom-left" alt="" />
        <img :src="cornerSkull" class="corner-skull-ornament bottom-right" alt="" />

        <div class="drawer-toggle-header" @click="isDrawerOpen = !isDrawerOpen">
          <div class="header-left-part">
            <span class="skull-sym">☠</span>
            <span class="drawer-header-text">展开技能与 NPC / 地图</span>
          </div>
          <component :is="isDrawerOpen ? ChevronUp : ChevronDown" :size="16" class="chevron-gold" />
        </div>

        <div v-if="isDrawerOpen" class="drawer-items-list">
          <div class="drawer-row-item" @click="$emit('switch-tab', 'tasks')">
            <div class="drawer-row-left">
              <Zap :size="14" style="color: var(--warlock-gold);" />
              <span class="drawer-row-name">技能</span>
            </div>
            <div class="drawer-row-right">
              <span class="drawer-status-tag">0/0 已解锁</span>
              <ChevronRight :size="14" class="arrow-dim" />
            </div>
          </div>

          <div class="drawer-row-item" @click="$emit('switch-tab', 'tasks')">
            <div class="drawer-row-left">
              <Skull :size="14" style="color: var(--warlock-soul-green);" />
              <span class="drawer-row-name">恶魔契约</span>
            </div>
            <div class="drawer-row-right">
              <span class="drawer-status-tag">0/0 已解锁</span>
              <ChevronRight :size="14" class="arrow-dim" />
            </div>
          </div>

          <div class="drawer-row-item" @click="$emit('switch-tab', 'archive')">
            <div class="drawer-row-left">
              <Crown :size="14" style="color: #ff9955;" />
              <span class="drawer-row-name">势力声望</span>
            </div>
            <div class="drawer-row-right">
              <span class="drawer-status-tag">0/0 已解锁</span>
              <ChevronRight :size="14" class="arrow-dim" />
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* ═══ 全景主容器 ═══ */
.wow-dashboard-container {
  display: grid;
  grid-template-columns: 320px 1fr 500px;
  gap: 14px;
  width: 100%;
  height: 100%;
  padding: 14px;
  box-sizing: border-box;
  overflow: hidden;
  background: transparent;
  transform: translateZ(0);
}

/* 哥特金石边框底板 */
.gothic-frame-box {
  background: linear-gradient(180deg, rgba(22, 12, 32, 0.94) 0%, rgba(10, 5, 15, 0.98) 100%);
  border: 1.5px solid var(--warlock-border-gold);
  border-radius: 6px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.9), inset 0 1px 0 rgba(200, 164, 93, 0.45);
  position: relative;
  overflow: hidden;
}

/* 装饰角标 */
.corner-skull-ornament {
  position: absolute;
  width: 24px;
  height: 24px;
  object-fit: contain;
  pointer-events: none;
  z-index: 8;
  opacity: 0.9;
  filter: drop-shadow(0 0 4px rgba(0, 0, 0, 0.8));
}

.corner-skull-ornament.top-left {
  top: -2px;
  left: -2px;
}

.corner-skull-ornament.top-right {
  top: -2px;
  right: -2px;
  transform: scaleX(-1);
}

.corner-skull-ornament.bottom-left {
  bottom: -2px;
  left: -2px;
  transform: scaleY(-1);
}

.corner-skull-ornament.bottom-right {
  bottom: -2px;
  right: -2px;
  transform: scale(-1);
}

/* 纹理条幅标题栏 */
.gothic-banner-header {
  height: 42px;
  background-size: 100% 100%;
  background-position: center;
  background-repeat: no-repeat;
  display: flex;
  align-items: center;
  justify-content: center;
  border-bottom: 1px solid rgba(200, 164, 93, 0.55);
  position: relative;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.85);
}

.logs-title-banner {
  justify-content: space-between;
  padding: 0 14px;
}

.header-title-text {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  font-weight: 800;
  color: var(--warlock-gold);
  letter-spacing: 2px;
  text-shadow: 0 1px 2px #000, 0 0 1px #000;
}

.skull-sym {
  color: var(--warlock-soul-green);
  text-shadow: 0 0 8px var(--warlock-soul-green-glow);
  font-size: 13px;
}

.star-sym {
  color: var(--warlock-gold);
  text-shadow: 0 0 8px var(--warlock-gold-glow);
  font-size: 13px;
}

/* ═══ 1. 左栏：角色卡片 ═══ */
.col-character {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 16px 14px;
  position: relative;
}

.lvl-badge-shield {
  position: absolute;
  top: 10px;
  left: 12px;
  background: linear-gradient(180deg, #2a1b38 0%, #12091c 100%);
  border: 1.5px solid var(--warlock-border-gold);
  border-radius: 4px;
  padding: 2px 8px;
  box-shadow: 0 4px 10px rgba(0, 0, 0, 0.85), inset 0 1px 0 rgba(255, 215, 0, 0.4);
  z-index: 10;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.lvl-txt-label {
  font-size: 9px;
  font-weight: 700;
  color: var(--warlock-gold);
}

.lvl-txt-num {
  font-size: 16px;
  font-weight: 900;
  color: var(--warlock-soul-green-bright);
  text-shadow: 0 0 8px var(--warlock-soul-green-glow);
}

.portrait-display-area {
  width: 240px;
  height: 310px;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-top: 6px;
}

.portrait-glow-halo {
  position: absolute;
  width: 200px;
  height: 250px;
  background: radial-gradient(circle at 50% 40%, rgba(72, 255, 72, 0.3) 0%, transparent 70%);
  pointer-events: none;
}

.portrait-main-img {
  width: 100%;
  height: 100%;
  object-fit: contain;
  filter: drop-shadow(0 0 16px rgba(72, 255, 72, 0.45));
  position: relative;
  z-index: 2;
}

.fel-ground-rune {
  position: absolute;
  bottom: 0;
  width: 200px;
  height: 28px;
  background: radial-gradient(ellipse at center, rgba(72, 255, 72, 0.6) 0%, transparent 70%);
  pointer-events: none;
  z-index: 1;
}

.char-identity-block {
  text-align: center;
  margin-top: 6px;
  width: 100%;
}

.char-name-gold {
  font-size: 24px;
  font-weight: 900;
  color: var(--warlock-gold);
  letter-spacing: 3px;
  text-shadow: 0 1px 2px #000, 0 0 1px #000;
  margin-bottom: 2px;
}

.char-title-fel {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  font-size: 13px;
  font-weight: 800;
  color: var(--warlock-soul-green-bright);
  letter-spacing: 2px;
  text-shadow: 0 1px 2px #000, 0 0 6px var(--warlock-soul-green-glow);
  margin-bottom: 8px;
}

.char-flavor-quote {
  font-size: 11px;
  color: #cbd5e1;
  font-style: italic;
  line-height: 1.5;
  padding: 0 4px;
  opacity: 0.92;
  text-shadow: 0 1px 2px #000;
}

.char-actions-grid {
  display: flex;
  flex-direction: column;
  gap: 6px;
  width: 100%;
  margin-top: auto;
}

.btn-half-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 6px;
}

.gothic-btn {
  background: linear-gradient(180deg, #2c1c3c 0%, #150c1e 100%);
  border: 1px solid var(--warlock-border-gold);
  border-radius: 4px;
  color: #f1f5f9;
  font-size: 12px;
  font-weight: 700;
  padding: 7px 10px;
  cursor: pointer;
  box-shadow: var(--gold-emboss);
  transition: all 0.2s ease;
  text-shadow: 0 1px 2px #000;
}

.gothic-btn:hover {
  background: linear-gradient(180deg, #402856 0%, #201230 100%);
  border-color: var(--warlock-gold);
  color: var(--warlock-gold-bright);
  box-shadow: 0 0 10px var(--warlock-gold-glow);
}

.btn-full {
  width: 100%;
}

/* ═══ 2. 中栏：主核心区 ═══ */
.col-center-core {
  display: flex;
  flex-direction: column;
  gap: 14px;
  height: 100%;
}

.exp-subpanel {
  padding-bottom: 12px;
  flex-shrink: 0;
}

.exp-stats-four-cols {
  display: grid;
  grid-template-columns: 1.4fr 1.2fr 1fr 1.2fr;
  padding: 10px 14px 6px;
  gap: 8px;
}

.stat-col {
  background: rgba(14, 8, 20, 0.75);
  border: 1px solid rgba(120, 90, 150, 0.4);
  border-radius: 4px;
  padding: 6px 10px;
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.stat-label {
  font-size: 11px;
  color: #94a3b8;
  font-weight: 700;
  margin-bottom: 2px;
}

.stat-val-huge {
  font-size: 20px;
  font-weight: 900;
  font-family: var(--font-mono);
  color: var(--warlock-soul-green-bright);
  text-shadow: 0 1px 2px #000;
}

.stat-val-mid {
  font-size: 14px;
  font-weight: 800;
  font-family: var(--font-mono);
  color: #ffffff;
  text-shadow: 0 1px 2px #000;
}

.stat-val-date {
  font-size: 12px;
  font-family: var(--font-mono);
  color: #cbd5e1;
  text-shadow: 0 1px 2px #000;
}

/* 双翼龙爪进度槽 */
.exp-progress-wrapper {
  display: flex;
  align-items: center;
  padding: 6px 16px 2px;
  gap: 4px;
}

.exp-wing-claw {
  width: 14px;
  height: 18px;
  background: linear-gradient(135deg, #ffd700 0%, #b8860b 100%);
  clip-path: polygon(0 50%, 100% 0, 100% 100%);
  filter: drop-shadow(0 0 4px rgba(255, 215, 0, 0.5));
}

.exp-wing-claw.right-claw {
  transform: scaleX(-1);
}

.exp-track-gold-frame {
  flex: 1;
  height: 18px;
  background: rgba(10, 5, 16, 0.95);
  border: 1.5px solid var(--warlock-border-gold);
  border-radius: 4px;
  position: relative;
  overflow: hidden;
  box-shadow: inset 0 2px 6px rgba(0, 0, 0, 0.9);
}

.exp-fill-green {
  height: 100%;
  background: linear-gradient(90deg, #166534 0%, #22c55e 50%, #48ff48 100%);
  box-shadow: 0 0 12px var(--warlock-soul-green);
  transition: width 0.4s ease;
}

.exp-center-pill {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  background: rgba(10, 5, 16, 0.92);
  border: 1px solid var(--warlock-border-gold);
  border-radius: 4px;
  padding: 0 8px;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.8);
}

.pct-text {
  font-family: var(--font-mono);
  font-size: 11px;
  font-weight: 900;
  color: var(--warlock-gold);
  text-shadow: 0 1px 2px #000;
}

.exp-bottom-numbers {
  text-align: right;
  padding: 0 18px;
  font-family: var(--font-mono);
  font-size: 11px;
  color: #94a3b8;
  text-shadow: 0 1px 2px #000;
}

/* ※ 暗影契约 ※ 下半区星盘 */
.contract-subpanel {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.astrolabe-stage {
  flex: 1;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  padding: 10px;
}

.center-magic-ring-wrapper {
  position: absolute;
  width: 320px;
  height: 320px;
  display: flex;
  align-items: center;
  justify-content: center;
  pointer-events: none;
}

.outer-glow-bg {
  position: absolute;
  width: 100%;
  height: 100%;
  object-fit: contain;
  opacity: 0.5;
  filter: blur(4px);
}

.rotating-magic-ring {
  width: 85%;
  height: 85%;
  object-fit: contain;
  opacity: 0.75;
  filter: drop-shadow(0 0 16px rgba(72, 255, 72, 0.55));
  animation: magic-rotate 40s linear infinite;
  position: relative;
  z-index: 2;
}

@keyframes magic-rotate {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.center-fel-flame {
  position: absolute;
  color: var(--warlock-soul-green);
  filter: drop-shadow(0 0 18px var(--warlock-soul-green));
  animation: flame-flicker 2.5s infinite ease-in-out;
  z-index: 3;
}

@keyframes flame-flicker {
  0%, 100% { transform: scale(0.95); opacity: 0.85; }
  50% { transform: scale(1.1); opacity: 1; }
}

/* 六维环形属性卡片 */
.attr-node-box {
  position: absolute;
  width: 185px;
  background: rgba(18, 11, 26, 0.94);
  border: 1px solid var(--warlock-border-gold);
  border-radius: 6px;
  padding: 6px 8px;
  display: flex;
  align-items: center;
  gap: 8px;
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.8), inset 0 1px 0 rgba(200, 164, 93, 0.3);
  cursor: pointer;
  transition: all 0.2s ease;
  z-index: 5;
}

.attr-node-box:hover {
  background: rgba(30, 18, 44, 0.98);
  border-color: var(--warlock-gold);
  box-shadow: 0 0 14px var(--warlock-gold-glow);
  transform: scale(1.03);
}

.node-top-left { top: 22px; left: 16px; }
.node-top-right { top: 22px; right: 16px; }
.node-mid-left { top: 48%; left: 8px; transform: translateY(-50%); }
.node-mid-right { top: 48%; right: 8px; transform: translateY(-50%); }
.node-bottom-left { bottom: 22px; left: 16px; }
.node-bottom-right { bottom: 22px; right: 16px; }

.attr-rune-diamond {
  width: 28px;
  height: 28px;
  background: linear-gradient(135deg, #2a1b38 0%, #100818 100%);
  border: 1px solid var(--warlock-border-gold);
  border-radius: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.8);
}

.diamond-icon {
  font-size: 14px;
}

.attr-node-content {
  flex: 1;
  min-width: 0;
}

.node-title-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.node-name {
  font-size: 12px;
  font-weight: 800;
  color: var(--warlock-gold);
  text-shadow: 0 1px 2px #000;
}

.node-lvl {
  font-family: var(--font-mono);
  font-size: 11px;
  font-weight: 800;
  color: var(--warlock-soul-green-bright);
}

.node-exp-line {
  font-family: var(--font-mono);
  font-size: 9px;
  color: #94a3b8;
  margin: 1px 0 3px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.node-mini-bar-track {
  width: 100%;
  height: 4px;
  background: rgba(10, 5, 16, 0.9);
  border-radius: 2px;
  overflow: hidden;
}

.node-mini-bar-fill {
  height: 100%;
  background: linear-gradient(90deg, #15803d 0%, #48ff48 100%);
}

/* ═══ 3. 右栏：装备与日志 ═══ */
.col-right-side {
  display: flex;
  flex-direction: column;
  gap: 14px;
  height: 100%;
}

.top-side-split {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
  flex: 1;
  min-height: 0;
}

/* 装备栏 */
.equip-subpanel {
  display: flex;
  flex-direction: column;
}

.equip-slots-layout {
  flex: 1;
  display: flex;
  justify-content: space-between;
  padding: 10px;
  position: relative;
}

.slots-col {
  display: flex;
  flex-direction: column;
  gap: 6px;
  z-index: 2;
}

.equip-slot-item {
  display: flex;
  align-items: center;
  gap: 6px;
}

.slot-icon-box {
  width: 34px;
  height: 34px;
  background: rgba(16, 9, 24, 0.95);
  border: 1px solid var(--warlock-border-gold);
  border-radius: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 15px;
  box-shadow: inset 0 2px 4px rgba(0, 0, 0, 0.8), 0 2px 4px rgba(0, 0, 0, 0.6);
  cursor: pointer;
  transition: all 0.2s ease;
}

.slot-icon-box:hover {
  border-color: var(--warlock-gold);
  box-shadow: 0 0 10px var(--warlock-gold-glow);
}

.slot-label {
  font-size: 10px;
  color: #94a3b8;
  font-weight: 700;
}

.equip-center-watermark {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  pointer-events: none;
  z-index: 1;
}

.demon-silhouette-img {
  width: 80px;
  height: 80px;
  object-fit: contain;
  opacity: 0.25;
  filter: drop-shadow(0 0 10px rgba(0, 0, 0, 0.9));
}

.watermark-title {
  font-size: 13px;
  font-weight: 800;
  color: #64748b;
  letter-spacing: 2px;
  margin-top: 4px;
}

.watermark-hint {
  font-size: 10px;
  color: #475569;
  margin-top: 2px;
  text-align: center;
  max-width: 140px;
}

/* 最近变化日志 */
.logs-subpanel {
  display: flex;
  flex-direction: column;
}

.refresh-tiny-btn {
  background: transparent;
  border: none;
  color: var(--warlock-gold);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 4px;
}

.refresh-tiny-btn.spinning {
  animation: spin 0.6s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.logs-scroll-area {
  flex: 1;
  overflow-y: auto;
  padding: 8px 10px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.log-entry-row {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  padding: 6px 8px;
  background: rgba(14, 8, 20, 0.7);
  border: 1px solid rgba(120, 90, 150, 0.35);
  border-radius: 4px;
}

.log-diamond-symbol {
  color: var(--warlock-soul-green);
  font-size: 11px;
  margin-top: 2px;
}

.log-info-text {
  flex: 1;
  min-width: 0;
}

.log-top-line {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.log-attr-name {
  font-size: 12px;
  font-weight: 800;
  color: #ffffff;
}

.log-exp-gain {
  font-family: var(--font-mono);
  font-size: 11px;
  font-weight: 800;
  color: var(--warlock-soul-green-bright);
}

.log-bottom-line {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 10px;
  color: #94a3b8;
  margin-top: 1px;
}

.view-all-logs-btn {
  background: linear-gradient(180deg, #251635 0%, #12091c 100%);
  border: none;
  border-top: 1px solid rgba(200, 164, 93, 0.35);
  color: var(--warlock-gold);
  font-size: 11px;
  font-weight: 800;
  padding: 8px;
  cursor: pointer;
  text-align: center;
}

.view-all-logs-btn:hover {
  color: #ffffff;
  background: linear-gradient(180deg, #361f4c 0%, #1c0e2c 100%);
}

/* 下半区折叠抽屉 */
.drawer-subpanel {
  flex-shrink: 0;
}

.drawer-toggle-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 14px;
  background: linear-gradient(180deg, #2a183d 0%, #160c22 100%);
  border-bottom: 1px solid rgba(200, 164, 93, 0.4);
  cursor: pointer;
}

.header-left-part {
  display: flex;
  align-items: center;
  gap: 6px;
}

.drawer-header-text {
  font-size: 12px;
  font-weight: 800;
  color: var(--warlock-gold);
  letter-spacing: 1px;
  text-shadow: 0 1px 2px #000;
}

.chevron-gold {
  color: var(--warlock-gold);
}

.drawer-items-list {
  padding: 6px 10px;
  display: flex;
  flex-direction: column;
  gap: 6px;
  background: rgba(12, 6, 18, 0.85);
}

.drawer-row-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 6px 10px;
  background: rgba(20, 12, 30, 0.7);
  border: 1px solid rgba(120, 90, 150, 0.35);
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.drawer-row-item:hover {
  background: rgba(36, 22, 54, 0.9);
  border-color: var(--warlock-gold);
}

.drawer-row-left {
  display: flex;
  align-items: center;
  gap: 8px;
}

.drawer-row-name {
  font-size: 12px;
  font-weight: 700;
  color: #ffffff;
}

.drawer-row-right {
  display: flex;
  align-items: center;
  gap: 6px;
}

.drawer-status-tag {
  font-size: 10px;
  color: #94a3b8;
  font-family: var(--font-mono);
}

.arrow-dim {
  color: #64748b;
}
</style>
