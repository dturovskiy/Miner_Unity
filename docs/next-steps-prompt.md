# Prompt Для Наступного Етапу

## Контекст

Проєкт: `miner_unity`

Поточний стан:

1. `Stage 0` завершений
2. `Stage 1` завершений
3. `Stage 2` завершений
4. `Stage 3` завершений
5. core gameplay працює стабільно: ground movement, mining, ladder, save or load базового рівня, stone fall
6. логування core gameplay уже сильне і структуроване

Оновлені документи:

1. [rewrite-master-checklist.md](./rewrite-master-checklist.md)
2. [architecture-rewrite-plan.md](./architecture-rewrite-plan.md)
3. [logging-observability-checklist.md](./logging-observability-checklist.md)

## Що Вже Підтверджено

1. `Unity/Error` і `Unity/Warning` у останніх верифікованих сесіях відсутні
2. `LadderEntered`, `LadderExited`, `LadderBlocked`, `DigStarted`, `DigHit`, `DigBlocked`, `DigCompleted`, `World/MiningHitApplied`, `World/TileDestroyed`, `World/TileMoved`, `Save/*`, `Load/*`, `Scene/*` уже працюють
3. mining-area біля печери й на камені вже виправлена
4. герой, стоячи на камені або на драбині, більше не ламає базову поведінку mining loop

## Що Ще Не Закрито Перед Stage 4

Потрібно добити observability tail:

1. додати `Scene/TransitionCompleted`
2. додати `Visibility/Updated`
3. додати `Visibility/RuleRejected`
4. додати майбутні hazard hooks:
   `Hero/HazardHit`
   `Hero/KillHookTriggered`
   майбутній `Hero/Killed`
5. прогнати фінальну observability smoke session

## Наступний Робочий Крок

Зробити останній observability pass перед `Stage 4`.

Пріоритет:

1. `SceneLoader`
2. visibility or fog owner
3. stone or hero hazard layer
4. smoke test і перевірка логів

## Що Потрібно Зробити

1. Додати в `SceneLoader` явну подію `Scene/TransitionCompleted`.
2. Додати в поточний fog or visibility owner структуровані події:
   `Visibility/Updated`
   `Visibility/RuleRejected`
3. Додати в stone or hero hazard path базові hook-події:
   `Hero/HazardHit`
   `Hero/KillHookTriggered` як заготовку під майбутній game over
   `Hero/Killed` тільки коли реально з’явиться death flow
4. Не роздувати шум:
   логувати тільки зміни стану, причини reject-ів і підсумки
5. Після змін виконати короткий smoke test:
   `New Game`
   `Back -> Continue`
   кілька dig-сценаріїв
   кілька ladder-сценаріїв
   падіння каменю

## Що Важливо Не Зламати

1. не чіпати стабільний mining loop без потреби
2. не чіпати ladder behavior без прямої причини
3. не вносити UI side effects у gameplay runtime
4. не додавати нові gameplay обов’язки в `ChunkManager`

## Exit Criteria

Завдання можна вважати завершеним, коли:

1. `Scene/TransitionCompleted` реально з’являється в логах
2. visibility рішення видно в логах окремо від fog render side effects
3. hazard hooks є в коді та не шумлять
4. остання smoke session читається без сліпих зон
5. після цього можна чесно переходити до `Stage 4`
