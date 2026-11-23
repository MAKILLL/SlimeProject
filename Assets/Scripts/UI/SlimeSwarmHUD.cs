using System;
using Unity.Entities;
using UnityEngine;

namespace SlimeSwarm
{
    // Пользовательский интерфейс MonoBehaviour, отображающий метрики и данные из мира Entity
    public class SlimeSwarmHUD : MonoBehaviour
    {
        private float _currentFps = 60f;
        private float _uiUpdateTimer = 0f;
        private int _cachedSlimeCount = 0;
        private float _cachedTowerHp = 10000f;
        private float _cachedTowerMaxHp = 10000f;
        private int _currentWave = 0;
        private bool _allWavesSpawned = false;

        // Данные о ближайшем слизне, рассчитанные в ECS
        private bool _hasNearestSlime = false;
        private float _nearestDistance = 0f;
        private float _nearestHp = 0f;

        // Последнее событие из мира Entity
        private string _lastEventMessage = "Ожидание волн...";

        private EntityQuery _slimeQuery;
        private EntityQuery _towerQuery;
        private EntityQuery _wavesQuery;
        private EntityQuery _nearestQuery;
        private bool _queriesInitialized = false;

        private void Awake()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
        }

        private void OnEnable()
        {
            // Подписываемся на события из мира Entity
            GameEvents.OnWaveCompleted += HandleWaveCompleted;
        }

        private void OnDisable()
        {
            // Отписываемся при деактивации
            GameEvents.OnWaveCompleted -= HandleWaveCompleted;
        }

        private void HandleWaveCompleted(int waveNumber)
        {
            _lastEventMessage = $"Событие: Волна {waveNumber} начата!";
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
                _nearestQuery = em.CreateEntityQuery(typeof(NearestSlimeInfo));
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

            // Читаем здоровье башни
            if (_towerQuery.CalculateEntityCount() > 0)
            {
                using var towers = _towerQuery.ToComponentDataArray<TowerComponent>(Unity.Collections.Allocator.Temp);
                if (towers.Length > 0)
                {
                    _cachedTowerHp = towers[0].Health;
                    _cachedTowerMaxHp = towers[0].MaxHealth;
                }
            }

            // Читаем состояние волн
            if (_wavesQuery.CalculateEntityCount() > 0)
            {
                using var waves = _wavesQuery.ToComponentDataArray<WavesHolder>(Unity.Collections.Allocator.Temp);
                if (waves.Length > 0)
                {
                    _currentWave = waves[0].CurrentWaveIndex;
                    _allWavesSpawned = waves[0].AllWavesSpawned;
                }
            }

            // Читаем данные о ближайшем противнике из синглтона ECS
            if (_nearestQuery.CalculateEntityCount() > 0)
            {
                using var nearestData = _nearestQuery.ToComponentDataArray<NearestSlimeInfo>(Unity.Collections.Allocator.Temp);
                if (nearestData.Length > 0)
                {
                    _hasNearestSlime = nearestData[0].HasTarget;
                    _nearestDistance = nearestData[0].Distance;
                    _nearestHp = nearestData[0].Health;
                }
            }
        }

        private void OnGUI()
        {
            float boxX = Screen.width - 400;
            GUI.Box(new Rect(boxX, 15, 385, 310), "Арена Слизней — DOTS Physics & Hybrid");

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };

            // FPS
            GUI.color = _currentFps >= 50f ? Color.green : (_currentFps >= 30f ? Color.yellow : Color.red);
            GUI.Label(new Rect(boxX + 15, 38, 355, 22), $"FPS: {_currentFps:F1}", labelStyle);
            GUI.color = Color.white;

            // Волна
            string waveText = _allWavesSpawned ? "Все волны вышли!" : $"Волна: {_currentWave + 1}";
            GUI.Label(new Rect(boxX + 15, 62, 355, 22), waveText, labelStyle);

            // Количество слизней
            GUI.Label(new Rect(boxX + 15, 86, 355, 22), $"Количество в рое: {_cachedSlimeCount:N0}", labelStyle);

            // Здоровье башни
            float hpPct = _cachedTowerMaxHp > 0f ? (_cachedTowerHp / _cachedTowerMaxHp) * 100f : 0f;
            GUI.Label(new Rect(boxX + 15, 110, 355, 22), $"Башня: {_cachedTowerHp:F0} / {_cachedTowerMaxHp:F0} ({hpPct:F0}%)", labelStyle);

            // Ближайший враг (данные от ECS Burst-системы)
            string nearestText = _hasNearestSlime 
                ? $"Ближайший слизень: {_nearestDistance:F1} м (HP: {_nearestHp:F0})"
                : "Ближайший слизень: не обнаружен";
            GUI.Label(new Rect(boxX + 15, 134, 355, 22), nearestText, labelStyle);

            // Редкое событие из Entity мира
            GUI.color = Color.cyan;
            GUI.Label(new Rect(boxX + 15, 158, 355, 22), _lastEventMessage, labelStyle);
            GUI.color = Color.white;

            // Подсказка управления
            GUIStyle hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Italic };
            GUI.Label(new Rect(boxX + 15, 185, 355, 20), "Управление: WASD / стрелки для перемещения башни", hintStyle);

            // Кнопка очистки
            if (GUI.Button(new Rect(boxX + 15, 215, 355, 32), "Уничтожить всех слизней (ECB)"))
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
