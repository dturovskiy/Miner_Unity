# План Очистки Та Реструктуризації

## Мета

Це один об'єднаний документ для:

1. інвентаризації поточного вмісту `Assets`
2. ручного видалення зайвого
3. майбутньої реструктуризації тек
4. розділення власного коду і сторонніх залежностей

Документ спеціально зібраний під ручну чистку, щоб ти міг видаляти й переносити файли без гадань.

## Поточний Стан

Після останньої ручної чистки зараз у верхньому рівні `Assets` залишилось:

1. `Assets/Animations`
2. `Assets/LitJSON`
3. `Assets/Materials`
4. `Assets/MobileDependencyResolver`
5. `Assets/Plugins`
6. `Assets/Prefabs`
7. `Assets/Resources`
8. `Assets/Scenes`
9. `Assets/Scripts`
10. `Assets/Sprites`
11. `Assets/TextMesh Pro`
12. `Assets/MapPlan.md`
13. `Assets/terrain_layout.txt`

Поточний стан `Assets/Scripts` уже значно чистіший:

1. `Assets/Scripts/Diagnostics`
2. `Assets/Scripts/GameRuntime`
3. `Assets/Scripts/Scenes`
4. `Assets/Scripts/SceneSystem`
5. `Assets/Scripts/Terrain`
6. `Assets/Scripts/UI`

Вже прибрано:

1. `Assets/OtherAssets`
2. `Assets/GameController.cs`
3. старий legacy save stack у `Assets/Scripts/Other`
4. `Assets/Scripts/ScreenController`
5. тестові `SaveLoadSystem`-штуки зі сцен

## Що Точно Потрібно Лишити

Лишаємо зараз:

1. `Assets/Plugins/Demigiant/DOTween`
2. `Assets/TextMesh Pro`
3. `Assets/Scripts`
4. `Assets/Scenes`
5. `Assets/Prefabs`
6. `Assets/Sprites`
7. `Assets/Animations`
8. `Assets/Materials`
9. `Assets/Resources/Crack.prefab`

Чому:

1. `DOTween` зараз реально використовується в `FadeScreenPanel` і `HudElement`
2. `TextMesh Pro` реально використовується в menu UI
3. `Resources/Crack.prefab` ще потрібен старому crack-flow, бо `TileBehaviour` вантажить його через `Resources.Load("Crack")`

## Що Можна Видаляти Зараз

Ці речі виглядають як безпечні кандидати на ручне видалення вже зараз:

1. `Assets/LitJSON`

Чому:

1. legacy save stack уже прибраний
2. прямих використань `LitJson` у поточному runtime-коді більше немає

## Що Краще Видаляти Після Короткої Перевірки

Це теж сильні кандидати на видалення, але я б робив це малими партіями з коротким reopen Unity після кожної партії.

Кандидати:

1. `Assets/MobileDependencyResolver`
2. `Assets/Resources/BillingMode.json`
3. `Assets/Resources/MapData.txt`
4. `Assets/Resources/MapCollisions.txt`
5. `Assets/Scripts/SceneSystem/Configs/TestScene.asset`

Чому:

1. `MobileDependencyResolver` схожий на залишок під мобільні SDK, але зараз прямих використань у проекті не видно
2. `BillingMode.json` схожий на старий хвіст від ads/purchasing-конфігурації
3. `MapData.txt` і `MapCollisions.txt` зараз не мають прямих звернень у коді
4. `TestScene.asset` не має підтвердженого production-використання

## Що Не Треба Тримати “Про Запас”

Ось важливе правило: не варто лишати пакети чи ассети тільки “про всяк випадок”.

Для реклами потім не потрібно спеціально зберігати:

1. `Assets/MobileDependencyResolver`
2. `Assets/LitJSON`
3. старі purchasing/ads хвости в `Resources`

Якщо реклама знадобиться пізніше:

1. для Unity Ads її можна буде знову підключити через `Package Manager`
2. для Google Mobile Ads / AdMob плагін теж можна буде додати пізніше через пакет або імпорт плагіна
3. `External Dependency Manager` або аналог зазвичай приїжджає разом із таким SDK або додається окремо під той стек, який реально обереш

Тобто нічого критичного “на майбутню рекламу” зараз тримати не треба.

## Що Можна Переписувати На Місці

Є файли, які не обов'язково перейменовувати або плодити заново. Їх можна спокійно переписувати в нуль під чисту архітектуру.

Нормальні кандидати на rewrite-in-place:

1. `Assets/Scripts/Scenes/MainScene/Hero/HeroController.cs`
2. `Assets/Scripts/Scenes/MainScene/Hero/HeroCollision.cs`
3. `Assets/Scripts/Scenes/MainScene/World/WorldGridService.cs`
4. `Assets/Scripts/Scenes/MainScene/LadderBehaviour.cs`

Що краще тримати як frozen reference:

1. `Assets/Scripts/Scenes/MainScene/MiningController.cs`
2. `Assets/Scripts/Scenes/MainScene/Crack.cs`
3. `Assets/Scripts/Scenes/MainScene/TileBehaviour.cs`

Причина:

1. ці файли ще корисні як референс
2. але вони не є нашою цільовою архітектурою
3. їх краще не розвивати, поки не прийде час відповідного rewrite

## Що Робити З Joystick

Твоя думка тут правильна: `Joystick` не є нашим core-дизайном, а imported asset.

