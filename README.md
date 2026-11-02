# ☠ LifeRPG · 绿火毁灭术 (v2.2)

> 当前修复候选：`v2.3.0-rc.7-audit`，后端 47 项及前端 8 项回归通过，原生交互验收待完成，见 [RC7 审核报告](docs/2026-11-02-RC7审核与修复.md)。`v2.3.0-rc.5-source` 为简化前回退点，`v2.3.0-rc.6-review` 保留为历史待审快照。回退方法见 [版本回退](docs/版本回退.md)。下文 v2.2 介绍保留为历史功能说明。

> **把现实人生锻造成一场暗黑哥特风的硬核 RPG。**  
> 基于 **Tauri v2 + Rust + Vue 3 + SQLite** 构建的纯本地、零 C 盘侵入、支持 **AI 军师** 与 **MCP (Model Context Protocol) 外接神域** 的 Windows 桌面生产力系统。

---

## ✨ 为什么做这一版 LifeRPG？

市面上绝大多数习惯打卡与游戏化待办应用（如 Habitica 等）存在三个致命痛点：
1. **扁平网页套壳感重**：缺乏真正的暗黑 RPG 沉浸感（厚重锻铁、暗金倒角、邪能魂火、打铁声效）。
2. **依赖云端账号与订阅**：个人习惯、行为流水、照片证据被迫上传第三方服务器，断网即残废。
3. **数据孤岛**：无法低成本接入用户自己的外部脚本（如腾讯会议参会考勤、抖音运营数据、Git 提交记录）。

**LifeRPG v2.2** 彻底推翻旧骨架，以 **Local-First（本地优先）+ 零系统盘污染 + 可选 AI/MCP 旁路** 的工程标准重写：
- **100% 绿色便携**：配置随身（`liferpg.config.json`），WebView2 运行时缓存强制重定向至程序同级 `.runtime\webview2\`，数据目录首次启动由用户自选，**实现真正的 C 盘零写入、注册表零污染**。
- **三档窗口形态无缝切换**：支持 `1460×920` 大屏指挥中心、`340×600` 桌面常驻挂件、`260×44` 极简胶囊护符，支持系统托盘与 `Ctrl+Shift+L` 全局呼出。
- **纯代码物理声学合成**：基于 Web Audio API 实时物理建模合成**铁砧锻造撞击声、重甲锁扣咬合声、翡翠魂火呼啸声、大教堂升级钟鸣**，0 外部音频文件、0 体积膨胀。

---

## 📸 真实运行界面预览

| 角色总览（暗影契约星盘） | 天赋神殿（三系专精树） |
| :---: | :---: |
| ![角色总览](docs/screenshot-dashboard.png) | ![天赋神殿](docs/screenshot-skills.png) |

| 遗物具装（12 槽对称纸娃娃 + 悬浮卡） | 桌面挂件 & 封印核心（设置与 MCP） |
| :---: | :---: |
| ![遗物具装](docs/screenshot-equipment.png) | ![封印核心](docs/screenshot-settings.png) |

---

## ⚔️ 核心特性矩阵

### 1. 完整闭环的 RPG 数值引擎 (`rpg_engine.rs`)
- **阶梯等级晋升**：采用 `100 × Level` 动态经验跨度，任务勾选与行为记录铭刻实时驱动主角总经验与六维属性经验上涨，触发「灵魂升华 (SOUL ASCENDED)」全屏特效与钟鸣和弦。
- **六维暗影契约**：`体魄 (Physique)`、`心智 (Mind)`、`纪律 (Discipline)`、`表达 (Expression)`、`创造 (Creativity)`、`资源 (Resources)` 六维雷达星盘，实时反映人生精力投向。
- **严密防刷与幂等回滚**：基于 SQLite 事务与 `RpgExperienceEvents` 事件表，重复点击绝不重复发经验；撤销任务或删除记录时自动精准扣回对应经验与属性等级。

### 2. 四象限行动契约 & 行为记录证据库
- **四象限矩阵**：按「重要且紧急 (Q1, +30)」「重要不紧急 (Q2, +25)」「不重要紧急 (Q3, +15)」「不重要不紧急 (Q4, +5)」组织行动契约；仪表盘首屏支持一键勾选结算。
- **图片自动压缩与大图灯箱**：上传生活照片或工作凭证时，Rust 后端自动使用 Lanczos3 算法将其重采样至长边 `≤1920px`、JPEG `85%` 质量落盘；内置原画比例大图预览灯箱（Lightbox）。

### 3. 12 槽纸娃娃装备系统 & 三系天赋神殿
- **现实器物映射**：将你现实中真正拥有的生产力工具（如人体工学椅、降噪耳机、客制化键盘）铸造为游戏内装备，穿戴后属性加成自动汇总注入角色六维面板与星盘。
- **魔兽级悬浮属性卡 (Tooltip)**：鼠标悬停任意装备槽或背包条目，即时弹出暗金双层浮雕属性卡（按总加成自动渲染史诗紫 / 稀有蓝 / 符文绿 / 骨白品级色）。
- **三系天赋神殿**：通用系、专业系、生活系三柱并列，支持技能解锁、封印与熟练度淬炼（每 100 点熟练度自动晋升技能等级）。

### 4. 可选 AI 军师旁路（OpenAI 兼容网关）
> 默认关闭，断网或不开网关时主流程 100% 正常运行，绝不阻塞核心打卡。
- **一句话建任务**：在任务页输入「明天下午三点去健身房练一小时」，自动解析出标题、象限、对应属性、经验值与截止日期。
- **现实物品哥特起名**：输入「降噪耳机」，AI 自动生成哥特装备名、现实铭文与两条契合的属性加成。
- **仪表盘 AI 教练 & 周度复盘**：根据近 7~30 天真实任务、记录与六维快照，生成亡灵导师风格的犀利复盘，并可一键存为行为记录。

### 5. 通用 MCP (Model Context Protocol) 外接神域
- **自主扩展任意工具**：在「封印核心 → 外接神域」中，像配置 Claude Desktop 一样填入任意本地 MCP Server 的 `Command`、`Args`（支持带空格路径与引号解析）与 `ENV` 环境变量。
- **25 秒硬超时与进程隔离**：内置基于 `stdio` JSON-RPC `2024-11-05` 规范的 Rust 客户端，一键「测试握手」探明工具清单；支持在界面直接传入 JSON 参数执行工具（如拉取腾讯会议考勤、抓取抖音运营数据），并将返回结果一键转存为行为记录（`+20 EXP`）。

---

## 🏗️ 架构与目录结构

```text
LifeRPG/
├── app/                          # Tauri v2 + Vue 3 主工程
│   ├── src/                      # 前端视图与哥特暗黑组件库
│   │   ├── components/           # GothicPanel(九宫格金属框), StatRing(星盘), ExpBar(流体条), ToastHost
│   │   ├── views/                # 10 大主视图 + SetupView(首启引导) + WidgetView(挂件) + MiniBar(胶囊)
│   │   └── utils/                # audio.js(Web Audio 物理音效合成), clipboard.js
│   └── src-tauri/                # Rust 核心后端
│       ├── capabilities/         # Tauri v2 显式 IPC 权限声明 (default.json)
│       └── src/
│           ├── db.rs             # 19 张 SQLite 表结构初始化与种子数据
│           ├── paths.rs          # 便携路径解析、目录预检探针、错误人话翻译
│           ├── rpg_engine.rs     # 等级曲线与属性经验分配纯函数引擎
│           └── services/         # 角色/任务/记录/装备/技能/世界/备份/导出/AI/MCP 十大服务
├── assets/                       # 贴图生成与九宫格合成流水线 (gen_textures.py, build_ui_assets.py)
├── scripts/                      # 构建环境引导与便携出包流水线
│   ├── build-env.ps1             # MSVC 环境变量与纯 ASCII 临时目录引导
│   └── package-portable.ps1      # 剥离低完整性标签 + 门禁校验 + 便携包打包脚本
├── docs/                         # 交付说明文档与真机运行截图
└── dist/                         # 免安装绿色便携包输出目录
```

### 便携版运行期数据布局（零 C 盘写入）

```text
<程序所在目录>/
├─ LifeRPG.exe
├─ 解除锁定.bat               ← 一键清除网络来源标记 (MOTW)，缓解 SmartScreen 拦截
├─ liferpg.config.json        ← 随身便携配置（含 dataDir 指针、快捷键、AI 与 MCP 列表）
└─ .runtime/webview2/         ← WebView2 渲染缓存就地收敛，绝不写 C:\Users

