Snake Powerups Drop-in (Unity 2020.3 LTS)
=========================================

Что внутри
----------
* Powerups: Freeze, Ghost, Shield, Magnet — рабочие реал-эффекты без Tuples и target-typed new.
* Спавн по таблице PowerupSO[] через PowerupSpawner. Пикап использует PowerupSO.sprite, есть fallback-префаб.
* Нижняя панель (PowerupStash) с лимитом по типу (по умолчанию 1). Клик — активировать.
* Верхняя панель активных эффектов (ActivePowerupUIItem) — круговой таймер (Image.fillAmount).
* Экранный тинт (ScreenTintOverlay) — полноэкранный Image, цвета эффектов смешиваются.
* Freeze: Time.timeScale=0.5, fixedDeltaTime в 2 раза меньше. Доп. множитель ~1.5 на speed/tick полях змеи (через reflection). Замедляет интервалы у всех спавнеров (~×2).
* Ghost: коллайдеры змеи -> isTrigger=true + глобальный флаг для обхода самоколлизии (GhostHookMarker.ShouldBlockSelfCollision()).
* Shield: временный Physics2D.IgnoreCollision на контакте с hazard-слоями.
* Magnet: не требователен к SpriteRenderer — работает с любым Renderer/WorldSpace Canvas. Авто-добавляет лёгкий 2D-триггер и kinematic Rigidbody2D к объектам-еда (теги: Food/Apple/Collectible или любой Renderer). Тянет к ближайшему сегменту змеи; при достижении попадёт в ваш обычный триггер головы и засчитает «съесть».
* Хвост: TailSegmentFixer.OnSegmentSpawned(..) нормализует scale и включает SpriteRenderer (вызовите из вашего SnakeController при создании сегмента).
* Snapshot tool: Tools → Project Snapshot — ZIP скриптов и активной сцены, использует System.IO.Compression.CompressionLevel.Optimal.

Установка (1–2 минуты)
----------------------
1) Скопируйте папку Scripts/ и Editor/ в ваш проект (Assets/). Ничего не ломает существующие классы.
2) Меню: Tools → Powerups → Clean Legacy — удалит старые легаси-компоненты (по известным именам).
3) Меню: Tools → Powerups → Apply Ghost Hook — добавит GhostHookMarker в сцену.
   В SnakeController в месте проверки самоколлизии добавьте гейт:
      if (!SnakeGame.Powerups.EditorTools.GhostHookMarker.ShouldBlockSelfCollision()) {
          // смерть от хвоста
      }
   При активном Ghost возвращает false и вы не умрёте от хвоста.
4) Создайте PowerupSO (Create → Snake → Powerups → Powerup SO) для Freeze/Ghost/Shield/Magnet.
5) Настройте PowerupSpawner: таблица PowerupSO[], fallback-префаб со SpriteRenderer+Collider2D или пустой объект — скрипт сам добавит.
6) UI (по желанию):
   - Нижняя панель: PowerupStash (укажите контейнер и префаб кнопки). Клик по StashItemUI вызовет активацию.
   - Верхняя панель: укажите PowerupSystem.activeContainer и activeItemPrefab (с Image.fillAmount).
   - Тинт: добавьте полноэкранный Image (Canvas Overlay), повесьте ScreenTintOverlay.
7) Shield: на компоненте ShieldContactIgnorer выставьте hazardLayers (например, стены/враги). Активируется автоматически при эффекте.
8) Magnet: ничего на еду ставить не нужно. MagnetRuntime сам найдёт еду (теги: Food/Apple/Collectible или любой Renderer/WorldSpace Canvas) и добавит им лёгкий trigger+Rigidbody2D. Радиус/скорость тяготения настраиваются в MagnetRuntime.

Примечания
----------
* Ограничения C#: без tuples и без target-typed new — соблюдены.
* Если у вас уже есть SnakeController, наш небольшой заглушечный класс будет проигнорирован;
  убедитесь, что классы находятся в разных пространствах имён или удалите заглушку SnakeController в SnakeLocator.cs.
* Freeze слегка ускоряет внутренние speed-поля змеи (~1.5×), чтобы с учётом Time.timeScale=0.5 змея не казалась чересчур медленной.
* Для сторонних спавнеров интервалы «spawnInterval/interval/minSpawnInterval/maxSpawnInterval» замедляются через reflection.
* Magnet тянет только находящиеся в радиусе magnetRadius; скорость и ускорение настраиваемы.

API точек интеграции
--------------------
* TailSegmentFixer: вызовите TailSegmentFixer.OnSegmentSpawned(newSeg) после создания сегмента.
* Ghost Hook: используйте GhostHookMarker.ShouldBlockSelfCollision() в проверке самоколлизии.

Где что лежит
-------------
Assets/Scripts/Powerups/Core/...
Assets/Scripts/Powerups/Effects/...
Assets/Scripts/Powerups/Shared/...
Assets/Editor/...

Удачи! Если что — пингуйте: добавим ваши названия тегов/полей в авто-сканер Magnet/Freeze.
