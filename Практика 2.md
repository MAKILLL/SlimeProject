# Практическая работа №2: Архитектура DOTS (Blob, DynamicBuffer, Singletons, Groups, ECB)

> **Проект:** «Арена Слизней» (*Slime Swarm Arena*)  
> **Версия Unity:** Unity 6 (6000.3.7f1) + Entities 1.3+ / Universal Render Pipeline (URP)  
> **Производительность:** ~180+ FPS при 10 000+ активных сущностей на экране.

---

## 1. Краткая суть работы (что мы сделали)

Во второй практике проект был расширен ключевыми архитектурными паттернами Unity DOTS:
1. **Blob Assets (Binary Large Objects):** Статическая конфигурация волн врагов вынесена из обычных C# списков в плоскую бинарную память (`BlobAssetReference<WavesBlob>`), оптимизированную для Burst и многопоточности.
2. **DynamicBuffer (`IBufferElementData`):** Башне добавлен динамический буфер входящего урона (`DamageBufferElement`) со встроенной емкостью в чанке (`[InternalBufferCapacity(8)]`).
3. **Singleton Component (`PrefabHolder`):** Хранилище сущностей-префабов (`GreenSlimePrefab`, `RedSlimePrefab`) организовано в виде отдельного синглтона.
4. **Процедурная генерация (`Unity.Mathematics.Random`):** Рандомизация углов и радиусов спавна реализована через потокобезопасный генератор `Unity.Mathematics.Random`.
5. **Группировка систем (`ComponentSystemGroup`):** Создана кастомная группа `WaveSystemGroup`, которая тактирует спавн волн раз в секунду, разгружая главный цикл симуляции.
6. **Спавн и деспавн через `EntityCommandBuffer` (ECB):** Создание и удаление сущностей происходят батчами без нарушения структурной целостности чанков.

---

## 2. Детальный разбор ключевых концепций (по методичке)

### 2.1. Blob Assets (Слайды 1–5)
* **Зачем нужен:** Обычные структуры C# (`List<T>`, `class`) ссылаются на кучу (heap), что вызывает сборку мусора (GC) и кэш-промахи процессора. Blob Asset упаковывает сложные иерархические неизменяемые данные (волны, враги в волне) в единый монолитный непрерывный блок памяти (blittable).
* **Структуры (`SlimeComponents.cs`):**
  ```csharp
  public struct WavesBlob
  {
      public BlobArray<Wave> Waves;
  }

  public struct Wave
  {
      public float WaitTimeBeforeWave;
      public BlobArray<EnemyInWave> Enemies;
  }

  public struct EnemyInWave
  {
      public EnemyType EnemyType;
      public int Amount;
  }
  ```
* **Как билдится (`WavesAuthoring.cs`):**
  1. Выделяется `BlobBuilder(Allocator.Temp)`.
  2. Через `blobBuilder.ConstructRoot<WavesBlob>()` задается корень.
  3. Для вложенных массивов вызывается `blobBuilder.Allocate(ref root.Waves, count)`.
  4. Получается `blobBuilder.CreateBlobAssetReference<WavesBlob>(Allocator.Persistent)`.
  5. Регистрируется через `AddBlobAsset(ref wavesRef, out _)`, чтобы Unity автоматически управлял временем жизни ссылки.
* **Как читается в системе (`WaveSpawnerSystem.cs`):**
  ```csharp
  var wavesHolder = SystemAPI.GetSingleton<WavesHolder>();
  ref var wavesBlob = ref wavesHolder.WavesReference.Value;
  ref var currentWave = ref wavesBlob.Waves[wavesHolder.CurrentWaveIndex];
  ```

---

### 2.2. DynamicBuffer (Слайды 5–6)
* **Зачем нужен:** Компоненты типа `IComponentData` имеют фиксированный размер. Если сущности требуется динамический список элементов переменной длины (например, очередь входящего урона), используется `IBufferElementData`.
* **Оптимизация емкости (`[InternalBufferCapacity(8)]`):**
  Первые 8 элементов хранятся прямо внутри чанка (ArchetypeChunk) рядом с остальными компонентами сущности без дополнительных аллокаций. Если элементов становится больше 8, буфер автоматически переносится в кучу памяти DOTS.
