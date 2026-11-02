# LifeRPG 参考图贴图包 v1

本包只制作参考图对应的图片、切片与字体样张；没有接入应用，没有修改现有页面或业务代码。

## 从哪里看

- `previews/ready-assets-contact.png`：清洁贴图总览。
- `previews/alpha-contact.png`：棋盘底透明通道预览。
- `previews/font-specimen.png`：原图字样与已选字体的实际内容排样。
- `previews/layer-composition-demo.png`：素材分层组合检查图，是静态排样，不是程序运行截图，也不是最终界面验收。
- `ready/`：110 张独立贴图。大图、图集分出的独立图、边框八片、按钮三片均按用途分目录。

## 原图逐像素切片

原图 `source/reference.png` 为 1672×941，未放大、未重绘。`raw-components/` 有 122 个语义组件切片，`source-grid/` 有 56 个不重叠的全图覆盖切片。后者重组为 `previews/source-reconstructed.png`，与原图像素逐字节相同。

坐标记录在 `slice-manifest.json` 和 `source-coordinates.csv`：左上角为原点，使用整数 `[x,y,width,height]`。所有原始切片保留图片中的文字、遮挡和邻接内容；不要把它们当作已去字透明素材。

## 去字、补齐与延展

`generated/` 保存实际生成的母版，`prompts/` 保存逐次完整提示词。`ready-manifest.json` 逐项列出来源母版、实际裁切坐标、尺寸、Alpha 范围和 SHA256。AI 补绘、去字和延展与原图无损裁切明确区分；被遮挡或画布外的内容属于补绘，不能声称是原图隐藏像素的 100% 还原。

覆盖内容：

| 参考区域 | 已制作图片 |
| --- | --- |
| 整体外壳 | 透明完整外框、四角、上下及左右边轨、空白标题冠饰 |
| 角色卡 | 透明角色、空白卡框及八片边框、延展场景、等级空牌、按钮四状态及左右端/中段 |
| 经验区域 | 空槽、满槽、可裁切能量填充、左右宝石端帽、细属性槽、内面板框 |
| 暗影契约 | 六类独立符文、四种节点框状态、核心徽记、连接符文环、独立祭坛地面 |
| 当前装备 | 十二类独立装备图标、空槽底板、去字并延展的暗影装备背景 |
| 最近变化 | 六类日志徽记、面板框、可复用按钮 |
| 技能/契约/声望入口 | 空白入口条、技能/契约/声望图标、上下/向右箭头、刷新图标、标题分隔饰条 |
| 背景与效果 | 普通横版世界背景、2172×724 超宽延展、两类竖版背景、暗色面板材质、左右独立火焰、地面火焰、薄雾、火星、闪光 |

共选择 24 张生成母版。`soul-fire-effects.png` 为已被替换的初稿；`soul-fire-effects-clean.png` 仅使用下排雾、火星和闪光，上排有截边的火焰已换为三张 `*-contained.png` 独立母版。薄内框曾生成成大骷髅外框，该次失败尝试只保留提示词与淘汰记录，没有混入可用贴图。

背景记录实际分辨率：横版 1672×941，超宽版 2172×724，角色与装备竖版 1024×1536；不把提示词请求的尺寸写成实际输出尺寸。材质延展可作低对比底图；未经接入后的铺排验收，不承诺任意缩放、拼接都没有接缝。

## 字体已配齐

无需另外提供字体。字体文件和各自 OFL 1.1 许可证在 `fonts/`，角色配置在 `fonts/font-roles.json`。

| 内容 | 已选字体与字重 |
| --- | --- |
| 中文主标题、角色名 | Noto Serif SC 700 / 600，匹配原图宋体衬线与较厚标题笔画 |
| 面板标题、按钮、小标签 | Noto Serif SC 600 / 500 |
| 任务与说明正文 | Noto Serif SC 400 |
| 经验总量 | Noto Serif SC 700，保留原图数字的衬线感 |
| 紧凑等级、百分比、增量 | Roboto Condensed 700 |
| 拉丁装饰标题 | Cinzel 600 |

这是根据图片字形和具体内容作出的视觉匹配，没有把候选字体宣称为已识别出的原图确切字体。样张中文字、标点和数字已检查无缺字。

官方来源：[Noto Serif SC](https://github.com/google/fonts/tree/main/ofl/notoserifsc)、[Roboto Condensed](https://github.com/google/fonts/tree/main/ofl/robotocondensed)、[Cinzel](https://github.com/google/fonts/tree/main/ofl/cinzel)。本包未收录 Windows 专有字体。

## 后续使用方式

原图切片不缩放即可核对像素；补绘图按清单坐标使用。边框八片固定四角，中段沿对应方向延展；标题冠饰独立锚定。按钮固定左右端，延展中段。能量条只裁切填充宽度，端帽保持原比例。符文与节点框是两层；人物、背景与框体是三层。字体作为实时文字使用，不把任务内容烘焙进贴图。

`scripts/slice-reference-ui.py` 与 `scripts/build-reference-asset-kit.py` 可在项目根重新生成切片、清单和预览，需要 Python、Pillow、fontTools；无需访问正式用户数据。检查结果在 `qa-report.json`。本轮验收包括原图像素一致性、PNG 解码、透明通道、框体中心透明、字体样张缺字及目视检查，不代替应用接入后的布局验收。
