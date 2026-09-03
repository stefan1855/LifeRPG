# LifeRPG 项目开发全景总结与技术沉淀

> **项目目标**：打造一款以“暗黑亡灵术士 / 绿火毁灭术 / 哥特魔兽”为视觉主题，通过 RPG 游戏化机制（等级、属性、四象限任务、装备技能、行为记录、经验回滚）管理个人成长与行动追踪的桌面应用。

---

## 一、 所有尝试过的技术架构

| 架构形态 | 技术栈 | 定位与实现机制 | 最终状态 / 评估 |
| :--- | :--- | :--- | :--- |
| **1. C# .NET 原生桌面 (`LifeRPG.Native`)** | .NET 8 + WPF + XAML + SQLite (`Microsoft.Data.Sqlite`) | **5层分层架构**：<br>• `LifeRPG.Core`：领域模型 + RpgEngine 纯计算层<br>• `LifeRPG.Data`：SQLite + Repository + 5次幂等迁移服务<br>• `LifeRPG.Storage`：Markdown导出 + 备份恢复 + 图片压缩<br>• `LifeRPG.App`：WPF UI + 7页Tab式导航 + DI容器<br>• `LifeRPG.Tests`：68个 xUnit 单元测试 | **功能最完整 (v0.1 → v1.3.0)**。<br>数据层、业务逻辑、任务引擎、经验回滚极其稳固；但 WPF 承载游戏级动态特效（发光Shader、绿火粒子、动态金属流光）较为吃力。 |
| **2. Web/Canvas 游戏化渲染 (`绿火毁灭术LifeRPG`)** | Electron + PixiJS v7 + better-sqlite3 + Node.js | **无边框透明窗口 + 2D WebGL 渲染**：<br>• Electron `frame: false` 无边框容器<br>• PixiJS v7 负责 2D 贴图切片、粒子特效与动态发光<br>• SQLite 本地持久化 | **完成一期毛坯 (30% 视觉)**。<br>核心链路贯通，但游戏级 UI 切片素材工程量巨大（需几十张高质量自描述贴图），贴图精度与对齐成本高。 |
| **3. 游戏引擎原生渲染 (`LifeRPG.Unity`)** | Unity 2022.3 (Built-in Pipeline) + uGUI + Mono.Data.Sqlite | **专业游戏 UI 渲染**：<br>• uGUI `Image.Type=Sliced` 原生九宫格切片<br>• 多层 Canvas 叠加 + 自定义 `UI_Glow.shader` 发光材质工厂<br>• SQLite 5张表底座，纯代码构建 UI (`new GameObject` + `AddComponent`) | **完成基础底座与 Shader 验证**。<br>具备极致的视觉上限和原生 Shader 渲染能力，但纯代码构建 UI 缺乏 Prefab 可视化维护效率较低。 |
| **4. 轻量桌面挂件 (`LifeRPG.Widget`)** | Tauri v2 (Rust) + Vue 3 + Vite + Tailwind/Lucide | **超轻量桌面常驻小部件**：<br>• Rust 后端系统级调用 + Web 前端展示<br>• 专注低内存常驻、悬浮窗、快捷记录与状态感知 | **桌面辅助形态验证**。<br>资源占用极低，适合作为常驻副屏/悬浮小部件使用。 |

---

## 二、 所有踩过的坑与技术教训

### 1. WPF / XAML 布局与事件陷阱
- **Star-height Grid + 嵌套 ScrollViewer 导致行塌缩**：外层 Grid 行设为 `Height="*"` 时，内部 `ScrollViewer` 不报告 desired height（默认返回“多少都行”），导致 Grid 直接分配 0 高度，页面内容全部不可见。
- **DataGrid/ListBox 吞鼠标滚轮事件**：WPF 内置控件自带 ScrollViewer 会拦截 `MouseWheel` 冒泡。解决方式是在外层挂载 `PreviewMouseWheel` 处理器，检测事件源并手动转发 `ScrollToVerticalOffset`。
- **父子控件滚轮事件互殴**：子控件自带滚轮转发逻辑时，外层不能再重复挂载全局滚轮转发，否则会导致滚动跳动与抖动。
- **多主题硬编码重构灾难**：后期尝试浅色主题改造时，因历史控件存在 400+ 处硬编码颜色值，替换成本过高且难以保证视觉统一，最终回退并确立“专注暗黑哥特唯一主题”的原则。
- **UserControl 构造函数注入限制**：WPF XAML 依赖无参构造函数，无法直接走 DI 构造函数注入，最终收敛为 `Initialize(repo, service)` 明确初始化注入模式。

