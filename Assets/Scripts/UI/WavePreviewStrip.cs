using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>准备阶段：下波怪物类型以色圆居中展示 + 悬停描述。</summary>
public class WavePreviewStrip : MonoBehaviour
{
    const float CircleSize = 52f;
    const float CircleSpacing = 18f;

    Transform _row;
    Text _tooltip;
    readonly List<GameObject> _icons = new List<GameObject>();

    public void Bind(Transform row, Font font, Text tooltip)
    {
        _row = row;
        _tooltip = tooltip;
        if (_tooltip != null)
        {
            _tooltip.gameObject.SetActive(false);
        }

        EnsureRowLayout();
    }

    void EnsureRowLayout()
    {
        if (_row == null)
        {
            return;
        }

        var layout = _row.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            layout = _row.gameObject.AddComponent<HorizontalLayoutGroup>();
        }

        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = CircleSpacing;
        layout.padding = new RectOffset(8, 8, 4, 4);
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childScaleWidth = false;
        layout.childScaleHeight = false;
    }

    public void ShowForWave(int waveDisplay)
    {
        ClearIcons();
        if (_row == null)
        {
            return;
        }

        EnsureRowLayout();
        var present = new List<EnemyGoldType>();
        CollectTypes(waveDisplay, present);
        for (int i = 0; i < present.Count; i++)
        {
            CreateIcon(present[i]);
        }
    }

    public void Hide()
    {
        ClearIcons();
        if (_tooltip != null)
        {
            _tooltip.gameObject.SetActive(false);
        }
    }

    static void CollectTypes(int wave, List<EnemyGoldType> into)
    {
        into.Clear();
        void Add(EnemyGoldType t)
        {
            if (!into.Contains(t))
            {
                into.Add(t);
            }
        }

        if (WaveSpawnBudget.IsBossWave(wave))
        {
            Add(EnemyGoldType.Boss);
            return;
        }

        if (WaveSpawnBudget.GetNormalCount(wave) > 0)
        {
            Add(EnemyGoldType.Normal);
        }

        if (WaveSpawnBudget.GetSwarmCount(wave) > 0)
        {
            Add(EnemyGoldType.Swarm);
        }

        if (WaveSpawnBudget.GetTankCount(wave) > 0)
        {
            Add(EnemyGoldType.Tank);
        }

        if (WaveSpawnBudget.GetDisassemblerCount(wave) > 0)
        {
            Add(EnemyGoldType.Disassembler);
        }

        if (WaveSpawnBudget.GetShieldCasterCount(wave) > 0)
        {
            Add(EnemyGoldType.ShieldCaster);
        }
    }

    void CreateIcon(EnemyGoldType type)
    {
        var go = new GameObject($"EnemyIcon_{type}");
        go.transform.SetParent(_row, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(CircleSize, CircleSize);

        var img = go.AddComponent<Image>();
        img.sprite = PrototypeSprites.Circle;
        img.color = EnemyCatalog.GetPreviewColor(type);
        img.raycastTarget = true;
        img.preserveAspect = true;

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = CircleSize;
        le.preferredHeight = CircleSize;
        le.minWidth = CircleSize;
        le.minHeight = CircleSize;

        var trigger = go.AddComponent<EventTrigger>();
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        EnemyGoldType captured = type;
        enter.callback.AddListener(_ => ShowTip(captured));
        trigger.triggers.Add(enter);
        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => HideTip());
        trigger.triggers.Add(exit);

        _icons.Add(go);
    }

    void ShowTip(EnemyGoldType type)
    {
        if (_tooltip == null)
        {
            return;
        }

        _tooltip.gameObject.SetActive(true);
        _tooltip.text = EnemyCatalog.GetDisplayName(type) + "\n" + EnemyCatalog.GetDescription(type);
    }

    void HideTip()
    {
        if (_tooltip != null)
        {
            _tooltip.gameObject.SetActive(false);
        }
    }

    void ClearIcons()
    {
        for (int i = 0; i < _icons.Count; i++)
        {
            if (_icons[i] != null)
            {
                Destroy(_icons[i]);
            }
        }

        _icons.Clear();
    }
}

/// <summary>敌人类型名/描述/预览色（准备条与提示）。</summary>
public static class EnemyCatalog
{
    public static string GetDisplayName(EnemyGoldType type)
    {
        switch (type)
        {
            case EnemyGoldType.Swarm:
                return GameLocalization.Text("Yellow Swarm", "黄潮");
            case EnemyGoldType.Tank:
                return GameLocalization.Text("Blue Tank", "蓝甲");
            case EnemyGoldType.Disassembler:
                return GameLocalization.Text("Purple Saboteur", "紫拆");
            case EnemyGoldType.ShieldCaster:
                return GameLocalization.Text("Golden Warden", "金盾");
            case EnemyGoldType.Boss:
                return GameLocalization.Text("Boss", "Boss");
            case EnemyGoldType.Elite:
                return GameLocalization.Text("Elite", "精英");
            default:
                return GameLocalization.Text("Red Grunt", "红兵");
        }
    }

    public static string GetDescription(EnemyGoldType type)
    {
        switch (type)
        {
            case EnemyGoldType.Swarm:
                return GameLocalization.Text("Fast, low HP.", "移速快，血量低。");
            case EnemyGoldType.Tank:
                return GameLocalization.Text("Slow, high HP.", "移速慢，血量高。");
            case EnemyGoldType.Disassembler:
                return GameLocalization.Text(
                    "Every 5s: 5s purple channel, then yanks 2 modules to hand.",
                    "每 5 秒吟唱 5 秒，卸下 2 个模块回手牌。");
            case EnemyGoldType.ShieldCaster:
                return GameLocalization.Text(
                    "Shields all foes for 3s (blocks ≤30 dmg). Gold channel + flash.",
                    "吟唱后为全体加 3 秒金盾（免疫 ≤30 伤害）；金色蓄力与闪屏。");
            case EnemyGoldType.Boss:
                return GameLocalization.Text("Final boss with massive HP.", "终局 Boss，超高血量。");
            default:
                return GameLocalization.Text("Standard foe.", "标准敌人。");
        }
    }

    public static Color GetPreviewColor(EnemyGoldType type)
    {
        switch (type)
        {
            case EnemyGoldType.Swarm: return new Color(1f, 0.9f, 0.2f, 1f);
            case EnemyGoldType.Tank: return new Color(0.25f, 0.45f, 1f, 1f);
            case EnemyGoldType.Disassembler: return new Color(0.7f, 0.25f, 0.95f, 1f);
            case EnemyGoldType.ShieldCaster: return new Color(1f, 0.82f, 0.2f, 1f);
            case EnemyGoldType.Boss: return new Color(1f, 0.4f, 0.15f, 1f);
            default: return new Color(0.9f, 0.25f, 0.25f, 1f);
        }
    }
}
