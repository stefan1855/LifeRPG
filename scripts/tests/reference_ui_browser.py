import json,time,subprocess,urllib.request,websocket,base64,tempfile,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; OUT=ROOT/'docs/reference-ui-2026-11-03';OUT.mkdir(exist_ok=True)
class Browser:
 def __init__(self):
  work=ROOT/'.tmp/reference-ui-audit';work.mkdir(parents=True,exist_ok=True)
  profile=Path(tempfile.mkdtemp(prefix='browser-',dir=work))
  browser=next((str(p) for p in [Path(r'C:\Program Files\Google\Chrome\Application\chrome.exe'),Path(r'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe')] if p.exists()),None)
  browser=browser or shutil.which('chromium') or shutil.which('google-chrome')
  if not browser:raise RuntimeError('Chromium browser is required')
  self.proc=subprocess.Popen([browser,'--headless=new','--disable-gpu','--no-first-run','--no-default-browser-check','--remote-debugging-port=0','--remote-allow-origins=*','--user-data-dir='+str(profile),'about:blank'],creationflags=0x08000000,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
  for _ in range(100):
   try:
    port=int((profile/'DevToolsActivePort').read_text().splitlines()[0])
    targets=json.load(urllib.request.urlopen(f'http://127.0.0.1:{port}/json/list',timeout=1));t=next(t for t in targets if t['type']=='page');self.ws=websocket.create_connection(t['webSocketDebuggerUrl'],timeout=35,suppress_origin=True);self.seq=0;return
   except Exception:time.sleep(.15)
  self.proc.terminate()
  raise RuntimeError('headless browser failed')
 def call(self,method,params={}):
  self.seq+=1;self.ws.send(json.dumps({'id':self.seq,'method':method,'params':params}))
  while True:
   x=json.loads(self.ws.recv())
   if x.get('id')==self.seq:
    if x.get('error'):raise Exception(x['error'])
    return x.get('result',{})
 def ev(self,s):
  r=self.call('Runtime.evaluate',{'expression':s,'awaitPromise':True,'returnByValue':True,'userGesture':True})
  if r.get('exceptionDetails'):raise Exception(r['exceptionDetails'])
  return r.get('result',{}).get('value')
 def size(self,w,h):self.call('Emulation.setDeviceMetricsOverride',{'width':w,'height':h,'deviceScaleFactor':1,'mobile':False})
 def shot(self,name):
  (OUT/(name+'.png')).write_bytes(base64.b64decode(self.call('Page.captureScreenshot',{'format':'png','captureBeyondViewport':False})['data']))
 def click(self,s):self.ev('document.querySelector('+json.dumps(s)+').click()');time.sleep(.4)
 def stop(self):
  try:self.call('Browser.close')
  except Exception:pass
  self.ws.close()
  try:self.proc.wait(timeout=5)
  except Exception:self.proc.terminate()
MOCK=r'''
(()=>{
 localStorage.setItem('liferpg_sfx_enabled','false');
 window.__qaErrors=[];const originalError=console.error.bind(console);console.error=(...args)=>{window.__qaErrors.push(args.map(x=>x?.stack||String(x)).join(' '));originalError(...args)};window.addEventListener('error',e=>__qaErrors.push(e.message));window.addEventListener('unhandledrejection',e=>__qaErrors.push(String(e.reason)));
 const keys=['physique','mind','discipline','expression','creativity','resources'];
 const attrs=keys.map((attributeKey,i)=>({id:attributeKey,attributeKey,level:19-i,experience:12000-i*600,expIntoCurrentLevel:1240+i*230,expSpan:2200,progressPercent:56+i*5,baseValue:0}));
 const character={id:'qa-character',name:'无面者',description:'在阴影中行走，聆听灵魂的低语。\n追寻知识，探索未知的深渊，\n直至成为暗影本身。',createdAt:'2024-01-15T08:00:00Z',lastGrowthAt:new Date().toISOString(),avatarImagePath:null};
 const dash={character,levelInfo:{level:27,totalExperience:125760,experienceRequiredForCurrentLevel:120000,experienceRequiredForNextLevel:130000,experienceToNextLevel:4240,experienceIntoCurrentLevel:5760,progressPercent:57.6},attributes:attrs,todayCompletedCount:3,todayTotalCount:5};
 const logs=Array.from({length:125},(_,i)=>({id:'log-'+i,attributeKey:keys[i%6],experienceDelta:300-i%6*25,reason:['完成运动训练','完成冥想修炼','抵抗心灵诱惑','与 NPC 对话','创造恶魔工艺品','收集灵魂碎片'][i%6],createdAt:new Date(Date.now()-(i+1)*3600000).toISOString()}));
 const tasks=[{id:'task-1',title:'完成今日阅读，记录成长',status:'Todo',attributeKey:'mind',quadrant:'Q2',baseExperience:25},{id:'task-2',title:'完成运动训练',status:'Todo',attributeKey:'physique',quadrant:'Q2',baseExperience:25}];
 const cfg={dataDir:'SYNTHETIC-QA',viewMode:'main',opacity:100,aiEnabled:false,mcpServers:[],navExpanded:false,notificationEnabled:false};
 window.__qa={dash,logs,tasks,cfg,calls:[]};
 window.__TAURI_INTERNALS__={metadata:{currentWindow:{label:'main'},currentWebview:{label:'main'}},transformCallback:()=>1,unregisterCallback:()=>{},convertFileSrc:p=>p,invoke:async(cmd,args={})=>{
  __qa.calls.push({cmd,args});
  if(cmd==='app_state')return {dataDir:'SYNTHETIC-QA'};
  if(cmd==='config_get')return cfg;
  if(cmd==='config_save'){Object.assign(cfg,args.config||args);return null;}
  if(cmd==='rpg_get_dashboard')return structuredClone(dash);
  if(cmd==='rpg_get_character')return structuredClone(character);
  if(cmd==='rpg_get_attributes')return structuredClone(attrs);
  if(cmd==='rpg_get_logs')return logs.slice(0,args.limit||50);
  if(cmd==='rpg_update_character'){Object.assign(character,{name:args.name,description:args.description});return character;}
  if(cmd==='rpg_set_avatar'){character.avatarImagePath=args.path;return character;}
  if(cmd==='task_list')return structuredClone(tasks);
  if(cmd==='task_complete'){let t=tasks.find(t=>t.id===args.id);t.status='Completed';dash.levelInfo.totalExperience+=25;return {applied:true,totalExperienceGranted:25};}
  if(cmd==='equip_conflicts')return [];
  if(cmd==='equip_snapshot')return {slots:[],totalBonuses:[],conflicts:[]};
  if(cmd==='backup_list'||cmd==='image_cleanup_batches')return [];
  if(cmd==='record_list')return [];
  if(cmd==='skill_list')return [{id:'skill-1',name:'知识铭刻',isUnlocked:true,experience:200,level:2,category:'通用',description:'已解锁的合成技能',prerequisites:[],bonuses:[]}];
  if(cmd==='npc_list')return [{id:'npc-1',name:'档案守卫'}];
  if(cmd==='map_list')return [{id:'map-1',name:'遗忘城堡',isUnlocked:true}];
  if(cmd==='ai_coach')return '完成一项行动，铭刻成长。';
  if(cmd==='notification_status')return {available:false};
  if(cmd.includes('categories')||cmd.endsWith('_list')||cmd==='mcp_get_servers')return [];
  return null;
 }};
})();
'''
