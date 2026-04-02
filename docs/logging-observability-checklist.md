# Чекліст Логування Та Спостережуваності

## Мета

Це робочий чекліст перед наступними етапами rewrite.

Його задача:

1. зафіксувати, які процеси мають бути видимими в логах
2. відділити корисне логування від шуму
3. закрити сліпі зони, які зараз заважають швидко розбирати баги
4. підготувати базу для `Stage 4`, visibility/fog track, інвентарю, магазину й прогресії

## Принципи Хорошого Логування

Мені для ефективної роботи потрібне не `більше` логів, а `правильніші` логи.

Логи мають бути:

1. структурованими через `Diag`, а не випадкові `Debug.Log`
2. причинно-наслідковими, щоб по події було видно `чому` щось сталося
3. прив’язаними до одного owner-а, а не дубльованими в трьох місцях
4. достатньо детальними для дебагу, але без перекачування шуму в кожен кадр
5. стабільними за назвами, щоб по ним можна було будувати звичний mental model

## Що Я Хочу Бачити В Логах

### 1. Session / Boot / Scene Flow

Обов’язково:

1. `General/SessionStarted`
2. `General/SceneLoaded`
3. `Game/LaunchModeResolved`
4. `Scene/TransitionPrepared`
5. `Scene/TransitionStarted`
6. `Scene/TransitionCompleted`
7. `Scene/TransitionCancelled`

Навіщо:

1. розуміти, як саме сцена була запущена: `direct`, `Continue`, `NewGame`
2. бачити, чи scene transition реально відбувся, а не обірвався посеред save
3. швидко відрізняти gameplay-баг від menu/navigation-багу

### 2. Save / Load / Persistence

Обов’язково:

1. `Save/Requested`
2. `Save/Succeeded`
3. `Save/Skipped`
4. `Save/Failed`
5. `Load/Requested`
6. `Load/Succeeded`
7. `Load/Failed`
8. `Load/MigrationUsed`
9. `Hero/AdjustedSavePosition`
10. `Hero/InvalidSavedPosition`

Важливі поля:

1. `reason`
2. `source`
3. `savePath`
4. `hasSave`
5. `worldBytes`
6. `fogBytes`
7. `heroPosition`
8. `fallbackPosition`

Навіщо:

1. зараз persistence ще частково сліпа
2. потрібно швидко ловити, коли баг іде від неправильного save policy, а не від gameplay
3. для `Stage 4` це критично

### 3. World Runtime

Обов’язково:

1. `World/Loaded`
2. `World/Saved`
3. `World/WorldGridReady`
4. `World/MiningHitApplied`
5. `World/TileDestroyed`
6. `World/TilePlaced`
7. `World/TileMoved`
8. `World/MutationRejected`

Важливі поля:

1. `cell`
2. `tile`
3. `previousTile`
4. `nextTile`
5. `reason`
6. `sourceSystem`
7. `hitsApplied`
8. `hitsRequired`

Навіщо:

1. світ у нас єдиний source of truth
2. якщо world runtime мовчить, ми бачимо тільки наслідки в `Hero*` або `ChunkManager`
3. хочу ловити rule rejects прямо на world layer, не тільки в hero layer

### 4. Hero Ground Core

Обов’язково:

1. `Hero/MoveInput`
2. `Hero/StateChanged`
3. `Hero/GroundedChanged`
4. `Hero/FallStarted`
5. `Hero/Landed`
6. `Hero/MoveBlocked`

Потрібні поля:

1. `currentCell`
2. `footCell`
3. `supportCell`
4. `supportTile`
5. `blockerCell`
6. `blockerTile`
7. `velocityX`
8. `velocityY`
9. `inputX`
10. `traversal`

Навіщо:

1. цей шар у нас уже хороший
2. його треба не роздути, а зберегти як еталон добре читабельного gameplay logging

### 5. Hero Mining

Обов’язково:

1. `Hero/DigStarted`
2. `Hero/DigHit`
3. `Hero/DigBlocked`
4. `Hero/DigCompleted`

Потрібні поля:

1. `direction`
2. `currentCell`
3. `targetCell`
4. `tile`
5. `hitIndex`
6. `hitsRequired`
7. `crackStage`
8. `reason`
9. `inputSource`

