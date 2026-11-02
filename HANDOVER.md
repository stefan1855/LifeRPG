# LifeRPG 项目交接文档（Codex 研发与维护指南）

> **更新时间**：2026-11-02  
> **最新候选包**：`dist/LifeRPG-portable-v2.3.0-rc.5.zip`（数据一致性、编辑保护与UI改进；详见文末）
> **当前稳定基线版本**：`v2.2.0`（最新便携包：`dist/LifeRPG-portable-v2.2.zip`）  
> **技术栈**：Tauri v2 + Rust + Vue 3 + Vite 6 + SQLite (`rusqlite` bundled) + Web Audio API

---

## 一、快速认知：唯一活跃代码库与红线约束

### 1. 唯一活跃代码目录（Single Source of Truth）
- **主工程路径**：`D:\Projects\LifeRPG\app\`
  - 前端源码：`D:\Projects\LifeRPG\app\src\`
  - 后端源码：`D:\Projects\LifeRPG\app\src-tauri\`
- **配套目录**：
  - `D:\Projects\LifeRPG\scripts\`：构建环境引导与便携包打包门禁脚本。
  - `D:\Projects\LifeRPG\assets\`：贴图生成与九宫格边框程序化合成 Python 脚本。
  - `D:\Projects\LifeRPG\docs\`：交付说明（`LifeRPG-v1-交付说明.md`）与真机验收截图。
  - `D:\Projects\LifeRPG\dist\`：历次构建的绿色便携包输出目录。
- **历史归档目录（只读参考，严禁修改）**：
  - `D:\Projects\LifeRPG\_archive\` 下存放了早期尝试过的 10 套旧方案（.NET 8 WPF `LifeRPG.Native`、Unity `LifeRPG.Unity`、Electron、旧版 `LifeRPG.Widget` 等）。**不要在 `_archive` 下修改任何代码**。

### 2. 项目主理人（柳贯一）的核心要求与工程铁律
1. **文件与系统盘安全红线**：
   - **严禁删除 C 盘任何文件**；未经明确拍板，严禁删除用户既有代码资产。
2. **100% 绿色便携与零 C 盘写入**：
   - 应用采取免安装便携形态：配置文件固定存放在 `exe` 同级的 `liferpg.config.json`。
   - WebView2 运行时缓存必须在创建窗口前通过环境变量 `WEBVIEW2_USER_DATA_FOLDER` 重定向至 `<exeDir>\.runtime\webview2\`。
   - 数据目录（`dataDir`）由用户首次启动时自主选择，内部结构固定为 `<dataDir>\data\liferpg.db`、`images\`、`backups\`，**绝不默认写入 `%LOCALAPPDATA%` 或注册表**。
3. **UI 文案与审美准则**：
   - 视觉定调为 **暗黑哥特 / 绿火毁灭术 / 魔兽世界厚重金属质感**（深渊黑 `#050805`、暗石板 `#0B120B`、暗古铜 `#4A3C1B`、亮金 `#C0A64C`、亡灵魂火绿 `#7CFF1F`、月光骨白 `#CFEA78`）。
   - **UI 文案只陈述客观事实**，禁止在界面中出现「为什么不存 C 盘」「证明没占 C 盘」等解释性文案。
4. **沟通与决策方式**：
   - 结论先行，极简汇报，用「现在怎么做、以后怎么做」的具体使用场景讲清楚，拒绝术语堆砌；涉及多方案分歧时先给 A/B 选项交由用户拍板后再改代码。

---

## 二、架构全景与核心源码地图

### 1. Rust 后端 (`app/src-tauri/`)

