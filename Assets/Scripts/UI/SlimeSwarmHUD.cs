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

        private EntityQuery _slimeQuery;
        private EntityQuery _towerQuery;
        private EntityQuery _spawnerQuery;
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
                _spawnerQuery = em.CreateEntityQuery(typeof(SlimeSpawnerComponent));
                _queriesInitialized = true;
            }
        }

        private void Update()
        {
            // Пропускаем первые 3 кадра загрузки для точного расчета FPS
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
        }

        private void OnGUI()
        {
            float boxX = Screen.width - 375;
            GUI.Box(new Rect(boxX, 15, 360, 240), "Арена Слизней (Slime Swarm Arena) — DOTS");

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            // FPS
            GUI.color = _currentFps >= 50f ? Color.green : (_currentFps >= 30f ? Color.yellow : Color.red);
            GUI.Label(new Rect(boxX + 15, 45, 330, 25), $"FPS: {_currentFps:F1}", labelStyle);
            GUI.color = Color.white;

            // Количество слизней
            GUI.Label(new Rect(boxX + 15, 75, 330, 25), $"Количество слизней в рое: {_cachedSlimeCount:N0}", labelStyle);

            // Здоровье башни
            float hpPct = _cachedTowerMaxHp > 0f ? (_cachedTowerHp / _cachedTowerMaxHp) * 100f : 0f;
            GUI.Label(new Rect(boxX + 15, 105, 330, 25), $"Здоровье башни: {_cachedTowerHp:F0} / {_cachedTowerMaxHp:F0} ({hpPct:F0}%)", labelStyle);

            // Кнопки управления спавном
            if (GUI.Button(new Rect(boxX + 15, 140, 150, 35), "+ 1 000 Слизней"))
            {
                SpawnAdditionalSlimes(1000);
            }

            if (GUI.Button(new Rect(boxX + 175, 140, 160, 35), "+ 10 000 Слизней"))
            {
                SpawnAdditionalSlimes(10000);
            }

            if (GUI.Button(new Rect(boxX + 15, 185, 320, 35), "Уничтожить всех слизней"))
            {
                ClearAllSlimes();
            }
        }

        private void SpawnAdditionalSlimes(int count)
        {
            if (!_queriesInitialized) InitQueries();
            if (!_queriesInitialized) return;

            var defaultWorld = World.DefaultGameObjectInjectionWorld;
            var em = defaultWorld.EntityManager;

            if (_spawnerQuery.CalculateEntityCount() > 0)
            {
                using var entities = _spawnerQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                for (int i = 0; i < entities.Length; i++)
                {
                    var spawner = em.GetComponentData<SlimeSpawnerComponent>(entities[i]);
                    spawner.SpawnCount += count;
                    em.SetComponentData(entities[i], spawner);
                }
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
