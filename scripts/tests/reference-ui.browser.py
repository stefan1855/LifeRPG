import sys,json,time,os
from pathlib import Path
sys.path.insert(0,str(Path(__file__).parent))
from reference_ui_browser import Browser,MOCK,OUT
base_url=os.environ.get('LIFERPG_UI_TEST_URL','http://127.0.0.1:5173')
b=Browser();result={'environment':'Headless Chromium, actual Vue components, synthetic Tauri command responses; not native desktop acceptance','url':base_url,'sizes':[],'checks':{}}
try:
 b.size(1460,920);b.call('Page.enable');b.call('Page.addScriptToEvaluateOnNewDocument',{'source':MOCK});b.call('Page.navigate',{'url':base_url});time.sleep(2);b.ev('document.fonts.ready.then(()=>true)');time.sleep(.4)
 assert b.ev('document.querySelectorAll(".contract-node").length')==6
 assert b.ev('document.querySelectorAll(".equipment-slot").length')==12
 assert b.ev('getComputedStyle(document.querySelector(".equipment-slots")).gridTemplateColumns.split(" ").length')==1
 assert b.ev('[...document.images].filter(i=>!i.complete||!i.naturalWidth).map(i=>i.src)')==[]
 result['checks']['sixRunesTwelveSingleColumnSlotsAndAllImagesLoaded']=True
 for w,h in [(1672,941),(1460,920),(1250,850),(1068,700),(760,560),(480,700)]:
  b.size(w,h);time.sleep(.3);b.ev('document.querySelector(".dash").scrollTop=0');time.sleep(.1)
  measure=b.ev('({width:innerWidth,height:innerHeight,overflow:[...document.querySelectorAll("html,body,.content,.dash")].filter(e=>e.getClientRects().length).filter(e=>e.scrollWidth>e.clientWidth+2).map(e=>({selector:e.className||e.tagName,w:e.clientWidth,scroll:e.scrollWidth})), columns:getComputedStyle(document.querySelector(".dashboard-stage")).gridTemplateColumns.split(" ").length})')
  assert not measure['overflow'],measure
  assert measure['columns']==(3 if w>=1460 else 2 if w>=1068 else 1),measure
  b.shot('dashboard-'+str(w));result['sizes'].append(measure)
 # Wide-stage content must end above the lower fixed frame.
 b.size(1460,920);time.sleep(.4)
 assert b.ev('document.querySelector(".character-panel").getBoundingClientRect().bottom')<880
 result['checks']['referenceColumnsFitBaselineWindow']=True
 # Existing workflows: character editor, paginated history, settlement, navigation.
 b.click('.edit-character');assert b.ev('!!document.querySelector("[role=dialog] input")')
 b.ev('(()=>{const e=document.querySelector("[role=dialog] input");e.value="验收角色";e.dispatchEvent(new Event("input",{bubbles:true}));})()')
 b.click('[role=dialog] .modal-foot button');assert b.ev('document.querySelector(".character-identity h1").textContent')=='验收角色'
 result['checks']['characterEditSavesThroughExistingApi']=True
 b.click('.inscription-link');assert b.ev('document.querySelectorAll(".full-history .recent-log").length')==100
 b.click('[role=dialog] .modal-foot button');assert b.ev('document.querySelectorAll(".full-history .recent-log").length')==125
 b.click('[role=dialog] button[aria-label="关闭弹窗"]');result['checks']['historyPagination100To125']=True
 b.click('.task-check');assert b.ev('__qa.calls.some(c=>c.cmd==="task_complete"&&c.args.id==="task-1")')
 assert b.ev('document.querySelectorAll(".pending-task").length')==1
 result['checks']['pendingTaskSettlementAndReload']=True
 b.click('.contract-node');assert b.ev('!!document.querySelector(".attrs-view")');result['checks']['runeNavigatesToAttributes']=True
 b.click('nav button[aria-label="角色仪表盘"]');time.sleep(.4);b.click('.equipment-slot');assert b.ev('!!document.querySelector(".equip-view")');result['checks']['equipmentSlotNavigatesToInventory']=True
 b.click('nav button[aria-label="角色仪表盘"]');time.sleep(.4);b.click('.portal');assert b.ev('document.querySelector(".page-caption").textContent')=='天赋神殿';result['checks']['portalNavigatesToSkills']=True
 b.click('nav button[aria-label="角色仪表盘"]');time.sleep(.4)
 # Empty and extreme text/experience render without horizontal overflow.
 b.ev('(()=>{__qa.dash.character.name="很长的角色名".repeat(12);__qa.dash.character.description="描述内容与成长记录。".repeat(90);__qa.dash.levelInfo.progressPercent=0;__qa.logs.length=0;__qa.tasks.length=0;})()');b.click('.dash-actions button:last-child')
 assert b.ev('document.querySelector(".soul-progress").getAttribute("aria-valuenow")')=='0'
 assert b.ev('document.querySelector(".dash").scrollWidth<=document.querySelector(".dash").clientWidth+2')
 b.shot('dashboard-stress');result['checks']['emptyLongTextAndZeroExperience']=True
 b.ev('__qa.dash.levelInfo.progressPercent=100');b.click('.dash-actions button:last-child');assert b.ev('document.querySelector(".soul-progress").getAttribute("aria-valuenow")')=='100'
 result['checks']['fullExperience']=True
 # Global typography and frame on an ordinary form page.
 b.click('nav button[aria-label="行动任务"]');b.shot('tasks');result['checks']['tasksPageLoads']=b.ev('!!document.querySelector(".tasks-view")')
 b.click('nav button[aria-label="封印核心"]');time.sleep(.5);b.shot('settings');result['checks']['settingsPageLoads']=b.ev('!!document.querySelector(".settings-view")')
 assert b.ev('document.querySelectorAll(".toast.error").length')==0
 result['runtimeErrors']=b.ev('__qaErrors');assert not result['runtimeErrors'],result['runtimeErrors']
 assert all(result['checks'].values()),result
 (OUT/'browser-checks.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(result,ensure_ascii=False,indent=2))
finally:b.stop()
