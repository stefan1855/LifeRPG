import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const source = readFileSync(new URL('../../app/src/views/SettingsView.vue', import.meta.url), 'utf8');
function action(name) {
  const start = source.indexOf(`async function ${name}()`);
  assert(start >= 0);
  return source.slice(start, source.indexOf('\n}', start) + 2);
}

function runner(aiCfg) {
  const requests = [];
  const ai = { enabled: false, baseUrl: '', model: '', modelLight: '', apiKey: '' };
  const api = {
    configGet: async () => ({}), getAutostart: async () => false,
    backupList: async () => [], appState: async () => ({}),
    aiConfigGet: async () => aiCfg, mcpGetServers: async () => [],
    configSave: async patch => requests.push(patch),
  };
  const factory = new Function('ai', 'api', `
    const mcpServers={}, settings={}, autostart={}, backups={}, dataDir={}, exeDir={}, runtimeDir={}, busy={};
    const notify={success(){},error(message){throw new Error(message)}};
    ${action('load')}
    ${action('saveAi')}
    return {load,saveAi};
  `);
  return { ...factory(ai, api), ai, requests };
}

test('settings load missing, null, and empty AI fields as blank', async () => {
  for (const cfg of [undefined, {}, { baseUrl: null, model: null, modelLight: null }, { baseUrl: '', model: '', modelLight: '' }]) {
    const r = runner(cfg);
    await r.load();
    assert.deepEqual(r.ai, { enabled: false, baseUrl: '', model: '', modelLight: '', apiKey: '' });
  }
});

test('settings retain user endpoint and models and send explicit clears without replacing key', async () => {
  const r = runner({ enabled: true, baseUrl: 'https://user.example/v1', model: 'main', modelLight: 'light', apiKey: 'saved-test-key' });
  await r.load();
  assert.equal(r.ai.baseUrl, 'https://user.example/v1');
  assert.equal(r.ai.model, 'main');
  assert.equal(r.ai.modelLight, 'light');
  assert.equal(r.ai.apiKey, '');
  r.ai.baseUrl = r.ai.model = r.ai.modelLight = '';
  assert.equal(await r.saveAi(), true);
  assert.deepEqual(r.requests[0], { aiEnabled: true, aiBaseUrl: '', aiModel: '', aiModelLight: '' });
  r.ai.apiKey = ' new-test-key ';
  await r.saveAi();
  assert.equal(r.requests[1].aiApiKey, 'new-test-key');
  assert.equal(r.ai.apiKey, '');
});
