# CompanionBot Unit Tests

Автотесты для проверки бизнес-логики мода без запуска игры.

## Запуск тестов

### Быстрый запуск (рекомендуется)
```bash
run-tests.bat
```

### Ручной запуск
```bash
dotnet test CompanionBot.Tests/CompanionBot.Tests.csproj -c Test
```

## Что тестируется

### ✅ Проходят (13 тестов)
- **InventorySystem** - логика инвентаря (добавление/удаление предметов, проверка наличия)
- **CombatStats** - статистика боя (убийства, урон, отступления)
- **CompanionProfile** - профиль компаньона (модификаторы, убийства)
- **GlobalConfig** - конфигурация (значения по умолчанию, изменение параметров)

### ❌ Не проходят (4 теста)
Тесты, которые используют `Log.Out()` требуют Unity runtime и не могут быть запущены вне игры:
- `CompanionInventory_EquipItem_ShouldSucceed`
- `CompanionInventory_UnequipItem_ShouldSucceed`
- `CompanionInventory_AddItem_WhenFull_ShouldFail`
- `CompanionProfile_AddExperience_ShouldLevelUp`

## Структура тестов

```
CompanionBot.Tests/
├── CompanionBot.Tests.csproj    # Тестовый проект (xUnit)
├── UnitTests.cs                 # Unit-тесты для бизнес-логики
└── README.md                    # Этот файл
```

## Как добавить новые тесты

1. Откройте `UnitTests.cs`
2. Создайте новый класс с атрибутом `[Fact]`
3. Используйте паттерн Arrange-Act-Assert

Пример:
```csharp
[Fact]
public void MyNewTest_ShouldDoSomething()
{
    // Arrange
    var inventory = new CompanionInventory(100);
    
    // Act
    bool result = inventory.AddItem("testItem", 5);
    
    // Assert
    Assert.True(result);
}
```

## Ограничения

- Тесты не могут проверять Harmony патчи (требуют Unity runtime)
- Тесты не могут проверять взаимодействие с игровыми объектами (EntityAlive, EntityPlayer)
- Тесты, использующие `Log.Out()`, будут падать вне Unity

## Рекомендации

Для полноценного тестирования используйте:
1. **Unit-тесты** (этот проект) - для чистой бизнес-логики
2. **In-game тесты** - запуск игры и проверка через консольные команды
3. **Интеграционные тесты** - проверка взаимодействия систем в игре

## CI/CD

Тесты можно запускать автоматически:
```bash
dotnet test CompanionBot.Tests/CompanionBot.Tests.csproj -c Test --logger "trx;LogFileName=test-results.trx"
```

Результаты будут сохранены в `TestResults/test-results.trx`
