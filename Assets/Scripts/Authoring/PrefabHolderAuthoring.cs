using Unity.Entities;
using UnityEngine;

namespace SlimeSwarm
{
    [DisallowMultipleComponent]
    public class PrefabHolderAuthoring : MonoBehaviour
    {
        [Header("Префабы для синглтон-хранилища")]
        public GameObject greenSlimePrefab;
        public GameObject redSlimePrefab;

        public class PrefabHolderBaker : Baker<PrefabHolderAuthoring>
        {
            public override void Bake(PrefabHolderAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new PrefabHolder
                {
                    GreenSlimePrefab = authoring.greenSlimePrefab != null 
                        ? GetEntity(authoring.greenSlimePrefab, TransformUsageFlags.Dynamic) 
                        : Entity.Null,
                    RedSlimePrefab = authoring.redSlimePrefab != null 
                        ? GetEntity(authoring.redSlimePrefab, TransformUsageFlags.Dynamic) 
                        : Entity.Null
                });
            }
        }
    }
}
