import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const source = readFileSync(new URL('../../app/src/views/TasksView.vue', import.meta.url), 'utf8');
const start = source.indexOf('async function quickCreate()');
const action = source.slice(start, source.indexOf('\n}', start) + 2);

function runner(title = '  散步十分钟  ') {
  const quickText = { value: title }, quickAdding = { value: false }, aiParsing = { value: false };
  const filter = { value: 'completed' }, quadrantFilter = { value: 'Q1' };
  const requests = [], notices = [];
  let refreshes = 0;
  const api = { createTask: input => new Promise((resolve, reject) => requests.push({ input, resolve, reject })) };
  const notify = { success: message => notices.push(message), error: message => notices.push(message) };
  const load = async () => { refreshes++; };
  const quickCreate = new Function('quickText', 'quickAdding', 'aiParsing', 'filter', 'quadrantFilter', 'api', 'notify', 'load', `${action}; return quickCreate;`)(quickText, quickAdding, aiParsing, filter, quadrantFilter, api, notify, load);
  return { quickCreate, quickText, quickAdding, aiParsing, filter, quadrantFilter, requests, notices, refreshes: () => refreshes };
}

test('quick add creates one task without AI and shows it under pending', async () => {
  const r = runner();
  const first = r.quickCreate();
  await r.quickCreate();
  assert.equal(r.requests.length, 1);
  assert.deepEqual(r.requests[0].input, { title: '散步十分钟' });
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
