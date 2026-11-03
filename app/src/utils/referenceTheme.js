// Visual aliases retain the six existing attribute keys and their business meaning.
export const themeAsset = name => new URL(`../assets/reference/${name}.webp`, import.meta.url).href;
const paths = {
  physique: ['暗影血肉', 'flesh', 'blood-flesh'],
  mind: ['虚空意志', 'will', 'void-will'],
  discipline: ['诅咒束缚', 'curse', 'curse-bind'],
  expression: ['低语之舌', 'whisper', 'whisper-tongue'],
  creativity: ['邪能造物', 'fel', 'fel-surge'],
  resources: ['灵魂碎片', 'soul', 'soul-fragment'],
};
export function themeAttribute(key) {
  const p = paths[key];
  return p ? { title: p[0], rune: themeAsset('rune-' + p[1]), history: themeAsset('history-' + p[2]) } : null;
}
