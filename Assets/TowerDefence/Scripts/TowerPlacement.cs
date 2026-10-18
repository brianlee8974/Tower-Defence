using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TowerPlacement : MonoBehaviour
{
    [Header("Assign the three tower prefabs")]
    [SerializeField] private Tower archerPrefab;
    [SerializeField] private Tower cannonPrefab;
    [SerializeField] private Tower magicianPrefab;
    [SerializeField] private Camera gameCamera;
    [Header("Context panels (optional)")]
    [SerializeField] private GameObject buildPanel;
    [SerializeField] private GameObject towerActionsPanel;
    [SerializeField] private Text upgradeCountLabel;
    [SerializeField] private Text upgradeCostLabel;
    [Header("Button labels (optional)")]
    [SerializeField] private Text archerButtonLabel;
    [SerializeField] private Text cannonButtonLabel;
    [SerializeField] private Text magicianButtonLabel;
    [SerializeField] private Text archerCostLabel;
    [SerializeField] private Text cannonCostLabel;
    [SerializeField] private Text magicianCostLabel;
    private Tower selectedTower;
    private TowerSpot selectedSpot;

    private void Start()
    {
        RefreshBuildCostLabels();
        HidePanels();
    }

    private void Update()
    {
        RefreshSpotHighlights();
        RefreshCostColors();
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame ||
            (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return;
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        if (gameCamera == null) return;
        Vector3 point = gameCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, -gameCamera.transform.position.z));
        Collider2D hit = Physics2D.OverlapPoint(point);
        if (hit == null) { ClearSelection(); return; }
        TowerSpot spot = hit.GetComponent<TowerSpot>();
        if (spot != null)
        {
            if (selectedTower != null && selectedTower != spot.Tower) selectedTower.ShowRange(false);
            selectedTower = spot.Tower;
            selectedSpot = selectedTower == null ? spot : null;
            if (selectedTower != null) selectedTower.ShowRange(true);
            ShowPanels(selectedTower == null);
            return;
        }
        Tower tower = hit.GetComponent<Tower>();
        if (tower != null)
        {
            if (selectedTower != null && selectedTower != tower) selectedTower.ShowRange(false);
            selectedTower = tower;
            selectedSpot = null;
            selectedTower.ShowRange(true);
            ShowPanels(false);
            return;
        }
        ClearSelection();
    }

    public void BuildArcher() => Build(archerPrefab, 75);
    public void BuildCannon() => Build(cannonPrefab, 100);
    public void BuildMagician() => Build(magicianPrefab, 125);

    private void Build(Tower prefab, int cost)
    {
        if (prefab == null || selectedSpot == null || selectedSpot.Tower != null || !GameManager.Instance.Spend(cost)) return;
        selectedTower = Instantiate(prefab, selectedSpot.transform.position, Quaternion.identity);
        selectedTower.SetPurchaseCost(cost);
        selectedTower.ApplyUpgrade(GameManager.Instance.GetUpgradeBonus(selectedTower.Kind));
        selectedSpot.SetTower(selectedTower);
        selectedSpot = null;
        selectedTower.ShowRange(true);
        ShowPanels(false);
    }

    public void UpgradeSelected()
    {
        if (selectedTower == null) return;
        GameManager.Instance.TryUpgrade(selectedTower.Kind);
        RefreshUpgradeLabel();
    }

    public void SellSelected()
    {
        if (selectedTower == null) return;
        selectedTower.ShowRange(false);
        foreach (TowerSpot spot in FindObjectsByType<TowerSpot>(FindObjectsSortMode.None))
            if (spot.Tower == selectedTower) spot.SetTower(null);
        GameManager.Instance.AddMoney(selectedTower.PurchaseCost / 2);
        Destroy(selectedTower.gameObject);
        selectedTower = null;
        selectedSpot = null;
        HidePanels();
    }

    private void ShowPanels(bool showBuild)
    {
        if (buildPanel != null) buildPanel.SetActive(showBuild);
        if (towerActionsPanel != null) towerActionsPanel.SetActive(!showBuild && selectedTower != null);
        RefreshUpgradeLabel();
    }

    private void HidePanels()
    {
        if (buildPanel != null) buildPanel.SetActive(false);
        if (towerActionsPanel != null) towerActionsPanel.SetActive(false);
        if (upgradeCountLabel != null) upgradeCountLabel.text = "0";
        if (upgradeCostLabel != null) upgradeCostLabel.text = string.Empty;
    }

    private void RefreshUpgradeLabel()
    {
        if (upgradeCountLabel == null && upgradeCostLabel == null) return;
        if (selectedTower == null)
        {
            if (upgradeCountLabel != null) upgradeCountLabel.text = "0";
            if (upgradeCostLabel != null) upgradeCostLabel.text = string.Empty;
            return;
        }

        int level = GameManager.Instance.GetUpgradeLevel(selectedTower.Kind);
        if (upgradeCountLabel != null) upgradeCountLabel.text = level.ToString();
        if (upgradeCostLabel != null)
            upgradeCostLabel.text = $"${GameManager.Instance.GetNextUpgradeCost(selectedTower.Kind)}";
        RefreshCostColors();
    }

    private void RefreshBuildCostLabels()
    {
        if (archerButtonLabel != null) archerButtonLabel.text = "Archer";
        if (cannonButtonLabel != null) cannonButtonLabel.text = "Cannon";
        if (magicianButtonLabel != null) magicianButtonLabel.text = "Magician";
        if (archerCostLabel != null) archerCostLabel.text = "$75";
        if (cannonCostLabel != null) cannonCostLabel.text = "$100";
        if (magicianCostLabel != null) magicianCostLabel.text = "$125";
        RefreshCostColors();
    }

    private void RefreshCostColors()
    {
        if (GameManager.Instance == null) return;

        int money = GameManager.Instance.Money;
        SetCostColor(archerCostLabel, money >= 75);
        SetCostColor(cannonCostLabel, money >= 100);
        SetCostColor(magicianCostLabel, money >= 125);

        if (upgradeCostLabel != null)
        {
            bool canAffordUpgrade = selectedTower != null &&
                money >= GameManager.Instance.GetNextUpgradeCost(selectedTower.Kind);
            SetCostColor(upgradeCostLabel, canAffordUpgrade);
        }
    }

    private static void SetCostColor(Text label, bool canAfford)
    {
        if (label != null) label.color = canAfford ? Color.white : Color.red;
    }

    private void RefreshSpotHighlights()
    {
        if (Mouse.current == null) return;

        TowerSpot hoveredSpot = null;
        if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            if (gameCamera != null)
            {
                Vector3 point = gameCamera.ScreenToWorldPoint(new Vector3(
                    mousePosition.x, mousePosition.y, -gameCamera.transform.position.z));
                foreach (Collider2D candidate in Physics2D.OverlapPointAll(point))
                {
                    hoveredSpot = candidate.GetComponent<TowerSpot>();
                    if (hoveredSpot == null) hoveredSpot = candidate.GetComponentInParent<TowerSpot>();
                    if (hoveredSpot != null && hoveredSpot.Tower == null) break;
                    hoveredSpot = null;
                }
            }
        }

        foreach (TowerSpot spot in FindObjectsByType<TowerSpot>(FindObjectsSortMode.None))
            spot.SetHighlighted(spot == selectedSpot || spot == hoveredSpot);
    }

    private void ClearSelection()
    {
        if (selectedTower != null) selectedTower.ShowRange(false);
        selectedSpot = null;
        selectedTower = null;
        HidePanels();
    }
}
