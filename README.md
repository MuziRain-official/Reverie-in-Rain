# Reverie in Rain

> 一款 PC 端 2D 类银河城动作游戏

## 项目简介

《Reverie in Rain》是一款 2D 类银河城（Metroidvania）动作游戏。玩家在一个相互连通的雨夜地图中探索、获得能力、解锁新区域，并与场景中的敌人战斗。

游戏侧重探索与手感的结合：

- **探索**：相互连通的地图单元，获得新能力后可回去打开此前无法通过的路径；
- **战斗**：近战为主的即时战斗，强调跳跃、冲刺、攻击之间的节奏；
- **氛围**：雨夜基调的 2D 光照与后处理，配合环境音与叙事碎片。

## 项目背景

项目不直接套用现成框架，而是自研一套轻量 GameFramework 作为底层支撑。原因有三：

1. **体量匹配**：商业框架（如 GameFramework、QFramework）功能远多于本项目所需，引入的成本高于自建；
2. **可维护**：代码量小、结构直白，便于初学者阅读与后续接手；
3. **可迁移**：框架层不含任何游戏概念，可整体复用到其他项目。

框架的具体设计与模块说明见 [Doc/framework.md](Doc/framework.md)。

## 技术选型

| 项 | 选型 | 说明 |
|-|-|-|
| 引擎 | Unity 6.3.11f1（6000.3.11f1） | 当前工程实际版本 |
| 渲染管线 | URP 2D（17.3.0） | 2D 光照与后处理 |
| 输入 | 新 Input System（1.19.0） | 键鼠 / 手柄统一 |
| UI | UGUI（2.0.0） | 2D 界面 |
| 2D | Tilemap / 2D Animation / Sprite Shape | 地图与角色动画 |
| 异步 | 协程（Coroutine） | 不使用 UniTask / async-await |
| 资源 | Resources（当前）→ 自研 AssetBundle（后续） | 前中期不走 AB，接口保持不变 |
| 逻辑热更 | XLua | 后续接入 |
| 测试 | Unity Test Framework（1.6.0） | EditMode 单测 |

**选型说明：**

- **协程**：所有异步逻辑统一走协程，不引入 UniTask。团队现有协程知识即可覆盖，降低学习成本，保持框架轻量；协程宿主由框架的全局入口统一提供。
- **资源**：当前阶段以 `Resources` 为主，只做一层薄封装。待内容规模上来后再换 AssetBundle 与热更，届时只替换封装内部实现，业务代码不改。
- **不用 Addressables**：面向的国内开发环境仍以 Unity 2021–2022 LTS 为主流，Unity 6 的本土工具链与生态适配尚未成熟，自研方案更可控、可迁移。

## 项目结构

```
Assets/
├── Scripts/
│   ├── GameFramework/    # 框架层（业务无关，可整体迁移）
│   │   ├── Core/         # 纯 C# 逻辑，不引用 UnityEngine
│   │   ├── Runtime/      # 依赖引擎的实现
│   │   └── Editor/       # 仅编辑器
│   └── Game/             # 业务层：玩家、敌人、战斗、地图、成长、叙事
├── Art/                  # 美术：精灵、动画、瓦片集、特效
├── Audio/                # 音频：BGM、SFX、UI
├── Prefabs/              # 预制体
├── Configs/              # 配置表
├── Lua/                  # 热更脚本
├── Scenes/               # 场景
└── Settings/             # 渲染管线等设置
```

程序集依赖方向（单向）：

```
Game ──► GameFramework.Runtime ──► GameFramework.Core ──► Unity 引擎
                    │
                    └──► GameFramework.Editor（仅编辑器）
```

## 文档

| 文档 | 内容 |
|-|-|
| [框架设计](Doc/framework.md) | 框架目标、分层与编码原则、20 个模块的设计与实现状态、启动顺序、项目结构、开发进度 |

> 新增模块时，在 `Doc/framework.md` 对应层的章节里补充设计说明，并把标记从 ⬜ 改为 ✅。其他方向的文档（如美术规范、关卡设计）在 `Doc/` 下单独新建。