### 2. 代码维护与 XAML 替换翻车
- **XAML 引号转义与 StaticResource 丢失**：大段 patch XAML 极易因引号转义漂移或缺失 StaticResource key 导致 XAML 解析崩溃。建立了“先扫描 Resource Key → 用 Python 脚本精确替换 → dotnet build 验证”的编辑铁律。
- **零容忍临时调试文件**：曾经随手创建的 `BootTest.cs` 导致入口点冲突 (`CS0017`) 和编译中断，后续强制要求**严禁创建临时 .cs 污染仓库**。
- **WSL 与 Windows 环境混淆**：WSL 环境下无 `dotnet` SDK，所有构建与运行必须在 Windows 原生环境（PowerShell/Git-Bash）执行。

### 3. Web & Electron 踩坑
- **无边框窗口交互**：Electron `frame: false` 下的窗口拖拽 (`-webkit-app-region: drag`) 与按钮点击事件冲突、CSP 策略限制、窗口穿透与阴影问题。
- **大数组栈溢出**：前端数据处理时使用 `Math.max(...array)` 处理海量记录时直接触发调用栈溢出，必须收敛为常规 `for` 循环遍历。

### 4. Unity 踩坑
- **纯代码动态拼 UI 维护难度大**：脱离 Unity Prefab 可视化编辑体系，全部用代码 `AddComponent` 和算坐标极易导致布局调优繁琐。
- **Build 列表缺失场景**：场景未加入 `EditorBuildSettings.scenes`，导致命令行打包和自动化测试无法直接索引场景。

---

## 三、 用户的所有核心要求与设计原则

### 1. 视觉风格与审美红线
- **主题定调**：浮夸暗黑哥特 / 亡灵术士 / 绿火毁灭术 / 魔兽风格（暗金边框、奥术紫微光、灵魂绿火焰、石质纹理、骷髅角标、哥特字体）。
- **像素级复刻（100% 重合度）**：对视觉质量极度挑剔，拒绝“差不多”和粗糙半成品；对标参考图必须达到像素级对齐。
- **拒绝对拉伸与模糊**：素材比例必须严丝合缝（方形图配方形槽，16:9 背景配标准画布），背景采用纯色渐变/平铺纹理防拉伸。
- **屏幕与分辨率基准**：以 1080P 屏幕、**1460×920** 窗口设计分辨率为基准。
- **流畅度底线**：界面交互与动画必须丝滑流畅，卡顿直接否决。

### 2. 界面交互与布局结构
- **Tab 式独立多页切换**：坚决摒弃拥挤的 Dashboard 网格堆砌和超长滚动条，采用左侧导航栏 + 右侧独立页面容器（首页、行动任务、行为记录、成长属性、图片证据、复盘归档、设置），点选即整区切换。
- **宽敞与呼吸感**：拒绝逼仄的折叠面板，偏好清晰明了的卡片与列表排布。

### 3. RPG 核心数值与游戏化规则
- **等级与经验曲线**：1 级起步，升级所需经验公式为 $100 \times N$（Lv.1 需 100 EXP，Lv.2 需 200 EXP...）。
- **六大核心成长属性**：体魄 (Physique)、心智 (Mind)、纪律 (Discipline)、表达 (Expression)、创造 (Creativity)、资源 (Resources) + 通用兜底 (General)。
- **四象限任务加成**：
  - 重要且紧急：+30 EXP
  - 重要不紧急：+25 EXP
  - 不重要紧急：+15 EXP
  - 不重要不紧急：+5 EXP
- **经验幂等与回滚机制**：任务完成发放经验必须防重复（基于 SourceId 唯一索引）；取消已完成任务必须自动扣回对应经验（支持回滚历史审计）。
- **图片证据与本地化**：记录支持图片佐证，入库自动压缩（JPEG 85% 质量、最大 1920px、保留 GIF 原样），数据完全存储于本地 SQLite。

### 4. 协作与工程规范
- **先讲概念再动手**：不接受一上来直接重构或重写布局，先理清概念、提供 A/B 方案供决策，确认后再动代码。
- **严禁临时垃圾代码**：禁止在生产代码中随手贴测试类或破坏 Git 纯净度。
- **验证为王**：任何改动必须以实际构建通过、自动化测试通过为交付标准。
