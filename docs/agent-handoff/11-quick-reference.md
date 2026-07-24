# 11 — 一页速查（打印级）

## 启动
`GameBootstrap` → 世界+UI → `WaveManager` 波1准备。

## 胜负
沙=0败；波25清完且沙>0胜。

## 核心公式
输出 ≈ 出球率 × 每球投能次数 × 伤/能  
生命 = 沙毫秒；战斗抽沙≈1000ms/s。

## 改难度
`WaveSpawnBudget.cs` 六张表（红黄蓝数量、沙buff、HP、间隔）。

## 改模块接线
Type → 实现类 → Catalog → Pricing → Placement Create/Ghost → Unlock? → Tooltip? → ClearEnergy?

## 合成
同型同级升级；收束器↔{门/中续/加速/聚变/裂变}→Bent。

## 草稿
每3波模块；每5波熔炉+祝福束缚（顺序：模块→熔炉→祝福）。

## 开局池
激光、收束器、雪花、火花。

## 真相优先级
代码 ≫ `docs/agent-handoff/` ≫ 旧 docs / skill 快照。
