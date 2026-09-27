using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Scenes;

namespace SlimeSwarm.Editor
{
    public static class SlimeSceneAutoSetup
    {
        [MenuItem("Tools/Slime Swarm/Setup Clean Scene", priority = 1)]
        public static void SetupCompleteScene()
        {
            Debug.Log("[SlimeSwarm] 🧹 Начинаем чистую пересборку сцен без рекурсии...");

            EnsureDirectory("Assets/Materials");
            EnsureDirectory("Assets/Prefabs");
            EnsureDirectory("Assets/Scenes");

            // 1. Материалы
            Material slimeMat = CreateOrGetMaterial("Assets/Materials/SlimeMaterial.mat", new Color(0.2f, 0.9f, 0.4f, 1f));
            Material towerMat = CreateOrGetMaterial("Assets/Materials/TowerMaterial.mat", new Color(0.9f, 0.3f, 0.2f, 1f));
            Material floorMat = CreateOrGetMaterial("Assets/Materials/FloorMaterial.mat", new Color(0.18f, 0.2f, 0.25f, 1f));

            // 2. Префаб слизня
            GameObject slimeGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            slimeGo.name = "SlimePrefab";
            slimeGo.GetComponent<MeshRenderer>().sharedMaterial = slimeMat;
            
            Collider sphereCol = slimeGo.GetComponent<Collider>();
            if (sphereCol != null) Object.DestroyImmediate(sphereCol);

            SlimeAuthoring slimeAuth = slimeGo.AddComponent<SlimeAuthoring>();
            slimeAuth.maxHealth = 100f;
            slimeAuth.maxStamina = 50f;
            slimeAuth.moveSpeed = 5.0f;
            slimeAuth.jumpHeight = 1.2f;
            slimeAuth.squashAmount = 0.35f;
            slimeAuth.bounceSpeed = 8f;

            string prefabPath = "Assets/Prefabs/SlimePrefab.prefab";
            GameObject slimePrefab = PrefabUtility.SaveAsPrefabAsset(slimeGo, prefabPath);
            GameObject.DestroyImmediate(slimeGo);

            // 3. Создаем абсолютно чистую SubScene: Assets/Scenes/SlimeSubScene.unity
            string subScenePath = "Assets/Scenes/SlimeSubScene.unity";
            Scene subSceneAsset = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "ArenaPlane";
            plane.transform.position = Vector3.zero;
            plane.transform.localScale = new Vector3(12, 1, 12);
            plane.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.name = "Tower";
            tower.transform.position = new Vector3(0, 1.5f, 0);
            tower.transform.localScale = new Vector3(3f, 3f, 3f);
            tower.GetComponent<MeshRenderer>().sharedMaterial = towerMat;
            var towerAuth = tower.AddComponent<TargetTowerAuthoring>();
            towerAuth.health = 10000f;
            towerAuth.attackRadius = 16f;
            towerAuth.damagePerSecond = 80f;

            GameObject spawner = new GameObject("SlimeSpawner");
            spawner.transform.position = Vector3.zero;
            var spawnerAuth = spawner.AddComponent<SlimeSpawnerAuthoring>();
            spawnerAuth.slimePrefab = slimePrefab;
            spawnerAuth.initialSpawnCount = 10000;
            spawnerAuth.spawnRadius = 45f;
            spawnerAuth.spawnInterval = 5f;
            spawnerAuth.continuousSpawn = false; // Отключен бесконечный автоспавн

            EditorSceneManager.SaveScene(subSceneAsset, subScenePath);

            // 4. Создаем чистую главную сцену: Assets/Scenes/SlimeSwarmArena.unity
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

            // Открываем сабсцену в редакторе
            EditorSceneManager.OpenScene(subScenePath, OpenSceneMode.Additive);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SlimeSwarm] ✅ Чистая сцена успешно собрана!");
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