* **Реализация (`SlimeComponents.cs`):**
  ```csharp
  [InternalBufferCapacity(8)]
  public struct DamageBufferElement : IBufferElementData
  {
      public float DamageAmount;
  }
  ```
* **Запекание (`TargetTowerAuthoring.cs`):**
  ```csharp
  AddBuffer<DamageBufferElement>(entity);
  ```
* **Обработка в системе (`TowerDamageBufferSystem.cs`):**
  Система суммирует весь накопленный за кадр урон из буфера, вычитает из `TowerComponent.Health` и очищает буфер через `damageBuffer.Clear()`.

---

### 2.3. Singleton Component для префабов (Слайд 8)
* **Зачем нужен:** В ECS префабы — это обычные сущности с компонентом-тегом `Prefab`. Хранить их ссылки на каждой сущности неэффективно. Мы создаем одну управляющую сущность с компонентом `PrefabHolder`.
* **Реализация (`PrefabHolderAuthoring.cs`):**
  ```csharp
  public struct PrefabHolder : IComponentData
  {
      public Entity GreenSlimePrefab;
      public Entity RedSlimePrefab;
  }
  ```
* В бейкере ссылки на префабы GameObject конвертируются через `GetEntity(prefab, TransformUsageFlags.Dynamic)`.
* В `WaveSpawnerSystem` префабы берутся одной строчкой:
  ```csharp
  var prefabs = SystemAPI.GetSingleton<PrefabHolder>();
  Entity toSpawn = prefabs.GreenSlimePrefab;
  ```

---

### 2.4. Процедурная случайность (`Unity.Mathematics.Random`) (Слайд 7)
* Обычный `UnityEngine.Random` не работает в потоках Burst Jobs.
* Используется `Unity.Mathematics.Random` внутри компонента `RandomHolder`.
* Начальный сид инициализируется в бейкере через `UnityEngine.Random.Range(1, 1000000)` или по индексу сущности (`entity.Index`).
* В `WaveSpawnerSystem`:
  ```csharp
  ref var rng = ref randomHolder.ValueRW.Random;
  float angle = rng.NextFloat(0f, math.PI * 2f);
  ```

---

### 2.5. Группировка систем (`ComponentSystemGroup`) (Слайды 9–10)
* **Проблема:** Если система спавна вызывается 60–200 раз в секунду каждый кадр, это лишняя нагрузка на проверку условий таймеров.
* **Решение:** Создана группа `WaveSystemGroup`, наследующая `ComponentSystemGroup`, которая запускает вложенные системы ровно **раз в 1 секунду**:
  ```csharp
  [UpdateInGroup(typeof(SimulationSystemGroup))]
  public partial class WaveSystemGroup : ComponentSystemGroup
  {
      private float _timer = 0f;
      protected override void OnUpdate()
      {
          _timer += SystemAPI.Time.DeltaTime;
          if (_timer >= 1.0f)
          {
              base.OnUpdate();
              _timer -= 1.0f;
          }
      }
  }
  ```
* `WaveSpawnerSystem` помечена атрибутом `[UpdateInGroup(typeof(WaveSystemGroup))]`.

---

### 2.6. Спавн через `EntityCommandBuffer` (ECB) (Слайд 7)
* Прямое создание (`EntityManager.Instantiate`) во время параллельного выполнения систем приводит к перестройке архетипов и блокировке памяти.
* Мы записываем команды в ECB (`BeginSimulationEntityCommandBufferSystem`), а движок выполняет их скопом в строго определенный момент кадра.
* В UI кнопка «Уничтожить всех слизней» также демонстрирует мгновенное пакетное удаление выборки через `em.DestroyEntity(_slimeQuery)`.

---

## 3. Файловая структура проекта

