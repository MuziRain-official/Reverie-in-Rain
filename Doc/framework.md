# 框架设计

> Reverie in Rain · 自研 Unity GameFramework
> 标记说明：✅ 已实现，⬜ 计划中

## 1\. 框架目标

《Reverie in Rain》是一款 PC 端 2D 类银河城动作游戏。项目不直接套用现成框架，而是自研一套轻量 GameFramework，作为整个游戏的底层支撑。

框架只做三件事：

1. **统一生命周期**：所有全局模块由框架注册、初始化、驱动和释放，避免单例散落各处、初始化顺序混乱。
2. **屏蔽底层细节**：把资源加载、对象创建、UI 打开、输入读取、存档读写等 Unity 原生操作封装成稳定接口，业务层不关心 AssetBundle、协程、Input System、PlayerPrefs 等实现。
3. **保持业务无关**：框架代码中不出现玩家、Boss、房间等游戏概念，可整体迁移到其他项目。

## 2\. 分层约定

|层|内容|依赖方向|
|-|-|-|
|框架层|本文描述的 20 个模块|不依赖业务|
|业务层|玩家、敌人、战斗、地图、成长、叙事|只依赖框架接口|
|内容层|Prefab、美术、音频、配置表、Lua|只通过地址被引用|

约束：业务层不直接使用 `Resources.Load`、`GameObject.Find`、`PlayerPrefs`、`Input.\*`（旧输入）等 Unity 原生 API，统一走框架；模块之间通过事件与接口通信，不互相持有内部实现。

### 2.1 编码原则

* **能少不多**：先满足功能，再谈健壮；够用即止，不为「以后可能需要」预留结构。
* **不防小概率**：几乎不会发生、或能靠使用约定规避的问题，不写代码去防御（签名校验、去重、异常包裹等）；写进约定即可。
* **面向初学者**：命名与流程直白，少用泛型技巧、委托嵌套、缓存池等；宁可有几行重复，也不为「优雅」抬高理解成本。
* **贴合项目**：以 2D 类银河城、小体量、小团队开发为前提，按 PC 端与现有框架（协程、字符串事件）的实际能力判断，不照搬大型框架的做法。
* **改动克制**：修 bug 就修 bug，不顺手重构、不引入抽象；优先删代码，其次才是加代码。

## 3\. 模块设计

框架共 20 个模块，按初始化依赖分为五层。「单例」「状态机」为纯模式/基类，不参与运行时初始化。

### 3.1 基础层

#### 单例（Singleton）✅

* `Singleton<T>`：纯 C# 进程级单例，实现 `IModule`。由 `GameEntry` 显式 `Create()` 并初始化，`Release()` 释放；`Instance` 在入口前被访问时兜底 `new`（不初始化）并输出一次警告。
* `MonoSingleton<T>`：MonoBehaviour 单例，`Instance` 访问时自动查找或创建 GameObject，默认常驻场景。子类用 `AutoInit` / `DontDestroyOnLoadEnabled` 开关控制是否自动初始化与跨场景常驻。
* 待办：若关闭 Domain Reload（Enter Play Mode Options），需为静态字段补复位，当前未实现。

#### 日志与工具（Log \& Utils）✅

* 日志分级 Debug / Info / Warn / Error，按频道过滤，支持全局静音（Error 始终输出）。
* 输出端抽象为 `ILogSink`：Core 不引用 UnityEngine，由 Runtime 的 `UnityLogSink` 注入。
* 工具：`MathUtil`、`RandomUtil`、`StringUtil`、`PathUtil`。
* ⬜ 调试面板（FPS、内存、当前房间）、文件落盘。

#### 事件中心（Event）✅

模块之间解耦通信的总线，最简单版：一个字典 + 字符串事件名。

