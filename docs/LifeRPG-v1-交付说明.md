# LifeRPG v1.0 交付说明

> 重做日期：2026-10-29 · 技术栈：Tauri v2 + Vue 3 + Rust + SQLite
> 定位：能每天用的暗黑哥特人生 RPG 桌面客户端，本地优先、零联网

---

## 一、这一版和过去四套半成品的区别

| | 过去 | 现在 |
|---|---|---|
| 架构 | WPF / Electron+Pixi / Unity / Tauri Widget 四套并存，三套半成品 | 只保留一套：`app/`（Tauri v2 + Vue 3 + Rust），旧的四套已归档到 `_archive/` |
| 数据 | 空库，19 张表零使用 | 同一套 schema，Rust 仓储重写，7 个单元测试护航 |
| 视觉 | 每套都停在「30% 视觉」 | 设计规格书落地的设计令牌 + 程序化合成金属边框 + 生成式贴图 |
| 交付 | 跑不起来 / 只有骨架 | 双击安装包即用，NSIS 安装器 + 免安装 exe |

---

## 二、目录结构

```
D:\Projects\LifeRPG\
├─ app\                     ← 主工程（唯一活跃代码）
│  ├─ src\                  Vue 3 前端
│  │  ├─ views\             7 个页面
│  │  ├─ components\        面板/经验条/契约环/弹窗/提示
│  │  ├─ styles\            设计令牌 + 基础层
│  │  └─ assets\textures\   全部贴图（webp，约 1.3 MB）
│  ├─ src-tauri\            Rust 后端
│  │  ├─ src\services\      任务/角色/记录/图片/备份/导出/统计
│  │  └─ tauri.conf.json    窗口 1460×920，无边框
│  └─ dist\                 前端构建产物
├─ assets\                  资产流水线（可重复执行）
│  ├─ gen_textures.py       调 gemini-3.1-flash-image 生成材质与装饰件
│  ├─ gen_procedural.py     程序化生成雾/火星/暗角
│  ├─ build_ui_assets.py    合成九宫格浮雕边框、进度条、按钮底板
│  ├─ qa_assets.py          贴图多模态质检
│  └─ qa_ui.py              界面截图多模态验收
├─ scripts\build-env.ps1    构建环境引导（必须）
├─ docs\                    本文档 + 设计规格书
└─ _archive\                旧的四套架构（只读参考）
```

---

## 三、功能清单（全部实测可用）

**角色仪表盘**：等级/经验条、六维暗影契约环（进度弧 + 呼吸辉光 + 余烬粒子）、今日任务进度、近期经验流水。

**行动任务**：四象限矩阵（重要紧急 +30 / 重要不紧急 +25 / 不重要紧急 +15 / 不重要不紧急 +5）、新建/编辑/删除、完成结算、撤销回滚、逾期标记、分类与象限筛选。

**行为记录**：标题/正文/分类/经验、多图证据（自动压缩到最长边 1920px、JPEG 85%）、关键词搜索、分类筛选。

**成长属性**：六维卡片、等级与进度、累计经验、最强/最需滋养契约。

**装备栏**：12 槽位（头/肩/胸/手/腰/腿/脚/腕/主手/副手/饰品一/饰品二），装备库可录入「现实物品 → 游戏名字 + 现实效果 + 属性加成」，同槽位互斥，一键装备/卸下，自动汇总加成合计。

**技能**：按分类分组，解锁/封印、熟练度 +20 一次、每 100 熟练度升一级，支持自定义技能。

**世界**：人物关系（好感度 -100~100，±10 微调，实时显示关系档位）+ 世界地图（地点解锁/封锁）。

**图片证据**：全部记录的图片汇聚成画廊，点击放大查看。

**复盘归档**：完成率、连续活跃天数、累计经验、30 天活跃度柱状图、分类分布、一键导出 Markdown。

**设置**：全局快捷键、透明度、到期提醒、开机自启、数据目录查看与迁移、一键备份（可指定目录）、从备份恢复（恢复前自动再备份一份）、Markdown 导出。

**三档形态**：大窗口（1460×920）/ 桌面挂件（340×600）/ 胶囊条（260×44），标题栏一键切换，形态记忆到配置。

