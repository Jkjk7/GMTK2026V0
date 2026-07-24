using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 分解区下方：默认只显示「查看已有增幅」按钮，展开后向下列出本局属性。
/// </summary>
public class RunStatsHud : MonoBehaviour
{
    Text _detail;
    Button _toggle;
    GameObject _panel;
    RectTransform _rootRt;
    RectTransform _panelRt;
    GridBoard _board;
    bool _expanded;
    Vector2 _collapsedMin;
    Vector2 _collapsedMax;
    Vector2 _expandedMin;
    Vector2 _expandedMax;

    public void Bind(
        RectTransform rootRt,
        Text detail,
        Button toggle,
        GameObject panel,
        RectTransform panelRt,
        GridBoard board,
        Vector2 collapsedMin,
        Vector2 collapsedMax,
        Vector2 expandedMin,
        Vector2 expandedMax)
    {
        _rootRt = rootRt;
        _detail = detail;
        _toggle = toggle;
        _panel = panel;
        _panelRt = panelRt;
        _board = board;
        _collapsedMin = collapsedMin;
        _collapsedMax = collapsedMax;
        _expandedMin = expandedMin;
        _expandedMax = expandedMax;

        if (_toggle != null)
        {
            _toggle.onClick.AddListener(ToggleExpanded);
        }

        if (RunModifiers.Instance != null)
        {
            RunModifiers.Instance.Changed += Refresh;
        }

        SetExpanded(false);
        Refresh();
    }

    void OnDestroy()
    {
        if (RunModifiers.Instance != null)
        {
            RunModifiers.Instance.Changed -= Refresh;
        }
    }

    void ToggleExpanded()
    {
        SetExpanded(!_expanded);
    }

    void SetExpanded(bool expanded)
    {
        _expanded = expanded;
        if (_panel != null)
        {
            _panel.SetActive(expanded);
        }

        if (_rootRt != null)
        {
            if (expanded)
            {
                _rootRt.anchorMin = _expandedMin;
                _rootRt.anchorMax = _expandedMax;
            }
            else
            {
                _rootRt.anchorMin = _collapsedMin;
                _rootRt.anchorMax = _collapsedMax;
            }

            _rootRt.offsetMin = Vector2.zero;
            _rootRt.offsetMax = Vector2.zero;
        }

        if (_toggle != null)
        {
            var label = _toggle.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = expanded ? "收起增幅" : "查看已有增幅";
            }

            var btnRt = _toggle.transform as RectTransform;
            if (btnRt != null)
            {
                if (expanded)
                {
                    // 展开时按钮贴在面板顶部
                    float totalH = _expandedMax.y - _expandedMin.y;
                    float btnH = _collapsedMax.y - _collapsedMin.y;
                    float btnNorm = totalH > 0.0001f ? btnH / totalH : 0.2f;
                    btnRt.anchorMin = new Vector2(0f, 1f - btnNorm);
                    btnRt.anchorMax = Vector2.one;
                }
                else
                {
                    btnRt.anchorMin = Vector2.zero;
                    btnRt.anchorMax = Vector2.one;
                }

                btnRt.offsetMin = Vector2.zero;
                btnRt.offsetMax = Vector2.zero;
            }
        }

        if (_panelRt != null && expanded)
        {
            float totalH = _expandedMax.y - _expandedMin.y;
            float btnH = _collapsedMax.y - _collapsedMin.y;
            float gap = 0.006f;
            float topCut = totalH > 0.0001f ? (btnH + gap) / totalH : 0.25f;
            _panelRt.anchorMin = new Vector2(0.04f, 0.04f);
            _panelRt.anchorMax = new Vector2(0.96f, 1f - topCut);
            _panelRt.offsetMin = Vector2.zero;
            _panelRt.offsetMax = Vector2.zero;
        }

        Refresh();
    }

    public void Refresh()
    {
        if (_detail == null || !_expanded)
        {
            return;
        }

        RunModifiers mod = RunModifiers.Instance;
        int burn = mod != null ? mod.GetBurnDamagePerTick() : RunModifiers.BaseBurnDamagePerTick;
        int burnExtra = mod != null ? mod.BurnDamageBonus + mod.FlameAmpBonus : 0;
        float aoe = mod != null ? mod.AoeRadiusMult : 1f;
        float speed = mod != null ? mod.EnemySpeedMult : 1f;
        int enchants = _board != null ? _board.CountEnchants() : 0;

        _detail.text =
            $"灼烧：{RunModifiers.BaseBurnDamagePerTick}+{burnExtra} = {burn} / {RunModifiers.BurnTickInterval:0.#}秒\n" +
            $"爆炸/吸引范围：×{aoe:0.##}\n" +
            $"敌人移速：×{speed:0.##}\n" +
            $"附魔格：{enchants}";
    }
}
