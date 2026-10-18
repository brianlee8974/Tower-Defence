using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Starting state")]
    [SerializeField] private int startingMoney = 300;
    [SerializeField] private int startingLives = 5;
    [Tooltip("Easy=.5, Normal=.7, Hard=1. These values scale tower damage, as in the Python game.")]
    [SerializeField, Range(0.1f, 1f)] private float damageMultiplier = 0.7f;

    [Header("Optional HUD references")]
    [SerializeField] private Text moneyLabel;
    [SerializeField] private Text livesLabel;
    [SerializeField] private Text waveLabel;
    [SerializeField] private Text statusLabel;

    [Header("Speed button images")]
    [SerializeField] private Image speedButtonImage;
    [SerializeField] private Sprite normalSpeedSprite;
    [SerializeField] private Sprite doubleSpeedSprite;

    public int Money { get; private set; }
    public int Lives { get; private set; }
    public int WaveIndex { get; set; }
    public float DamageMultiplier => damageMultiplier;
    public float GameSpeed { get; private set; } = 1f;
    public bool IsPaused { get; private set; } = true;
    public bool IsGameOver { get; private set; }
    private readonly int[] upgradeLevels = new int[3];
    private readonly int[] nextUpgradeCosts = { 50, 60, 60 };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Money = startingMoney;
        Lives = startingLives;
        RefreshHud();
    }

    private void Update()
    {
        Time.timeScale = IsPaused ? 0f : GameSpeed;
        RefreshSpeedButtonImage();
    }

    public bool Spend(int amount)
    {
        if (amount < 0 || Money < amount) return false;
        Money -= amount;
        RefreshHud();
        return true;
    }

    public void AddMoney(int amount)
    {
        Money += Mathf.Max(0, amount);
        RefreshHud();
    }

    public bool TryUpgrade(TowerKind kind)
    {
        int index = (int)kind;
        if (!Spend(nextUpgradeCosts[index])) return false;
        upgradeLevels[index]++;
        int increase = kind == TowerKind.Archer ? upgradeLevels[index] * 3 :
                      kind == TowerKind.Cannon ? upgradeLevels[index] * 5 : upgradeLevels[index] * 8;
        foreach (Tower tower in FindObjectsByType<Tower>(FindObjectsSortMode.None))
            if (tower.Kind == kind) tower.ApplyUpgrade(increase);
        nextUpgradeCosts[index] += kind == TowerKind.Cannon ? 40 : 30;
        return true;
    }

    public int GetUpgradeLevel(TowerKind kind)
    {
        return upgradeLevels[(int)kind];
    }

    public int GetNextUpgradeCost(TowerKind kind)
    {
        return nextUpgradeCosts[(int)kind];
    }

    public float GetUpgradeBonus(TowerKind kind)
    {
        int level = upgradeLevels[(int)kind];
        int step = kind == TowerKind.Archer ? 3 : kind == TowerKind.Cannon ? 5 : 8;
        return step * level * (level + 1) / 2f;
    }

    public void EnemyEscaped()
    {
        Lives--;
        if (Lives <= 0) EndGame("DEFEAT");
        RefreshHud();
    }

    public void SetPaused(bool paused)
    {
        if (!IsGameOver) IsPaused = paused;
    }

    public void SetDifficulty(float multiplier)
    {
        damageMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
    }

    public void TogglePause() => SetPaused(!IsPaused);
    public void SetNormalSpeed() => GameSpeed = 1f;
    public void ToggleDoubleSpeed() => GameSpeed = GameSpeed > 1f ? 1f : 2f;

    private void RefreshSpeedButtonImage()
    {
        if (speedButtonImage == null) return;
        Sprite desired = GameSpeed > 1f ? doubleSpeedSprite : normalSpeedSprite;
        if (desired != null && speedButtonImage.sprite != desired)
            speedButtonImage.sprite = desired;
    }

    public void EndGame(string message)
    {
        IsGameOver = true;
        IsPaused = true;
        if (statusLabel != null) statusLabel.text = message;
    }

    public void SetStatus(string message)
    {
        if (statusLabel != null) statusLabel.text = message;
    }

    public void RefreshHud()
    {
        if (moneyLabel != null) moneyLabel.text = Money.ToString();
        if (livesLabel != null) livesLabel.text = Lives.ToString();
        if (waveLabel != null) waveLabel.text = $"Wave {WaveIndex + 1}/15";
    }
}