* 内部一张 `Dictionary<string, List<Subscription>>`：事件名 → 订阅列表；带参与不带参的订阅混存，发布时按回调类型筛选。
* 支持 `Subscribe` / `Publish` / `Unsubscribe`，各有带参与不带参重载；带 owner 的订阅可按对象批量退订。
* 派发前复制订阅列表再遍历，回调里增删订阅不会打乱本次顺序。
* 单个回调抛异常只记录日志并跳过，不中断其余订阅者。
* 约定：同一事件名的带参订阅只用一个参数类型（框架不校验）；`Unsubscribe(name, cb)` 会移除该回调的所有 owner 订阅，需要按对象隔离时用 `UnsubscribeAll(owner)`。

#### 状态机（FSM）✅

* `FsmBase<TState>` 泛型状态机：子类定义状态枚举，构造函数里 `Register` 各状态的进入/更新/退出回调。
* 提供 `Start` / `Update` / `ChangeState` / `Is`，切换时先退旧再进新，并触发 `OnStateChanged` 钩子。
* ⬜ 层次状态机（处理「移动中可攻击」这类父子状态）。

#### 计时器（Timer）✅

统一由框架 Update 驱动的延时 / 循环任务，替代业务各自开协程。

* `Delay(秒, 回调)` 延时一次，`Repeat(间隔, 回调)` 周期触发，返回 `Handle` 用于 `Cancel()`。
* 由 `GameEntry.Update` 每帧 `Tick(deltaTime)` 驱动，Core 不直接读 `Time`。
* 快照遍历，回调里新增或取消计时器不影响本帧顺序；单回调异常只记录不中断。
* 受 `Time.timeScale` 影响，暂停即停。一帧内掉过多个周期只触发一次。
* ⬜ 对象池复用计时单元、不受 timeScale 影响的任务。

### 3.2 数据层

#### 全局数据（GameContext）✅

运行时状态的单一事实来源（黑板书）。

* 一张 `Dictionary<string, object>`，存当前 HP、当前房间、已解锁技能等跨模块共享的值。
* `Get` 随时读最新值；`Set` 写值并通过事件中心广播（事件名加 `ctx.` 前缀）；`Subscribe` 订阅变化，带 owner 版本供 UI 面板随对象退订。
* key 由业务层常量类 `GameKeys` 定义，框架不出现游戏概念；约定同一 key 只用一种类型。
* 与事件的分工：事件管「发生了什么事」，GameContext 管「现在是什么状态」。
* ⬜ 存档导出 / 恢复（等 Save 模块）。

#### 配置（Config）✅

把数值与规则从代码里挪到表格。

* 配置表用 CSV：第一行表头，第一列 id，字段内不出现英文逗号，放在 `Assets/Resources/Configs/`。
* 解析与查询在 Core（`ConfigTable` / `ConfigRow` / `Config`），读文件在 Runtime（`CsvLoader`）。
* `Config.Instance.Table("Enemies").Get(1001).GetInt("hp")`：表名取表、id 取行、列名取值（int / float / string / bool）。
* 表在启动时注册，运行时只读。
* ⬜ 编辑器校验（重复 id、空引用、非法数值）、热更下发、运行时重载。

#### 本地化（Localization）⬜

把所有「给人看的文字」从代码里抽离。

* 业务只通过 key 取文本（`Loc.Get("ability.doubleJump")`），文本放语言表。
* 按语言分表（zh-CN / en-US …），启动时只加载当前语言，切换后发事件刷新面板。
* 支持占位符（`"造成 {0} 点伤害"`）、key 回退、缺失 key 兜底。
* 中文字形体积大，需 TMP 字形图集按需烘焙与字体回退链；切换语言时字体联动。
* 语言作为设置项，读 Config 默认值、存 Save 持久化。
* 编辑器校验：缺失 key、冗余 key、占位符一致性、漏翻译。
* 与配置的分界：配置管数值与规则，本地化专管文本。

### 3.3 资源层

#### 热更（HotUpdate）⬜

