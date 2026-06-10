using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Modular wave/round system.
/// Attach to an empty GameObject in the scene.
/// Configure waves in the Inspector — enemy types, counts, positions,
/// and upgrade drops are all data-driven so you can tweak them without code.
/// </summary>
public class WaveManager : MonoBehaviour
{
    // ───────────────────────── Singleton ─────────────────────────
    public static WaveManager Instance;

    // ───────────────────────── Data Classes ──────────────────────
    [System.Serializable]
    public class EnemySpawnEntry
    {
        [Tooltip("The enemy prefab to spawn (must have EnemyController).")]
        public GameObject enemyPrefab;

        [Tooltip("How many of this enemy to spawn in this wave.")]
        public int count = 1;

        [Tooltip("Seconds between each individual spawn of this entry.")]
        public float spawnInterval = 1f;

        [Tooltip("Possible spawn positions. One is chosen at random per spawn. " +
                 "If empty, a random position within spawnAreaRadius is used.")]
        public Transform[] spawnPoints;
    }

    [System.Serializable]
    public class UpgradeSpawnEntry
    {
        [Tooltip("The upgrade prefab to spawn (must have UpgradePickup).")]
        public GameObject upgradePrefab;

        [Tooltip("Possible spawn positions. One is chosen at random. " +
                 "If empty, a random position within spawnAreaRadius is used.")]
        public Transform[] spawnPoints;
    }

    [System.Serializable]
    public class Wave
    {
        [Tooltip("Display name shown in the UI (e.g. 'Round 1').")]
        public string waveName = "Round";

        [Tooltip("Number of enemies that must be defeated before the NEXT wave starts. " +
                 "Set to 0 to auto-calculate from total enemies in this wave.")]
        public int enemiesToDefeatToComplete = 0;

        [Tooltip("Seconds to wait before spawning enemies after the wave starts.")]
        public float delayBeforeSpawn = 2f;

        [Tooltip("Enemy groups to spawn this wave.")]
        public EnemySpawnEntry[] enemySpawns;

        [Tooltip("Upgrades that appear at the START of this wave.")]
        public UpgradeSpawnEntry[] upgradeSpawns;
    }

    // ───────────────────────── Inspector Fields ─────────────────
    [Header("Waves")]
    [Tooltip("Define each round here. Order = progression order.")]
    public Wave[] waves;

    [Header("Fallback Random Spawn Area")]
    [Tooltip("If an EnemySpawnEntry has no spawnPoints, enemies spawn randomly " +
             "within this radius around the WaveManager object.")]
    public float spawnAreaRadius = 20f;

    [Header("UI (Optional)")]
    [Tooltip("Text element to show the current round name.")]
    public TextMeshProUGUI waveText;

    [Tooltip("Text element to show remaining enemies.")]
    public TextMeshProUGUI enemiesRemainingText;

    // ───────────────────────── Runtime State ────────────────────
    private int currentWaveIndex = -1;
    private int enemiesDefeatedThisWave;
    private int enemiesToDefeat;
    private bool waveInProgress;

