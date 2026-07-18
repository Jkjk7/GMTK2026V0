using UnityEngine;

/// <summary>
/// 布局引用集中点：世界锚点 + UI 控制器。由 Bootstrap 运行时组装或将来由 Prefab 注入。
/// </summary>
public sealed class GameLayoutView : MonoBehaviour
{
    [Header("World")]
    public Camera worldCamera;
    public Transform worldRoot;
    public Transform battleRoot;
    public Transform boardRoot;
    public Transform gridAnchor;
    public Transform emitterAnchor;
    public Transform mageAnchor;
    public Transform enemySpawnAnchor;
    public Transform enemyEndAnchor;
    public Transform enemyRoot;
    public Transform moduleRoot;

    [Header("UI")]
    public Canvas canvas;
    public HandController handController;
    public ShopController shopController;
    public CombatHUD combatHud;
    public CanvasGroup resultOverlay;
}
