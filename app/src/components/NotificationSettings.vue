<script setup>
import { onMounted, ref } from 'vue';
import { api, fmtDateTime } from '../api';
import { notify } from '../stores/toast';
const status = ref(null), busy = ref(false), result = ref('');
async function refresh() { try { status.value = await api.notificationStatus(); } catch (e) { result.value = e.message || String(e); } }
async function test() {
  if (busy.value) return;
  busy.value = true; result.value = '';
  try {
    const sent = await api.notificationTest();
    await new Promise(resolve => setTimeout(resolve, 1200));
    const received = await api.notificationReceipt(sent.tag);
    result.value = received ? '已在 Windows 通知记录中确认本次测试。横幅是否显示取决于系统通知与勿扰设置。' : '已提交 Windows，暂未在通知记录中确认，请检查系统通知与勿扰设置。';
    notify.info(result.value);
  } catch (e) { result.value = '测试失败：' + (e.message || e); notify.error(result.value); }
  finally { busy.value = false; await refresh(); }
}
onMounted(refresh);
</script>
<template>
  <div class="notification-state">
    <p>应用运行时每 30 秒检查到期任务；同一任务的同一截止时间只提交一次。失败后重试，退出应用后不检查。</p>
    <p v-if="status">系统通道：{{ status.available ? '可用' : '不可用' }} · 提醒{{ status.enabled ? '已开启' : '已关闭' }}</p>
    <p v-if="status?.lastSubmitted">最近提交：{{ fmtDateTime(status.lastSubmitted) }}</p>
    <p v-if="status?.error || status?.lastError" class="error">{{ status.error || status.lastError }}</p>
    <p>便携通知的系统来源可能显示为 Windows PowerShell，通知标题为 LifeRPG。</p>
    <div class="actions"><button class="btn btn-ghost btn-sm" :disabled="busy" @click="test">{{ busy ? '测试中…' : '发送测试通知' }}</button><button class="btn btn-ghost btn-sm" :disabled="busy" @click="refresh">刷新状态</button></div>
    <p v-if="result" role="status">{{ result }}</p>
  </div>
</template>
<style scoped>.notification-state{font-size: 12px;line-height:1.8;color:var(--t-time);display:flex;flex-direction:column;gap:5px;overflow-wrap:anywhere}.actions{display:flex;flex-wrap:wrap;gap:8px}.error{color:var(--t-warn)}</style>