* 资源侧：对比远端版本清单，差量下载 AssetBundle 到本地缓存，校验（hash / 签名）后切换。
* 逻辑侧：通过 XLua 加载 Lua 脚本，热修复 UI、配置、数值、关卡逻辑。
* 负责版本号、下载进度、失败回滚与本地缓存清理。
* 约束：核心战斗与性能敏感逻辑仍用 C#。

#### 资源（Resource）✅

对 `Resources` 的薄封装，当前阶段不走 AB / Addressables。

* `Load<T>(path)` 同步、`LoadAsync<T>(path, cb)` 异步、`LoadAll<T>(folder)` 整个文件夹；路径相对 `Assets/Resources/`，不带扩展名。
* 一层缓存，键为「类型名:路径」，避免同名路径的不同类型资源互相覆盖。
* 异步回调保证晚于当前调用栈（缓存命中也先等一帧），不会时而同步时而异步。
* 加载失败 `Log.Error` 并返回 null。
* `Clear()` 清空缓存并调 `UnloadUnusedAssets`，切房间 / 过场时统一释放。
* 迁移点：换 AB 时只替换模块内部实现，业务侧的 `Load<T>(path)` 调用不变。
* 不做：引用计数、句柄、单资源释放、缓存容量上限。

#### 对象池（Pool）✅

* `Get(path, 位置, 旋转)` 取出、`Release(obj)` 归还，路径与 Resource 一致。
* 两个字典：`路径 → 空闲对象栈`，以及 `使用中对象 → 它来自哪个路径`。后者让 `Release` 不用传路径，也能识别重复归还。
* 归池时复原：停用、复位变换、清零 Rigidbody2D 速度；对象挂回池对象下，跨场景保留。
* `Clear()` 只销毁空闲对象，使用中的对象随场景一并销毁，只清理失效记录。
* 不做：预热、容量上限、缩容、取出/放回回调、泛型 `Pool<T>`。

### 3.4 服务层

#### 输入（Input）✅

对 project-wide Input Actions 的薄封装，业务只读逻辑动作。

* 底层用 `Assets/InputSystem\_Actions.inputactions`（Player 表已配好 Move / Jump / Attack / Interact / Crouch / Sprint），键鼠与手柄绑定由 Input System 负责。
* `InputReader.Instance` 提供 `MoveAxis`、`JumpPressed`、`JumpHeld`。
* `Lock()` / `Unlock()` 控制输入屏蔽，按层数计数，所有使用方解锁后才恢复。
* 类名用 `InputReader` 避免与 `UnityEngine.Input` 冲突。
* ⬜ 输入缓冲（input buffer）、按键重映射与配置持久化。

#### 音频（Audio）⬜

* BGM 支持淡入淡出、跨场景延续、按区域切换。
* SFX 走 AudioSource 池，限制同帧同名音效数量避免爆音，支持 2D 空间衰减。
* 按总线分组（Master / BGM / SFX / UI），统一音量与静音设置并写入存档。
* 战斗事件直接触发音效，业务层不持有 AudioSource。

#### UI 框架（UI）✅

* `UIManager` 是业务唯一入口，只做转发：`Register` / `Unregister` / `Show` / `Hide` / `HideAll` / `Back` / `IsShowing` / `GetLayer`。
* 启动时自动创建根 Canvas（ScreenSpaceOverlay，1920×1080 适配，跨场景常驻）与六个层：场景 / HUD / 窗口 / 弹窗 / 提示 / 加载，按此顺序叠放；窗口层与弹窗层是栈式层，其余为平铺层。场景里没有 `EventSystem` 时一并补一个，按钮才能点击。
* 界面由业务实例化后用 `Register(layer, id, screen)` 登记，登记后自动隐藏并挂到层节点下；`Show` 只按 id 转发，不负责加载预制体，加载仍归 Resource / 业务。
* 界面基类 `UIScreen` 统一生命周期：`Show`（写入参数 → `OnPropsSet` → `OnShow` → 播进入动画）→ `Hide`（`OnHide` → 播退出动画 → 停用）；子类只覆写这三个钩子，不自己 `SetActive`。打开 / 隐藏完成广播 `ui.shown` / `ui.hidden`，参数为界面 id。
* 动画是可选的 `ScreenAnim` 组件，留空即直接切换；实现时必须调用 `onFinished`，否则界面不会停用、事件也不会发。连续开关时旧的动画回调会被丢弃，避免退出动画把刚打开的界面关掉。
* 窗口层的 `Hide` 等同于「返回上一步」：弹栈并恢复上一个窗口（带回它打开时的参数）；面板层则是单纯隐藏。`Back()` 先弹窗后窗口逐层退回，没有可关的界面时返回 false，交给上层流程。
* `Hide(false)` / `HideAll(false)` 跳过退出动画、立即停用，供切场景与释放时使用。
* 框架不提供销毁接口：需要回收时先 `Unregister`，再由业务 `Destroy` 界面实例。
* ⬜ 按 id 自动加载界面预制体、异步打开（加载期间显示加载层）、返回键输入接入、界面池化、Toast 排队、内置淡入淡出 / 缩放动画。

