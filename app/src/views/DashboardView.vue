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
    <div class="dash-toolbar"><span class="archive-mark">✧ <span>SOUL ARCHIVE</span> <i></i> 成长档案</span><div class="dash-actions"><span class="today-badge">今日完成 <b>{{ snap?.todayCompletedCount ?? '—' }}</b> / {{ snap?.todayTotalCount ?? '—' }}</span><button class="btn btn-sm btn-ghost" :disabled="coachBusy" @click="askCoach"><Sparkles :size="13"/>AI 教练</button><button class="btn btn-sm" :disabled="loading" @click="load"><RefreshCw :size="13" :class="{spinning:loading}"/>刷新</button></div></div>
    <p v-if="loadError" class="load-error" role="alert">仪表盘暂未更新：{{ loadError }}。请点击刷新重试。</p>
    <div v-if="loading&&!snap" class="dash-loading">正在唤醒灵魂档案…</div>
    <div v-else-if="snap" class="dashboard-stage">
      <RelicPanel class="character-panel">
        <div class="character-scene" :class="{custom:avatarUrl}"><img v-if="avatarUrl" :src="avatarUrl" class="character-art custom-art" :style="{objectFit:character.avatarFit||'contain'}" alt="角色头像"/><img v-else src="../assets/dashboard/faceless-cutout-v1.png" class="character-art" alt="无面术士，手持绿火与法杖"/><div class="scene-vignette"></div><span class="level-pennant">Lv.<strong>{{ level.level || 1 }}</strong></span><span class="scene-caption">THE FACELESS ONE</span></div>
        <div class="character-identity"><span class="identity-rule" aria-hidden="true">◆</span><h1 :title="character.name">{{ character.name }}</h1><p class="character-title">✧ 暗影学徒 ✧</p><p class="character-description">{{ character.description || '每一次行动，都在铭刻新的成长。' }}</p></div>
        <div class="character-controls"><label v-if="character.avatarImagePath" class="avatar-fit-label">头像显示<select :value="character.avatarFit||'contain'" @change="saveAvatarFit"><option value="contain">完整显示</option><option value="cover">铺满裁切</option></select></label><button class="btn btn-sm" :disabled="avatarBusy" @click="setAvatar()">选择头像</button><button class="btn btn-sm btn-ghost" :disabled="avatarBusy||!character.avatarImagePath" @click="setAvatar(true)">清除头像</button><button class="btn btn-primary edit-character" @click="openCharEdit"><Pencil :size="13"/>编辑角色资料</button></div>
      </RelicPanel>
      <div class="center-column">
        <RelicPanel title="经验区域" crest="✦" class="experience-panel">
          <div class="experience-summary"><div class="soul-total"><span>灵魂能量</span><strong class="num">{{ (level.totalExperience||0).toLocaleString() }}</strong><small>累计 EXP</small></div><div class="next-level"><span>距离下一级</span><b class="num">{{ (level.experienceToNextLevel||0).toLocaleString() }}</b><small>Lv.{{ (level.level||1)+1 }}</small></div><div class="contract-dates"><span>契约之日</span><b>{{ shortDate(character.createdAt) }}</b><span>最近成长</span><b>{{ character.lastGrowthAt?relativeTime(character.lastGrowthAt):'尚无记录' }}</b></div></div>
          <div class="soul-progress" role="progressbar" aria-label="当前等级经验" :aria-valuenow="progress" aria-valuemin="0" aria-valuemax="100"><div class="soul-fluid" :style="{width:progress+'%'}"></div><span class="num">{{ progress.toFixed(1) }}%</span><i class="xp-cap left">◆</i><i class="xp-cap right">◆</i></div><p class="experience-caption num">{{ level.experienceIntoCurrentLevel||0 }} / {{ xpSpan }} EXP</p>
        </RelicPanel>
        <RelicPanel title="暗影契约" crest="✧" eyebrow="SIX PATHS" class="contract-panel"><details class="contract-details" :open="dashboardWide"><summary>展开六维成长详情</summary><StatRing :attributes="attributes" :level="level.level||1" @navigate="emit('navigate',$event)"/><p class="contract-hint">六道契约 · 点击符文查看成长</p></details></RelicPanel>
      </div>
      <div class="right-column">
        <details class="inventory-history" :open="dashboardWide"><summary>查看装备与最近变化</summary><div class="inventory-panels">
          <RelicPanel title="当前装备" crest="⚒" class="inventory-panel"><DashboardEquipment :snapshot="equipment" @navigate="emit('navigate',$event)"/></RelicPanel>
          <RelicPanel title="最近变化" crest="✦" class="history-panel"><div class="recent-logs"><div v-if="!logs.length" class="empty-inscription">尚无成长记录<br/><small>完成行动，点亮契约</small></div><div v-for="log in logs" :key="log.id" class="recent-log"><span class="log-rune" aria-hidden="true">{{ attrMeta(log.attributeKey).sigil }}</span><div class="log-content"><div class="log-line"><b>{{ attrMeta(log.attributeKey).label }}</b><strong class="num" :class="{negative:log.experienceDelta<0}">{{ log.experienceDelta>0?'+':'' }}{{ log.experienceDelta }} EXP</strong></div><p :title="log.reason">{{ log.reason }}</p><time>{{ relativeTime(log.createdAt) }}</time></div></div></div><button class="inscription-link" @click="loadHistory(true)">查看全部日志 <ChevronRight :size="12"/></button></RelicPanel>
        </div></details>
        <RelicPanel title="待办行动" crest="⚔" class="action-panel"><template #actions><button class="heading-link" @click="emit('navigate','tasks')">全部任务 <ChevronRight :size="12"/></button></template><div class="pending-list"><p v-if="!pending.length" class="empty-inscription compact">当前没有待办行动</p><div v-for="task in pending" :key="task.id" class="pending-task"><button class="task-check" :disabled="completing.has(task.id)" :aria-label="`完成任务：${task.title}`" @click="completeTask(task)"><Check :size="13"/></button><button class="task-name" :title="task.title" @click="emit('navigate','tasks')">{{ task.title }}</button><span :class="{overdue:task.dueAt&&new Date(task.dueAt)<new Date()}">{{ task.dueAt?shortDate(task.dueAt):'待安排' }}</span></div></div></RelicPanel>
        <RelicPanel title="技能与尘世" crest="✧" class="portals-panel"><button class="portal" @click="emit('navigate','skills')"><Zap :size="15"/><span>天赋神殿</span><small>{{ skills.filter(s=>s.isUnlocked).length }} / {{ skills.length }} 已解锁</small><ChevronRight :size="14"/></button><button class="portal" @click="emit('navigate','world')"><Users :size="15"/><span>人物关系</span><small>{{ npcs.length }} 位 NPC</small><ChevronRight :size="14"/></button><button class="portal" @click="emit('navigate','world')"><MapPin :size="15"/><span>尘世地图</span><small>{{ maps.filter(m=>m.isUnlocked).length }} / {{ maps.length }} 已解锁</small><ChevronRight :size="14"/></button></RelicPanel>
      </div>
    </div>
    <ModalShell v-if="showCharEdit" :state="charForm" :busy="saving" title="铭刻角色真名" :width="460" @close="showCharEdit=false"><label class="field">角色名号<input v-model="charForm.name" maxlength="80"/></label><label class="field">灵魂判词<textarea v-model="charForm.description" rows="4" maxlength="1000"></textarea></label><template #footer><button class="btn btn-primary" :disabled="saving" @click="submitCharEdit">{{ saving?'保存中…':'铭刻保存' }}</button></template></ModalShell>
    <ModalShell v-if="showCoach" title="AI 教练" @close="showCoach=false"><p class="coach-response">{{ coachBusy?'正在推演成长建议…':coachText }}</p></ModalShell>
    <ModalShell v-if="showHistory" title="成长日志" :width="600" @close="showHistory=false"><p v-if="historyError" class="load-error" role="alert">{{ historyError }}</p><p v-if="!historyBusy&&!historyLogs.length&&!historyError" class="empty-inscription">尚无成长记录</p><div class="full-history"><div v-for="log in historyLogs" :key="log.id" class="recent-log"><span class="log-rune" aria-hidden="true">{{ attrMeta(log.attributeKey).sigil }}</span><div class="log-content"><div class="log-line"><b>{{ attrMeta(log.attributeKey).label }}</b><strong class="num" :class="{negative:log.experienceDelta<0}">{{ log.experienceDelta>0?'+':'' }}{{ log.experienceDelta }} EXP</strong></div><p>{{ log.reason }}</p><time>{{ new Date(log.createdAt).toLocaleString('zh-CN') }}</time></div></div></div><p v-if="historyBusy" class="empty-inscription compact">正在读取日志…</p><template #footer><span class="history-count">已显示 {{ historyLogs.length }} 条</span><button v-if="historyError||historyLogs.length===historyLimit" class="btn btn-sm" :disabled="historyBusy" @click="loadHistory(!!historyError)">{{ historyError?'重试':'加载更多' }}</button></template></ModalShell>
  </div>