| 文件路径 | 核心职责与实现要点 |
|---|---|
| `src/lib.rs` | 应用入口：重定向 `WEBVIEW2_USER_DATA_FOLDER`；注册 6 个 Tauri 插件（`opener`, `autostart`, `dialog`, `notification`, `global-shortcut`, `single-instance`）；挂载系统托盘；启动时强制还原窗口（`show` + `unminimize` + `set_focus`）并恢复置顶（`is_always_on_top`）；后台线程同步开机自启状态；注册全部 42 个 Tauri 命令。 |
| `src/paths.rs` | 便携路径解析：`exe_dir()`、`portable_config_path()`、`webview_data_dir()`、`normalize_data_dir()`、`prepare_data_dir()`、`probe_writable()`；内置 `describe_dir_error()` 将 `os error 5/3/19/112` 翻译为中文可操作处置建议，并在写入失败时自动调用 `icacls` 检测自身 `exe` 是否带有 `Low Mandatory Level` 标签。 |
| `src/db.rs` | SQLite 数据库管理：开启 `WAL` 与 `foreign_keys`；`ensure_schema()` 维护完整 19 张表结构（兼容原 .NET PascalCase 表名与列名）；`seed_starter_content()` 在首次真实打开数据库时写入初始任务、技能、地点与 5 件首发装备（内存单测不触发种子数据）。 |
| `src/rpg_engine.rs` | 纯函数数值引擎：`calculate_level()` 按 `100 * Level` 阶梯计算等级与当前级进度百分比；`resolve_attribute()` 映射六大属性（`physique / mind / discipline / expression / creativity / resources`）；`allocate_task_experience()` 分配经验。 |
| `src/commands.rs` | Tauri 命令层：包含角色、任务、记录、属性、装备、技能、世界、备份、导出、配置、窗口控制、AI 军师、外接 MCP 等全部命令。<br>**关键机制**：`window_set_view_mode` 在改变窗口物理尺寸（`main` 1460×920 / `widget` 340×600 / `mini` 260×44）的同时持久化 `viewMode`，并通过 `window.eval("window.__onHostViewMode(...)")` 同步前端视图状态。 |
| `src/services/config_service.rs` | 便携配置管理：`WidgetSettings` 结构体；**必须使用 `save_merged` / `merge_with_patch` 进行局部配置更新**，防止前端局部保存（如只传快捷键和透明度）时把 `data_dir`、`ai_api_key`、`mcp_servers` 覆盖为空。 |
| `src/services/character_service.rs` | 角色与经验核心事务：<br>1. `update_character()`：修改主角名号与灵魂判词；<br>2. `get_attributes()`：查询六维属性并**实时聚合已穿戴装备的词条加成注入 `base_value`**；<br>3. `apply_task_completion()` / `rollback_task_completion()`：基于 `RpgExperienceEvents` 幂等发放与回滚任务经验；<br>4. `sync_record_experience()`：新建/修改/删除「行为记录」时自动计算经验差值并同步角色总经验与等级。 |
| `src/services/ai_service.rs` | 可选 AI 旁路服务：默认关闭，通过 `reqwest::blocking` 调用 OpenAI 兼容接口（默认 `http://127.0.0.1:8317/v1`）。所有能力函数（`review`, `parse_task`, `name_equipment`, `coach`）**显式接收 `&AiConfig` 参数**以便单测注入；内置 `parse_json()` 自动剥离 ````json` 围栏。 |
| `src/services/mcp_service.rs` | 通用 Stdio MCP 客户端：遵循 JSON-RPC 2.0 (`2024-11-05`) 协议，实现 `initialize` 握手、`tools/list` 探明与 `tools/call` 调用。<br>**加固要点**：`split_args()` 支持单双引号包裹的带空格路径；`execute_stdio_session()` 采用独立线程 + `mpsc::recv_timeout` **25 秒硬超时保护**，超时自动 `child.kill()`。 |
| `src/services/{action_task,record,equipment,skill,world,stats,backup,export,image}_service.rs` | 各业务模块 CRUD 服务：其中 `backup_service.rs` 使用 `VACUUM INTO` 生成一致性快照后打包 ZIP，恢复前自动备份当前状态并清理 `-wal/-shm`；`image_service.rs` 将导入图片缩放至长边 `≤1920px`、JPEG `85%` 质量，并提供 Base64 DataURL 读取。 |
| `capabilities/default.json` | Tauri v2 IPC 权限白名单：显式授予 `core:default`、`dialog:default`、`opener:default`、`autostart:default`、`notification:default`、`global-shortcut:default` 以及自绘标题栏必需的 `core:window:allow-hide / show / close / minimize / unminimize / maximize / toggle-maximize / start-dragging / set-always-on-top` 等写权限。 |

### 2. Vue 3 前端 (`app/src/`)

| 文件路径 | 核心职责与实现要点 |
|---|---|
| `App.vue` | 根容器：启动时调用 `appState()` 与 `configGet()`，若无 `dataDir` 则渲染 `SetupView.vue`；支持 `main / widget / mini` 三形态切换；监听 `window.__onHostViewMode` 响应托盘菜单切换；绑定 `--app-opacity` 实现全局透明度调节；管理 10 项左侧导航。 |
| `views/SetupView.vue` | 首次启动数据目录引导页：支持「系统文件夹弹窗点选 + 手动输入/粘贴路径」双模输入；输入后自动调用 `dataDirCheck` 预检可写性；初始化失败时展示常驻错误面板（含完整诊断文本、一键复制按钮、一键改用推荐目录 `<exeDir>\LifeRPG-Data`）。 |
| `views/DashboardView.vue` | 角色总览：展示高清术士剪影头像、角色名号/判词编辑弹窗、经验流体条、左侧 380px 六维暗影契约星盘（`StatRing.vue`）、右侧今日统计、**支持首屏直接打勾完成的待办列表**、近期经验流水、AI 教练入口。 |
| `views/TasksView.vue` | 行动任务：顶部 AI 一句话建任务输入栏 + 四象限（Q1~Q4）矩阵看板，支持任务完成、撤销完成（经验自动回滚）、编辑、删除与逾期高亮。 |
| `views/RecordsView.vue` | 行为记录：支持按关键词和分类筛选、新建/编辑带经验值的生活记录、多图上传与删除、点击缩略图弹出原画比例大图灯箱（Lightbox）。 |
| `views/AttributesView.vue` | 成长属性：展示六维属性等级、进度条、最强/最弱契约统计，以及当前穿戴装备提供的 `装备 +N` 绿火加成标签。 |
| `views/EquipmentView.vue` | 遗物具装：左侧为 **12 槽左右严格对称纸娃娃布局**（中央为术士立绘底座与总加成汇总），右侧为装备库（空状态带 12 格锻铁幽灵槽位）；鼠标悬停任意槽位或条目弹出**魔兽级品级染色悬浮属性卡（Tooltip）**；编辑弹窗支持「AI 起名 + 加成建议」。 |
| `views/SkillsView.vue` | 天赋神殿：**通用系 / 专业系 / 生活系**三列专精并排架构，左侧贯通翠绿能量导管，右侧各 Tier 阶位卡片高度严格锁定为 `128px` 水平对齐，未填满阶位显示「待启封」神龛槽位；支持解除封印、凝神封印与 `+20` 熟练度淬炼。 |
| `views/WorldView.vue` | 尘世投影：左侧人物关系网（NPC 好感度 `-100 ~ +100` 调节与关系档位染色），右侧羊皮纸底纹地图据点解锁。 |
| `views/ImagesView.vue` | 真容画卷：汇聚所有行为记录中的图片证据画廊，点击打开大图预览弹窗。 |
| `views/ArchiveView.vue` | 复盘归档：展示完成率、连续活跃天数（Streak）、近 30 天活跃度柱状图、分类占比；支持一键导出 Markdown 与「AI 周复盘」（生成后可编辑并一键存为行为记录）。 |
| `views/SettingsView.vue` | 封印核心（设置页）：<br>1. 窗口与快捷（全局快捷键、60%~100% 实时透明度滑块、到期通知、开机自启）；<br>2. 哥特声学（全局音效开关 + 4 种物理合成音效试听按钮）；<br>3. 数据位置（展示数据/程序/缓存路径、一键迁移数据目录、导出 Markdown）；<br>4. AI 军师（开关、网关地址、主力/轻量模型、密钥、测试连接）；<br>5. **外接神域 · MCP 契约**（增删改查本地 Stdio MCP 服务、一键「测试握手」探测工具、**点击工具徽章传入 JSON 参数直接执行并一键转存为行为记录 `+20 EXP`**）；<br>6. 备份与恢复（立即热备份、恢复前自动保护备份）。 |
| `views/WidgetView.vue` & `MiniBar.vue` | 桌面小组件（340×600）与胶囊条（260×44）：挂件带四角铸件、13px 加厚六维流体条、双行待办打卡与底部「记一笔 / 置顶」；胶囊条支持**双击整条直接展开为挂件**。 |
| `utils/audio.js` | 离线物理音效合成器：基于 Web Audio API 实时振荡器与滤波器合成 `hammer`（打铁撞击）、`equip`（重甲卡扣）、`soulFlame`（魂火呼啸）、`levelUp`（Dm 大教堂钟鸣琶音），开关状态持久化于 `localStorage`。 |

---

## 三、构建、测试与出包标准流程（必读！踩坑必炸）

### ⚠️ 两个致命环境陷阱（动手前必须清楚）

1. **陷阱 A：MSVC 头文件缺失与中文用户名 `TEMP` 路径导致 `cl.exe D8050` 编译失败**
   - 本机 Windows 用户目录含中文（`C:\Users\苏丽珂`），`cargo` / `cc-rs` 在非 ASCII 的 `TEMP` 下调用 `cl.exe` 会直接报 `D8050` 崩溃；且默认 PowerShell 会话未加载 VS2022 的 `INCLUDE` 和 `LIB`。
   - **铁律**：每次执行 `cargo` 或 `npx tauri build` 前，**必须在同一终端会话中先点源运行 `scripts\build-env.ps1`**：
     ```powershell
     . "D:\Projects\LifeRPG\scripts\build-env.ps1"
     ```
     该脚本会自动配置 MSVC 路径，并将 `TEMP`/`TMP` 指向纯 ASCII 的 `D:\Projects\LifeRPG\.tmp\build_temp`，将 `CARGO_TARGET_DIR` 指向 `D:\Projects\LifeRPG\.tmp\cargo-target`。

2. **陷阱 B：`D:\Projects` 沙箱目录带有 `Low Mandatory Level`（低完整性标签）导致 `os error 5 拒绝访问`**
   - `D:\Projects` 目录链带有 Windows `Low Mandatory Level` 标签，在此目录下编译生成的 `liferpg.exe` 会继承该标签。
   - **后果**：若直接运行带 `Low` 标签的 `exe`，或未剥离标签就直接打包分发，进程会被 Windows 强制降权为低完整性进程（Low IL），**无法向任何普通目录（如 `D:\F\liferpg` 或用户桌面）创建文件夹或写入文件，必报 `拒绝访问 (os error 5)`**，且直接在 `D:\Projects` 下运行会导致 WebView2 无法写缓存而白屏。
   - **铁律**：
     - **出包必须走 `scripts\package-portable.ps1`**：该脚本会自动执行 `icacls ... /setintegritylevel (OI)(CI)Medium /T` 将产物恢复为中完整性（Medium IL），并扫描全目录确认 `0 个 Low 标签` 后再打包 ZIP。
     - **真机运行验收时**，必须将便携包解压到 `%TEMP%\liferpg-e2e` 等普通中完整性目录下启动，**严禁在 `D:\Projects` 内部直接双击或启动 `liferpg.exe`**。

---

### 标准命令速查表

#### 1. 运行后端单元测试（门禁要求：0 failed, 0 warnings）
```powershell
. "D:\Projects\LifeRPG\scripts\build-env.ps1"
cargo test --manifest-path "D:\Projects\LifeRPG\app\src-tauri\Cargo.toml" --lib
```
- 当前共 **21 项常规单元测试 + 2 项 `#[ignore]` 真实网关联调测试**。
- 如需跑真实 AI 网关联调测试（需本地 `127.0.0.1:8317` 在线，且必须加 `--test-threads=1` 串行执行以防上游模型并发限制）：
  ```powershell
  cargo test --manifest-path "D:\Projects\LifeRPG\app\src-tauri\Cargo.toml" --lib -- --ignored --test-threads=1
  ```

