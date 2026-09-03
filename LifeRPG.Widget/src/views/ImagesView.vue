<script setup>
import { Image as ImageIcon } from 'lucide-vue-next';

defineProps({
  tasks: {
    type: Array,
    default: () => [],
  },
});
</script>

<template>
  <div class="images-page-container">
    <div class="images-header-bar gothic-frame-box">
      <div class="header-left">
        <span class="skull-sym">☠</span>
        <span class="page-title">打 卡 图 片 证 据 库</span>
        <span class="skull-sym">☠</span>
      </div>
      <div class="images-info">
        <span>SQLite 本地高保真存储</span>
      </div>
    </div>

    <div class="images-content-area gothic-frame-box">
      <div class="empty-images">
        <ImageIcon :size="48" style="color: #64748b; opacity: 0.6;" />
        <div class="empty-title">暂无图片证据记录</div>
        <div class="empty-hint">
          在完成重要契约或里程碑时，可上传打卡截图或成果照片，自动按 85% 质量压缩本地归档。
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.images-page-container {
  display: flex;
  flex-direction: column;
  gap: 12px;
  width: 100%;
  height: 100%;
  padding: 14px;
  box-sizing: border-box;
  overflow: hidden;
  background: radial-gradient(circle at 50% 20%, #120a1c 0%, #08040d 100%);
}

.gothic-frame-box {
  background: linear-gradient(180deg, rgba(26, 16, 38, 0.95) 0%, rgba(12, 7, 18, 0.98) 100%);
  border: 1px solid var(--warlock-border-gold);
  border-radius: 6px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.8), inset 0 1px 0 rgba(200, 164, 93, 0.35);
  position: relative;
  overflow: hidden;
}

.images-header-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 16px;
  flex-shrink: 0;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 8px;
}

.page-title {
  font-size: 14px;
  font-weight: 800;
  letter-spacing: 2px;
  color: var(--warlock-gold);
  text-shadow: 0 0 10px var(--warlock-gold-glow), 0 1px 2px #000;
}

.images-info {
  font-size: 11px;
  color: #94a3b8;
}

.images-content-area {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
}

.empty-images {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 40px;
}

.empty-title {
  font-size: 15px;
  font-weight: 700;
  color: #ffffff;
  margin-top: 14px;
}

.empty-hint {
  font-size: 12px;
  color: #94a3b8;
  max-width: 420px;
  text-align: center;
  margin-top: 6px;
  line-height: 1.5;
}
</style>
