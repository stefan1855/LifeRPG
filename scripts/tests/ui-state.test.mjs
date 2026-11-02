import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
const source=readFileSync(new URL('../../app/src/views/SettingsView.vue',import.meta.url),'utf8');
function fn(name){const start=source.indexOf('function '+name+'(');assert(start>=0);const begin=source.slice(start-6,start)==='async '?'async ':'';return begin+source.slice(start,source.indexOf('\n}',start)+2);}
function runner(){
 const writes=[],requests=[];
 const api={mcpCallTool:(server,tool,args)=>new Promise(resolve=>requests.push({server,tool,args,resolve})),createRecord:async input=>writes.push(input)};
 const ref=value=>({value});const quiet=new Proxy({},{get:()=>()=>{}});
 const factory=new Function('ref','api','notify','sfx','saving',`
 const activeToolModal=ref(null),toolArgsJson=ref('{}'),toolResult=ref(''),toolFailed=ref(false),toolCalling=ref(false);
 let toolRevision=0;const resultContext=ref(null);
 ${fn('closeToolRunner')}
 ${fn('openToolRunner')}
 ${fn('executeMcpTool')}
 ${fn('saveToolResultAsRecord')}
 return {openToolRunner,closeToolRunner,executeMcpTool,saveToolResultAsRecord,activeToolModal,toolResult,toolCalling};`);
 return {...factory(ref,api,quiet,quiet,ref(false)),writes,requests};
}
test('late MCP response cannot be attached to another tool or saved',async()=>{
 const r=runner();r.openToolRunner({id:'A',name:'Server A'},{name:'toolA'});const a=r.executeMcpTool();
 r.closeToolRunner();r.openToolRunner({id:'B',name:'Server B'},{name:'toolB'});
 r.requests[0].resolve({content:'from A'});await a;
 assert.equal(r.toolResult.value,'');await r.saveToolResultAsRecord();assert.equal(r.writes.length,0);
 const b=r.executeMcpTool();r.requests[1].resolve({content:'from B'});await b;await r.saveToolResultAsRecord();
 assert.equal(r.writes.length,1);assert.equal(r.writes[0].content,'from B');assert(r.writes[0].title.includes('Server B (toolB)'));
});
test('failed MCP result cannot grant record experience',async()=>{
 const r=runner();r.openToolRunner({id:'A',name:'A'},{name:'toolA'});const work=r.executeMcpTool();
 r.requests[0].resolve({content:'failed',isError:true});await work;await r.saveToolResultAsRecord();assert.equal(r.writes.length,0);
});