| Файл | Назначение |
|---|---|
| `Assets/Scripts/Components/SlimeComponents.cs` | Все структуры ECS: теги, параметры слизней, башни, `PrefabHolder`, `WavesBlob`, `Wave`, `EnemyInWave`, `WavesHolder`, `DamageBufferElement`, `RandomHolder`. |
| `Assets/Scripts/Authoring/WavesAuthoring.cs` | MonoBehaviour + Baker: формирует `WavesBlob` через `BlobBuilder`, добавляет `WavesHolder` и `RandomHolder`. |
| `Assets/Scripts/Authoring/PrefabHolderAuthoring.cs` | MonoBehaviour + Baker: создает Singleton `PrefabHolder` с entity-префабами зеленого и красного слизня. |
| `Assets/Scripts/Authoring/TargetTowerAuthoring.cs` | MonoBehaviour + Baker: создает сущность башни и вешает `AddBuffer<DamageBufferElement>`. |
| `Assets/Scripts/Authoring/SlimeAuthoring.cs` | MonoBehaviour + Baker: генерирует префабы слизней (Health, Stamina, Movement, Animation, Squash & Stretch). |
| `Assets/Scripts/Systems/WaveSystemGroup.cs` | Кастомная группа систем с шагом обновления 1.0 сек. |
| `Assets/Scripts/Systems/WaveSpawnerSystem.cs` | Система спавна волн: читает Blob, инстанциирует через ECB, вычисляет координаты с помощью `Unity.Mathematics.Random`. |
| `Assets/Scripts/Systems/TowerDamageBufferSystem.cs` | Обработка `DynamicBuffer<DamageBufferElement>` башни. |
| `Assets/Scripts/Systems/SlimeMovementSystem.cs` | Быстрое перемещение и атака башни роем слизней. |
| `Assets/Scripts/Systems/SlimeAnimationSystem.cs` | Процедурная squash & stretch анимация через `PostTransformMatrix`. |
| `Assets/Scripts/UI/SlimeSwarmHUD.cs` | OnGUI HUD: счетчик FPS, номер текущей волны из Blob, число активных слизней, HP башни и кнопка очистки. |
| `Assets/Editor/SlimeSceneAutoSetup.cs` | Автоматическая генерация и настройка сцены (URP-материалы, префабы, SubScene, волны). Пункт меню: `Tools/Slime Swarm/Setup Practice 2 Scene`. |

---

## 4. Шпаргалка для защиты практики перед преподавателем

**В: Зачем использовать BlobAsset вместо обычной структуры или ScriptableObject?**  
> *О: ScriptableObject и обычные C# классы лежат в управляемой памяти (.NET Managed Heap). К ним нельзя обращаться из высокопроизводительного нативного кода Burst Compiler, а обращение по ссылкам вызывает постоянные кэш-промахи CPU. BlobAsset — это плоский, неизменяемый, бинарный блок неуправляемой памяти. Он дедуплицируется, идеально дружит с Burst и считывается процессором в разы быстрее.*

**В: Чем DynamicBuffer отличается от обычного IComponentData?**  
> *О: Обычный `IComponentData` имеет строго фиксированный размер в архетипе чанка. `DynamicBuffer<T>` реализует интерфейс динамического списка переменной длины. Благодаря `[InternalBufferCapacity(N)]` первые N элементов хранятся прямо в памяти чанка локально, что гарантирует максимальную производительность без аллокаций.*

**В: Зачем нам кастомная группа `WaveSystemGroup`?**  
> *О: По умолчанию системы в `SimulationSystemGroup` вызываются каждый кадр (например, 180 раз в секунду). Для логики спавна волн проверять таймеры так часто бессмысленно. Наследуясь от `ComponentSystemGroup`, мы переопределили `OnUpdate` и вызываем `base.OnUpdate()` раз в 1 секунду, экономя такты процессора.*

**В: Почему нельзя использовать `UnityEngine.Random` в спавнере?**  
> *О: `UnityEngine.Random` — это статический класс с глобальным состоянием, не являющийся потокобезопасным и недоступный внутри заданий Burst. Для DOTS используется структура `Unity.Mathematics.Random`, сид которой хранится непосредственно в компоненте `RandomHolder`.*