    // ───────────────────────── Unity Lifecycle ───────────────────
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        Debug.Log($"[WaveManager] Start() called. Waves configured: {(waves != null ? waves.Length : 0)}");
        StartNextWave();
    }

    // ───────────────────────── Public API ────────────────────────

    /// <summary>
    /// Call this from EnemyController when an enemy dies.
    /// </summary>
    public void OnEnemyDefeated()
    {
        if (!waveInProgress) return;

        enemiesDefeatedThisWave++;
        UpdateUI();

        if (enemiesDefeatedThisWave >= enemiesToDefeat)
        {
            waveInProgress = false;
            StartNextWave();
        }
    }

    /// <summary>
    /// Manually advance to the next wave (e.g. from a debug button).
    /// </summary>
    public void ForceNextWave()
    {
        StopAllCoroutines();
        waveInProgress = false;
        StartNextWave();
    }

    /// <summary>Returns the current wave index (0-based), or -1 before the first wave.</summary>
    public int CurrentWaveIndex => currentWaveIndex;

    /// <summary>Returns true while a wave is actively running.</summary>
    public bool IsWaveInProgress => waveInProgress;

    // ───────────────────────── Core Logic ────────────────────────

    private void StartNextWave()
    {
        currentWaveIndex++;

        if (currentWaveIndex >= waves.Length)
        {
            Debug.Log("[WaveManager] All waves completed!");
            UpdateWaveText("All Rounds Complete!");
            return;
        }

        Wave wave = waves[currentWaveIndex];
        enemiesDefeatedThisWave = 0;

        // Auto-calculate kill target if set to 0
        enemiesToDefeat = wave.enemiesToDefeatToComplete;
        if (enemiesToDefeat <= 0)
        {
            enemiesToDefeat = 0;
            foreach (var entry in wave.enemySpawns)
                enemiesToDefeat += entry.count;
        }

        waveInProgress = true;
        Debug.Log($"[WaveManager] Starting wave {currentWaveIndex}: '{wave.waveName}', enemies to defeat: {enemiesToDefeat}");
        UpdateWaveText(wave.waveName);
        UpdateUI();

        StartCoroutine(SpawnWaveRoutine(wave));
    }

    private IEnumerator SpawnWaveRoutine(Wave wave)
    {
        Debug.Log($"[WaveManager] SpawnWaveRoutine started for '{wave.waveName}'");

        // Spawn upgrades immediately, avoiding duplicate locations within the same wave
        if (wave.upgradeSpawns != null)
        {
            // Track used spawn point indices per UpgradeSpawnEntry to avoid overlaps
            HashSet<string> usedPositions = new HashSet<string>();

            foreach (var upgrade in wave.upgradeSpawns)
            {
                if (upgrade.upgradePrefab == null) continue;

                Vector3 pos;
                if (upgrade.spawnPoints != null && upgrade.spawnPoints.Length > 0)
                {
                    // Build a list of unused spawn points
                    List<Transform> available = new List<Transform>();
                    foreach (var sp in upgrade.spawnPoints)
                    {
                        if (sp != null && !usedPositions.Contains(sp.position.ToString()))
                            available.Add(sp);
                    }

                    // If all points are taken, fall back to the full list
                    if (available.Count == 0)
                    {
                        foreach (var sp in upgrade.spawnPoints)
                        {
                            if (sp != null) available.Add(sp);
                        }
                    }

                    Transform chosen = available[Random.Range(0, available.Count)];
                    pos = chosen.position;
                    usedPositions.Add(pos.ToString());
                }
                else
                {
                    pos = GetRandomSpawnPosition();
                }

                Instantiate(upgrade.upgradePrefab, pos, Quaternion.identity);
            }
        }

        // Wait before enemies arrive
        if (wave.delayBeforeSpawn > 0f)
        {
            Debug.Log($"[WaveManager] Waiting {wave.delayBeforeSpawn}s before spawning enemies...");
            yield return new WaitForSeconds(wave.delayBeforeSpawn);
        }

        // Spawn each enemy group
        if (wave.enemySpawns != null)
        {
            foreach (var entry in wave.enemySpawns)
            {
                if (entry.enemyPrefab == null)
                {
                    Debug.LogWarning("[WaveManager] Skipping entry — enemyPrefab is null!");
                    continue;
                }

                Debug.Log($"[WaveManager] Spawning {entry.count}x {entry.enemyPrefab.name}");

                for (int i = 0; i < entry.count; i++)
                {
                    Vector3 pos = GetSpawnPosition(entry);
                    GameObject spawned = Instantiate(entry.enemyPrefab, pos, Quaternion.identity);
                    Debug.Log($"[WaveManager] Spawned '{spawned.name}' at {pos}");

                    if (entry.spawnInterval > 0f && i < entry.count - 1)
                        yield return new WaitForSeconds(entry.spawnInterval);
                }
            }
        }
        else
        {
            Debug.LogWarning("[WaveManager] enemySpawns is null!");
        }

        Debug.Log("[WaveManager] SpawnWaveRoutine finished.");
    }

    // ───────────────────────── Helpers ───────────────────────────

    private Vector3 GetSpawnPosition(EnemySpawnEntry entry)
    {
        if (entry.spawnPoints != null && entry.spawnPoints.Length > 0)
        {
            Transform point = entry.spawnPoints[Random.Range(0, entry.spawnPoints.Length)];
            return point.position;
        }

        return GetRandomSpawnPosition();
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector2 circle = Random.insideUnitCircle * spawnAreaRadius;
        return transform.position + new Vector3(circle.x, 0f, circle.y);
    }

    private void UpdateWaveText(string text)
    {
        if (waveText != null)
            waveText.text = text;
    }

    private void UpdateUI()
    {
        if (enemiesRemainingText != null)
        {
            int remaining = Mathf.Max(0, enemiesToDefeat - enemiesDefeatedThisWave);
            enemiesRemainingText.text = "Enemies Left: " + remaining;
        }
    }
}
