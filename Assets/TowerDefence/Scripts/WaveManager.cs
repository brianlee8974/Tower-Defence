using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public class EnemyType
    {
        public Sprite sprite;
        public float size = 1f;
    }

    [Header("Scene setup")]
    [SerializeField] private Enemy enemyPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private EnemyType[] enemyTypes = new EnemyType[3];
    [SerializeField] private float pixelsToUnits = 0.01f;
    [Header("Combined start/pause button")]
    [SerializeField] private Image waveButtonImage;
    [SerializeField] private Sprite playSprite;
    [SerializeField] private Sprite pauseSprite;

    // Counts per wave for the 12 Python enemy variants: weak, normal, then strong.
    private static readonly int[,] Counts = {
        {20,0,0,0,0,0,0,0,0,0,0,0}, {50,0,0,0,0,0,0,0,0,0,0,0},
        {30,20,0,0,0,0,0,0,0,0,0,0}, {30,30,20,0,0,0,0,0,0,0,0,0},
        {15,15,15,1,0,0,0,0,0,0,0,0}, {0,0,0,0,20,0,0,0,0,0,0,0},
        {0,0,0,0,50,0,0,0,0,0,0,0}, {0,0,0,0,30,20,0,0,0,0,0,0},
        {0,0,0,0,20,20,20,0,0,0,0,0}, {0,0,0,0,15,15,15,1,0,0,0,0},
        {0,0,0,0,0,0,0,0,20,20,20,0}, {0,0,0,1,0,0,0,0,5,5,5,0},
        {0,0,0,1,0,0,0,1,5,5,5,0}, {0,0,0,2,0,0,0,2,5,5,5,0},
        {0,0,0,3,0,0,0,3,0,0,0,1}
    };

    private int waveIndex;
    private bool waveRunning;
    private Coroutine spawnRoutine;

    private void Update() => RefreshWaveButton();

    public void OnWaveButtonPressed()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;
        if (waveRunning)
        {
            GameManager.Instance.TogglePause();
            RefreshWaveButton();
        }
        else StartNextWave();
    }

    private void RefreshWaveButton()
    {
        if (waveButtonImage == null) return;
        if (GameManager.Instance == null) return;
        Sprite desired = !GameManager.Instance.IsPaused && waveRunning ? pauseSprite : playSprite;
        if (desired != null && waveButtonImage.sprite != desired) waveButtonImage.sprite = desired;
    }

    public void StartNextWave()
    {
        if (waveRunning || GameManager.Instance == null || GameManager.Instance.IsGameOver) return;
        if (waveIndex >= Counts.GetLength(0)) { GameManager.Instance.EndGame("VICTORY"); return; }
        GameManager.Instance.WaveIndex = waveIndex;
        GameManager.Instance.RefreshHud();
        GameManager.Instance.SetStatus(string.Empty);
        GameManager.Instance.SetPaused(false);
        waveRunning = true;
        spawnRoutine = StartCoroutine(SpawnWave(waveIndex));
    }

    private IEnumerator SpawnWave(int wave)
    {
        for (int type = 0; type < 12; type++)
        {
            for (int count = 0; count < Counts[wave, type]; count++)
            {
                SpawnEnemy(wave, type);
                yield return new WaitForSeconds(Random.Range(1f / 3f, 5f / 3f));
            }
        }
        spawnRoutine = null;
        while (FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length > 0 && !GameManager.Instance.IsGameOver)
            yield return null;
        waveRunning = false;
        waveIndex++;
        if (waveIndex >= Counts.GetLength(0)) GameManager.Instance.EndGame("VICTORY");
        else
        {
            GameManager.Instance.SetStatus("Wave complete — start the next wave");
            GameManager.Instance.SetPaused(true);
        }
    }

    private void SpawnEnemy(int wave, int type)
    {
        float hp, speed, reward, size = 1f;
        int group = type / 4;
        int shape = type % 4;
        float w = wave;
        switch (group)
        {
            case 0:
                hp = shape == 0 ? 30 * (w + 1) : shape == 1 ? 40 * (w + 1.5f) : shape == 2 ? 50 * (w + 2) : 2000 * (((w + 1) / 5f) * 5);
                speed = shape == 0 ? 1.2f + Random.Range(1, (wave + 1) * 8 + 1) / 100f : 1f + Random.Range(1, (wave + 1) * (shape == 1 ? 6 : shape == 2 ? 4 : 2) + 1) / 100f;
                reward = shape == 0 ? 10 + w : shape == 1 ? 20 + w : shape == 2 ? 30 + w : 200 * (((w + 1) / 5f) * 5);
                break;
            case 1:
                hp = shape == 0 ? 40 * (w + 1) : shape == 1 ? 50 * (w + 1.5f) : shape == 2 ? 60 * (w + 2) : 3000 * (((w + 1) / 5f) * 5);
                speed = shape == 0 ? 1.2f + Random.Range(30, (wave + 30) * 2 + 1) / 100f : 1f + Random.Range(shape == 1 ? 24 : shape == 2 ? 15 : 10, (wave + (shape == 1 ? 24 : shape == 2 ? 15 : 10)) * 2 + 1) / 100f;
                reward = shape == 0 ? 15 + w : shape == 1 ? 25 + w : shape == 2 ? 35 + w : 300 * (((w + 1) / 5f) * 5);
                break;
            default:
                hp = shape == 0 ? 60 * (w + 1) : shape == 1 ? 80 * (w + 1.5f) : shape == 2 ? 100 * (w + 2) : 5000 * (((w + 1) / 5f) * 5);
                speed = shape == 0 ? 1.2f + Random.Range(60, (wave + 60) * 2 + 1) / 100f : 1f + Random.Range(shape == 1 ? 50 : shape == 2 ? 30 : 20, Mathf.FloorToInt((wave + (shape == 1 ? 50 : shape == 2 ? 30 : 20)) * (shape == 3 ? 1.5f : 2f)) + 1) / 100f;
                reward = shape == 0 ? 20 + w : shape == 1 ? 30 + w : shape == 2 ? 40 + w : 400 * (((w + 1) / 5f) * 5);
                break;
        }
        if (shape == 3) size = 2f;
        int spriteGroup = shape == 3 ? group : shape;
        EnemyType enemyType = enemyTypes[Mathf.Clamp(spriteGroup, 0, enemyTypes.Length - 1)];
        Vector3 position = spawnPoint != null ? spawnPoint.position : waypoints[0].position;
        Enemy enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        enemy.Initialize(waypoints, hp, speed * 30f * pixelsToUnits, Mathf.RoundToInt(reward), enemyType.sprite, size * enemyType.size);
    }
}
