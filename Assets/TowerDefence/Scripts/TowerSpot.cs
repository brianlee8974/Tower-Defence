using UnityEngine;

public class TowerSpot : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spotRenderer;
    [SerializeField] private Sprite highlightedSprite;

    public Tower Tower { get; private set; }

    private Sprite normalSprite;

    private void Awake()
    {
        if (spotRenderer == null) spotRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spotRenderer != null) normalSprite = spotRenderer.sprite;
    }

    public void SetTower(Tower tower)
    {
        Tower = tower;
        if (spotRenderer == null) return;

        spotRenderer.enabled = tower == null;
        if (tower == null) spotRenderer.sprite = normalSprite;
    }

    public void SetHighlighted(bool highlighted)
    {
        if (spotRenderer == null) return;
        if (Tower != null)
        {
            spotRenderer.enabled = false;
            return;
        }
        spotRenderer.enabled = true;
        spotRenderer.sprite = highlighted && highlightedSprite != null ? highlightedSprite : normalSprite;
    }
}