**系统集成**：无边框自绘标题栏、置顶、最小化/最大化/隐藏到托盘、**系统托盘常驻**（左键唤回，右键可切大窗口/挂件/胶囊/退出）、单实例、全局快捷键呼出、**开机自启**（配置为准，启动时自动校准注册表）、前端异常兜底（组件报错弹提示不白屏，挂载失败显示降级页）。

**AI 军师（v1.2，可选）**：四个能力，全部走本机 OpenAI 兼容网关，**默认关闭**，断网或网关离线时按钮报错但核心功能不受影响。

| 能力 | 入口 | 干什么 |
|---|---|---|
| AI 复盘 | 复盘归档页 | 汇总本周任务/记录/六维 → 生成复盘，可编辑后一键存为行为记录 |
| AI 建任务 | 行动任务页顶部输入框 | 一句话「明天下午三点健身一小时」→ 解析成标题/象限/属性/经验/截止日期，填入弹窗供确认 |
| AI 起名 | 装备编辑弹窗 | 填现实物品（如「降噪耳机」）→ 生成哥特游戏名 + 效果描述 + 属性加成建议 |
| AI 教练 | 仪表盘右上 | 读六维数据 → 一条 80 字内的成长建议 |

配置在设置页「AI 军师」区块：网关地址、主力模型（复盘）、轻量模型（建任务）、密钥，带「测试连接」。

---

## 三点五、便携与数据位置（v1.1 起）

**绿色版**：整个程序是一个文件夹，拷到哪都能跑，不写注册表、不装服务。

```
D:\任意位置\LifeRPG\
├─ LifeRPG.exe
├─ liferpg.config.json     ← 便携配置（含数据目录指针、快捷键、形态）
└─ .runtime\webview2\      ← WebView2 缓存，已从 C 盘挪出来
```

**数据目录**：首次启动会引导你选一处目录（建议 D 盘空文件夹），目录内自动生成：

```
<你选的目录>\
├─ data\liferpg.db     ← 数据库
├─ images\             ← 图片证据
└─ backups\            ← 备份 zip
```

设置页可以「迁移数据目录」：整体复制到新位置并切换指针，原目录不删。

**C 盘占用**：除系统自带的 WebView2 运行时外，本应用不往 C 盘写任何东西。

---

## 四、数据与安全

