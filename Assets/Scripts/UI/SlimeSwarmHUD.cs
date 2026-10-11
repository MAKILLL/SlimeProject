using Unity.Entities;
using UnityEngine;

namespace SlimeSwarm
{
    public class SlimeSwarmHUD : MonoBehaviour
    {
        private float _currentFps = 60f;
        private float _uiUpdateTimer = 0f;
        private int _cachedSlimeCount = 0;
        private float _cachedTowerHp = 10000f;
        private float _cachedTowerMaxHp = 10000f;
        private int _currentWave = 0;
        private bool _allWavesSpawned = false;

        private EntityQuery _slimeQuery;
        private EntityQuery _towerQuery;
        private EntityQuery _wavesQuery;
        private bool _queriesInitialized = false;

        private void Awake()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
        }

        private void Start()
        {
            InitQueries();
            UpdateCachedData();
        }

        private void InitQueries()
        {
            var defaultWorld = World.DefaultGameObjectInjectionWorld;
            if (defaultWorld != null && defaultWorld.IsCreated)
            {
                var em = defaultWorld.EntityManager;
                _slimeQuery = em.CreateEntityQuery(typeof(SlimeTag));
                _towerQuery = em.CreateEntityQuery(typeof(TargetTowerTag), typeof(TowerComponent));
                _wavesQuery = em.CreateEntityQuery(typeof(WavesHolder));
                _queriesInitialized = true;
            }
        }

        private void Update()
        {
            if (Time.frameCount > 3 && Time.unscaledDeltaTime > 0.0001f)
            {
                float instantFps = 1.0f / Time.unscaledDeltaTime;
                _currentFps = Mathf.Lerp(_currentFps, instantFps, Time.unscaledDeltaTime * 6f);
            }

            _uiUpdateTimer += Time.unscaledDeltaTime;
            if (_uiUpdateTimer >= 0.15f)
            {
                _uiUpdateTimer = 0f;
                UpdateCachedData();
            }
        }

        private void UpdateCachedData()
        {
            if (!_queriesInitialized)
            {
                InitQueries();
                if (!_queriesInitialized) return;
            }

            _cachedSlimeCount = _slimeQuery.CalculateEntityCount();

            if (_towerQuery.CalculateEntityCount() > 0)
            {
                using var towers = _towerQuery.ToComponentDataArray<TowerComponent>(Unity.Collections.Allocator.Temp);
                if (towers.Length > 0)
                {
                    _cachedTowerHp = towers[0].Health;
                    _cachedTowerMaxHp = towers[0].MaxHealth;
                }
            }

            if (_wavesQuery.CalculateEntityCount() > 0)
            {
                using var waves = _wavesQuery.ToComponentDataArray<WavesHolder>(Unity.Collections.Allocator.Temp);
                if (waves.Length > 0)
                {
                    _currentWave = waves[0].CurrentWaveIndex;
                    _allWavesSpawned = waves[0].AllWavesSpawned;
                }
            }
        }

        private void OnGUI()
        {
            float boxX = Screen.width - 385;
            GUI.Box(new Rect(boxX, 15, 370, 245), "Арена Слизней — Практика №2 (DOTS Blob)");

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            // FPS
            GUI.color = _currentFps >= 50f ? Color.green : (_currentFps >= 30f ? Color.yellow : Color.red);
            GUI.Label(new Rect(boxX + 15, 42, 340, 25), $"FPS: {_currentFps:F1}", labelStyle);
            GUI.color = Color.white;

            // Волна из Blob
            string waveText = _allWavesSpawned ? "Все волны вышли!" : $"Волна (Blob): {_currentWave + 1}";
            GUI.Label(new Rect(boxX + 15, 68, 340, 25), waveText, labelStyle);

            // Количество слизней
            GUI.Label(new Rect(boxX + 15, 94, 340, 25), $"Количество в рое: {_cachedSlimeCount:N0}", labelStyle);

            // Здоровье башни
            float hpPct = _cachedTowerMaxHp > 0f ? (_cachedTowerHp / _cachedTowerMaxHp) * 100f : 0f;
            GUI.Label(new Rect(boxX + 15, 120, 340, 25), $"Башня: {_cachedTowerHp:F0} / {_cachedTowerMaxHp:F0} ({hpPct:F0}%)", labelStyle);

            // Кнопка очистки
            if (GUI.Button(new Rect(boxX + 15, 160, 340, 35), "Уничтожить всех слизней (ECB)"))
            {
                ClearAllSlimes();
            }
        }

        private void ClearAllSlimes()
        {
            if (!_queriesInitialized) InitQueries();
            if (!_queriesInitialized) return;

            var defaultWorld = World.DefaultGameObjectInjectionWorld;
            var em = defaultWorld.EntityManager;
            em.DestroyEntity(_slimeQuery);
        }
    }
}