</template>

<style scoped>
.dash{flex:1;min-width:0;min-height:0;overflow-y:auto;overflow-x:hidden;padding:2px 3px 8px;container:dashboard / inline-size}.dash-toolbar{display:flex;justify-content:space-between;align-items:center;gap:12px;padding:0 2px 10px;min-height:41px;flex-wrap:wrap}.archive-mark{font:12px var(--f-title);color:#b2a36e;display:flex;align-items:center;gap:9px;letter-spacing:.12em}.archive-mark>span{font-size: 12px;color:#8b8159}.archive-mark i{height:12px;width:1px;background:#65532b}.dash-actions{display:flex;align-items:center;gap:8px}.today-badge{font-size: 12px;color:#a59b73;margin-right:8px}.today-badge b{color:#c5e280;font-weight:500}.dashboard-stage{display:grid;grid-template-columns:minmax(0,.95fr) minmax(0,1.3fr) minmax(0,1.4fr);gap:12px;min-height:730px}.center-column,.right-column{display:flex;flex-direction:column;gap:12px;min-width:0}.character-panel{min-width:0;overflow:hidden}.character-panel :deep(.relic-body){padding:0;gap:0;position:relative}.character-scene{position:relative;min-height:415px;flex:1;isolation:isolate;background:url('../assets/dashboard/citadel-scene-v1.png') center/cover}.character-art{position:absolute;inset:0;width:100%;height:100%;object-fit:contain;object-position:center bottom;filter:drop-shadow(0 0 14px #567f0c35);z-index:1}.custom-art{object-fit:cover;object-position:center}.scene-vignette{position:absolute;inset:0;z-index:2;background:linear-gradient(#01050120,transparent 35%,#03060310 72%,#030603);pointer-events:none}.level-pennant{position:absolute;left:18px;top:18px;z-index:3;width:55px;min-height:77px;padding:8px 3px 12px;background:linear-gradient(#18200ae8,#060a03eb);border:1px solid #8c7c39;outline:1px solid #292e11;outline-offset:3px;text-align:center;font:18px var(--f-title);color:#c6ee6c;box-shadow:0 4px 20px #000;clip-path:polygon(0 0,100% 0,100% 90%,50% 100%,0 90%)}.level-pennant strong{display:block;font:600 30px/1.1 var(--f-title)}.scene-caption{position:absolute;z-index:3;bottom:12px;left:0;right:0;text-align:center;font:9px var(--f-title);letter-spacing:.3em;color:#a6ad6b}.character-identity{padding:0 22px 16px;text-align:center;background:radial-gradient(ellipse at top,#1f2c0e66,transparent 75%)}.identity-rule{display:block;color:#8b9d37;font-size: 12px;margin-top:-5px;position:relative;z-index:3}.character-identity h1{font:600 clamp(26px,3cqi,40px)/1.3 var(--f-title);color:#c6e97a;text-shadow:0 0 22px #8fc92a33;margin:9px 0 5px;overflow-wrap:anywhere}.character-title{font:16px var(--f-title);color:#d2bd73;letter-spacing:.13em;margin:0 0 13px}.character-description{font:13px/1.9 var(--f-title);color:#b9b79b;white-space:pre-line;overflow-wrap:anywhere;max-height:104px;overflow:auto;margin:0}.character-controls{padding:0 20px 20px;display:grid;grid-template-columns:1fr 1fr;gap:8px}.edit-character{grid-column:1/-1;justify-content:center;min-height:39px}.experience-summary{display:grid;grid-template-columns:1.15fr 1fr 1fr;gap:10px;align-items:start;padding:2px 3px 5px}.experience-summary>div{display:flex;flex-direction:column;gap:4px;min-width:0}.experience-summary span{font:12px var(--f-title);color:#b5a578}.soul-total strong{font:500 clamp(23px,2.5cqi,35px)/1.2 var(--f-title);color:#d4f47d;text-shadow:0 0 15px #77b61a35;overflow-wrap:anywhere}.experience-summary small{font:9px var(--f-title);color:#8f875f}.next-level b{font:500 23px/1.6 var(--f-title);color:#bbad85;overflow-wrap:anywhere}.contract-dates b{font:10px/1.5 var(--f-body);font-weight:400;color:#c6b571}.contract-dates span:nth-of-type(2){margin-top:4px}.soul-progress{position:relative;height:26px;margin:4px 10px 0;border:1px solid #a08b3c;background:#030603;box-shadow:inset 0 0 0 3px #0b1208,0 0 0 3px #302b16,inset 0 0 12px #000;isolation:isolate}.soul-fluid{position:absolute;inset:3px auto 3px 3px;max-width:calc(100% - 6px);background:linear-gradient(#cce96b80,#7faa18bb 40%,#375a06dd),url('../assets/textures/bar_fill.webp') center/220px 100%;box-shadow:0 0 12px #8dc72166;transition:width .8s}.soul-progress>span{position:absolute;inset:0;display:grid;place-items:center;color:#f1f2d9;font-size:13px;text-shadow:0 1px 3px #000,1px 0 2px #000}.xp-cap{position:absolute;top:50%;transform:translateY(-50%);font-style:normal;color:#b9cc5a;text-shadow:0 0 6px #83b925;background:#101707;border:1px solid #75672a;line-height:17px;font-size:18px}.xp-cap.left{left:-12px}.xp-cap.right{right:-12px}.experience-caption{text-align:center;color:#b3a273;font-size: 12px;margin:0}.contract-panel{flex:1}.contract-panel :deep(.relic-body){justify-content:center;padding:6px 12px 14px;gap:4px}.contract-hint{text-align:center;font:10px var(--f-title);color:#8b8a61;letter-spacing:.13em;margin:8px 0 0}.inventory-history{display:grid;grid-template-columns:minmax(0,1.1fr) minmax(0,1fr);gap:10px;flex:1;min-height:430px}.inventory-panel :deep(.relic-body),.history-panel :deep(.relic-body){padding:10px;gap:8px}.inventory-history :deep(.relic-head){padding:10px 12px 8px;gap:6px}.inventory-history :deep(.relic-head h2){font-size:15px}.recent-logs{min-height:0;flex:1;overflow-y:auto;max-height:364px}.recent-log{display:flex;gap:8px;padding:10px 0;border-bottom:1px solid #68572c3d}.log-rune{flex-shrink:0;width:25px;height:30px;display:grid;place-items:center;font-size:19px;color:#bed463;clip-path:polygon(50% 0,100% 25%,100% 75%,50% 100%,0 75%,0 25%);background:linear-gradient(135deg,#66782c,#0c1506 30%,#1c2c0e 75%,#849938)}.log-content{min-width:0;flex:1}.log-line{display:flex;flex-wrap:wrap;gap:3px 6px;align-items:center}.log-line b{font:12px var(--f-title);color:#c9bf8a}.log-line strong{font-size: 12px;font-weight:500;color:#c5ec71}.log-line strong.negative{color:#d99b75}.log-content p{font:11px/1.7 var(--f-body);color:#aaa486;margin:4px 0 3px;display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden;overflow-wrap:anywhere}.log-content time{display:block;text-align:right;font-size: 12px;color:#9a8d63}.inscription-link{min-height:29px;border:1px solid #6a5731;background:linear-gradient(#1c1c12,#080c07);color:#c1b68b;font:11px var(--f-title);display:flex;justify-content:center;align-items:center;gap:5px;cursor:pointer}.action-panel :deep(.relic-body){padding:6px 12px 10px}.heading-link{display:flex;align-items:center;gap:2px;font-size: 12px;color:#c0b17b;background:none;border:0;cursor:pointer}.pending-list{height:68px;max-height:68px;overflow:auto}.pending-task{display:flex;align-items:center;gap:8px;min-height:33px;border-bottom:1px solid #55492250;padding:4px 0}.task-check{display:grid;place-items:center;border:1px solid #7f8546;background:#0a1306;color:#98ac50;width:22px;height:22px;flex-shrink:0;cursor:pointer}.task-check:hover{color:#d4ff80;background:#24390d}.task-check:disabled{opacity:.4}.task-name{flex:1;min-width:0;overflow:hidden;white-space:nowrap;text-overflow:ellipsis;font:12px var(--f-body);text-align:left;color:#c0b995;border:0;background:none;cursor:pointer}.pending-task>span{font-size: 12px;color:#918461;white-space:nowrap}.pending-task>span.overdue{color:#d09d71}.portals-panel :deep(.relic-body){padding:5px 12px 10px;gap:5px}.portal{display:flex;align-items:center;gap:9px;min-height:31px;padding:5px 8px;color:#cebd82;border:1px solid #5a4b2d;background:linear-gradient(100deg,#181b104f,#020502);font:13px var(--f-title);cursor:pointer}.portal small{margin-left:auto;color:#a19874;font:10px var(--f-body)}.portal>svg{color:#a69a57;flex-shrink:0}.portal:hover,.inscription-link:hover{border-color:#a19d4d;color:#d4eb94}.empty-inscription{text-align:center;color:#a89b70;font:13px/2 var(--f-title);padding:30px 4px}.empty-inscription small{font:11px var(--f-body);color:#8e8a68}.empty-inscription.compact{padding:8px}.load-error{font-size:12px;color:#e2b77f;padding:10px;border:1px solid #7c5d32}.dash-loading{padding:100px 20px;text-align:center;color:#b5c282}.field{display:flex;flex-direction:column;gap:8px;margin-bottom:16px;font-size:13px}.coach-response{white-space:pre-wrap;line-height:1.9;font-size:13px}.spinning{animation:spin 1s linear infinite}@keyframes spin{to{transform:rotate(360deg)}}
@container dashboard (max-width:1180px){.dashboard-stage{grid-template-columns:minmax(0,.85fr) minmax(0,1.15fr)}.right-column{grid-column:1/-1;display:grid;grid-template-columns:1.2fr 1fr;align-items:start}.inventory-history{grid-row:1/3;min-height:430px}.character-scene{min-height:410px}.character-panel{min-height:700px}.contract-panel{min-height:460px}}
@container dashboard (max-width:730px){.dashboard-stage{grid-template-columns:1fr;min-height:0}.character-panel{min-height:600px}.character-scene{min-height:380px}.character-identity h1{font-size:32px}.right-column{display:flex;grid-column:auto}.inventory-history{min-height:435px}.contract-panel{min-height:460px}.dash-toolbar{gap:8px}.archive-mark{display:none}.dash-actions{flex:1;justify-content:flex-end}.today-badge{margin-right:auto}.experience-summary{gap:8px}.contract-dates b{font-size: 12px}}
@media(prefers-reduced-motion:reduce){.spinning{animation:none}.soul-fluid{transition:none}}
</style>

<style scoped>
.dashboard-stage{min-height:730px}.experience-panel :deep(.relic-body){padding:14px 18px 12px}.contract-dates b{font-size: 12px}.character-description{font-size:14px}.character-controls .btn{font-size:12px}.recent-log{padding:9px 0}.log-content p{font-size:12px}.pending-task>span{font-size: 12px}.portal small{font-size: 12px}.experience-summary small{font-size: 12px}
</style>

<style scoped>
.experience-summary{padding:0 3px;gap:10px}.experience-summary>div{gap:3px}.experience-summary span{line-height:15px}.experience-summary small{line-height:12px}.soul-total strong{font-size:32px;line-height:35px}.next-level b{font-size:23px;line-height:32px}.contract-dates b{line-height:13px}.contract-dates span:nth-of-type(2){margin-top:1px}.experience-panel :deep(.relic-body){padding:12px 18px;gap:7px}.soul-progress{height:25px;margin-top:3px}.experience-caption{line-height:16px}.task-check>svg{opacity:0}.task-check:hover>svg,.task-check:focus-visible>svg{opacity:1}
@container dashboard (max-width:730px){.right-column{align-items:stretch}.inventory-history{width:100%}.character-scene{min-height:380px}.character-panel{min-height:600px}.contract-panel{min-height:0}}
</style>


<style scoped>
.full-history .log-content p{display:block;overflow:visible;font-size:13px}.full-history .log-line b{font-size:14px}.full-history .log-line strong{font-size:12px}.history-count{margin-right:auto;font-size:12px;color:#b4a778}
</style>
<style scoped>
.archive-mark,.archive-mark>span,.today-badge,.experience-summary span,.experience-summary small,.contract-dates b,.experience-caption,.contract-hint,.log-content time,.log-line strong,.heading-link,.pending-task>span,.portal small,.inscription-link{font-size:12px}.log-content p,.log-line b,.task-name,.portal{font-size:14px}.log-content p{line-height:1.65}.task-check{width:27px;height:27px}.pending-task{min-height:36px}.pending-list{height:74px;max-height:74px}.character-controls .btn{font-size:13px}.character-description{font-family:var(--f-body)}.inventory-history>summary,.contract-details>summary{display:none}.contract-details{min-width:0}.inventory-history:not([open])>section{display:flex}.avatar-fit-label{grid-column:1/-1;display:flex;align-items:center;justify-content:space-between;font-size:12px;color:#b8b18b}.avatar-fit-label select{max-width:130px;background:#0a1107;color:#d9d7a0;border:1px solid #65582d;padding:4px}
@container dashboard (max-width:1180px){.dashboard-stage{grid-template-columns:minmax(0,.85fr) minmax(0,1.15fr);align-items:start}.center-column,.right-column{display:contents}.character-panel{grid-column:1;grid-row:1/4;align-self:stretch}.experience-panel{grid-column:2;grid-row:1}.action-panel{grid-column:2;grid-row:2}.contract-panel{grid-column:2;grid-row:3;min-height:0}.inventory-history{grid-column:1/-1;grid-row:4;min-height:430px}.portals-panel{grid-column:1/-1;grid-row:5}}
@container dashboard (max-width:730px){.dashboard-stage{grid-template-columns:minmax(0,1fr)}.character-panel{grid-column:1;grid-row:1;min-height:180px}.character-panel :deep(.relic-body){display:grid;grid-template-columns:38% minmax(0,1fr);grid-template-rows:1fr auto}.character-scene{grid-row:1/3;min-height:180px}.character-identity{padding:10px 12px 6px}.character-identity h1{font-size:25px;margin:3px 0}.character-title{font-size:13px;margin-bottom:6px}.character-description{font-size:12px;line-height:1.6;max-height:40px}.character-controls{padding:0 12px 12px;gap:5px}.character-controls .btn{font-size:12px;min-height:28px}.edit-character{min-height:30px}.identity-rule,.scene-caption{display:none}.level-pennant{left:10px;top:10px;width:40px;min-height:55px;font-size:13px;padding:5px 2px 8px}.level-pennant strong{font-size:23px}.action-panel{grid-column:1;grid-row:2}.experience-panel{grid-column:1;grid-row:3}.contract-panel{grid-column:1;grid-row:4}.inventory-history{grid-column:1;grid-row:5;display:block;min-height:0;border:1px solid #66552e;background:#080e07;padding:8px}.portals-panel{grid-column:1;grid-row:6}.inventory-history>summary,.contract-details>summary{display:list-item;cursor:pointer;color:#cdbb7d;font:14px var(--f-body);padding:6px}.inventory-history:not([open])>section{display:none}.inventory-history[open]>section{margin-top:10px}.contract-details:not([open])>:not(summary){display:none}.contract-details[open]>summary{margin-bottom:8px}.contract-panel :deep(.relic-body){padding:8px 12px}.dash-toolbar{flex-wrap:wrap}.dash-actions{flex-wrap:wrap}.contract-dates{font-size:12px}.experience-summary{grid-template-columns:1fr 1fr}.contract-dates{display:none}}
</style>
<style scoped>
.inventory-history{display:block;min-height:0}.inventory-panels{display:grid;grid-template-columns:minmax(0,1.1fr) minmax(0,1fr);gap:10px;min-height:430px}.inventory-history:not([open])>.inventory-panels{display:none}
@container dashboard (max-width:730px){.inventory-panels{grid-template-columns:minmax(0,1fr);min-height:0}.inventory-history[open]>.inventory-panels{margin-top:10px}.inventory-panels .inventory-panel{min-height:450px}.inventory-panels .history-panel{min-height:240px}}
</style>
