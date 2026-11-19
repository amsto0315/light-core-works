# 光子光核 · 作品集 2026

三个从零制作的原创作品，覆盖 **Unity 3D、实时图形、生成式设计**。
全部作品不依赖游戏模板或现成玩法代码；没有使用外部图片素材。

打开本文件夹的 **`index.html`** 开始浏览。

## 公开链接

- 作品集主页：https://amsto0315.github.io/light-core-works/
- 回声测绘：https://amsto0315.github.io/light-core-works/works/echo-survey/index.html
- 引力花园：https://amsto0315.github.io/light-core-works/works/gravity-garden/index.html
- 字场：https://amsto0315.github.io/light-core-works/works/type-field/index.html
- PDF：https://amsto0315.github.io/light-core-works/docs/%E5%85%89%E5%AD%90%E5%85%89%E6%A0%B8_%E4%BD%9C%E5%93%81%E9%9B%86.pdf

（也可以直接看 `docs/公开链接.md`。）

---

## 作品

### 01 · 回声测绘 / ECHO SURVEY
**Unity 6 · 第一人称 · 5–8 分钟**

一个没有光的测绘场。玩家手里只有声呐：按左键发出脉冲，波前经过的地方，建筑、柱子和地面才会短暂显形。找到并靠近三座信标。

- 四组脉冲同时存在；表面着色器逐片元计算到波前的距离，产生环形显形、余辉和网格线。
- 程序化生成的声呐音效，不依赖音频素材。
- Windows 构建：`works/echo-survey/EchoSurvey_Windows_x64.zip`
- 作品页：`works/echo-survey/index.html`

### 02 · 引力花园 / GRAVITY GARDEN
**HTML + Canvas 2D + Web Audio**

9,000 个粒子组成的实时引力场。鼠标是引力源，点击释放脉冲；粒子之间的轨迹、发光和声音都是实时计算。

- 稳定粒子模拟 + 旋涡力 + 边界循环。
- 拖尾、密度、引力可调；支持保存画面。
- 直接打开：`works/gravity-garden/index.html`

### 03 · 字场 / TYPE FIELD
**HTML + Canvas 2D · 生成式字体海报**

输入一句话，使用种子驱动的流场 / 径向 / 波纹算法，生成一张可导出的字体海报。

- 确定性随机：同一个种子永远生成同一张图。
- 三种构图、三种配色、密度与扰动可调。
- 导出 PNG / SVG；设置保存在本地。
- 直接打开：`works/type-field/index.html`

---

## 目录

```
光子光核_作品集/
├─ index.html                    作品集主页
├─ works/
│  ├─ echo-survey/               Unity 作品页 + Windows 构建
│  ├─ gravity-garden/            引力花园
│  └─ type-field/                字场
├─ screenshots/                  作品截图
├─ docs/
│  ├─ 作品说明.md                每件作品的设计、技术与边界
│  └─ 申请说明.md                面向光子光核的申请表述
│  └─ 光子光核_作品集.pdf        可直接上传的 PDF 版
└─ README.md
```

## 运行方式

- **网页作品**：双击 `index.html`，或直接打开 `works/.../index.html`；无需服务器、无需安装。
- **Unity 作品**：解压 `works/echo-survey/EchoSurvey_Windows_x64.zip`，双击 `EchoSurvey.exe`。Windows x64，建议独显/集显均可。
- **Unity 源码**：`works/echo-survey/source/EchoSurvey`，用 Unity 6 打开后 Play。

## 技术栈

- Unity 6（6000.6.7f1），Built-in Render Pipeline，C# / HLSL。
- HTML5 Canvas 2D、Web Audio API、原生 JavaScript。
- 没有第三方运行时库；所有视觉与声音由代码生成。

## 关于原创性

三件作品的核心规则、代码、关卡与视觉均由本人完成。创作过程中参考了实时渲染、生成艺术和交互设计的一般性资料，但没有复制任何现有游戏、教程项目或商业作品的玩法、关卡与素材。

## 截图

见 `screenshots/`：
- `echo-survey.png`
- `gravity-garden.png`
- `type-field.png`

---

2026 · 光子光核共创营申请作品集
