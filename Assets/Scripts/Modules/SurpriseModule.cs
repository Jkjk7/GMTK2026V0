using UnityEngine;

/// <summary>
/// 惊喜：按种子对若干格写入随机种类附魔；同位置结果固定。
/// </summary>
public class SurpriseModule : FireEnchantModule
{
    public override ModuleType ModuleType => global::ModuleType.Surprise;

    protected override CellEnchant GetKindForIndex(int index)
    {
        return EnchantSeedUtil.RollKind(Cell, ModuleType, Salt, index);
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
