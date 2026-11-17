# DrawCallStats

给 Celeste / Everest 用的诊断 mod：一帧里 SpriteBatch 提交了多少次绘制、有没有合批、GC 和实体数多少。

用来验证「大厅卡顿是不是因为小贴图没走图集、绘制顺序散、SpriteBatch 合不了批」这种猜测。
**它只负责量，不负责修**；卡顿未必是渲染的问题，先把数拿出来。

## 装法

1. 下载 release 里的 zip，解压到 `Celeste/Mods/`，确认路径是 `Mods/DrawCallStats/everest.yaml`。
2. 启动游戏，左上角就有面板。F8 切显隐，F9 把最近 600 帧写进 `log.txt`（也可以把 `AutoDumpLog` 设成 true）。
3. 卡顿复现一遍，按 F9，然后把 `log.txt` 里 `DrawCallStats` 开头的行贴回来。

## 面板读法

```
draw 1432  flush 9  gpu 11
texSwitch 6  tex 4
ent 812  gc0 +0  16.7ms
159.1 sprite/flush (batch ok)
```

| 项 | 含义 |
| --- | --- |
| `draw` | 本帧提交的 sprite 数（SpriteBatch.Draw / DrawString 调用数，FNA 里是 PushSprite 次数） |
| `flush` | 批提交次数：每个 `End()` 一次，sprite 超过 2048 时自动多一次 |
| `gpu` | 真正的 GPU 绘制命令数（一个 flush 里每段连续纹理一次 `DrawIndexedPrimitives`） |
| `texSwitch` | 提交过程中换纹理的次数 |
| `tex` | 本帧用到的不同纹理张数 |
| `ent` | 场景实体数 |
| `gc0` | 本帧 GC gen0 回收次数 |
| `ms` | 帧耗时 |
| `sprite/flush` | 每个 flush 平均装了多少 sprite，越大合批越好 |

## 怎么判断是哪种问题

- `sprite/flush` 很大（几十上百）而 `flush` 很小 → **合批是好的**，卡顿去别处找（实体数、`gc0`、脚本、联机、碰撞）。
- `sprite/flush` 只有个位数、`flush` 接近几百 → 合批确实被打断了，`texSwitch` 通常也很大，属于真的要合并图集/调整绘制顺序。
- `gc0` 每帧都涨 → GC 压力，不是 draw call 的锅。
- `ent` 上万 → 实体/粒子数量问题。
- `gpu` 明显大于 `flush`（比如 3~10 倍）→ 纹理在频繁穿插，`flush` 里被切成很多段。

源码里 Celeste 跑的是 FNA（不是 MonoGame），FNA 没有被 HookGen 处理，所以这里用
`MonoMod.RuntimeDetour` 显式挂 `SpriteBatch.PushSprite` / `SpriteBatch.FlushBatch` /
`GraphicsDevice.DrawIndexedPrimitives`，每个 detour 都带 `DetourConfig` id。
`Monocle` 没有 Debug 组件，面板是一个挂在 `Scene` 上的 `Renderer`。

## 构建

需要一份 Everest 1.4+ 的安装目录（要有 `Celeste.dll` / `FNA.dll` / `MMHOOK_Celeste.dll` /
`MonoMod.*.dll`）：

```
dotnet build Source/DrawCallStats.csproj -c Release -p:CelesteRoot=/path/to/Celeste
```

产物在 `Code/DrawCallStats.dll`。

## 注意

- 面板自己画的那几笔不算进显示值（先定格再画）。
- `draw` 里包含 `SpriteFont` 的每个字形（每个字都是一次 PushSprite，字体纹理通常是共用的一张）。
- 它只统计 SpriteBatch；3D 的 `DrawUserPrimitives` 之类不在 `gpu` 里。
