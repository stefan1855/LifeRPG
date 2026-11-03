<script setup>
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue';
import { RefreshCw, Sparkles, Check, Pencil, ChevronRight, Zap, Users, MapPin } from 'lucide-vue-next';
import { open } from '@tauri-apps/plugin-dialog';
import RelicPanel from '../components/RelicPanel.vue';
import DashboardEquipment from '../components/DashboardEquipment.vue';
import StatRing from '../components/StatRing.vue';
import ModalShell from '../components/ModalShell.vue';
import { api, attrMeta, relativeTime } from '../api';
import { notify, showLevelUp } from '../stores/toast';
import { sfx } from '../utils/audio';
import { themeAsset,themeAttribute } from '../utils/referenceTheme';
const emit = defineEmits(['navigate']);
const dashElement=ref(null),dashboardWide=ref(true);
let resizeObserver;
onMounted(()=>{resizeObserver=new ResizeObserver(([e])=>dashboardWide.value=e.contentRect.width>730);resizeObserver.observe(dashElement.value);});
onUnmounted(()=>resizeObserver?.disconnect());
const loading = ref(true), snap = ref(null), logs = ref([]), pending = ref([]), equipment = ref(null);
const skills = ref([]), npcs = ref([]), maps = ref([]), loadError = ref('');
const saving = ref(false), completing = reactive(new Set()), avatarBusy = ref(false), avatarUrl = ref('');
let avatarPath;
const level = computed(() => snap.value?.levelInfo || {});
const character = computed(() => snap.value?.character || {});
const attributes = computed(() => snap.value?.attributes || []);
const xpSpan = computed(() => (level.value.experienceRequiredForNextLevel - level.value.experienceRequiredForCurrentLevel) || 100);
const progress = computed(() => Math.max(0, Math.min(100, level.value.progressPercent || 0)));
const shortDate = value => value ? new Date(value).toLocaleDateString('zh-CN', { year:'numeric', month:'2-digit', day:'2-digit' }) : '—';
async function load() {
  if (loading.value && snap.value) return;
  loading.value = true; loadError.value = '';
  try {
    const [d,l,t,e,s,n,m] = await Promise.all([api.dashboard(),api.logs(14),api.tasks(),api.equipSnapshot(),api.skillList(),api.npcList(),api.mapList()]);
    snap.value=d; logs.value=l; equipment.value=e; skills.value=s; npcs.value=n; maps.value=m;
    pending.value=t.filter(x=>x.status!=='Completed').sort((a,b)=>(a.dueAt?new Date(a.dueAt).getTime():Infinity)-(b.dueAt?new Date(b.dueAt).getTime():Infinity)).slice(0,6);
    if (avatarPath !== d.character.avatarImagePath) {
      avatarPath=d.character.avatarImagePath;
      avatarUrl.value='';
      if (avatarPath) { try { avatarUrl.value=await api.imageDataUrl(avatarPath); } catch(e) { avatarPath=undefined; notify.warn('头像读取失败：'+e.message); } }
    }
  } catch(e) { loadError.value=e.message; notify.error('加载仪表盘失败：'+e.message); }
  finally { loading.value=false; }
}
async function completeTask(t) {
  if (completing.has(t.id)) return;
  completing.add(t.id);
  try {
    const r=await api.completeTask(t.id);
    if(r.applied&&!r.isDuplicate) { sfx.hammer(); notify.success(`完成「${t.title}」，获得 +${r.totalExperienceGranted} EXP`); if(r.leveledUp)showLevelUp(r.newLevel); }
    await load();
  } catch(e) { notify.error('结算任务失败：'+e.message); }
  finally { completing.delete(t.id); }
}
const showCharEdit=ref(false),charForm=reactive({name:'',description:''});
function openCharEdit() { charForm.name=character.value.name||'人生主角';charForm.description=character.value.description||'';showCharEdit.value=true; }
async function submitCharEdit() {
  if(!charForm.name.trim())return notify.warn('名号不能为空');
  if(saving.value)return;
  saving.value=true;
  try { await api.updateCharacter(charForm.name.trim(),charForm.description.trim());showCharEdit.value=false;sfx.hammer();await load();notify.success('角色铭牌已更新'); }
  catch(e){notify.error('保存失败：'+e.message);}finally{saving.value=false;}
}
async function saveAvatarFit(event){try{await api.setAvatarFit(event.target.value);await load();}catch(e){notify.error(e.message);}}
async function setAvatar(clear=false) {
  if(avatarBusy.value)return;
  avatarBusy.value=true;
  try {
    const path=clear?null:await open({multiple:false,filters:[{name:'图片',extensions:['png','jpg','jpeg','webp']}]});
    if(!clear&&!path)return;
    await api.setAvatar(path);await load();notify.success(clear?'已恢复默认立绘':'头像已更新');
  }catch(e){notify.error('更新头像失败：'+e.message);}finally{avatarBusy.value=false;}
}
const coachBusy=ref(false),coachText=ref(''),showCoach=ref(false);
const showHistory=ref(false),historyLogs=ref([]),historyBusy=ref(false),historyError=ref('');
let historyLimit=100;
async function loadHistory(reset=false) {
  if(historyBusy.value)return;
  showHistory.value=true;historyBusy.value=true;historyError.value='';
  if(reset){historyLimit=100;historyLogs.value=[];}
  else historyLimit+=100;
  try{historyLogs.value=await api.logs(historyLimit);}
  catch(e){historyError.value=e.message;}
  finally{historyBusy.value=false;}
}
async function askCoach(){if(coachBusy.value)return;coachBusy.value=true;showCoach.value=true;coachText.value='';try{coachText.value=await api.aiCoach();}catch(e){notify.error('AI 教练失败：'+e.message);showCoach.value=false;}finally{coachBusy.value=false;}}
onMounted(load);
</script>