#### 存档（Save）⬜

* 需要持久化的模块实现统一接口并注册，由框架集中收集。
* 支持多存档槽、自动存档与手动存档、版本号与迁移。
* 写入用临时文件替换防止崩溃损坏，保留备份槽；设置与进度拆分为独立文件。
* 存档内容：位置、房间、生命/能量、能力、地图探索、Boss、NPC、设置。

#### 场景（Scene）⬜

* 统一同步 / 异步加载、卸载、激活场景，封装 `SceneManager` 与加载进度。
* 供房间（切地图单元）与游戏流程（切 Boot/主菜单/游玩）复用。
* 加载期间配合过场界面。

### 3.5 玩法层

#### 实体（Entity）⬜

* 统一生命周期：Spawn → Init → Show → Update → Hide → Release。
* 与对象池联动：池化对象通过 Entity 接口统一取出 / 放回并重置状态。
* 与事件中心联动：按 owner 自动订阅 / 退订，对象销毁后不漏回调。
* 可选 `EntityComponent` 组件接口（普通组件 + 接口约束，非 ECS）；Player / Enemy / NPC 属业务层，继承 Entity。

#### 房间（Room）⬜

* 房间数据描述：ID、场景、包围盒、门、敌人、存档点、相机边界。
* 负责房间加载 / 卸载、玩家从哪扇门进入、落点与朝向、相机边界切换。
* 切换时配合过场（淡入淡出、冻结输入）。
* 对外提供当前房间、已探索房间与连通关系，供地图与存档使用。

#### 相机（Camera）⬜

* 跟随平滑、房间边界约束、镜头过渡、锁定战斗区域（Boss 房锁镜头）。
* 进入房间时切换相机边界。
* 震屏由事件中心驱动，业务不直接操作相机。

#### 游戏流程（GameFlow）⬜

* 流程：Boot → Logo → 标题 → 主菜单 → 选档 → 载入 → 游玩 → 暂停 → 结算。
* 负责场景 / 流程的顶层调度与切换，由全局入口装配后启动。
* 与房间互补：房间管地图单元，流程管应用状态。

## 4\. 模块关系与启动顺序

* 单例与全局入口负责装配，其余模块不互相直接引用。
* 事件中心是模块间的主要通信方式；资源、对象池、音频、UI、输入由业务层直接调用。
* 状态机、计时器为纯模式 / 基础组件，供各模块与业务直接使用。

```
日志与工具 → 事件中心 → 计时器 → 全局数据 → 配置 → 本地化 → 热更 → 资源 → 对象池
→ 输入 → 音频 → UI → 存档 → 场景 → 实体 → 房间 → 相机 → 游戏流程
```

关闭时逆序释放。当前 `GameEntry` 已注册：日志 → 事件中心 → 计时器 → 全局数据 → 配置 → 输入 → UI。

## 5\. 项目文件结构