Рекомендація:

1. поки лишити як є
2. якщо вирішимо повністю підлаштувати під себе, не ламати “оригінал” далі
3. замість цього зробити свій fork і вже його змінювати

Куди його потім класти:

1. `Assets/Game/Scripts/Input`
2. або `Assets/Game/ThirdPartyForks/Joystick`

## Цільова Структура

Оновлена цільова структура без `_Project`:

1. `Assets/Game`
2. `Assets/ThirdParty`

Рекомендована структура всередині `Assets/Game`:

1. `Assets/Game/Art/Animations`
2. `Assets/Game/Art/Materials`
3. `Assets/Game/Art/Sprites`
4. `Assets/Game/Content/Scenes`
5. `Assets/Game/Content/Prefabs`
6. `Assets/Game/Content/Resources`
7. `Assets/Game/Data/Configs`
8. `Assets/Game/Data/Maps`
9. `Assets/Game/Docs`
10. `Assets/Game/Scripts`

Рекомендована структура всередині `Assets/ThirdParty`:

1. `Assets/ThirdParty/DOTween`
2. `Assets/ThirdParty/TextMeshPro`
3. `Assets/ThirdParty/MobileDependencyResolver`
4. `Assets/ThirdParty/JoystickImported`

Останній пункт умовний:

1. якщо захочемо фізично відокремити imported joystick від свого коду
2. якщо ні, можна просто пізніше зробити fork і не чіпати старий варіант

## План Переносу Тек

Коли дійдемо до реального переносу структури, цільова карта така:

1. `Assets/Animations` -> `Assets/Game/Art/Animations`
2. `Assets/Materials` -> `Assets/Game/Art/Materials`
3. `Assets/Sprites` -> `Assets/Game/Art/Sprites`
4. `Assets/Prefabs` -> `Assets/Game/Content/Prefabs`
5. `Assets/Scenes` -> `Assets/Game/Content/Scenes`
6. `Assets/Resources` -> `Assets/Game/Content/Resources`
7. `Assets/Scripts` -> `Assets/Game/Scripts`
8. `Assets/MapPlan.md` -> `Assets/Game/Docs/MapPlan.md`
9. `Assets/terrain_layout.txt` -> `Assets/Game/Data/Maps/terrain_layout.txt`
10. `Assets/Scripts/SceneSystem/Configs/*.asset` -> `Assets/Game/Data/Configs/Scenes`
11. `Assets/Plugins/Demigiant/DOTween` -> `Assets/ThirdParty/DOTween`
12. `Assets/TextMesh Pro` -> `Assets/ThirdParty/TextMeshPro`

Умовний перенос:

1. `Assets/MobileDependencyResolver` -> `Assets/ThirdParty/MobileDependencyResolver`, якщо вирішиш не видаляти його зараз

## Порядок Ручної Очистки

Я б радив такий порядок:

1. видалити `Assets/LitJSON`
2. відкрити Unity і дочекатись реімпорту
3. перевірити `MenuScene` і `MainScene`
4. потім прибрати `Assets/MobileDependencyResolver`
5. знову відкрити Unity і перевірити сцени
6. потім окремо перевірити й прибрати `BillingMode.json`, `MapData.txt`, `MapCollisions.txt`
7. в кінці прибрати `TestScene.asset`, якщо він точно ніде не потрібен

## Пакети Unity З Packages/manifest.json

За поточним кодом сильні кандидати на прибирання:

1. `com.unity.ads`
2. `com.unity.analytics`
3. `com.unity.ai.navigation`
4. `com.unity.collab-proxy`
5. `com.unity.multiplayer.center`
6. `com.unity.purchasing`
7. `com.unity.test-framework`
8. `com.unity.timeline`
9. `com.unity.visualscripting`
10. `com.unity.xr.legacyinputhelpers`
11. `com.unity.ide.vscode`

Що залишити:

1. `com.unity.2d.sprite`
2. `com.unity.2d.tilemap`
3. `com.unity.ugui`
4. `com.unity.ide.visualstudio`, якщо користуєшся ним
5. всі `com.unity.modules.*`

## Питання Про Рекламу На Майбутнє

Якщо пізніше захочеш рекламу:

1. Unity Ads можна буде підключити заново через `Package Manager`
2. офіційна документація рекомендує ставити пакет `Advertisement Legacy` з Unity Package Manager або за назвою `com.unity.ads`
3. Google Mobile Ads / AdMob теж можна додати пізніше через пакетний шлях або імпорт плагіна
4. такі SDK зазвичай самі приносять потрібний dependency manager або вимагають його окремо під свій стек

Тобто зараз тримати старі рекламні хвости в проекті не потрібно.

## Чому Раніше Я Пропонував `_Project`

Причина була суто практична, не технічна:

1. `_Project` часто ставлять, щоб власні теки були зверху списку
2. це швидко відділяє свій код від imported assets
3. це просто звичка з великих проектів, а не правило Unity

Але це не обов'язково.

Для вашого проекту зараз я вважаю кращим і чистішим:

1. `Assets/Game`
2. `Assets/ThirdParty`

Так воно читається простіше і виглядає природніше.

## Важлива Примітка

Великі переноси тек краще робити всередині Unity, а не через Explorer.

Причина:

1. Unity збереже `.meta`
2. GUID не зламаються
3. сцени й префаби не втратять посилання