<template>
  <div ref="dashElement" class="dash" :aria-busy="loading">
    <div class="dash-toolbar"><span class="archive-mark">今日的行动，铭刻灵魂的成长</span><div class="dash-actions"><span class="today-badge">今日完成 <b>{{ snap?.todayCompletedCount ?? '—' }}</b> / {{ snap?.todayTotalCount ?? '—' }}</span><button class="btn btn-sm btn-ghost" :disabled="coachBusy" @click="askCoach"><Sparkles :size="13"/>AI 教练</button><button class="btn btn-sm" :disabled="loading" @click="load"><RefreshCw :size="13" :class="{spinning:loading}"/>刷新数据</button></div></div>
    <p v-if="loadError" class="load-error" role="alert">仪表盘暂未更新：{{ loadError }}。请点击刷新重试。</p>
    <div v-if="loading&&!snap" class="dash-loading">正在唤醒灵魂档案…</div>
    <div v-else-if="snap" class="dashboard-stage">
      <RelicPanel class="character-panel">
        <div class="character-scene" :class="{custom:avatarUrl}"><img v-if="avatarUrl" :src="avatarUrl" class="character-art custom-art" :style="{objectFit:character.avatarFit||'contain'}" alt="角色头像"/><img v-else :src="themeAsset('character-cutout')" class="character-art" alt="身着黑铁古铜护甲的无面术士，举起绿色魂火"/><div class="scene-vignette"></div><span class="level-pennant">Lv.<strong>{{ level.level || 1 }}</strong></span></div>
        <div class="character-identity"><h1 :title="character.name">{{ character.name }}</h1><p class="character-title">✧ 暗影学徒 ✧</p><p class="character-description">{{ character.description || '每一次行动，都在铭刻新的成长。' }}</p></div>
        <div class="character-controls"><label v-if="character.avatarImagePath" class="avatar-fit-label">头像显示<select :value="character.avatarFit||'contain'" @change="saveAvatarFit"><option value="contain">完整显示</option><option value="cover">铺满裁切</option></select></label><button class="btn btn-sm" :disabled="avatarBusy" @click="setAvatar()">选择头像</button><button class="btn btn-sm btn-ghost" :disabled="avatarBusy||!character.avatarImagePath" @click="setAvatar(true)">清除头像</button><button class="btn btn-primary edit-character" @click="openCharEdit"><Pencil :size="13"/>编辑角色资料</button></div>
      </RelicPanel>
      <div class="center-column">
        <RelicPanel title="经验区域" crest="✦" class="experience-panel">
          <div class="experience-summary"><div class="soul-total"><span>灵魂能量</span><strong class="num experience-total">{{ (level.totalExperience||0).toLocaleString() }}</strong></div><div class="next-level"><span>距离下一级</span><b class="num">{{ (level.experienceToNextLevel||0).toLocaleString() }}</b><small>EXP · Lv.{{ (level.level||1)+1 }}</small></div><div class="contract-dates"><span>契约之日</span><b>{{ shortDate(character.createdAt) }}</b><span>最近成长</span><b>{{ character.lastGrowthAt?relativeTime(character.lastGrowthAt):'尚无记录' }}</b></div></div>
          <div class="soul-progress" role="progressbar" aria-label="当前等级经验" :aria-valuenow="progress" aria-valuemin="0" aria-valuemax="100"><div class="soul-trough"><div class="soul-fluid" :style="{clipPath:`inset(0 ${100-progress}% 0 0)`}"></div></div><span class="num">{{ progress.toFixed(1) }}%</span></div><p class="experience-caption num">{{ level.experienceIntoCurrentLevel||0 }} / {{ xpSpan }} EXP</p>
        </RelicPanel>
        <RelicPanel title="暗影契约" crest="✧" class="contract-panel"><details class="contract-details" :open="dashboardWide"><summary>展开六维成长详情</summary><StatRing :attributes="attributes" :level="level.level||1" @navigate="emit('navigate',$event)"/></details></RelicPanel>
      </div>
      <div class="right-column">
        <details class="inventory-history" :open="dashboardWide"><summary>查看装备与最近变化</summary><div class="inventory-panels">
          <RelicPanel title="当前装备" crest="⚒" class="inventory-panel"><DashboardEquipment :snapshot="equipment" @navigate="emit('navigate',$event)"/></RelicPanel>
          <RelicPanel title="最近变化" crest="✦" class="history-panel"><div class="recent-logs"><div v-if="!logs.length" class="empty-inscription">尚无成长记录<br/><small>完成行动，点亮契约</small></div><div v-for="log in logs.slice(0,6)" :key="log.id" class="recent-log"><span class="log-rune" aria-hidden="true"><img v-if="themeAttribute(log.attributeKey)" :src="themeAttribute(log.attributeKey).history" alt=""/><span v-else>{{ attrMeta(log.attributeKey).sigil }}</span></span><div class="log-content"><div class="log-line"><b :title="attrMeta(log.attributeKey).label">{{ themeAttribute(log.attributeKey)?.title || attrMeta(log.attributeKey).label }}</b><strong class="num" :class="{negative:log.experienceDelta<0}">{{ log.experienceDelta>0?'+':'' }}{{ log.experienceDelta }} EXP</strong></div><p :title="log.reason">{{ log.reason }}</p><time>{{ relativeTime(log.createdAt) }}</time></div></div></div><button class="btn btn-sm inscription-link" @click="loadHistory(true)">查看全部日志 <ChevronRight :size="12"/></button></RelicPanel>
        </div></details>

        <RelicPanel title="技能与尘世入口" crest="✧" class="portals-panel"><button class="portal" @click="emit('navigate','skills')"><img :src="themeAsset('icon-skill')" alt=""/><span>天赋神殿</span><small>{{ skills.filter(s=>s.isUnlocked).length }} / {{ skills.length }} 已解锁</small><ChevronRight :size="14"/></button><button class="portal" @click="emit('navigate','world')"><img :src="themeAsset('icon-contract')" alt=""/><span>人物关系</span><small>{{ npcs.length }} 位 NPC</small><ChevronRight :size="14"/></button><button class="portal" @click="emit('navigate','world')"><img :src="themeAsset('icon-reputation')" alt=""/><span>尘世地图</span><small>{{ maps.filter(m=>m.isUnlocked).length }} / {{ maps.length }} 已解锁</small><ChevronRight :size="14"/></button></RelicPanel>
      </div>
        <RelicPanel title="待办行动" crest="⚔" class="action-panel"><template #actions><button class="heading-link" @click="emit('navigate','tasks')">全部任务 <ChevronRight :size="12"/></button></template><div class="pending-list"><p v-if="!pending.length" class="empty-inscription compact">当前没有待办行动</p><div v-for="task in pending" :key="task.id" class="pending-task"><button class="task-check" :disabled="completing.has(task.id)" :aria-label="`完成任务：${task.title}`" @click="completeTask(task)"><Check :size="13"/></button><button class="task-name" :title="task.title" @click="emit('navigate','tasks')">{{ task.title }}</button><span :class="{overdue:task.dueAt&&new Date(task.dueAt)<new Date()}">{{ task.dueAt?shortDate(task.dueAt):'待安排' }}</span></div></div></RelicPanel>
    </div>
    <ModalShell v-if="showCharEdit" :state="charForm" :busy="saving" title="铭刻角色真名" :width="460" @close="showCharEdit=false"><label class="field">角色名号<input v-model="charForm.name" maxlength="80"/></label><label class="field">灵魂判词<textarea v-model="charForm.description" rows="4" maxlength="1000"></textarea></label><template #footer><button class="btn btn-primary" :disabled="saving" @click="submitCharEdit">{{ saving?'保存中…':'铭刻保存' }}</button></template></ModalShell>
    <ModalShell v-if="showCoach" title="AI 教练" @close="showCoach=false"><p class="coach-response">{{ coachBusy?'正在推演成长建议…':coachText }}</p></ModalShell>
    <ModalShell v-if="showHistory" title="成长日志" :width="600" @close="showHistory=false"><p v-if="historyError" class="load-error" role="alert">{{ historyError }}</p><p v-if="!historyBusy&&!historyLogs.length&&!historyError" class="empty-inscription">尚无成长记录</p><div class="full-history"><div v-for="log in historyLogs" :key="log.id" class="recent-log"><span class="log-rune" aria-hidden="true"><img v-if="themeAttribute(log.attributeKey)" :src="themeAttribute(log.attributeKey).history" alt=""/><span v-else>{{ attrMeta(log.attributeKey).sigil }}</span></span><div class="log-content"><div class="log-line"><b :title="attrMeta(log.attributeKey).label">{{ themeAttribute(log.attributeKey)?.title || attrMeta(log.attributeKey).label }}</b><strong class="num" :class="{negative:log.experienceDelta<0}">{{ log.experienceDelta>0?'+':'' }}{{ log.experienceDelta }} EXP</strong></div><p>{{ log.reason }}</p><time>{{ new Date(log.createdAt).toLocaleString('zh-CN') }}</time></div></div></div><p v-if="historyBusy" class="empty-inscription compact">正在读取日志…</p><template #footer><span class="history-count">已显示 {{ historyLogs.length }} 条</span><button v-if="historyError||historyLogs.length===historyLimit" class="btn btn-sm" :disabled="historyBusy" @click="loadHistory(!!historyError)">{{ historyError?'重试':'加载更多' }}</button></template></ModalShell>
  </div>