```
Assets/
├── Scripts/                          # 全部 C# 代码
│   ├── GameFramework/                # 框架层（业务无关，可整体迁移）
│   │   ├── Core/                     # ── 程序集 GameFramework.Core ──
│   │   │   ├── Singleton/            # 单例基类
│   │   │   ├── Log/                  # 日志（ILogSink 输出端抽象）
│   │   │   ├── Utils/                # 通用工具
│   │   │   ├── Event/                # 事件中心
│   │   │   ├── FSM/                  # 泛型状态机
│   │   │   ├── Timer/                # 计时器
│   │   │   ├── GameContext/          # 全局数据
│   │   │   ├── Config/               # 配置（解析与查询）
│   │   │   └── Localization/         # 本地化
│   │   ├── Runtime/                  # ── 程序集 GameFramework.Runtime ──
│   │   │   ├── Singleton/            # MonoBehaviour 单例基类
│   │   │   ├── Log/                  # UnityLogSink
│   │   │   ├── GameEntry/            # 全局入口、生命周期装配、逐帧驱动
│   │   │   ├── Config/               # CsvLoader（读文件）
│   │   │   ├── Resource/             # 资源（Resources 封装）
│   │   │   ├── HotUpdate/            # 热更
│   │   │   ├── Pool/                 # 对象池
│   │   │   ├── Input/                # 输入
│   │   │   ├── Audio/                # 音频
│   │   │   ├── UI/                   # UI 框架
│   │   │   ├── Save/                 # 存档
│   │   │   ├── Scene/                # 场景
│   │   │   ├── Entity/               # 实体
│   │   │   ├── Room/                 # 房间
│   │   │   ├── Camera/               # 相机
│   │   │   └── GameFlow/             # 游戏流程
│   │   └── Editor/                   # ── 程序集 GameFramework.Editor（仅编辑器）──
│   └── Game/                         # 业务层 ── 程序集 Game ──
│       ├── GameKeys.cs               # 全局数据 key 常量
│       ├── Player/                   # 玩家
│       ├── Enemy/                    # 敌人
│       ├── Combat/                   # 战斗
│       ├── Map/                      # 地图
│       ├── Progression/              # 成长
│       └── Narrative/                # 叙事
├── Art/                              # 内容层：美术
├── Audio/                            # 内容层：音频资源
├── Prefabs/                          # 内容层：预制体
├── Configs/                          # 内容层：配置表
├── Lua/                              # 内容层：Lua 热更脚本
├── Scenes/                           # 场景
└── Settings/                         # 渲染管线等设置
```

程序集依赖方向（单向，不可逆）：

```
Game ──► GameFramework.Runtime ──► GameFramework.Core ──► Unity 引擎
                    │
                    └──► GameFramework.Editor（仅编辑器，运行时不可见）
```

* `autoReferenced` 均为 `false`，程序集需显式声明引用。
* 框架层不引用业务层，编译期即强制「框架不依赖业务」。

## 6\. 开发顺序

|阶段|模块|验收标准|
|-|-|-|
|P0|单例、日志与工具、事件中心、状态机、计时器、全局数据、配置、资源、对象池、输入、UI、场景、实体、房间、相机、游戏流程|能启动、能加载、能开面板、能切房间|
|P1|音频、存档、本地化|能存能读、音效受音量设置控制、文本可切换|
|P2|热更|能检测版本、下载并加载新资源|

## 7\. 当前进度

已实现 11 个模块：

* **基础层**：单例、日志与工具、事件中心、状态机、计时器
* **数据层**：全局数据、配置
* **资源层**：资源（Resources）、对象池
* **服务层**：输入、UI

装配层：`IModule` 统一生命周期、`GameEntry` 单列表注册与逐帧驱动。

进行中 / 待办：

* \[ ] P0 剩余：场景、实体、房间、相机、游戏流程
* \[ ] P1：音频、存档、本地化
* \[ ] P2：热更
* \[ ] `GameFramework.Core.Tests` EditMode 单测
* \[ ] 接入业务层验证

