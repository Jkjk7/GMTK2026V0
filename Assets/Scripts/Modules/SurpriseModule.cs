using UnityEngine;

/// <summary>
/// 惊喜：按卡牌实例种子对固定格写入固定种类附魔；移动不改布局。
/// </summary>
public class SurpriseModule : FireEnchantModule
{
    public override ModuleType ModuleType => global::ModuleType.Surprise;

    protected override CellEnchant GetKindForIndex(int index)
    {
        return EnchantSeedUtil.RollKind(Cell, ModuleType, InstanceSeed, index);
    }

    public override void RefreshVisual()
    {
        base.RefreshVisual();
        var body = GetComponent<SpriteRenderer>();
        if (body != null)
        {
            body.color = ModuleCatalog.GetDisplayColor(ModuleType);
        }
    }
}