Навіщо:

1. mining уже покритий добре
2. це один із найкращих поточних шматків observability
3. його формат треба просто зберегти

### 6. Hero Ladder

Обов’язково:

1. `Hero/LadderEntered`
2. `Hero/LadderExited`
3. `Hero/LadderBlocked`

Потрібні поля:

1. `direction`
2. `ladderCell`
3. `currentCell`
4. `reason`
5. `source`
6. `topStanding`
7. `hasPassiveSupport`
8. `hasClimbAction`

Навіщо:

1. ladder була найболючішою зоною
2. зараз `Entered/Exited` у нас уже є
3. але `LadderBlocked` як gameplay event ще фактично відсутній

### 7. Fog / Visibility / Minimap

Обов’язково:

1. `Fog/Loaded`
2. `Fog/Saved`
3. `Fog/Revealed`
4. `Fog/Skipped`
5. `Visibility/Updated`
6. `Visibility/RuleRejected`

Потрібні поля:

1. `center`
2. `reason`
3. `radiusLeftRight`
4. `radiusUp`
5. `radiusDown`
6. `lanternLevel`
7. `discoveredCount`

Навіщо:

1. visibility буде окремим великим треком
2. без цього ми будемо знову ловити “чому туман не відкрився” постфактум

### 8. Stone / Hazard

Обов’язково:

1. `Stone/FallScheduled`
2. `Stone/LandingResolved`
3. `Stone/FallExecuted`
4. `Stone/StartCheckRejected`
5. `Hero/HazardHit` або майбутній `Hero/Killed`

Навіщо:

1. stone flow уже видно добре
2. коли додамо death/game over, hazard logging не повинен будуватись з нуля

### 9. UI / Actions

Обов’язково:

1. `UI/NewGameRequested`
2. `UI/ContinueRequested`
3. `UI/BackRequested`
4. `UI/PlaceLadderRequested`
5. `UI/PlaceLadderCompleted`
6. `UI/PlaceLadderBlocked`

Навіщо:

1. UI не має вирішувати gameplay
2. але UI має лишати слід, коли ініціює дію

## Що Вже Покрито Добре

Зараз уже добре покрито:

1. ground core
2. mining
3. stone fall
4. fog basic reveal flow
5. ladder enter and exit
6. ladder placement через UI

## Сліпі Зони На Зараз

### Критичні

1. `GamePersistenceService` майже без `Diag`
2. `SceneLoader` майже без `Diag`
3. `WorldRuntime` не логить власні rule rejects і mutation decisions
4. `HeroLadder` не має повноцінного `Hero/LadderBlocked`

### Другорядні

1. `CameraFollow` без структурованих логів
2. `Crack` і `StoneView` майже без власних причинно-наслідкових логів
3. imported joystick логічно лишається тихим, і це нормально
4. `HeroCollision`, `HeroGroundSensor`, `HeroWallSensor`, `HeroMotor` логять мало, але зараз їх достатньо покриває `HeroGroundCore`

## Який Мінімум Мені Потрібен Перед Наступними Етапами

Перед `Stage 4` я хочу закрити такий мінімум:

1. повне save/load logging
2. scene transition logging
3. world runtime rejection logging
4. gameplay `LadderBlocked`
5. явні `Fog/Visibility` rule logs на майбутній visibility-track

## Чого Я Не Хочу В Логах

Не треба:

1. логити кожен helper-method
2. логити кожен кадр без зміни стану
3. дублювати одну й ту ж подію на hero, world і chunk рівні одночасно
4. замінювати `Diag` випадковими `Debug.Log`

## Порядок Реалізації

Ось порядок, який дасть найбільше користі:

1. `GamePersistenceService`
2. `SceneLoader`
3. `WorldRuntime`
4. `HeroLadder`
5. `ChunkManager` visibility/fog rules

## Exit Criteria Для Logging Pass

Logging pass можна вважати завершеним, коли:

1. save/load повністю читається по структурованих подіях
2. scene transitions видно від початку до завершення
3. world runtime пояснює основні mutation rejects
4. ladder має не тільки `Entered/Exited`, а й `Blocked`
5. остання чиста smoke session читається без “сліпих плям” у core flow
