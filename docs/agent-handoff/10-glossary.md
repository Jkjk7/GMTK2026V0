# 10 — 术语表（中英 / 代码名）

| 玩家向中文 | 代码 / 英文 | 含义 |
|------------|-------------|------|
| 沙漏 / 沙子 | SandClock, sand ms | 生命资源 |
| 熔炉 | Emitter | 抽沙射球装置 |
| 能量球 / 光球 | EnergyBall | 投能载体 |
| 收束器 | Redirector | 90° 路径 |
| 拐弯版 | Bent / PathShape.Bent | 功能模块 L 形 |
| 查理激光塔 | Projectile | 单体激光 |
| 大卫炸弹塔 | Bomb | 最左 AOE |
| 雪花发射塔 | IceLaser | 寒控制 |
| 火花发射塔 | Spark | 灼烧弹 |
| 比特币采矿机 | Miner | 能换金 |
| 黑洞发射器 | BlackHole | 聚怪 |
| 火焰增幅 | FlameAmp | 全局灼烧加伤 |
| 分裂器 | Splitter | T 形一分二 |
| 传送门 | Portal | 成对传送 |
| 中续器 | Relay | 吸能后续命 |
| 加速器 | Accelerator | 球速×1.5 |
| 核聚变 | Fusion | 5 合 1 |
| 核裂变 | Fission | 能裂多球 |
| 火附魔 / 惊喜 | FireEnchant / Surprise | 种子附魔格 |
| 热浪 | Heatwave | 全屏灼烧 |
| 准备阶段 | Preparing | 可经营，不抽沙 |
| 破防 / 漏怪 | Breach | 敌人到终点 |
| 沙 buff | SandBuff | 附着强化怪 |
| 融化 | Melt | 烧寒反应 |
| 灼烧 / 寒冷 | Burn / Chill(Slow) | DoT / 减速 |
| 附魔格 | CellEnchant | 格增益/减益 |
| 诅咒格 | Cursed cell | 不可建造 |
| 束缚 | Curse (blessing pair) | 每5波负面 |
| 祝福 | Blessing | 每5波正面 |
| 草稿 / 三选一 | Draft | Unlock/Emitter/Bless |
| 商店池 | RunModulePool | 本局可售类型 |
| 投入金 | InvestedGold | 分解基数 |
| 阶段 stage | (wave-1)//5 | 物价与罚沙档 |
| 可建造窗 | BoardExpand | 3/5/7 |
| 分解 | Scrap | 右上回收 |

## 坐标与方向

- 棋盘：(0,0) 左下；Col→右；Row→上。
- GridDirection：Up/Right/Down/Left。
- 入口：球从熔炉向 **Right** 进入 (0,3)。
- 战斗：敌人 X 递减走向终点。