<用户自选的数据目录>/
├─ data/liferpg.db            ← SQLite 主数据库（WAL 模式）
├─ images/                    ← 自动重采样压缩后的 JPG 证据图库
└─ backups/                   ← 一致性热备份压缩包 (VACUUM INTO + ZIP)
```

---

## 🚀 快速开始（免安装便携版）

1. 解压 `dist/LifeRPG-portable-v2.2.zip` 到任意非系统盘目录（例如 `D:\LifeRPG`）。
2. 若首次打开遇到 Windows SmartScreen 提示「Windows 已保护你的电脑」：
   - 点「**更多信息**」→「**仍要运行**」；或者先双击目录内的 **`解除锁定.bat`** 再启动 `LifeRPG.exe`。
3. 首次启动会进入「**初次唤醒**」引导页：点击「选择目录」或直接粘贴路径（如 `D:\LifeRPG-Data`），界面显示绿色「**目录可用，可以写入**」后点「开始使用」即可。

---

## 🛠️ 本地开发与构建指南

### 环境要求
- Windows 10 / 11 (x64)
- Rust 1.77+ & Visual Studio 2022 Build Tools (C++)
- Node.js 20+

### 运行单元测试与构建出包

```powershell
# 1. 加载构建环境（自动注入 MSVC 头文件/库路径并隔离 ASCII 临时目录）
. .\scripts\build-env.ps1

# 2. 运行 Rust 后端全量单元测试（21 项核心单测 + 2 项网关集成测试）
cargo test --manifest-path .\app\src-tauri\Cargo.toml --lib

# 3. 编译前端与 Tauri Release 产物
cd .\app
npx tauri build --no-bundle

# 4. 使用固定门禁脚本打包绿色便携版（自动剥离 Low 完整性标签并校验条目）
cd ..
pwsh -NoProfile -File .\scripts\package-portable.ps1 -Version 2.2
```

---

## 📜 开源协议与致谢

- 字体资产：`Cinzel`（拉丁哥特标题）、`JetBrains Mono`（等宽数值）、`霞鹜文楷 Lite`（中文正文字集子集化打包，离线零 CDN 依赖）。
- 本项目遵循 MIT 协议开源，所有用户数据 100% 归用户本地所有。
