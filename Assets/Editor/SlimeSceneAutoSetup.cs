using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Scenes;

namespace SlimeSwarm.Editor
{
    // =========================================================================
    // АВТОМАТИЧЕСКАЯ НАСТРОЙКА СЦЕНЫ (ПРАКТИКИ №1-3)
    // Генерирует полностью готовую SubScene со всеми компонентами DOTS в один клик.
    // =========================================================================
    public static class SlimeSceneAutoSetup
    {
        [MenuItem("Tools/Slime Swarm/Setup Practice 3 Scene", priority = 1)]
        [MenuItem("Tools/Slime Swarm/Setup Practice 2 Scene", priority = 2)]
        public static void SetupCompleteScene()
        {
            Debug.Log("[SlimeSwarm] 🚀 Начинаем настройку сцены для Практики №3 (Jobs + Blob + DynamicBuffer)...");

            EnsureDirectory("Assets/Materials");
            EnsureDirectory("Assets/Prefabs");
            EnsureDirectory("Assets/Scenes");

            // 1. Материалы URP
            Material greenMat = CreateOrGetMaterial("Assets/Materials/GreenSlimeMaterial.mat", new Color(0.2f, 0.9f, 0.4f, 1f));
            Material redMat = CreateOrGetMaterial("Assets/Materials/RedSlimeMaterial.mat", new Color(0.95f, 0.25f, 0.25f, 1f));
            Material towerMat = CreateOrGetMaterial("Assets/Materials/TowerMaterial.mat", new Color(0.2f, 0.5f, 0.95f, 1f));
            Material floorMat = CreateOrGetMaterial("Assets/Materials/FloorMaterial.mat", new Color(0.18f, 0.2f, 0.25f, 1f));

            // 2. Создаем 2 префаба для Singleton-хранилища PrefabHolder
            GameObject greenPrefab = CreateSlimePrefab("Assets/Prefabs/GreenSlimePrefab.prefab", greenMat, 100f, 4.5f);
            GameObject redPrefab = CreateSlimePrefab("Assets/Prefabs/RedSlimePrefab.prefab", redMat, 70f, 6.5f);

            // 3. Создаем абсолютно чистую SubScene
            string subScenePath = "Assets/Scenes/SlimeSubScene.unity";
            Scene subSceneAsset = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            // Арена (Plane)
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "ArenaPlane";
            plane.transform.position = Vector3.zero;
            plane.transform.localScale = new Vector3(12, 1, 12);
            plane.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

            // Башня (с DynamicBuffer DamageBufferElement)
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.name = "Tower";
            tower.transform.position = new Vector3(0, 1.5f, 0);
            tower.transform.localScale = new Vector3(3f, 3f, 3f);
            tower.GetComponent<MeshRenderer>().sharedMaterial = towerMat;
            var towerAuth = tower.AddComponent<TargetTowerAuthoring>();
            towerAuth.health = 10000f;
            towerAuth.attackRadius = 16f;
            towerAuth.damagePerSecond = 80f;

            // Хранилище префабов (Singleton по методичке №2 стр. 8)
            GameObject prefabHolderGo = new GameObject("PrefabHolder");
            var prefAuth = prefabHolderGo.AddComponent<PrefabHolderAuthoring>();
            prefAuth.greenSlimePrefab = greenPrefab;
            prefAuth.redSlimePrefab = redPrefab;

            // Конфигуратор волн (Blob по методичке №2 стр. 1-4)
            GameObject wavesGo = new GameObject("WavesManager");
            var wavesAuth = wavesGo.AddComponent<WavesAuthoring>();
            
            // Настраиваем 3 волны
            var wavesList = new List<EnemiesWave>
            {
                new EnemiesWave
                {
                    WaitTimeBeforeWave = 0f, // 1-я волна сразу на старте
                    EnemiesInWave = new List<WaveEnemy>
                    {
                        new WaveEnemy { EnemyType = EnemyType.Green, Amount = 4000 }
                    }
                },
                new EnemiesWave
                {
                    WaitTimeBeforeWave = 4f, // 2-я волна через 4 сек
                    EnemiesInWave = new List<WaveEnemy>
                    {
                        new WaveEnemy { EnemyType = EnemyType.Green, Amount = 4000 },
                        new WaveEnemy { EnemyType = EnemyType.Red, Amount = 2000 }
                    }
                },
                new EnemiesWave
                {
                    WaitTimeBeforeWave = 5f, // 3-я волна через 5 сек
                    EnemiesInWave = new List<WaveEnemy>
                    {
                        new WaveEnemy { EnemyType = EnemyType.Green, Amount = 5000 },
                        new WaveEnemy { EnemyType = EnemyType.Red, Amount = 3000 }
                    }
                }
            };
            wavesAuth._waves = wavesList;
            EditorUtility.SetDirty(wavesGo);
            EditorUtility.SetDirty(prefabHolderGo);

            EditorSceneManager.SaveScene(subSceneAsset, subScenePath);

            // 4. Создаем главную сцену: Assets/Scenes/SlimeSwarmArena.unity
            string mainScenePath = "Assets/Scenes/SlimeSwarmArena.unity";
            Scene mainScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Камера
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera mainCam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<SlimeSwarmHUD>();
            camGo.transform.position = new Vector3(0, 70, -65);
            camGo.transform.rotation = Quaternion.Euler(50, 0, 0);
            mainCam.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);
            mainCam.clearFlags = CameraClearFlags.SolidColor;

            // Свет
            GameObject lightGo = new GameObject("Directional Light");
            Light dirLight = lightGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
            dirLight.intensity = 1.2f;

            // SubScene хост
            GameObject subSceneHost = new GameObject("SlimeSubScene");
            var subSceneComp = subSceneHost.AddComponent<SubScene>();
            SceneAsset subSceneObj = AssetDatabase.LoadAssetAtPath<SceneAsset>(subScenePath);
            subSceneComp.SceneAsset = subSceneObj;
            subSceneComp.AutoLoadScene = true;

            EditorSceneManager.SaveScene(mainScene, mainScenePath);

            // Открываем сабсцену в редакторе аддитивно
            EditorSceneManager.OpenScene(subScenePath, OpenSceneMode.Additive);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SlimeSwarm] ✅ Сцена для Практики №3 успешно настроена и сохранена!");
        }

        private static GameObject CreateSlimePrefab(string path, Material mat, float hp, float speed)
        {
            GameObject slimeGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            slimeGo.GetComponent<MeshRenderer>().sharedMaterial = mat;

            Collider sphereCol = slimeGo.GetComponent<Collider>();
            if (sphereCol != null) Object.DestroyImmediate(sphereCol);

            SlimeAuthoring slimeAuth = slimeGo.AddComponent<SlimeAuthoring>();
            slimeAuth.maxHealth = hp;
            slimeAuth.moveSpeed = speed;
            slimeAuth.attackRange = 2.0f;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(slimeGo, path);
            GameObject.DestroyImmediate(slimeGo);
            return prefab;
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        private static Material CreateOrGetMaterial(string path, Color color)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader uShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(uShader);
                mat.color = color;
                mat.SetFloat("_Smoothness", 0.7f);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }
    }
}
