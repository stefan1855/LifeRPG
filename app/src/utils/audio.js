/**
 * LifeRPG 哥特暗黑音效系统（基于 Web Audio API 纯代码程序化合成）
 * 零音频依赖、零外部网络、零本地空间占用，纯离线实时声学物理建模
 */

let ctx = null;
let enabled = localStorage.getItem('liferpg_sfx_enabled') !== 'false';

function getContext() {
  if (!ctx && typeof window !== 'undefined') {
    const AudioContextClass = window.AudioContext || window.webkitAudioContext;
    if (AudioContextClass) {
      ctx = new AudioContextClass();
    }
  }
  if (ctx && ctx.state === 'suspended') {
    ctx.resume().catch(() => {});
  }
  return ctx;
}

export const sfx = {
  isEnabled: () => enabled,
  setEnabled: (val) => {
    enabled = !!val;
    localStorage.setItem('liferpg_sfx_enabled', enabled ? 'true' : 'false');
  },
  toggle: () => {
    sfx.setEnabled(!enabled);
    return enabled;
  },

  /** 铁砧锻造声（任务勾选、熟练度加点、确认）：金属撞击高频泛音 + 沉闷余震 */
  hammer: () => {
    if (!enabled) return;
    const ac = getContext();
    if (!ac) return;
    const t = ac.currentTime;

    // 1. 主金属撞击声（钢轨泛音 980Hz & 1420Hz）
    const osc1 = ac.createOscillator();
    const osc2 = ac.createOscillator();
    const gainMetal = ac.createGain();

    osc1.type = 'triangle';
    osc1.frequency.setValueAtTime(980, t);
    osc1.frequency.exponentialRampToValueAtTime(320, t + 0.28);

    osc2.type = 'sine';
    osc2.frequency.setValueAtTime(1420, t);
    osc2.frequency.exponentialRampToValueAtTime(450, t + 0.22);

    gainMetal.gain.setValueAtTime(0.28, t);
    gainMetal.gain.exponentialRampToValueAtTime(0.001, t + 0.32);

    osc1.connect(gainMetal);
    osc2.connect(gainMetal);
    gainMetal.connect(ac.destination);

    osc1.start(t);
    osc2.start(t);
    osc1.stop(t + 0.35);
    osc2.stop(t + 0.35);

    // 2. 初始敲击瞬间的爆破杂音（Transient Strike）
    try {
      const bufferSize = ac.sampleRate * 0.04;
      const buffer = ac.createBuffer(1, bufferSize, ac.sampleRate);
      const data = buffer.getChannelData(0);
      for (let i = 0; i < bufferSize; i++) {
        data[i] = (Math.random() * 2 - 1) * Math.exp(-i / (bufferSize * 0.25));
      }
      const noise = ac.createBufferSource();
      noise.buffer = buffer;
      const filter = ac.createBiquadFilter();
      filter.type = 'bandpass';
      filter.frequency.setValueAtTime(2400, t);

      const noiseGain = ac.createGain();
      noiseGain.gain.setValueAtTime(0.35, t);
      noiseGain.gain.exponentialRampToValueAtTime(0.001, t + 0.05);

      noise.connect(filter);
      filter.connect(noiseGain);
      noiseGain.connect(ac.destination);

      noise.start(t);
    } catch (_) {}
  },

  /** 装备锁扣与穿脱声（咔哒金属重甲咬合）：双重错落高通卡扣声 */
  equip: () => {
    if (!enabled) return;
    const ac = getContext();
    if (!ac) return;
    const t = ac.currentTime;

    [0, 0.045].forEach((delay, idx) => {
      const osc = ac.createOscillator();
      const gain = ac.createGain();
      osc.type = 'sawtooth';
      const freq = idx === 0 ? 820 : 640;
      osc.frequency.setValueAtTime(freq, t + delay);
      osc.frequency.exponentialRampToValueAtTime(180, t + delay + 0.06);

      gain.gain.setValueAtTime(0.18, t + delay);
      gain.gain.exponentialRampToValueAtTime(0.001, t + delay + 0.07);

      osc.connect(gain);
      gain.connect(ac.destination);

      osc.start(t + delay);
      osc.stop(t + delay + 0.08);
    });
  },

  /** 魂火呼啸与解锁声（技能解锁、封印突破、法阵点亮）：滤波扫频 + 翡翠微风 */
  soulFlame: () => {
    if (!enabled) return;
    const ac = getContext();
    if (!ac) return;
    const t = ac.currentTime;

    // 低频深渊轰鸣
    const sub = ac.createOscillator();
    const subGain = ac.createGain();
    sub.type = 'sine';
    sub.frequency.setValueAtTime(95, t);
    sub.frequency.exponentialRampToValueAtTime(45, t + 0.45);
    subGain.gain.setValueAtTime(0.25, t);
    subGain.gain.exponentialRampToValueAtTime(0.001, t + 0.5);

    sub.connect(subGain);
    subGain.connect(ac.destination);
    sub.start(t);
    sub.stop(t + 0.52);

    // 呼啸中高频带通
    try {
      const len = ac.sampleRate * 0.4;
      const buf = ac.createBuffer(1, len, ac.sampleRate);
      const ch = buf.getChannelData(0);
      for (let i = 0; i < len; i++) {
        ch[i] = (Math.random() * 2 - 1) * 0.5;
      }
      const noise = ac.createBufferSource();
      noise.buffer = buf;

      const bp = ac.createBiquadFilter();
      bp.type = 'bandpass';
      bp.Q.setValueAtTime(4.0, t);
      bp.frequency.setValueAtTime(260, t);
      bp.frequency.exponentialRampToValueAtTime(1100, t + 0.18);
      bp.frequency.exponentialRampToValueAtTime(420, t + 0.4);

      const nGain = ac.createGain();
      nGain.gain.setValueAtTime(0.001, t);
      nGain.gain.linearRampToValueAtTime(0.3, t + 0.12);
      nGain.gain.exponentialRampToValueAtTime(0.001, t + 0.42);

      noise.connect(bp);
      bp.connect(nGain);
      nGain.connect(ac.destination);

      noise.start(t);
    } catch (_) {}
  },

  /** 角色升级与传奇觉醒：哥特钟鸣琶音 (D4 -> F4 -> A4 -> D5) + 魂火绽放 */
  levelUp: () => {
    if (!enabled) return;
    const ac = getContext();
    if (!ac) return;
    const t = ac.currentTime;

    // Dm 和弦琶音 (293.66, 349.23, 440.00, 587.33)
    const chord = [293.66, 349.23, 440.0, 587.33];
    chord.forEach((freq, idx) => {
      const start = t + idx * 0.11;
      const osc = ac.createOscillator();
      const gain = ac.createGain();

      osc.type = 'sine';
      osc.frequency.setValueAtTime(freq, start);

      gain.gain.setValueAtTime(0.22, start);
      gain.gain.exponentialRampToValueAtTime(0.001, start + 0.85);

      osc.connect(gain);
      gain.connect(ac.destination);

      osc.start(start);
      osc.stop(start + 0.9);
    });

    // 附带一次深渊魂火呼啸
    setTimeout(() => {
      sfx.soulFlame();
    }, 200);
  },
};