#### 2. 仅构建前端（快速验证 Vue 语法与资源打包）
```powershell
Set-Location "D:\Projects\LifeRPG\app"
npm run build
```

#### 3. 构建 Release 二进制并打包绿色便携版
```powershell
# 第一步：加载构建环境并编译 Release exe（约需 2.5 ~ 4 分钟）
. "D:\Projects\LifeRPG\scripts\build-env.ps1"
Set-Location "D:\Projects\LifeRPG\app"
npx tauri build --no-bundle

# 第二步：运行固定出包脚本（自动剥离 Low 完整性标签、生成说明与解除锁定脚本、校验 ZIP 条目白名单）
Set-Location "D:\Projects\LifeRPG"
pwsh -NoProfile -File ".\scripts\package-portable.ps1" -Version 2.2
```

#### 4. 版本号升级四处对齐清单
每次发布新版本（例如升至 `2.3.0`）时，必须同步更新以下 4 个位置：
1. `app/package.json` 中的 `"version"`
2. `app/src-tauri/Cargo.toml` 中的 `version`
3. `app/src-tauri/tauri.conf.json` 中的 `"version"`
4. `app/src/views/SettingsView.vue` 关于面板中的版本号文本

---

## 四、无框架真机 UI 走查与点击验证方法

在 Windows 原生环境下验收桌面端 UI 时，不要只信编译通过，按以下步骤在 `%TEMP%` 沙箱中拉起真机并验证：

