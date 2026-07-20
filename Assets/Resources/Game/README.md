# Game resources

将 `GameSkin` ScriptableObject 放到此目录后命名为：

`Assets/Resources/Game/GameSkin.asset`

创建方式（Unity 内）：
1. 右键 `Assets/Resources/Game`
2. Create → Game → Game Skin
3. 文件名改为 `GameSkin`
4. 在 Inspector 拖入正式 Sprite（可留空，运行时回退 PrototypeSprites）

代码通过 `Resources.Load<GameSkin>("Game/GameSkin")` 加载；缺失时自动 CreateInstance 兜底，游戏仍可启动。
