# План Очищення Та Реструктуризації

## Мета

Це робочий документ для поточного стану репозиторію після cleanup і першої хвилі реструктуризації.

Він відповідає на три питання:

1. що вже впорядковано
2. що ще свідомо не чіпаємо
3. де тепер лежать ключові скрипти та data-асети

## Поточний Стан Assets

У верхньому рівні `Assets` зараз важливі такі теки:

1. `Assets/Scripts`
2. `Assets/Data`
3. `Assets/Scenes`
4. `Assets/Prefabs`
5. `Assets/Resources`
6. `Assets/Animations`
7. `Assets/Materials`
8. `Assets/Sprites`
9. `Assets/Plugins`
10. `Assets/Text Mesh Pro`
11. `Assets/MobileDependencyResolver`

Також окремо ще лежать:

1. `Assets/MapPlan.md`
2. `Assets/terrain_layout.txt`

## Що Вже Впорядковано

Ми вже прибрали або винесли з `Scripts` усе, що не мало там лежати:

1. menu texts винесені в `Assets/Data/MenuText`
2. scene config assets винесені в `Assets/Data/SceneConfigs`
3. terrain tile assets і `TileAtlas.asset` винесені в `Assets/Data/TerrainRegistry`

Це важливо, бо `Assets/Scripts` тепер знову є папкою зі скриптами, а не змішаним складом із `.asset` і `.txt`.

## Поточна Структура Scripts

Зараз структура скриптів така:

1. `Assets/Scripts/Diagnostics/Runtime`
2. `Assets/Scripts/GameRuntime`
3. `Assets/Scripts/Gameplay/Camera`
4. `Assets/Scripts/Gameplay/Hero`
5. `Assets/Scripts/Gameplay/Hero/Diagnostics`
6. `Assets/Scripts/Gameplay/Ladders`
7. `Assets/Scripts/Gameplay/Mining/Legacy`
8. `Assets/Scripts/Gameplay/World`
9. `Assets/Scripts/Input/JoystickImported`
10. `Assets/Scripts/SceneSystem`
11. `Assets/Scripts/Terrain/Core`
12. `Assets/Scripts/Terrain/Generation`
13. `Assets/Scripts/Terrain/Registry`
14. `Assets/Scripts/Terrain/Rendering`
15. `Assets/Scripts/Terrain/Simulation`
16. `Assets/Scripts/UI/Common`
17. `Assets/Scripts/UI/Menu`

## Логіка Нового Розкладання

Ми сортуємо не “по сценах”, а “по відповідальності”.

Приклади:

1. hero-логіка живе в `Gameplay/Hero`
2. логічний world facade живе в `Gameplay/World`
3. terrain runtime розбитий на `Core`, `Generation`, `Registry`, `Rendering`, `Simulation`
4. imported joystick винесений в окремий `Input/JoystickImported`
5. menu UI-скрипти винесені в `UI/Menu`

## Що Свідомо Лишаємо Як Є

Поки що лишаємо без агресивного cleanup:

1. `Assets/MobileDependencyResolver`
2. `Assets/Plugins/Demigiant/DOTween`
3. `Assets/Text Mesh Pro`
4. `Assets/MapPlan.md`
5. `Assets/terrain_layout.txt`

Причини:

1. `MobileDependencyResolver` повернувся після видалення, бо його ще тягне monetization stack
2. `DOTween` і `Text Mesh Pro` реально використовуються
3. `MapPlan.md` і `terrain_layout.txt` не входять у поточний runtime, але поки що можуть лишатися як reference/archive

## Статус Cleanup

`Stage 0` закрито.

Що зафіксовано на фіналі:

1. `PlayButton` більше не викликає прямий reset save-стану і лише передає режим запуску в gameplay bootstrap
2. save/load, spawn героя і старт камери працюють стабільно після ручних smoke-тестів
3. дубльований набір hero-компонентів прибрано з `MainScene`, тому hero-діагностика більше не повинна подвоюватись
4. `Heartbeat` і boot-лог приведені до читабельного рівня

## Що Далі Не Чіпаємо Без Окремого Рішення

Без окремого рішення не чіпаємо:

1. ads/purchasing stack
2. `MobileDependencyResolver`
3. legacy reference-файли для мапи

Це вже окремий cleanup-етап, не частина `Stage 1`.

## Практичний Висновок

Поточний стан хороший для продовження rewrite.

Ми вже:

1. очистили репозиторій від явного legacy-сміття
2. розклали скрипти по змістовних теках
3. винесли data-асети зі `Scripts`
4. зберегли працездатну локальну збірку

На цьому етапі правильний наступний фокус:

1. перейти в `Stage 1 - Ground Core`
2. не повертатись до cleanup-робіт без окремої причини

## Окрема Нотатка По Видимості Та Туману Війни

Цю тему більше не трактуємо як дрібний баг cleanup.

Зафіксовано:

1. поточна fog-логіка в `ChunkManager` прив’язана до `TileID.Tunnel` і не підходить для фінального digging loop
2. нам треба окремо розділити `постійно відкриту карту для мінімапи` і `тимчасову видимість від ліхтаря героя`
3. `HiddenArea` має лишитись view-рівнем, а не власником правил відкриття
4. радіус видимості має залежати від рівня ліхтаря, а не бути захардкоженим назавжди
5. `Tunnel` треба трактувати як сімейство бекграундних спрайтів для відкритого простору, а не як остаточний gameplay-сенс клітинки
6. у проєкті вже є база для цього рендеру: `Tunnel`, `Tunnel_Top`, `Tunnel_Bottom`, але логіка вибору варіанту ще не оформлена як окрема система

Це винесено в окремий visibility-track у головному плані rewrite і не блокує старт `Stage 1`.
