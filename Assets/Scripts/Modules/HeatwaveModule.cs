using UnityEngine;

/// <summary>
/// 热浪：容20/耗20；射速间隔与灼烧时长随等级；全屏 [灼烧] + 战场红闪。
/// </summary>
public class HeatwaveModule : ModuleBase
{
    public const int EnergyCap = 20;
    public const int EnergyCost = 20;

    [SerializeField] int currentEnergy;
    float _cooldown;
    SpriteRenderer _body;
    SpriteRenderer _hudFill;
    TextMesh _levelLabel;
    ModuleCooldownHud _cooldownHud;

    public override ModuleType ModuleType => global::ModuleType.Heatwave;
    public int CurrentEnergy => currentEnergy;
    public int EnergyCapacity => EnergyCap;

    float FireInterval => ModuleCatalog.GetHeatwaveFireInterval(ModuleLevel);

    public void ClearEnergy()
    {
        currentEnergy = 0;
        _cooldown = 0f;
        ClearEnergyResidue();
        RefreshVisual();
    }

    public override void ApplyCardData(ModuleCardData data)
    {
        base.ApplyCardData(data);
        currentEnergy = Mathf.Min(currentEnergy, EnergyCap);
        EnsureLevelLabel(data.Level);
        RefreshVisual();
    }

    void Update()
    {
        float cdTotal = FireInterval * EnchantFireIntervalMultiplier;
        if (_cooldown > 0f)
        {
            _cooldown -= Time.deltaTime;
        }

        if (_cooldownHud == null)
        {
            _cooldownHud = ModuleCooldownHud.Ensure(transform, new Vector3(0f, 1.15f, 0f));
        }

        _cooldownHud.SetCooldown(_cooldown > 0f ? _cooldown : 0f, cdTotal);

        if (currentEnergy < EnergyCost || _cooldown > 0f)
        {
            return;
        }

        Fire();
    }

    void Fire()
    {
        currentEnergy -= EnergyCost;
        _cooldown = FireInterval * EnchantFireIntervalMultiplier;
        float duration = ModuleCatalog.GetHeatwaveBurnDuration(ModuleLevel);
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy e = enemies[i];
            if (e != null && e.IsAlive)
            {
                e.ApplyBurn(duration);
            }
        }

        HeatwaveFlash.Play();
        RefreshVisual();
    }

    public override void OnBallEnter(EnergyBall ball)
    {
        if (ball == null)
        {
            return;
        }

        currentEnergy = AbsorbBallEnergy(ball, currentEnergy, EnergyCap);
        RefreshVisual();
    }

    public override void RefreshVisual()
    {
        EnsureVisual();
        EnsureHud();
        if (_body != null)
        {
            _body.color = ModuleCatalog.GetDisplayColor(ModuleType);
        }

        if (_hudFill != null)
        {
            float t = EnergyCap > 0 ? currentEnergy / (float)EnergyCap : 0f;
            _hudFill.transform.localScale = new Vector3(Mathf.Clamp01(t), 1f, 1f);
            _hudFill.color = _cooldown > 0f
                ? new Color(0.5f, 0.5f, 0.5f, 1f)
                : new Color(1f, 0.35f, 0.2f, 1f);
        }
    }

    void EnsureVisual()
    {
        if (_body != null)
        {
            return;
        }

        _body = gameObject.GetComponent<SpriteRenderer>();
        if (_body == null)
        {
            _body = gameObject.AddComponent<SpriteRenderer>();
        }

        _body.sprite = PrototypeSprites.Square;
        _body.sortingOrder = 8;
        transform.localScale = Vector3.one * 0.7f;
    }

    void EnsureHud()
    {
        if (_hudFill != null)
        {
            return;
        }

        var bg = new GameObject("EnergyHud");
        bg.transform.SetParent(transform, false);
        bg.transform.localPosition = new Vector3(0f, -0.65f, 0f);
        bg.transform.localScale = new Vector3(1f, 0.16f, 1f);
        var bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = PrototypeSprites.Square;
        bgSr.color = new Color(0.1f, 0.1f, 0.12f, 0.85f);
        bgSr.sortingOrder = 11;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(bg.transform, false);
        fill.transform.localScale = new Vector3(0f, 1f, 1f);
        _hudFill = fill.AddComponent<SpriteRenderer>();
        _hudFill.sprite = PrototypeSprites.Square;
        _hudFill.color = new Color(1f, 0.35f, 0.2f, 1f);
        _hudFill.sortingOrder = 12;
    }

    void EnsureLevelLabel(int level)
    {
        if (level <= 1)
        {
            if (_levelLabel != null)
            {
                _levelLabel.gameObject.SetActive(false);
            }

            return;
        }

        if (_levelLabel == null)
        {
            var go = new GameObject("LevelLabel");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            go.transform.localScale = new Vector3(0.08f, 0.08f, 1f);
            _levelLabel = go.AddComponent<TextMesh>();
            _levelLabel.anchor = TextAnchor.MiddleCenter;
            _levelLabel.fontSize = 40;
            _levelLabel.color = Color.white;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = 12;
            }
        }

        _levelLabel.gameObject.SetActive(true);
        _levelLabel.text = $"Lv{level}";
    }
}

/// <summary>热浪全屏红闪（短命 Sprite）。</summary>
public static class HeatwaveFlash
{
    public static void Play(float duration = 0.3f)
    {
        var go = new GameObject("HeatwaveFlash");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PrototypeSprites.Square;
        sr.color = new Color(1f, 0.15f, 0.08f, 0.35f);
        sr.sortingOrder = 80;
        Camera cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            float h = cam.orthographicSize * 2f;
            float w = h * cam.aspect;
            go.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
            go.transform.localScale = new Vector3(w * 1.1f, h * 1.1f, 1f);
        }
        else
        {
            go.transform.localScale = new Vector3(40f, 24f, 1f);
        }

        Object.Destroy(go, duration);
    }
}
