using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    private static Sprite healthBarPixelSprite;

    [SerializeField] private Slider healthBar;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField, Min(0f)] private float healthBarGap = 0.08f;
    [SerializeField, Min(0.01f)] private float healthBarHeight = 0.06f;
    [SerializeField, Min(0f)] private float healthBarSidePadding = 0.04f;
    private Transform[] waypoints;
    private int waypointIndex;
    private float health;
    private float maxHealth;
    private int reward;
    private float movementSpeed;
    private bool resolved;
    private Transform healthBarRoot;
    private Transform healthBarFill;
    private float healthBarLocalWidth;
    private float healthBarLocalHeight;

    public float PathProgress => waypointIndex;
    public float HealthFraction => maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 1f;

    public void Initialize(Transform[] route, float hp, float speed, int cash, Sprite sprite, float size)
    {
        waypoints = route;
        health = hp;
        maxHealth = hp;
        movementSpeed = speed;
        reward = cash;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && sprite != null) spriteRenderer.sprite = sprite;
        transform.localScale = Vector3.one * size;
        if (healthBar != null)
        {
            healthBar.maxValue = hp;
            healthBar.value = hp;
        }
        else if (spriteRenderer != null)
        {
            CreateWorldHealthBar(size);
        }
        RefreshHealthBar();
    }

    private void Update()
    {
        if (resolved || waypoints == null || waypoints.Length == 0) return;
        if (waypointIndex >= waypoints.Length) { Escape(); return; }
        transform.position = Vector3.MoveTowards(transform.position, waypoints[waypointIndex].position,
            movementSpeed * Time.deltaTime);
        if (Vector2.Distance(transform.position, waypoints[waypointIndex].position) <= 0.01f)
        {
            waypointIndex++;
            if (waypointIndex >= waypoints.Length) Escape();
        }
    }

    public void TakeDamage(float amount)
    {
        if (resolved) return;
        health -= amount;
        RefreshHealthBar();
        if (health <= 0)
        {
            resolved = true;
            GameManager.Instance?.AddMoney(reward);
            Destroy(gameObject);
        }
    }

    private void Escape()
    {
        if (resolved) return;
        resolved = true;
        GameManager.Instance?.EnemyEscaped();
        Destroy(gameObject);
    }

    private void CreateWorldHealthBar(float enemySize)
    {
        if (healthBarRoot != null || spriteRenderer == null) return;
        if (healthBarPixelSprite == null) healthBarPixelSprite = CreateHealthBarPixelSprite();

        healthBarRoot = new GameObject("HealthBar").transform;
        healthBarRoot.SetParent(transform, false);
        float safeSize = Mathf.Max(0.01f, enemySize);
        float worldWidth = spriteRenderer.bounds.size.x + healthBarSidePadding;
        healthBarLocalWidth = worldWidth / safeSize;
        healthBarLocalHeight = healthBarHeight / safeSize;
        float localTop = spriteRenderer.bounds.extents.y / safeSize + healthBarGap / safeSize;
        healthBarRoot.localPosition = new Vector3(0f, localTop, 0f);

        CreateHealthBarPiece("Background", healthBarRoot, new Color(0.12f, 0.12f, 0.12f, 0.85f),
            healthBarLocalWidth, healthBarLocalHeight, spriteRenderer.sortingOrder + 2);

        GameObject fillObject = new GameObject("Fill");
        healthBarFill = fillObject.transform;
        healthBarFill.SetParent(healthBarRoot, false);
        SpriteRenderer fillRenderer = fillObject.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = healthBarPixelSprite;
        fillRenderer.color = new Color(0.2f, 0.9f, 0.25f, 0.95f);
        fillRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        fillRenderer.sortingOrder = spriteRenderer.sortingOrder + 3;
        RefreshHealthBar();
    }

    private void CreateHealthBarPiece(string objectName, Transform parent, Color color,
        float width, float height, int sortingOrder)
    {
        GameObject piece = new GameObject(objectName);
        piece.transform.SetParent(parent, false);
        piece.transform.localScale = new Vector3(width, height, 1f);
        SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
        renderer.sprite = healthBarPixelSprite;
        renderer.color = color;
        renderer.sortingLayerID = spriteRenderer.sortingLayerID;
        renderer.sortingOrder = sortingOrder;
    }

    private void RefreshHealthBar()
    {
        if (healthBar != null) healthBar.value = Mathf.Clamp(health, 0f, maxHealth);
        if (healthBarFill == null) return;

        float fraction = HealthFraction;
        float fillWidth = healthBarLocalWidth * fraction;
        healthBarFill.localScale = new Vector3(fillWidth, healthBarLocalHeight, 1f);
        healthBarFill.localPosition = new Vector3(-healthBarLocalWidth * 0.5f + fillWidth * 0.5f, 0f, 0f);
    }

    private static Sprite CreateHealthBarPixelSprite()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "Runtime Health Bar Pixel";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        sprite.name = "Runtime Health Bar Pixel";
        return sprite;
    }
}