1. **解压并配置测试沙箱**：
   - 将 `dist\LifeRPG-portable-vX.Y.zip` 解压至 `$env:TEMP\liferpg-e2e`，写入测试用 `liferpg.config.json`（指定 `dataDir` 到 `$env:TEMP\liferpg-data`）。
   - 使用 `Start-Process -FilePath "$t\LifeRPG.exe" -WorkingDirectory $t -WindowStyle Normal -PassThru` 启动。
2. **模拟点击与截图取证（Python ctypes）**：
   - 通过 `FindWindowW(None, "LifeRPG · 绿火毁灭术")` 获取顶层窗口句柄 `hwnd`。
   - 通过 `EnumChildWindows` 找到类名为 `Chrome_RenderWidgetHostHWND` 的 WebView2 渲染子窗口句柄 `render_hwnd`。
   - 向 `render_hwnd` 投递 `WM_LBUTTONDOWN (0x0201)` 与 `WM_LBUTTONUP (0x0202)` 即可模拟任意坐标点击（例如左侧导航栏 `x=110`，第 `i` 个菜单项 `y = 56 + 14 + i*45 + 20`；右上角自绘标题栏按钮 `y=28`，关闭 `x=w-31`、最大化 `x=w-67`、最小化 `x=w-103`）。
   - 截图使用 `PrintWindow(hwnd, hdcMem, 2)`（flag `2 = PW_RENDERFULLCONTENT`），切换页面后必须计算两张截图的像素差（`ImageChops.difference` 均值 `> 5.0`）以确认页面真正发生了切换。
   - **验收完毕后务必清理**：`Stop-Process -Name "LifeRPG" -Force` 并删除 `%TEMP%\liferpg-*` 临时目录。