</template>












<style scoped>
.dash{flex:1;min-width:0;min-height:0;overflow:auto;padding:0 3px 28px;container:dashboard/inline-size}
.dash-toolbar{display:flex;align-items:center;justify-content:space-between;gap:12px;min-height:40px;margin:0 3px 12px}.archive-mark{font-size:12px;color:#9c9673;letter-spacing:.08em}.dash-actions{display:flex;align-items:center;gap:8px;flex-wrap:wrap}.today-badge{font-size:12px;color:#b0a079}.today-badge b{color:#d0e594}.dash-actions .btn{min-height:32px}.dashboard-stage{display:grid;grid-template-columns:minmax(0,.96fr) minmax(0,1.3fr) minmax(0,1.32fr);gap:12px;align-items:stretch;min-height:730px}.center-column,.right-column{display:flex;flex-direction:column;gap:12px;min-width:0}.character-panel{min-width:0;overflow:hidden}.character-panel::after{border-image-source:url('../assets/reference/character-card-frame.webp');border-image-slice:200 210;border-image-width:24px;inset:-3px}.character-panel :deep(.relic-body){padding:0;gap:0;position:relative}.character-scene{position:relative;min-height:385px;flex:1;isolation:isolate;background:url('../assets/reference/character-scene-extended.webp') center 65%/cover no-repeat}.character-art{position:absolute;inset:0;width:100%;height:112%;object-fit:contain;object-position:center bottom;z-index:1;filter:drop-shadow(0 0 9px #4d750c30)}.custom-art{height:100%;object-position:center}.scene-vignette{position:absolute;inset:0;z-index:2;background:linear-gradient(#03070310,transparent 50%,#060b06 100%);pointer-events:none}.level-pennant{position:absolute;left:16px;top:16px;z-index:3;width:62px;height:90px;padding:13px 4px 10px;background:url('../assets/reference/level-pennant.webp') center/100% 100% no-repeat;text-align:center;font:700 21px/1.05 var(--f-mono);color:#b5ed5e}.level-pennant strong{display:block;font-size:33px;line-height:1.2}.character-identity{position:relative;z-index:2;padding:0 20px 17px;text-align:center;background:linear-gradient(#060b06,#070d07)}.character-identity h1{font:600 clamp(29px,2.8cqi,42px)/1.35 var(--f-title);color:#b8e76b;text-shadow:0 0 10px #7cff1f20;margin:0 0 6px;overflow-wrap:anywhere}.character-title{font-size:20px;font-weight:600;color:#d3cc78;letter-spacing:.1em;margin-bottom:11px}.character-description{font:14px/1.9 var(--f-body);color:#c0b89e;white-space:pre-line;overflow-wrap:anywhere;max-height:106px;overflow:auto;margin:0}.character-controls{padding:0 20px 23px;display:grid;grid-template-columns:1fr 1fr;gap:8px}.character-controls .btn{min-width:0;padding-left:10px;padding-right:10px;font-size:14px}.edit-character{grid-column:1/-1;min-height:39px}.avatar-fit-label{grid-column:1/-1;display:flex;justify-content:space-between;align-items:center;font-size:12px;color:#b9af85}.avatar-fit-label select{max-width:130px;background:#0a1107;color:#d9d7a0;border:1px solid #65582d;padding:4px}
.experience-panel{flex:none}.experience-panel :deep(.relic-head){min-height:36px;padding-top:6px;padding-bottom:4px}.experience-panel :deep(.relic-body){padding:7px 15px 10px;gap:3px}.contract-panel :deep(.relic-head){min-height:41px;padding-top:7px;padding-bottom:5px}.experience-panel :deep(.relic-head),.contract-panel :deep(.relic-head){justify-content:center}.experience-panel :deep(.relic-rule),.contract-panel :deep(.relic-rule){display:none}.experience-summary{display:grid;grid-template-columns:1.15fr 1fr 1fr;gap:10px;padding:0 3px 3px}.experience-summary>div{min-width:0;display:flex;flex-direction:column;gap:4px}.experience-summary span{font-size:14px;color:#c2af7c}.experience-summary small{font-size:12px;color:#a89c75}.experience-total{font:700 clamp(27px,2.65cqi,40px)/1.3 var(--f-title);color:#cfea78;overflow-wrap:anywhere}.next-level b{font-size:27px;line-height:1.5;color:#afa381}.contract-dates b{font-size:11px;line-height:1.4;font-weight:400;color:#c9b980}.contract-dates span{font-size:12px;line-height:1.4}.contract-dates span:nth-of-type(2){margin-top:2px}.soul-progress{position:relative;height:48px;background:url('../assets/reference/experience-empty.webp') center/100% 100% no-repeat;margin:0 -3px}.soul-trough{position:absolute;left:8.8%;right:8.8%;top:32%;height:37%;overflow:hidden}.soul-fluid{position:absolute;inset:0;background:url('../assets/reference/experience-fill.webp') left center/100% 100% no-repeat;transition:clip-path .6s}.soul-progress>span{position:absolute;inset:0;display:grid;place-items:center;color:#f3f2d8;font:700 19px var(--f-mono);text-shadow:0 1px 3px #000,1px 0 2px #000}.experience-caption{text-align:center;font:500 15px var(--f-title);color:#b6a97f;margin:-2px 0 0}.contract-panel{flex:1}.contract-panel :deep(.relic-body){padding:5px 13px 35px;justify-content:center}.contract-details{min-width:0}.contract-details>summary,.inventory-history>summary{display:none}
.inventory-history{flex:none;min-height:0}.inventory-panels{height:auto;display:grid;grid-template-columns:minmax(0,1.13fr) minmax(0,1fr);gap:10px;min-height:530px}.inventory-panel :deep(.relic-body),.history-panel :deep(.relic-body){padding:8px 10px 12px;gap:8px}.inventory-panel :deep(.relic-head),.history-panel :deep(.relic-head){padding:11px 11px 7px;gap:5px}.inventory-panel :deep(.relic-head h2),.history-panel :deep(.relic-head h2){font-size:17px;letter-spacing:.02em}.inventory-panel :deep(.relic-rule),.history-panel :deep(.relic-rule){display:none}.recent-logs{min-height:0;flex:1;display:flex;flex-direction:column;overflow:auto;max-height:408px}.recent-log{display:flex;align-items:flex-start;gap:7px;padding:8px 0;border-bottom:1px solid #65542c55;min-width:0}.recent-logs .recent-log{flex:1;min-height:69px}.log-rune{flex:none;width:34px;height:38px;display:grid;place-items:center;color:#bad25c}.log-rune img{width:100%;height:100%;object-fit:contain}.log-content{min-width:0;flex:1}.log-line{display:flex;align-items:baseline;gap:4px;flex-wrap:wrap;line-height:1.3}.log-line b{font-size:13px;font-weight:600;color:#d2c48a}.log-line strong{font-size:13px;color:#b9e458;font-weight:600}.log-line strong.negative{color:#de9276}.log-content p{font-size:12px;color:#b7ae8c;line-height:1.5;overflow:hidden;display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow-wrap:anywhere}.log-content time{display:block;text-align:right;font-size:11px;line-height:1.2;color:#9c9371;margin-top:3px}.inscription-link{width:100%;font-size:12px!important;padding-left:8px!important;padding-right:8px!important}.empty-inscription{color:#a59b79;text-align:center;font-size:14px;line-height:1.9;margin:auto;padding:16px 5px}.empty-inscription small{font-size:12px}.empty-inscription.compact{padding:4px}.portals-panel{flex:none}.portals-panel :deep(.relic-head h2){font-size:18px}.portals-panel :deep(.relic-body){padding:4px 12px 12px;gap:6px}.portal{display:flex;align-items:center;gap:8px;width:100%;min-height:36px;padding:5px 12px;border:0;cursor:pointer;background:url('../assets/reference/portal-row-empty.webp') center/100% 100% no-repeat;color:#d5c280;font:500 16px var(--f-body);text-align:left}.portal>img{width:23px;height:23px;object-fit:contain}.portal small{margin-left:auto;font:12px var(--f-body);color:#a99c74}.portal:hover{color:#d2e78a;filter:brightness(1.12)}
.action-panel{grid-column:1/-1}.action-panel :deep(.relic-head){min-height:38px;padding:8px 15px}.action-panel :deep(.relic-body){padding:4px 14px 12px}.pending-list{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:4px 16px}.pending-task{display:flex;align-items:center;gap:7px;min-width:0;min-height:34px}.task-check{display:grid;place-items:center;flex:none;width:26px;height:26px;border:1px solid #776533;background:#0a1208;color:#a9bf6a;cursor:pointer}.task-check:disabled{opacity:.5;cursor:wait}.task-name{border:0;background:none;color:#c4bd94;font:14px var(--f-body);white-space:nowrap;overflow:hidden;text-overflow:ellipsis;text-align:left;cursor:pointer;min-width:0;flex:1}.pending-task>span{font-size:12px;color:#968d6c;white-space:nowrap}.pending-task>span.overdue{color:#e0a16e}.heading-link{display:flex;gap:4px;align-items:center;border:0;background:transparent;color:#b2bd71;font:12px var(--f-body);cursor:pointer}.load-error{color:#e0a16e;padding:9px;font-size:13px}.dash-loading{padding:70px;text-align:center;color:#b9c67a}.coach-response{white-space:pre-wrap;font-size:15px;line-height:1.9}.full-history .log-content p{display:block;overflow:visible;font-size:14px}.full-history .recent-log{padding:14px 0}.history-count{margin-right:auto;color:#b8ab7e;font-size:13px}
@container dashboard (max-width:1130px){.dashboard-stage{grid-template-columns:minmax(0,.92fr) minmax(0,1.25fr);align-items:start}.center-column,.right-column{display:contents}.character-panel{grid-column:1;grid-row:1/3;align-self:stretch}.experience-panel{grid-column:2;grid-row:1}.contract-panel{grid-column:2;grid-row:2;min-height:0}.inventory-history{grid-column:1/-1;grid-row:3}.inventory-panels{min-height:560px;grid-template-columns:1fr 1fr}.portals-panel{grid-column:1/-1;grid-row:4}.action-panel{grid-column:1/-1;grid-row:5}.pending-list{grid-template-columns:repeat(2,minmax(0,1fr))}.character-scene{min-height:350px}.contract-panel :deep(.relic-body){padding-bottom:30px}}
@container dashboard (max-width:700px){.dash-toolbar{flex-wrap:wrap;gap:6px}.archive-mark{display:none}.dash-actions{width:100%;justify-content:flex-end;gap:5px}.today-badge{margin-right:auto}.dashboard-stage{grid-template-columns:minmax(0,1fr)}.character-panel{grid-column:1;grid-row:1;min-height:240px}.character-panel :deep(.relic-body){display:grid;grid-template-columns:40% minmax(0,1fr);grid-template-rows:1fr auto}.character-scene{grid-row:1/3;min-height:240px}.character-art{height:100%}.character-identity{padding:18px 13px 10px;background:#070c07}.character-identity h1{font-size:27px}.character-title{font-size:16px}.character-description{font-size:13px;max-height:70px}.character-controls{padding:0 12px 17px;gap:5px}.character-controls .btn{font-size:12px;min-height:30px}.level-pennant{left:9px;top:9px;width:44px;height:65px;font-size:15px;padding-top:10px}.level-pennant strong{font-size:24px}.experience-panel{grid-column:1;grid-row:2}.contract-panel{grid-column:1;grid-row:3}.inventory-history{grid-column:1;grid-row:4}.portals-panel{grid-column:1;grid-row:5}.action-panel{grid-column:1;grid-row:6}.inventory-panels{height:auto;grid-template-columns:1fr;min-height:0}.inventory-panel{min-height:540px}.history-panel{min-height:330px}.recent-logs{max-height:420px}.inventory-history>summary,.contract-details>summary{display:list-item;cursor:pointer;font-size:14px;color:#d0c282;padding:8px}.inventory-history{border:1px solid #60542d;padding:8px;background:#080d08}.inventory-history:not([open])>.inventory-panels,.contract-details:not([open])>:not(summary){display:none}.experience-summary{grid-template-columns:1fr 1fr 1fr;gap:7px}.experience-summary span{font-size:12px}.experience-total{font-size:29px}.next-level b{font-size:25px}.contract-dates b{font-size:11px}.pending-list{grid-template-columns:1fr}.contract-panel :deep(.relic-body){padding-bottom:32px}}
@media(prefers-reduced-motion:reduce){.soul-fluid{transition:none}}
</style>