- 数据库：`%LOCALAPPDATA%\LifeRPG\data\liferpg.db`（SQLite，WAL）
- 图片：`%LOCALAPPDATA%\LifeRPG\images\`
- 备份：`%LOCALAPPDATA%\LifeRPG\backups\*.zip`（内含 db + 全部图片）
- 配置：`%LOCALAPPDATA%\LifeRPG\config.json`
- 首次启动会自动继承旧 `LifeRPG.Native` 库（若存在），并写入 3 条「入门契约」避免首屏空荡，可自由删除
- **全程零联网**：字体、贴图、依赖全部本地打包

---

## 五、如何构建

```powershell
cd D:\Projects\LifeRPG\app
. ..\scripts\build-env.ps1     # 必须：注入 MSVC 环境 + 把 TEMP 指到纯 ASCII 路径
npm install
npm run build                  # 前端
npx tauri build                # 出安装包（release 编译约 8-10 分钟）
```

产物（因为 `CARGO_TARGET_DIR` 指到了 D 盘）：
- 免安装：`D:\Projects\LifeRPG\.tmp\cargo-target\release\liferpg.exe`（5.9 MB）
- 安装包：`D:\Projects\LifeRPG\.tmp\cargo-target\release\bundle\nsis\LifeRPG_1.0.0_x64-setup.exe`（2.7 MB）

> 安装包建议装到默认位置（`%LOCALAPPDATA%\Programs\LifeRPG`），**不要装到 `D:\Projects` 下**，原因见下节第 3 条。

### 构建踩坑（已固化进 build-env.ps1）

1. 本机用户名含中文，`cl.exe` 在非 ASCII 的 `TEMP` 下会报 `D8050 未能将命令行放入调试记录`；把 `TEMP/TMP` 指到 `D:\Projects\LifeRPG\.tmp\build_temp` 即可。
2. PowerShell 会话默认没有 MSVC 的 `INCLUDE/LIB`，`cl` 会报 `C1034 stdarg.h 不包括路径集`；脚本里已显式注入。
3. **不要从 `D:\Projects` 里直接运行编译出的 exe**：该工作区带低完整性标签，进程会被降级，写不了用户目录，WebView2 也起不来（表现为窗口空白 / ERR_CONNECTION_REFUSED）。把 exe 复制到 `%TEMP%` 或安装到正常目录再跑。

---

## 六、验证记录

- `cargo test --lib` → **12 passed / 0 failed**（含 AI JSON 容错解析与降级测试）；`--include-ignored --test-threads=1` → **14 passed**（含 2 项真实网关联调）
  - 角色初始化、任务完成发经验、重复完成幂等、撤销回滚
  - 行为记录 CRUD + 关键词过滤
  - 统计聚合（任务/记录/经验/分类/30 天）
  - 备份打包 + 列表 + Markdown 导出
  - 装备：入库 → 装备 → 槽位填充 + 加成合计 → 卸下
  - 技能：解锁 → 加 120 熟练度 → 升到 Lv.2 余 20
  - 世界：好感度 +30/-10/上限截断、地点解锁
- 稳定性实测（2026-10-29）：
  - 开机自启端到端——配置 `autostart:true` 启动后，`HKCU\...\Run` 出现 `LifeRPG = <exe路径>` 注册项；关闭后注册项同步清除
  - 单实例——二次启动的进程自动退出（进程数保持 1），原窗口保留
  - 常驻内存 144.5 MB（主进程 + WebView2），符合 <200 MB 目标
- `npm run build` → 1763 模块，产物 gzip 后约 54 KB JS + 8 KB CSS
- `npx tauri build` → release 编译 2 分钟，NSIS 安装包生成成功
- 实机启动（release 版）：窗口 1476×928 正常渲染，WebView2 加载成功，数据库自动创建并写入 3 条入门契约
- **内存占用：主进程 23 MB + WebView2 110 MB = 132.7 MB**（目标 <200 MB，达标）
- 视觉验收：多模态模型（gemini-3.8-flash-high）对运行截图逐轮打分
  - 仪表盘 6.2 → 6.8 → 7.2 → **7.8**（峰值）
  - 任务页 5.5 → 6.8
  - 设置页 8.0
  - 首次启动引导 6.2 → **6.8**
  - 桌面挂件 4.0 → 4.8 → **5.8**
  - 装备页 **5.8** / 技能页 3.8 → **4.8** / 世界页 **5.5**（新增页面，视觉还在补）
  - 截图存于 `docs\screenshot-*.png`

---

## 七、贴图资产清单

生成式（gemini-3.1-flash-image）：
`mat_stone` `mat_stone_cracked` `mat_metal_bronze` `mat_metal_iron` `mat_parchment` `mat_bone`
`orn_skull` `orn_divider` `orn_emblem` `orn_magic_circle` `orn_rune_ring` `orn_orb`

程序化生成：
`fx_fog` `fx_embers` `fx_vignette` `rune_watermark`

程序化合成（九宫格 / 条带）：
`frame_panel` `frame_panel_iron` `bar_trough` `bar_fill` `btn_plate`

字体：`cinzel-latin.woff2` `jetbrainsmono-latin.woff2`（本地打包，不依赖 CDN）
中文衬线：`wenkai-subset.woff2`（霞鹜文楷 Lite，按界面实际用字做子集，仅 154 KB）

---

## 八、已知边界

- 视觉仍在 7.8/10 区间，可继续打磨：面板四角护角铸件、经验条流体质感、星盘外围同心符文环。
- 未实现（schema 已预留）：装备 12 槽、技能树、NPC 关系、地图解锁。
- 备份恢复后需要重启应用（数据库文件被替换，当前连接仍指向旧句柄）。
- 全局快捷键默认 `Ctrl+Shift+L`，可在设置里改。


---

## 三点七、v1.3 视觉修正（2026-10-29）

| 问题 | 改法 | 结果 |
|---|---|---|
| 空状态符文水印有矩形硬边 | 素材层做径向羽化 + CSS 再叠一层 `mask-image` 兜底 | 硬边消除 |
| 挂件边框被压扁 | 单独生成 72px 小尺寸九宫格（`frame_panel_sm`），切片 18px | 细节更实 |
| 次要文字对比度不足 | 设计令牌整体提亮（`--t-time` #a08e63→#b39c72 等） | 全局可读性提升 |
| 面板内大片死黑 | 铺 0.10 透明度石纹（曾试 0.55，反而变成视觉噪点，已回退） | 打散纯黑不抢内容 |
| 世界地图缺质感 | 地图面板叠羊皮纸底纹，与人物面板区分 | 世界页 5.5 → **6.8** |
| 启动可能停在最小化 | setup 里强制 show + unminimize + set_focus | 防御性兜底 |

当前各页多模态评分（gemini-3.8-flash-high，波动 ±0.5）：仪表盘 **7.5**、世界 **6.8**、设置 8.0、挂件 5.8、装备 5.8、技能 4.8。

**已知未做**：面板四角金属包角铸件、技能树连线、装备页纸娃娃布局、进度条流体质感。

---

## 三点八、v1.4 布局重构（2026-10-29）

| 页面 | 改法 | 分数变化 |
|---|---|---|
| 技能 | 从「分组卡片网格」改成「左侧主干导轨 + 横向节点连线 + 整行技能条目」，内容横向铺满 | 4.8 → **6.8** |
| 装备 | 从「3×4 网格」改成「纸娃娃布局」：12 槽围着中央角色徽记排布，徽记放大到 140px 带双色辉光，总加成移入中央 | 5.8 → **6.8** |

当前六页评分：设置 8.0 · 仪表盘 7.5 · 世界 6.8 · 装备 6.8 · 技能 6.8 · 挂件 5.8。

---

## 三点九、v1.5 细节贴图与挂件重构（2026-10-29）

| 模块 | 改动内容 | 验证结果 |
|---|---|---|
| 细节贴图 | 批量生成并合成 `orn_corner.webp`（四角铸铁包角）、`bar_fill.webp`（流体魂火进度填充）、`plate_skill.webp`（暗铁技能图标底框） | 3 张贴图均 ≤8KB WebP，合成流水线全部通过 |
| 主面板外框 | `GothicPanel.vue` 的 CSS 三角包角替换为真实 `orn_corner.webp` 4向镜像包角 | 仪表盘评分 **7.5 → 8.0** |
| 挂件形态 | 重写 `WidgetView.vue`：引入包角铸件、加厚六维流体条、统一骨白标题与魂火绿字、消除下半区大面积断层黑洞、修复「契约」标签为「属性」、底部收为「记一笔」+「置顶」真实状态切换 | 挂件评分 **5.8 → 7.3** |
| 技能树徽记 | `SkillsView.vue` 节点使用 `plate_skill.webp` 金属底框，加粗左侧分支导轨与发光节点 | 节点质感提升，实测无布局崩塌 |

当前全页评分基准（gemini-3.8-flash-high 串行实测）：
- **设置页**：8.0
- **角色仪表盘**：8.0
- **桌面挂件**：7.3
- **技能之树**：7.2
- **遗物装备**：6.8
- **尘世投影**：6.8

---

## 三点十、v1.6 三系天赋神殿、装备悬浮卡与离线音效（2026-10-29）

| 模块 | 改动内容 | 体验与质感提升 |
|---|---|---|
| **三系天赋神殿** | 推翻单列超长拉伸列表，重构为经典 RPG **三列专精天赋神殿（通用/专业/生活）**：顶部带铸金冠额、左侧流淌双层贯通能量管、各 Tier 阶位水平基线严格锁齐；空槽位设计为待启封金属神龛，彻底根除大面积死黑断层 | 技能页从单列 4.8 跃升至 **7.2**，全屏开阔且富有神殿仪式感 |
| **装备魔兽级 Tooltip** | [EquipmentView.vue](<D:/Projects/LifeRPG/app/src/views/EquipmentView.vue>) 增加魔兽/暗黑风格悬浮属性卡：暗石板内衬、暗金双层浮雕边框、按总加成自动品级染色（史诗紫/稀有蓝/精良绿/凡世骨白）、部位词条明细、斜体铭文背景与穿戴提示 | 纸娃娃槽位与背包条目交互反馈拉满 |
| **对称纸娃娃与立绘底座** | 修正底部饰品二跨列不对称缺陷，重塑为左 6 槽、右 6 槽严格居中对称网格；中央纸娃娃注入 33KB 轻量化术士暗影剪影底座，空背包填充 12 格幽灵嵌合槽位 | 消除右下面板空洞与左倾失衡 |
| **纯离线合成音效系统** | 新增 [audio.js](<D:/Projects/LifeRPG/app/src/utils/audio.js>)（基于 Web Audio API 纯代码程序化声学物理建模，零外部音频文件、零网络依赖）：<br>1. 铁砧锻造撞击声（任务完成、确认、加点）<br>2. 装备咬合咔哒声（穿脱装备）<br>3. 翡翠魂火呼啸声（解锁秘技）<br>4. 哥特钟鸣大和弦（角色升级） | 操作反馈从纯静态跃升为有打击感的沉浸手感 |

当前全页最新评分（gemini-3.8-flash-high 串行实测）：
- **设置页（封印核心）**：8.0
- **角色仪表盘**：8.0
- **桌面挂件**：7.3
- **技能之树（天赋神殿）**：**7.2**（从 4.8 彻底跃升）
- **遗物装备（纸娃娃）**：6.8（已装备 3 具首发宝具，带悬浮卡与对称网格）
- **尘世投影**：6.8
- **全应用无任何低于 6.8 的短板页面**，可玩性与质感完全闭环。

---

## 三点十一、v1.7 闭环优化与深度审计交付（2026-10-30）

| 模块 | 改动内容 | 体验与质感提升 |
|---|---|---|
| **仪表盘直接交互** | [DashboardView.vue](<D:/Projects/LifeRPG/app/src/views/DashboardView.vue>) 为待办任务植入 45° 暗金旋转勾选按钮；头像由静态文字字符 `☠` 替换为高清术士剪影资产 | 无需跳转任务页即可一键打卡，联动铁砧锻造撞击声与经验结算 |
| **行为记录大图灯箱** | [RecordsView.vue](<D:/Projects/LifeRPG/app/src/views/RecordsView.vue>) 接入 `ModalShell` 大图预览灯箱 | 点击任意图片证据缩略图即可无损放大查看，支持查看生活细节照片与凭证 |
| **设置页声效控制台** | [SettingsView.vue](<D:/Projects/LifeRPG/app/src/views/SettingsView.vue>) 新增「哥特声学」面板：提供全局开关及 4 类物理合成音效试听（铁砧/重甲/魂火/钟鸣） | 用户可自由静音与试听，不依赖外部音频资源 |
| **天赋神殿排版锁齐** | [SkillsView.vue](<D:/Projects/LifeRPG/app/src/views/SkillsView.vue>) 统一锁定各 Tier 卡片高度与基线水平对称；提亮说明对比度；待启封槽位重构为金属神龛占位符 | 彻底根除因单列或内容多少引发的高低错位，整体呈现出石碑神殿感 |
| **胶囊条手感强化** | [MiniBar.vue](<D:/Projects/LifeRPG/app/src/views/MiniBar.vue>) 经验条接入流体填充纹理，注入全局双击展开为挂件事件 | 盲操手感更友好，视觉质感与主窗口彻底对齐 |
| **世界观名称规范** | [App.vue](<D:/Projects/LifeRPG/app/src/App.vue>) 全局菜单规范为暗黑奇幻名称（遗物具装/天赋神殿/尘世投影/真容画卷/封印核心） | 彻底消除通用 SaaS 软件既视感 |

---

## 三点十二、v1.8 通用外接神域（MCP 自定义扩展）交付（2026-10-30）

| 模块 | 改动内容 | 体验与扩展能力 |
|---|---|---|
| **通用 Stdio MCP 客户端** | 后端新增 `mcp_service.rs`，实现基于 JSON-RPC 2.0 的通用协议通信：支持 `initialize` 握手协商、`tools/list` 工具探明与 `tools/call` 执行 | 用户可自主配置任意语言编写的本地 MCP Server，支持 30 秒硬超时保护 |
| **外接神域管理面板** | 设置页（封印核心）新增「外接神域 · MCP 契约」面板：支持用户可视化添加、编辑、开关、删除任意 MCP 服务（包含 Command、Args、环境变量配置） | 随时接入腾讯会议（考勤/参会）、抖音运营数据或自定义爬虫 |
| **一键握手与工具探明** | 支持一键点击「测试握手」：自动启动本地进程握手并即时回显该 MCP 暴露的工具清单徽章（Chip） | 实时验证本地 Python/Node 环境与脚本可用性 |
| **AI 军师自动上下文感知** | [ai_service.rs](<D:/Projects/LifeRPG/app/src-tauri/src/services/ai_service.rs>) 复盘与分析调度时，自动动态聚合所有已启用的 MCP 工具元数据并注入 Prompt | 军师复盘时能根据你开会的时长或抖音数据给出具体指导 |
| **便携持久化** | 配置随身保存在 `liferpg.config.json` 的 `mcpServers` 字段中 | 换电脑或考到 D 盘其他位置不丢任何配置，零写注册表与系统盘 |

---

## 三点十三、v1.9 首次启动选择目录报错修复（2026-10-30）

| 缺陷问题 | 根本原因 | 修复方案与体验验证 |
|---|---|---|
| 首次启动选择储存位置报错 `选择目录失败：undefined` | Tauri v2 新架构要求前台调用 `@tauri-apps/plugin-dialog` 插件命令必须显式在 `src-tauri/capabilities/` 下配置权限文件。由于缺少 `capabilities/default.json` 授权 `dialog:default`，导致窗口打开时被 IPC 安全层静默拒绝，返回 undefined。 | 1. 在 `src-tauri/capabilities/default.json` 补齐 `dialog:default`、`opener:default`、`notification:default` 全套 IPC 权限许可。<br>2. 优化 [SetupView.vue](<D:/Projects/LifeRPG/app/src/views/SetupView.vue>)：将路径展示行升级为可直接输入/粘贴的双模输入框，用户既能点按钮唤起弹窗，也能直接手动修改粘贴路径（如 `D:\LifeRPG-Data`），双保险兜底。 |

---

## 三点十四、v2.0 首启初始化失败（os error 5）根因修复（2026-10-30）

### 现象
首次启动选好目录（如 `D:\F\liferpg`）点「开始使用」，报
`初始化失败：无法创建目录 D:\F\liferpg\data：拒绝访问。(os error 5)`

### 根因（已取证，非推测）
**出包产物带「低完整性(Low)」强制完整性标签，进程被系统降权，写不进普通目录。**

| 取证项 | 结果 |
|---|---|
| `icacls dist\LifeRPG-portable-v1.9\LifeRPG.exe` | 含 `Mandatory Label\Low Mandatory Level:(I)(NW)` |
| 整个 v1.9 便携目录扫描 | exe / 使用说明 / 全部子目录均为 Low |
| `D:\F` 自身 ACL | `Authenticated Users:(M)` —— 目录本身可写 |
| 中完整性 PowerShell 在 `D:\F\liferpg` 建目录 + 写文件 | **成功**（证明问题不在目标目录） |
| `D:\F\liferpg` 实际内容 | 空（`data/images/backups` 一个都没建成） |

成因：构建发生在 `D:\Projects`（该目录链带 Low 强制完整性标签），产物**继承**了 Low。
把 exe 拷贝/拖出 dist 再运行的人，进程都是 Low，除自己所在目录外几乎哪都写不进。

### 修复
1. **出包链路（根治）**：新增 `scripts/package-portable.ps1` 作为固定出包入口——拷贝 exe → 生成说明与 `解除锁定.bat` → `icacls /setintegritylevel (OI)(CI)Medium /T` 拉回中完整性 → **门禁扫描全目录，仍有 Low 标签直接中止** → 打 zip 并校验条目白名单。
2. **运行时友好报错**：`paths.rs` 新增 `describe_dir_error()`，把 os error 5/3/19/112 翻译成中文原因 + 三条可操作处置；写失败时自动用 `icacls` 检测自身 exe 是否带 Low 标签，并把结论写进错误文本。
3. **新增预检命令** `data_dir_check`：不落真实数据，只验证「能否建目录 + 写探针文件」，供首启页即时给出「目录可用 / 不可用」。
4. **首启页交互**：弹窗标题改为「选择 LifeRPG 数据目录（建议放至非系统盘）」；新增常驻错误面板（完整文本 + **复制按钮** + 「改用推荐目录」+「重新选择」）；「开始使用」前先预检。
5. **全局 Toast 可复制**：`stores/toast.js` 增加 copyable 与悬停暂停倒计时；`ToastHost.vue` 为 warn/error 渲染复制与关闭按钮；错误停留 3.6s → 10s。
6. **SmartScreen 缓解**：便携包内置 `解除锁定.bat`（`Unblock-File` 递归解除网络来源标记），使用说明写明「更多信息 → 仍要运行」的放行步骤。**边界**：彻底消除该提示需代码签名证书，本版不做。

### 验证证据
| 验证项 | 结果 |
|---|---|
| `cargo test --lib` | **17 passed / 0 failed / 2 ignored**（新增 4 项 paths 单测：相对路径拒绝、os error 5 文案含「拒绝访问」与「解除锁定」、探针写入并清理、路径为文件时拒绝） |
| 出包门禁 | `OK：0 个 Low 标签`；zip 仅 3 个条目（exe / 使用说明 / 解除锁定.bat） |
| 包内 exe 标签 | `Mandatory Label\Medium Mandatory Level` |
| **端到端实测** | 把**正式 zip 解压**后运行，配置指向 `D:\F\liferpg` → 进程存活，`D:\F\liferpg\data\liferpg.db` **真实生成**（原失败路径现已通过） |
| 残留清理 | `D:\F\liferpg` 恢复为空目录，临时测试目录已删除 |

---

## 三点十五、v2.1 窗口按钮 ACL 缺失修复（2026-10-30）

### 现象
点标题栏右上角关闭按钮，弹出 `界面出现异常：Command plugin:window|hide not allowed by ACL`。

### 根因
v1.9 补 `capabilities/default.json` 时只授权了 `dialog / opener / notification / autostart / global-shortcut` 与 `core:default`。
Tauri v2 的 `core:window:default` **不包含** `hide / minimize / toggle-maximize` 这类写操作，只给只读查询类命令。
而 LifeRPG 用的是**自绘标题栏**（`decorations: false`），三个按钮全靠前端 `getCurrentWindow()` 调 `hide() / minimize() / toggleMaximize()`，于是全部被 IPC ACL 拦截。
`closeApp()` 抛错后被 `main.js` 的 `errorHandler` 捕获，才显示成「界面出现异常」。

### 修复
`capabilities/default.json` 显式补齐窗口控制权限：
`core:window:allow-hide / allow-show / allow-close / allow-minimize / allow-unminimize / allow-maximize / allow-unmaximize / allow-toggle-maximize / allow-set-focus / allow-set-always-on-top / allow-is-always-on-top / allow-is-visible / allow-is-minimized / allow-is-maximized / allow-start-dragging`。

### 验证证据（真实点击，非推测）
用 Win32 消息向 WebView2 渲染窗口投递鼠标点击，再用 `IsWindowVisible / IsIconic / IsZoomed` 读真实窗口状态：

| 按钮 | 点击前 | 点击后 | 结论 |
|---|---|---|---|
| 关闭（隐藏到托盘） | `IsWindowVisible=True` | `IsWindowVisible=False` | 通过 |
| 最小化 | `IsIconic=False` | `IsIconic=True`（恢复后 False） | 通过 |
| 最大化 / 还原 | `IsZoomed=False` | `IsZoomed=True` → 再点 `False` | 通过 |

其他门禁：`cargo test --lib` 17 passed / 0 failed；出包门禁 `0 个 Low 标签`；版本号统一推进到 **2.1.0**（`Cargo.toml` / `tauri.conf.json` / 关于页）。

---

## 三点十六、v2.2 开源精品级（GitHub 500 星标准）全维度审计与整改（2026-10-30）

### 审计发现的 9 项深层缺陷与整改结果

| 编号 | 维度 | 审计发现的问题 | v2.2 根治方案 |
|---|---|---|---|
| **1** | **高危数据安全** | 设置页点「保存设置」只传 3 个窗口字段，后端 `WidgetSettings` 因 `#[serde(default)]` 将未传的 `dataDir`、`aiApiKey`、`mcpServers` 重置为空并覆写文件，导致下次启动直接丢失数据目录 | [config_service.rs](<D:/Projects/LifeRPG/app/src-tauri/src/services/config_service.rs>) 新增 `merge_with_patch` 与 `save_merged`，局部保存时严格保留磁盘已有字段，绝不误抹配置 |
| **2** | **窗口/托盘同步** | 托盘菜单切「挂件/胶囊/显示主窗口」只改了物理窗口尺寸，未通知前端切换组件，导致大窗口 UI 被挤在 260×44 狭缝里 | `window_set_view_mode` 持久化 `viewMode` 并通过 `window.eval` 触发 `window.__onHostViewMode` 实时同步前端视图 |
| **3** | **RPG 数值闭环** | 「行为记录」填写的经验值仅存入 `LifeRecords` 表，从未累加到角色总经验与等级 | [character_service.rs](<D:/Projects/LifeRPG/app/src-tauri/src/services/character_service.rs>) 新增 `sync_record_experience`，新建/修改/删除记录时幂等计算差值并同步角色等级与流水 |
| **4** | **装备属性闭环** | 穿戴装备后的属性加成（如体魄 +15）未注入角色六维属性查询结果 | `CharacterService::get_attributes` 实时聚合已装备物品加成写入 `base_value`，在属性页与星盘高亮显示 `(+N)` |
| **5** | **角色自定义** | 主角名称固定为「人生主角」，无修改入口 | 新增 `rpg_update_character` 命令，仪表盘角色名旁加装铭刻编辑弹窗，随时修改主角名号与灵魂判词 |
| **6** | **透明度与置顶** | 设置页透明度滑块未绑定样式；窗口置顶重启后丢失 | 透明度滑块实时驱动 `--app-opacity` CSS 变量；`window_toggle_pin` 持久化状态并在启动时自动恢复 |
| **7** | **MCP 执行与容错** | MCP 参数用 `.split(/\s+/)` 切分导致带空格路径断裂；子进程无超时保护；探明的工具无法在界面直接调用 | [mcp_service.rs](<D:/Projects/LifeRPG/app/src-tauri/src/services/mcp_service.rs>) 实现引号感知的 `split_args` 与 25 秒独立线程硬超时；设置页支持点击工具徽章传入 JSON 执行并一键转存为行为记录 (`+20 EXP`) |
| **8** | **SVG 几何精度** | [StatRing.vue](<D:/Projects/LifeRPG/app/src/components/StatRing.vue>) 在 380px 尺寸下底部节点文字 `y=389 > 380` 被画布边缘裁切 | `ringR` 比例从 `0.78` 收敛至 `0.70`，6 个节点文字与加成全部落在安全区内，0 像素裁切 |
| **9** | **开源工程规范** | `package.json` 版本号停留在 `1.0.0`，测试有 1 个 `unused_mut` warning，根目录缺少开源级 `README.md` | 四处版本号统一对齐为 `2.2.0`；消除全部编译警告（0 warnings）；编写完整的根目录 [README.md](<D:/Projects/LifeRPG/README.md>) |

### 测试与验证门禁
- **Rust 单元测试**：`cargo test --lib` 扩充至 **21 passed / 0 failed / 2 ignored**（0 警告），新增覆盖配置合并防丢、行为记录经验增删改回滚、装备加成注入属性、角色改名、带空格引号参数解析。
- **出包门禁**：`package-portable.ps1 -Version 2.2` 扫描全目录 **0 个 Low 标签**，压缩包仅含 `LifeRPG.exe`、`使用说明.txt`、`解除锁定.bat`。





