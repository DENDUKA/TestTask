# Импорт справочника терминалов

Сервис для периодического импорта данных о терминалах из JSON-файла в базу данных PostgreSQL. Реализован как фоновая задача (BackgroundService) с использованием Quartz.NET.

## 🚀 Реализованный функционал

### 1. Импорт данных
- **Источник:** JSON-файл (`Infrastructure/Files/terminals.json`).
- **Десериализация:** Используется `System.Text.Json` в режиме `PropertyNameCaseInsensitive = true`.
- **Периодичность:** Запуск ежедневно в **02:00 MSK** (учитывается часовой пояс, независимо от системного времени сервера).
- **Стратегия обновления:** Полная замена данных (Full Replace) в рамках одной транзакции:
  1. Удаление всех существующих записей (телефонов и офисов).
  2. Массовая вставка (Bulk Insert) новых данных.
  3. Если файл пуст или невалиден, старые данные не удаляются.

### 2. Производительность и оптимизация
- **Bulk Operations:** Используется `EF Core Batching` (`AddRangeAsync`) для эффективной вставки большого количества связанных данных (Офисы + Телефоны).
- **Индексы:** Добавлены индексы БД для полей `Code` и `CityCode` для ускорения выборок (миграция `AddIndexes`).
- **Скорость:** Время полного цикла импорта (чтение + парсинг + замена в БД) для ~300 записей составляет менее **1 секунды**.

### 3. Инфраструктура
- **Docker:** Автоматическое развертывание контейнера `postgres:latest` при старте приложения.
- **Миграции:** Автоматическое применение миграций EF Core при запуске.
- **Логирование:** Структурированное логирование всех этапов (Start, Parse, DB Operations, Error).
- **Graceful Shutdown:** Корректная обработка остановки приложения (ожидание завершения активных джобов).

## 🛠 Технический стек

- **Платформа:** .NET 9 (Console Application / Worker Service)
- **Планировщик:** Quartz.NET
- **База данных:** PostgreSQL
- **ORM:** Entity Framework Core 9
- **Контейнеризация:** Docker.DotNet (для управления контейнерами из кода)

## ⚙️ Настройка и запуск

### Предварительные требования
- .NET 9 SDK
- Docker Desktop (должен быть запущен)

### Конфигурация (`appsettings.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=dellin_dictionary;Username=postgres;Password=postgres"
  },
  "CronSettings": {
    "Schedule": "0 0 2 * * ?" 
  },
  "DockerSettings": {
    "ContainerName": "postgres-db",
    "Image": "postgres:latest",
    "Port": 5432,
    "Password": "postgres"
  }
}
```

### Запуск
```bash
dotnet run --project task/TestTask.csproj
```

При первом запуске приложение:
1. Проверит и запустит Docker-контейнер с PostgreSQL.
2. Создаст базу данных `dellin_dictionary`.
3. Применит миграции (создаст таблицы и индексы).
4. Запустит задачу импорта (сразу при старте + запланирует на 02:00 MSK).

## 📊 Структура БД

### Таблица `Offices`
- `Id` (PK)
- `Code` (Indexed) - Код терминала
- `CityCode` (Indexed) - Код города
- `Type` - Тип (PVZ / WAREHOUSE)
- `Coordinates` (Owned Type) - Широта/Долгота
- `Address...` - Адресные поля
- `WorkTime` - Время работы

### Таблица `Phones`
- `Id` (PK)
- `OfficeId` (FK) - Ссылка на офис
- `PhoneNumber` - Номер
- `Additional` - Дополнительная информация
