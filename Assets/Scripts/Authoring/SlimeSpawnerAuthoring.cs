using Unity.Entities;
using UnityEngine;

namespace SlimeSwarm
{
    [DisallowMultipleComponent]
    public class SlimeSpawnerAuthoring : MonoBehaviour
    {
        [Header("Префаб слизня")]
        public GameObject slimePrefab;

        [Header("Параметры волны")]
        public int initialSpawnCount = 5000;
        public float spawnRadius = 45f;
        public float spawnInterval = 3f;
        public bool continuousSpawn = false;

        public class SlimeSpawnerBaker : Baker<SlimeSpawnerAuthoring>
        {
            public override void Bake(SlimeSpawnerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                Entity prefabEntity = authoring.slimePrefab != null
                    ? GetEntity(authoring.slimePrefab, TransformUsageFlags.Dynamic)
                    : Entity.Null;

                AddComponent(entity, new SlimeSpawnerComponent
                {
                    SlimePrefab = prefabEntity,
                    SpawnCount = authoring.initialSpawnCount,
                    SpawnRadius = authoring.spawnRadius,
                    SpawnInterval = authoring.spawnInterval,
                    Timer = 0f,
                    ContinuousSpawn = authoring.continuousSpawn
                });
            }
        }
    }
}
