using System.Collections;
using UnityEngine;

public enum TowerKind { Archer, Cannon, Magician }

public class Tower : MonoBehaviour
{
    private const int RangeTextureSize = 128;
    private const float RangeSpriteSize = RangeTextureSize / 100f;
    private static Sprite sharedRangeSprite;

    [SerializeField] private TowerKind kind;
    [SerializeField] private float range = 2f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float shotsPerSecond = 1f;
    [SerializeField] private float splashRadius = 0.8f;
    [SerializeField] private int purchaseCost = 75;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite towerBaseSprite;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite[] attackFrames;
    [SerializeField, Min(0.1f)] private float attackFrameRate = 10f;
    private float cooldown;
    private Coroutine animationRoutine;
    private Vector3 characterRestLocalPosition;
    private SpriteRenderer rangeRenderer;

    public TowerKind Kind => kind;
    public int PurchaseCost => purchaseCost;
    public void SetPurchaseCost(int value) => purchaseCost = value;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null) return;
        if (idleSprite == null) idleSprite = spriteRenderer.sprite;

        SpriteRenderer originalRenderer = spriteRenderer;
        CreateCompositeVisuals(originalRenderer);
        characterRestLocalPosition = spriteRenderer.transform.localPosition;

        BoxCollider2D selectionCollider = GetComponent<BoxCollider2D>();
        if (selectionCollider != null)
        {
            selectionCollider.offset = new Vector2(-0.035f, 0.63f);
            selectionCollider.size = new Vector2(0.85f, 1.75f);
        }
    }

    private void CreateCompositeVisuals(SpriteRenderer originalRenderer)
    {
        if (towerBaseSprite != null)
        {
            GameObject baseObject = new GameObject("TowerBaseVisual");
            baseObject.transform.SetParent(transform, false);
            baseObject.transform.localPosition = new Vector3(-0.125f, 0.255f, 0f);
            SpriteRenderer baseRenderer = baseObject.AddComponent<SpriteRenderer>();
            CopyRendererStyle(originalRenderer, baseRenderer);
            baseRenderer.sprite = towerBaseSprite;
            baseRenderer.sortingOrder = originalRenderer.sortingOrder;
        }

        GameObject characterObject = new GameObject("TowerCharacterVisual");
        characterObject.transform.SetParent(transform, false);
        characterObject.transform.localPosition = GetCharacterOffset();
        SpriteRenderer characterRenderer = characterObject.AddComponent<SpriteRenderer>();
        CopyRendererStyle(originalRenderer, characterRenderer);
        characterRenderer.sprite = idleSprite;
        characterRenderer.sortingOrder = originalRenderer.sortingOrder + 1;

        // The prefab's original renderer is replaced by the offset character renderer.
        originalRenderer.enabled = false;
        spriteRenderer = characterRenderer;
    }

    private Vector3 GetCharacterOffset()
    {
        // Offsets are relative to the center of the Pre Tower sign, converted at 100 pixels/unit.
        switch (kind)
        {
            case TowerKind.Archer: return new Vector3(-0.05f, 1.005f, 0f);
            case TowerKind.Cannon: return new Vector3(0.015f, 1.08f, 0f);
            case TowerKind.Magician: return new Vector3(-0.06f, 1.005f, 0f);
            default: return new Vector3(0f, 1.1f, 0f);
        }
    }

    private static void CopyRendererStyle(SpriteRenderer source, SpriteRenderer destination)
    {
        destination.sharedMaterial = source.sharedMaterial;
        destination.color = source.color;
        destination.sortingLayerID = source.sortingLayerID;
        destination.sortingOrder = source.sortingOrder;
        destination.flipX = source.flipX;
        destination.flipY = source.flipY;
        destination.maskInteraction = source.maskInteraction;
    }

    private void Update()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (idleSprite == null && spriteRenderer != null) idleSprite = spriteRenderer.sprite;
        cooldown -= Time.deltaTime;
        if (cooldown > 0) return;
        Enemy target = FindTarget();
        if (target == null) return;
        Fire(target);
        cooldown = 1f / Mathf.Max(0.1f, shotsPerSecond);
    }

    private Enemy FindTarget()
    {
        Enemy best = null;
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (Vector2.Distance(transform.position, enemy.transform.position) > range) continue;
            if (best == null || enemy.PathProgress > best.PathProgress) best = enemy;
        }
        return best;
    }

    private void Fire(Enemy target)
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = target.transform.position.x < transform.position.x;

        // Recoil opposite the facing direction: away from the enemy being attacked.
        float recoilDirection = spriteRenderer != null && spriteRenderer.flipX ? 1f : -1f;
        PlayAttackAnimation(recoilDirection);
        float scaledDamage = damage * (GameManager.Instance != null ? GameManager.Instance.DamageMultiplier : 1f);
        if (kind == TowerKind.Cannon)
        {
            foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                if (Vector2.Distance(target.transform.position, enemy.transform.position) <= splashRadius)
                    enemy.TakeDamage(enemy == target ? scaledDamage : scaledDamage * 0.5f);
        }
        else target.TakeDamage(scaledDamage);
    }

    public void ApplyUpgrade(float damageIncrease, float rangeIncrease = 0f)
    {
        damage += damageIncrease;
        range += rangeIncrease;
    }

    public void ShowRange(bool show)
    {
        if (show && rangeRenderer == null) CreateRangeVisual();
        if (rangeRenderer == null) return;

        rangeRenderer.transform.localScale = Vector3.one * (range * 2f / RangeSpriteSize);
        rangeRenderer.enabled = show;
    }

    private void CreateRangeVisual()
    {
        GameObject rangeObject = new GameObject("RangeOverlay");
        rangeObject.transform.SetParent(transform, false);
        rangeObject.transform.localPosition = Vector3.zero;
        rangeRenderer = rangeObject.AddComponent<SpriteRenderer>();
        rangeRenderer.sprite = GetRangeSprite();
        rangeRenderer.color = new Color(0.55f, 0.55f, 0.55f, 0.38f);
        rangeRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        rangeRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        rangeRenderer.enabled = false;
    }

    private static Sprite GetRangeSprite()
    {
        if (sharedRangeSprite != null) return sharedRangeSprite;

        Texture2D texture = new Texture2D(RangeTextureSize, RangeTextureSize, TextureFormat.RGBA32, false);
        texture.name = "Runtime Tower Range Circle";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[RangeTextureSize * RangeTextureSize];
        Vector2 center = new Vector2((RangeTextureSize - 1) * 0.5f, (RangeTextureSize - 1) * 0.5f);
        float radius = RangeTextureSize * 0.5f - 1f;
        for (int y = 0; y < RangeTextureSize; y++)
        {
            for (int x = 0; x < RangeTextureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                pixels[y * RangeTextureSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        sharedRangeSprite = Sprite.Create(texture, new Rect(0, 0, RangeTextureSize, RangeTextureSize),
            new Vector2(0.5f, 0.5f), 100f);
        sharedRangeSprite.name = "Runtime Tower Range Circle";
        return sharedRangeSprite;
    }

    private void PlayAttackAnimation(float recoilDirection)
    {
        if (spriteRenderer != null && attackFrames != null && attackFrames.Length > 0)
        {
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            spriteRenderer.transform.localPosition = characterRestLocalPosition;
            animationRoutine = StartCoroutine(PlaySpriteFrames(recoilDirection));
        }
        else if (animator != null) animator.SetTrigger("Attack");
    }

    private IEnumerator PlaySpriteFrames(float recoilDirection)
    {
        float frameDuration = 1f / Mathf.Max(0.1f, attackFrameRate);
        for (int i = 0; i < attackFrames.Length; i++)
        {
            Sprite frame = attackFrames[i];
            if (frame == null) continue;
            if (kind == TowerKind.Cannon && i >= attackFrames.Length - 2)
                spriteRenderer.transform.localPosition = characterRestLocalPosition +
                    new Vector3(recoilDirection * 0.1f, 0f, 0f);
            spriteRenderer.sprite = frame;
            yield return new WaitForSeconds(frameDuration);
        }
        spriteRenderer.transform.localPosition = characterRestLocalPosition;
        spriteRenderer.sprite = idleSprite;
        animationRoutine = null;
    }
}