---

## 五、本地外部服务资产与后续演进方向（Backlog）

### 1. 本机可复用的外部服务资产
- **本地 OpenAI 兼容模型网关**：`http://127.0.0.1:8317/v1`（由本机 `cli-proxy-api` 承载）。
  - 注意：调用智谱模型时一律使用小写 `glm-5.3` / `glm-5.3-flash`（不要用大写 `GLM-5.3`），且智谱渠道有并发限制，测试时必须串行请求。
- **用户现成的本地 MCP Server 脚本**：
  - 腾讯会议考勤与会议管理：`D:\Projects\mcp-servers\tencent-meeting-mcp\server.py`
  - 抖音账号数据抓取：`D:\Projects\mcp-servers\douyin-mcp\server.py`

### 2. 已预留钩子、可供 Codex 后续继续演进的方向
1. **Obsidian 知识库自动同步**：
   - `WidgetSettings.obsidian_vault` 字段已在前后端配置结构中预留（用户本机 Obsidian 根目录为 `D:\F\Obsidian\`）。
   - 后续可在设置页开放路径选择，实现将「行为记录」或「AI 周复盘」自动以双链 Markdown 格式同步写入用户的 Obsidian 仓库指定文件夹。
2. **AI 军师多轮自动 Tool Calling（Function Calling 闭环）**：
   - 目前 v2.2 已支持在设置页手动点击执行任意 MCP 工具并将结果一键转存为行为记录（`+20 EXP`），且 `ai_service::review` 会将已启用 MCP 的工具描述注入军师上下文。
   - 若后续需要 AI 军师在点击「AI 复盘 / AI 教练」时**全自动在后台发起 `tool_calls -> McpService::call_tool -> 二次推理`**，可在 `AiService::chat` 中扩展 OpenAI `tools` 字段与多轮消息回传循环。
3. **任务到期系统级桌面通知轮询**：
   - `tauri-plugin-notification` 与 `capabilities/default.json` 中的 `notification:default` 均已就绪，设置页也有 `notificationEnabled` 开关；后续可在前端或 Rust 后台加一个轻量定时检查器，当今日 `Pending` 任务到达 `DueAt` 时弹出系统通知。


## 2026-10-31 源码修复进展

已完成第一轮数据安全、经验一致性与 UI 交互修复，详见 `docs/2026-10-31-修复进展.md`。当前便携 ZIP 仍为此前 v2.2 产物，不代表最新源码。后端测试现为 28 项通过、2 项真实网关测试跳过。新图片按相对文件名解析；备份恢复使用 SQLite backup，禁止恢复测试触碰真实配置目录。


## 2026-10-31 测试候选包与验收

- 最新测试候选包：`dist/LifeRPG-portable-v2.3.0-rc.1.zip`（稳定基线仍按此前版本保留）。
- 验收报告：`docs/2026-10-31-验收报告.md`。30 项后端测试通过，2 项真实网关测试跳过。
- 原生 Windows 100% DPI；125%/150%/200% 为 WebView2 渲染缩放，不能宣称已做系统原生高 DPI 或跨屏验收。
- 首次目录初始化先调用 `Db::open_at` 验证，再保存配置；恢复校验业务表和外键；图片目录读取失败必须使备份失败。
- 关键截图与 JSON 证据：`docs/acceptance-2026-10-31/`。

## 2026-10-31 原生 DPI 验收补充

- 最新测试候选包为 `dist/LifeRPG-portable-v2.3.0-rc.2.zip`。
- 本机已实际切换 Windows 125% / 150% / 175%，窗口 DPI 分别为 120 / 144 / 168；10 页、动态切换、重启及原生输入通过。桌面最终恢复 100% / 96 DPI。
- 修复 DPI 变化后的原生窗口边界，以及 175% 下设置页长路径撑宽双列布局。
- 原生 200% 受本机标准缩放上限限制未测；只有单显示器，跨屏仍未测。其他未完成项见原报告。
- 报告 `docs/2026-10-31-原生DPI验收.md`；证据 `docs/native-dpi-2026-10-31/`。

## 2026-10-31 RC3 三板块收尾

- 最新候选包：`dist/LifeRPG-portable-v2.3.0-rc.3.zip`；报告 `docs/2026-10-31-三板块实现与验收.md`，证据 `docs/integrations-2026-10-31/`。
- 图片整理：扫描超过24小时的未引用图片，用户审阅后隔离、恢复或输入确认后永久删除；清理前重查引用，隔离区随迁移复制。无自动删除；本轮未清理正式图库。
- 数据维护互斥覆盖迁移与数据访问。备份先检查引用，临时归档完成后发布；当前缺图时恢复前保留可读旧状态与缺图清单。360图哈希回归通过。
- AI四功能和抖音真实查询通过；MCP参数用JSON数组，默认UTF8，业务失败不能领取经验。耗时命令使用spawn_blocking，避免阻塞客户端在异步运行时内panic。
- 通知有测试/状态入口、同步系统提交、失败重试与持久化去重，系统记录确认及重启去重通过。便携通道来源可能显示PowerShell，界面已说明。
- 41项后端测试通过，0失败，2项默认真实网关测试跳过，真实调用另有证据。腾讯会议握手通过，业务查询因IP白名单按用户要求暂缓，管理员配置后重试。


## 2026-10-31 RC4 仪表盘视觉改造

- 最新候选包：`dist/LifeRPG-portable-v2.3.0-rc.4.zip`；上一候选 RC3 保留。
- 实现参考图的角色立绘 / 经验六维 / 装备日志三栏，64px 图标导航，内容容器响应式两栏、一栏。任务可结算，符文 / 装备 / 技能 / 世界可跳转。
- 新素材及完整提示词在 `app/src/assets/dashboard/`（内置 image_gen 模式）；文字和数据由真实 Vue 组件渲染。`RelicPanel.vue`、`DashboardEquipment.vue` 供仪表盘使用，`StatRing.vue` 重做。
- 新命令 `rpg_set_avatar(path: Option<String>)` 在操作读锁与后台任务中导入或清除头像引用，复用现有图片私有目录与备份 / 清理引用保护。清除引用不会直接删除旧图片。
- 成长日志弹窗每次加载增加 100 条。默认待办区域两行预览，可独立滚动，避免数据增加挤出首屏。
- 最终发布程序的真实点击、头像、任务经验、十页导航、空状态和 125 条日志分页通过；5 种窗口尺寸无横向溢出。新界面本轮仅做 Windows 原生 100% / 96 DPI，不将窗口尺寸测试表述为系统高 DPI 复验。
- 后端 41 通过、0 失败、2 项默认网关测试跳过。报告 `docs/2026-10-31-仪表盘视觉改造与验收.md`；证据 `docs/dashboard-2026-10-31/`。
- ZIP SHA256 `7d4dbd18ed380d729efe6b7d7839cc4aa312f69e5e14276158b789ed47125b4c`；exe SHA256 `f17cca2d70b6083af41b554bf715000b0b94c6a97b154cae77d9b1a2ac91cbad`。
- 既有配置损坏保护、已完成任务编辑、附件 dirty 提示和腾讯会议白名单仍按前文处理，本轮未解决这些独立问题。


## 2026-11-02 RC5 修复与验收

- 最新候选包 `dist/LifeRPG-portable-v2.3.0-rc.5.zip`，RC4 保留；报告 `docs/2026-11-02-RC5修复与验收.md`，证据 `docs/rc5-2026-11-02/`。
- 配置损坏/读取失败不再回退旧版目录。数据库访问和保存均保护；启动恢复入口先保留原件；配置修改统一互斥更新。新 config_recover、app_state.configError。
- 任务/记录经验使用 IMMEDIATE 写事务。重复完成保留日期；已完成任务经验与成长属性前后端锁定。并发账本回归通过。
- 装备保存/穿戴事务化、换槽互斥。有效穿戴视图统一快照和属性；equip_conflicts/equip_repair 提供预览及先备份后修复。
- MCP 返回与执行上下文绑定；AI 编辑增加会话检查。ModalShell 支持 state 快照、busy 和 initialDirty，覆盖附件、加成和程序化填充。
- 连续活跃独立计算；新增按图片分页 image_gallery。头像透明度保留，新增 AvatarFit 迁移、rpg_set_avatar_fit。导航 navExpanded 持久化；窄屏优先待办，详细区折叠，正文字号提高。
- 后端46通过、0失败、2项网关默认跳过；前端MCP竞态2通过；最终发布程序五尺寸、十页导航、配置损坏/权限拒绝/恢复、透明头像、附件、键盘及历史冲突备份修复通过。
- 原生125%/150%/175%各10页及键盘、挂件/胶囊通过，最终恢复100%（96 DPI）；系统缩放期间过渡矩形有临时越界，稳定后边界通过。200%及跨屏未测。腾讯会议白名单继续暂缓，本轮不新增外部联调。
- 源码清单及哈希 docs/rc5-2026-11-02/source-manifest.json；未提交既有Git删除项。包和受测exe字节一致，哈希见 artifact-hashes.json。

## 腾讯会议白名单恢复核验（2026-11-02）

用户更新白名单后，使用 RC5 隔离副本重新执行只读 `get_user_meetings`，业务查询成功，返回会议列表分页字段，未再出现500125。此前“白名单暂缓”属于历史状态；当前会议列表查询链路已跑通。未创建或修改会议，未将会议内容写入报告。
