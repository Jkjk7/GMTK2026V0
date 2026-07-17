using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 右侧商店面板占位。
/// 仅绘制约 5~6 个空槽视觉，不实现购买逻辑。
/// </summary>
public class ShopPanelPlaceholder : MonoBehaviour
{
    public const int PlaceholderSlotCount = 6;

    /// <summary>
    /// 在给定父节点下创建占位槽 UI。
    /// </summary>
    public void Build(Transform parent, Font font)
    {
        for (int i = 0; i < PlaceholderSlotCount; i++)
        {
            var slot = new GameObject($"ShopSlot_{i}");
            slot.transform.SetParent(parent, false);
            var rt = slot.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120f, 56f);

            var img = slot.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.15f, 0.85f);

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(slot.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = font;
            text.text = $"商店 {i + 1}";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.55f, 0.55f, 0.6f, 1f);
            text.fontSize = 14;
            var trt = text.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
        }
    }
}
