import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const source = readFileSync(new URL('../../app/src/views/TasksView.vue', import.meta.url), 'utf8');
const start = source.indexOf('async function quickCreate()');
const action = source.slice(start, source.indexOf('\n}', start) + 2);
const defaults = new Function(`return (${source.match(/const taskDefaults = (\{[^\n]+\});/)[1]});`)();

function runner(title = '  散步十分钟  ') {
  const quickText = { value: title }, quickAdding = { value: false }, aiParsing = { value: false };
  const filter = { value: 'completed' }, quadrantFilter = { value: 'Q1' };
  const requests = [], notices = [];
  let refreshes = 0;
  const api = { createTask: input => new Promise((resolve, reject) => requests.push({ input, resolve, reject })) };
  const notify = { success: message => notices.push(message), error: message => notices.push(message) };
  const load = async () => { refreshes++; };
  const quickCreate = new Function('quickText', 'quickAdding', 'aiParsing', 'filter', 'quadrantFilter', 'api', 'notify', 'load', 'taskDefaults', `${action}; return quickCreate;`)(quickText, quickAdding, aiParsing, filter, quadrantFilter, api, notify, load, defaults);
  return { quickCreate, quickText, quickAdding, aiParsing, filter, quadrantFilter, requests, notices, refreshes: () => refreshes };
}

test('quick add creates one task without AI and shows it under pending', async () => {
  const r = runner();
  const first = r.quickCreate();
  await r.quickCreate();
  assert.equal(r.requests.length, 1);
  assert.deepEqual(r.requests[0].input, { title: '散步十分钟', quadrant: 'Q2', growthAttribute: 'mind', experience: 25 });
  assert.equal(r.quickAdding.value, true);
  r.requests[0].resolve();
  await first;
  assert.equal(r.quickText.value, '');
  assert.equal(r.filter.value, 'pending');
  assert.equal(r.quadrantFilter.value, '');
  assert.equal(r.refreshes(), 1);
  assert.equal(r.quickAdding.value, false);
});

test('failed quick add preserves input and permits retry', async () => {
  const r = runner();
  const work = r.quickCreate();
  r.requests[0].reject(new Error('磁盘写入失败'));
  await work;
  assert.equal(r.quickText.value, '  散步十分钟  ');
  assert.equal(r.quickAdding.value, false);
  assert.equal(r.refreshes(), 0);
  assert(r.notices[0].includes('磁盘写入失败'));
  const retry = r.quickCreate();
  assert.equal(r.requests.length, 2);
  r.requests[1].resolve();
  await retry;
});

test('finishing a request preserves a new title typed while saving', async () => {
  const r = runner();
  const work = r.quickCreate();
  r.quickText.value = '读两页书';
  r.requests[0].resolve();
  await work;
  assert.equal(r.quickText.value, '读两页书');
});

test('blank input and active AI parsing do not create tasks', async () => {
  const r = runner('   ');
  await r.quickCreate();
  r.quickText.value = '散步';
  r.aiParsing.value = true;
  await r.quickCreate();
  assert.equal(r.requests.length, 0);
});

test('Enter during Chinese IME composition does not submit', () => {
  const begin = source.indexOf('function onQuickEnter(event)');
  const handler = source.slice(begin, source.indexOf('\n}', begin) + 2);
  let calls = 0;
  const create = () => calls++;
  const enter = new Function('quickCreate', `${handler}; return onQuickEnter;`)(create);
  let prevented = 0;
  const preventDefault = () => prevented++;
  enter({ isComposing: true, keyCode: 13, preventDefault });
  enter({ isComposing: false, keyCode: 229, preventDefault });
  assert.equal(calls, 0);
  assert.equal(prevented, 0);
  enter({ isComposing: false, keyCode: 13, preventDefault });
  assert.equal(calls, 1);
  assert.equal(prevented, 1);
});

test('AI parsing preserves a new title typed while its response is pending', async () => {
  const begin = source.indexOf('async function aiCreate()');
  const handler = source.slice(begin, source.indexOf('\n}', begin) + 2);
  const quickText = { value: '旧任务' }, aiParsing = { value: false };
  let resolve;
  const response = new Promise(done => { resolve = done; });
  const form = {};
  const create = new Function('deps', `
    const { quickText, aiParsing, form, api } = deps;
    const quickAdding={value:false},draftGenerated={value:false},showTaskOptions={value:false};
    let editorRevision=0;
    const openCreate=()=>editorRevision++;
    const quadrantMeta=()=>({exp:25});
    const localDateTime=value=>value;
    const notify={warn(){},success(){},error(){}};
    ${handler}; return aiCreate;
  `)({ quickText, aiParsing, form, api: { aiParseTask: () => response } });
  const work = create();
  quickText.value = '下一项任务';
  resolve({ title: '解析后的旧任务', quadrant: 'Q2', growthAttribute: 'mind', experience: 25 });
  await work;
  assert.equal(quickText.value, '下一项任务');
  assert.equal(form.title, '解析后的旧任务');
  assert.equal(aiParsing.value, false);
});
