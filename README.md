# wkr-rul-system — руководство по запуску и работе с проектом

Для исполнения большинства скриптов в ps нужно расширить политику выполнения Set-ExecutionPolicy -Scope CurrentUser RemoteSigned

## 1. Назначение проекта

`wkr-rul-system` — демонстрационный программный комплекс для ВКР, предназначенный для сбора телеметрии шпинделя, расчёта производных параметров процесса резания, формирования признаков для модели машинного обучения и прогноза остаточного ресурса режущего инструмента.

Общая логика проекта:

```text
Телеметрия станка / тестовый источник
        ↓
Backend ASP.NET Core Web API
        ↓
CuttingMath.cs: расчёт v, Ne, Pz
        ↓
PostgreSQL: архив телеметрии
        ↓
RulWorker: выбор окна резания
        ↓
FeatureExtractor.cs: признаки mean/std/slope
        ↓
ML-service FastAPI + XGBoost
        ↓
RUL-прогноз + уровень состояния
        ↓
PostgreSQL: журнал прогнозов и тревог
```

Проект связан с ВКР следующим образом:

- **PostgreSQL** хранит телеметрию, прогнозы, инструменты и тревожные события.
- **Backend** выполняет роль серверного уровня АСУ ТП: принимает данные, считает производные параметры, сохраняет телеметрию, вызывает ML-service и записывает результат.
- **ML-service** содержит готовую модель XGBoost и возвращает прогноз RUL.
- **Flyway** управляет структурой базы данных через SQL-миграции.
- **Adminer** используется как простой web-интерфейс для просмотра PostgreSQL.
- **Collector** будет добавлен позже как отдельный сервис для сбора данных от внешней среды / станка / эмулятора.

---

## 2. Текущая архитектура проекта

Примерная структура проекта:

```text
wkr-rul-system/
│
├── backend/
│   └── Backend.Api/
│       ├── Controllers/
│       ├── Data/
│       ├── Models/
│       ├── Services/
│       ├── Backend.Api.csproj
│       ├── Program.cs
│       └── Dockerfile
│
├── db/
│   └── migrations/
│       ├── V1__schema.sql
│       ├── V2__routines.sql
│       ├── V3__seed.sql
│       └── V4__predictions_and_alarms.sql
│
├── infra/
│   └── docker-compose.yml
│
├── ml-service/
│   ├── app.py
│   ├── feature_contract.py
│   ├── train_xgb.py
│   ├── make_demo_train_features.py
│   ├── requirements.txt
│   ├── Dockerfile
│   ├── model.json
│   ├── metrics.json
│   └── .dockerignore
│
└── scripts/
    ├── start-clean.ps1
    ├── start.ps1
    ├── check.ps1
    ├── db-port.ps1
    └── test-lifecycle.ps1
```

Если папки `scripts` пока нет, её можно создать вручную. Готовые команды для скриптов приведены ниже.

---

## 3. Сервисы Docker Compose

В `infra/docker-compose.yml` используются следующие сервисы.

### 3.1 `db`

PostgreSQL 16. Хранит:

- `wkr.telemetry_spindle` — телеметрия шпинделя;
- `wkr.tools` — справочник инструмента;
- `wkr.rul_predictions` — журнал прогнозов остаточного ресурса;
- `wkr.alarm_events` — журнал предупреждений и критических событий;
- `flyway_schema_history` — служебная таблица Flyway.

### 3.2 `flyway`

Сервис для применения SQL-миграций. Он не должен работать постоянно.

Логика работы:

```text
запустился → подключился к PostgreSQL → применил новые SQL-файлы → завершился
```

### 3.3 `adminer`

Web-интерфейс для просмотра PostgreSQL.

Открывается по адресу:

```text
http://localhost:8080
```

### 3.4 `ml`

Python/FastAPI сервис с моделью XGBoost.

Открывается по адресу:

```text
http://localhost:8001
```

Основные endpoints:

```text
GET  /health
GET  /model/info
POST /predict
```

### 3.5 `backend`

ASP.NET Core Web API.

Открывается по адресу:

```text
http://localhost:8000
```

Основные endpoints:

```text
GET  /health
GET  /swagger
POST /api/telemetry
GET  /api/rul/last
GET  /api/alarms/last
```

---

## 4. Основные Docker Compose теги

### 4.1 `services`

Главный блок, в котором описываются контейнеры проекта.

Пример:

```yaml
services:
  db:
  flyway:
  adminer:
  ml:
  backend:
```

Каждый вложенный элемент — отдельный сервис.

---

### 4.2 `image`

Используется, когда контейнер запускается из готового образа.

Пример:

```yaml
db:
  image: postgres:16
```

Это значит: Docker скачает и запустит готовый образ PostgreSQL 16.

---

### 4.3 `build`

Используется, когда образ нужно собрать из исходного кода.

Пример:

```yaml
backend:
  build:
    context: ../backend/Backend.Api
    dockerfile: Dockerfile
```

Это значит:

```text
Docker зайдёт в папку ../backend/Backend.Api,
найдёт Dockerfile,
соберёт образ backend.
```

---

### 4.4 `context`

Папка, которую Docker видит во время сборки.

Важно: если `context` указан неправильно, Docker не увидит `.csproj`, `Program.cs`, `app.py` или `requirements.txt`.

---

### 4.5 `dockerfile`

Имя Dockerfile внутри `context`.

Обычно:

```yaml
dockerfile: Dockerfile
```

---

### 4.6 `environment`

Переменные окружения внутри контейнера.

Пример backend:

```yaml
environment:
  ASPNETCORE_URLS: http://+:8000
  ConnectionStrings__Pg: Host=db;Port=5432;Database=wkr;Username=wkr;Password=wkr;GSS Encryption Mode=Disable
  Ml__BaseUrl: http://ml:8001
  Rul__WindowSize: "50"
  Rul__PeriodSeconds: "5"
```

Для .NET двойное подчёркивание `__` означает вложенность настроек.

Например:

```text
ConnectionStrings__Pg
```

соответствует:

```json
{
  "ConnectionStrings": {
    "Pg": "..."
  }
}
```

---

### 4.7 `ports`

Проброс портов из контейнера на Windows.

Пример:

```yaml
ports:
  - "8000:8000"
```

Означает:

```text
localhost:8000 на Windows → порт 8000 внутри контейнера
```

Для PostgreSQL лучше использовать динамический порт:

```yaml
ports:
  - "127.0.0.1::5432"
```

Это означает:

```text
Docker сам выберет свободный порт на Windows
и пробросит его на 5432 внутри контейнера.
```

Проверить текущий порт PostgreSQL:

```powershell
docker compose port db 5432
```

Пример результата:

```text
127.0.0.1:62676
```

Значит pgAdmin4 должен подключаться к:

```text
Host: 127.0.0.1
Port: 62676
Database: wkr
Username: wkr
Password: wkr
```

---

### 4.8 `volumes`

Используются для хранения данных или подключения папок.

Пример PostgreSQL:

```yaml
volumes:
  - pgdata:/var/lib/postgresql/data
```

Это значит: данные PostgreSQL хранятся не внутри контейнера, а в отдельном Docker volume.

Если выполнить:

```powershell
docker compose down
```

контейнеры удалятся, но база останется.

Если выполнить:

```powershell
docker compose down -v
```

удалятся контейнеры и volume с базой данных.

---

### 4.9 bind-volume

Пример:

```yaml
volumes:
  - ../db/migrations:/flyway/sql
```

Это подключает папку проекта `db/migrations` внутрь контейнера Flyway как `/flyway/sql`.

Именно поэтому Flyway видит твои SQL-файлы.

---

### 4.10 `depends_on`

Указывает порядок запуска сервисов.

Пример:

```yaml
backend:
  depends_on:
    db:
      condition: service_healthy
    ml:
      condition: service_healthy
```

Это значит: backend будет запущен только после того, как `db` и `ml` станут healthy.

---

### 4.11 `healthcheck`

Проверяет, готов ли сервис к работе.

Пример PostgreSQL:

```yaml
healthcheck:
  test: ["CMD-SHELL", "pg_isready -U wkr -d wkr"]
  interval: 40s
  timeout: 30s
  retries: 10
```

Если БД отвечает, контейнер получает статус:

```text
healthy
```

---

## 5. Dockerfile backend — объяснение

Файл:

```text
backend/Backend.Api/Dockerfile
```

Пример:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Backend.Api.csproj ./
RUN dotnet restore Backend.Api.csproj

COPY . ./
RUN dotnet publish Backend.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8000
ENTRYPOINT ["dotnet", "Backend.Api.dll"]
```

### Что происходит

1. Берётся .NET SDK образ.
2. Копируется `.csproj`.
3. Выполняется `dotnet restore`.
4. Копируется весь код backend.
5. Выполняется `dotnet publish`.
6. Берётся лёгкий runtime-образ ASP.NET.
7. В него копируется опубликованное приложение.
8. Запускается `Backend.Api.dll`.

---

## 6. Dockerfile ML-service — объяснение

Файл:

```text
ml-service/Dockerfile
```

Пример:

```dockerfile
FROM python:3.11-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

EXPOSE 8001

CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "8001"]
```

### Что происходит

1. Берётся Python 3.11.
2. Создаётся рабочая папка `/app`.
3. Копируется `requirements.txt`.
4. Устанавливаются Python-зависимости.
5. Копируются `app.py`, `model.json`, `metrics.json` и остальные файлы.
6. Запускается FastAPI через Uvicorn.

Важно: для локального Python тоже лучше использовать Python 3.11, а не 3.14.

---

## 7. Чистый запуск проекта с нуля

Используй этот сценарий, если:

- база сломалась;
- миграции были сильно изменены;
- проект переносится на новый ПК;
- нужно проверить развёртывание с нуля.

Команды:

```powershell
cd C:\Users\grib9\wkr-rul-system\infra

docker compose down -v

docker compose up -d db

docker compose --profile migrate run --rm flyway

docker compose up -d adminer
docker compose up -d --build ml backend

docker compose ps
```

### Что должно получиться

В `docker compose ps` должны быть контейнеры:

```text
infra-db-1        Up (healthy)
infra-ml-1        Up (healthy)
infra-backend-1   Up
infra-adminer-1   Up
```

---

## 8. Обычный запуск проекта

Используй, если база уже создана и миграции применены.

```powershell
cd C:\Users\grib9\wkr-rul-system\infra

docker compose up -d adminer

docker compose up -d ml backend

docker compose ps
```

Если менял код backend или ML-service:

```powershell
docker compose up -d --build ml backend
```

Если менял только backend:

```powershell
docker compose up -d --build backend
```

Если менял только ML-service:

```powershell
docker compose up -d --build ml
```

---

## 9. Запуск Flyway вручную

Используй, когда добавил новый SQL-файл миграции:

```text
V5__something.sql
V6__new_table.sql
```

Команды:

```powershell
cd C:\Users\grib9\wkr-rul-system\infra

docker compose up -d db

docker compose run --rm flyway
```

Проверка таблиц:

```powershell
docker compose exec db psql -U wkr -d wkr -c "\dt wkr.*"
```

Проверка функций:

```powershell
docker compose exec db psql -U wkr -d wkr -c "\df wkr.*"
```

Проверка истории Flyway:

```powershell
docker compose exec db psql -U wkr -d wkr -c "select installed_rank, version, description, success, installed_on from flyway_schema_history order by installed_rank;"
```

---

## 10. Подключение к PostgreSQL через pgAdmin4

Сначала узнай порт PostgreSQL:

```powershell
cd C:\Users\grib9\wkr-rul-system\infra

docker compose port db 5432
```

Пример:

```text
127.0.0.1:62676
```

В pgAdmin4:

```text
Host name/address: 127.0.0.1
Port: 62676
Maintenance database: wkr
Username: wkr
Password: wkr
```

Если контейнер пересоздавался, порт может измениться. Тогда снова выполни:

```powershell
docker compose port db 5432
```

---

## 11. Подключение к БД через Adminer

Открой:

```text
http://localhost:8080
```

Параметры:

```text
System: PostgreSQL
Server: db
Username: wkr
Password: wkr
Database: wkr
```

Почему `Server: db`, а не `127.0.0.1`?

Потому что Adminer работает внутри Docker-сети, где PostgreSQL доступен по имени сервиса `db`.

---

## 12. Проверка backend

Проверка health:

```powershell
Invoke-RestMethod http://localhost:8000/health
```

Ожидаемый результат:

```json
{
  "ok": true,
  "service": "backend"
}
```

Swagger:

```text
http://localhost:8000/swagger
```

Логи backend:

```powershell
docker compose logs --tail 150 backend
```

Следить за логами в реальном времени:

```powershell
docker compose logs -f backend
```

---

## 13. Проверка ML-service

Проверка health:

```powershell
Invoke-RestMethod http://localhost:8001/health
```

Паспорт модели:

```powershell
Invoke-RestMethod http://localhost:8001/model/info
```

Проверка внутри контейнера:

```powershell
docker compose exec ml python -c "import json; m=json.load(open('/app/metrics.json', encoding='utf-8')); print(len(m['features'])); print(m['features'])"
```

Ожидаемый результат:

```text
18
['p_mean', 'p_std', 'p_slope', 'i_mean', 'i_std', 'i_slope', 'rpm_mean', 'rpm_std', 'rpm_slope', 'v_mean', 'v_std', 'v_slope', 'ne_mean', 'ne_std', 'ne_slope', 'pz_mean', 'pz_std', 'pz_slope']
```

---

## 14. Проверка базы данных

Проверить таблицы:

```powershell
docker compose exec db psql -U wkr -d wkr -c "\dt wkr.*"
```

Проверить количество телеметрии:

```powershell
docker compose exec db psql -U wkr -d wkr -c "select count(*) from wkr.telemetry_spindle;"
```

Проверить последние прогнозы:

```powershell
docker compose exec db psql -U wkr -d wkr -c "select id, ts, machine_id, tool_id, rul_minutes, alarm_level, alarm_code, state, model_version from wkr.rul_predictions order by ts desc limit 10;"
```

Проверить последние тревоги:

```powershell
docker compose exec db psql -U wkr -d wkr -c "select id, ts, machine_id, tool_id, rul_minutes, alarm_level, alarm_code, alarm_message from wkr.alarm_events order by ts desc limit 10;"
```

Посмотреть признаки и объяснение последнего прогноза:

```powershell
docker compose exec db psql -U wkr -d wkr -c "select id, rul_minutes, alarm_code, features_json, explanation_json from wkr.rul_predictions order by ts desc limit 1;"
```

---

## 15. Очистка тестовых данных

Если нужно удалить тестовую телеметрию, прогнозы и тревоги:

```powershell
docker compose exec db psql -U wkr -d wkr -c "truncate table wkr.alarm_events, wkr.rul_predictions, wkr.telemetry_spindle restart identity;"
```

Это удалит данные, но не удалит таблицы.

---

## 16. Проверка полного жизненного цикла

Цель проверки:

```text
новый инструмент → нормальное состояние → предупреждение → критическое состояние
```

Перед тестом можно очистить старые данные:

```powershell
docker compose exec db psql -U wkr -d wkr -c "truncate table wkr.alarm_events, wkr.rul_predictions, wkr.telemetry_spindle restart identity;"
```

---

### 16.1 Нормальное состояние

```powershell
$uri = "http://localhost:8000/api/telemetry"
$machineId = "HAAS_VF2_NGC_01"
$toolId = "T12"
$diameter = 10.0

for ($i = 0; $i -lt 70; $i++) {
  $body = @{
    ts = (Get-Date).ToUniversalTime().ToString("o")
    machineId = $machineId
    toolId = $toolId
    spindleRpm = 8200
    spindleCurrentA = [single](10.0 + (Get-Random -Minimum -10 -Maximum 10) / 100.0)
    spindlePowerKw = [single](2.1 + (Get-Random -Minimum -10 -Maximum 10) / 100.0)
    feedMmMin = 1200
    program = "OP10_NORMAL_TEST"
    cutFlag = $true
    toolDiameterMm = [single]$diameter
    spindleTorqueNm = $null
  } | ConvertTo-Json

  Invoke-RestMethod -Method Post -Uri $uri -Body $body -ContentType "application/json" | Out-Null
  Start-Sleep -Milliseconds 80
}

Start-Sleep -Seconds 10
```

Проверка:

```powershell
docker compose exec db psql -U wkr -d wkr -c "select id, ts, rul_minutes, alarm_level, alarm_code, state from wkr.rul_predictions order by ts desc limit 5;"
```

Ожидаемо:

```text
TOOL_RUL_OK
normal
alarm_level = 0
```

---

### 16.2 Предупреждение

```powershell
$uri = "http://localhost:8000/api/telemetry"
$machineId = "HAAS_VF2_NGC_01"
$toolId = "T12"
$diameter = 10.0

for ($i = 0; $i -lt 80; $i++) {
  $wear = $i / 79.0

  $body = @{
    ts = (Get-Date).ToUniversalTime().ToString("o")
    machineId = $machineId
    toolId = $toolId
    spindleRpm = [int](8200 + (Get-Random -Minimum -40 -Maximum 40))
    spindleCurrentA = [single](18.0 + 4.0 * $wear + (Get-Random -Minimum -20 -Maximum 20) / 100.0)
    spindlePowerKw = [single](5.0 + 1.5 * $wear + (Get-Random -Minimum -10 -Maximum 10) / 100.0)
    feedMmMin = 1200
    program = "OP10_WARNING_TEST"
    cutFlag = $true
    toolDiameterMm = [single]$diameter
    spindleTorqueNm = $null
  } | ConvertTo-Json

  Invoke-RestMethod -Method Post -Uri $uri -Body $body -ContentType "application/json" | Out-Null
  Start-Sleep -Milliseconds 80
}

Start-Sleep -Seconds 10
```

Проверка:

```powershell
docker compose exec db psql -U wkr -d wkr -c "select id, ts, rul_minutes, alarm_level, alarm_code, state from wkr.rul_predictions order by ts desc limit 10;"
```

Если RUL ниже 60 минут:

```text
TOOL_RUL_WARNING
warning
alarm_level = 1
```

---

### 16.3 Критическое состояние

```powershell
$uri = "http://localhost:8000/api/telemetry"
$machineId = "HAAS_VF2_NGC_01"
$toolId = "T12"
$diameter = 10.0

for ($i = 0; $i -lt 100; $i++) {
  $wear = $i / 99.0

  $body = @{
    ts = (Get-Date).ToUniversalTime().ToString("o")
    machineId = $machineId
    toolId = $toolId
    spindleRpm = [int](8200 + (Get-Random -Minimum -80 -Maximum 80))
    spindleCurrentA = [single](22.0 + 3.0 * $wear + (Get-Random -Minimum -30 -Maximum 30) / 100.0)
    spindlePowerKw = [single](7.0 + 1.2 * $wear + (Get-Random -Minimum -20 -Maximum 20) / 100.0)
    feedMmMin = 1200
    program = "OP10_CRITICAL_TEST"
    cutFlag = $true
    toolDiameterMm = [single]$diameter
    spindleTorqueNm = $null
  } | ConvertTo-Json

  Invoke-RestMethod -Method Post -Uri $uri -Body $body -ContentType "application/json" | Out-Null
  Start-Sleep -Milliseconds 80
}

Start-Sleep -Seconds 10
```

Проверка:

```powershell
docker compose exec db psql -U wkr -d wkr -c "select id, ts, rul_minutes, alarm_level, alarm_code, state, required_action from wkr.rul_predictions order by ts desc limit 10;"
```

Если RUL ниже 15 минут:

```text
TOOL_RUL_STOP
critical
alarm_level = 2
```

---

## 17. Проверка через API

Последний прогноз:

```powershell
Invoke-RestMethod "http://localhost:8000/api/rul/last?machineId=HAAS_VF2_NGC_01&toolId=T12"
```

Последняя тревога:

```powershell
Invoke-RestMethod "http://localhost:8000/api/alarms/last?machineId=HAAS_VF2_NGC_01&toolId=T12"
```

Если тревоги нет, это значит, что состояние пока нормальное.

---

## 18. Обучение ML-модели локально

Перейти в папку ML-service:

```powershell
cd C:\Users\grib9\wkr-rul-system\ml-service
```

Создать виртуальное окружение на Python 3.11:

```powershell
py -3.11 -m venv .venv
```

Активировать:

```powershell
.\.venv\Scripts\activate
```

Обновить pip:

```powershell
python -m pip install --upgrade pip
```

Установить зависимости:

```powershell
pip install -r requirements.txt
```

Сгенерировать демонстрационный датасет:

```powershell
python .\make_demo_train_features.py
```

Обучить модель:

```powershell
python .\train_xgb.py
```

Проверить `metrics.json`:

```powershell
python -c "import json; m=json.load(open('metrics.json', encoding='utf-8')); print(m['model_version']); print(len(m['features'])); print(m['mae'], m['rmse'], m['r2'])"
```

Ожидаемо:

```text
xgb_rul_v3_formula_features
18
...
```

После обучения пересобрать ML-service:

```powershell
cd C:\Users\grib9\wkr-rul-system\infra

docker compose build ml --no-cache

docker compose up -d ml
```

---

## 19. Полезные команды Docker

### Список контейнеров проекта

```powershell
docker compose ps
```

### Все контейнеры Docker

```powershell
docker ps -a
```

### Логи всех сервисов

```powershell
docker compose logs --tail 100
```

### Логи конкретного сервиса

```powershell
docker compose logs --tail 100 backend

docker compose logs --tail 100 ml

docker compose logs --tail 100 db
```

### Следить за логами

```powershell
docker compose logs -f backend
```

### Перезапуск сервиса

```powershell
docker compose restart backend
```

### Остановить проект без удаления базы

```powershell
docker compose down
```

### Остановить проект с удалением базы

```powershell
docker compose down -v
```

### Пересобрать backend

```powershell
docker compose build backend --no-cache

docker compose up -d backend
```

### Пересобрать ML-service

```powershell
docker compose build ml --no-cache

docker compose up -d ml
```

### Посмотреть образы

```powershell
docker images
```

### Посмотреть volume

```powershell
docker volume ls
```

### Удалить неиспользуемые контейнеры, сети и кэш

```powershell
docker system prune
```

### Удалить неиспользуемые volume

```powershell
docker volume prune
```

Осторожно: `volume prune` может удалить неиспользуемые базы данных других проектов.

---

## 20. Полезные команды PostgreSQL

### Открыть psql внутри контейнера

```powershell
docker compose exec db psql -U wkr -d wkr
```

Выйти из psql:

```text
\q
```

### Список схем

```sql
\dn
```

### Список таблиц схемы wkr

```sql
\dt wkr.*
```

### Список функций схемы wkr

```sql
\df wkr.*
```

### Описание таблицы

```sql
\d wkr.telemetry_spindle
```

### Количество строк

```sql
select count(*) from wkr.telemetry_spindle;
```

### Последние 10 строк телеметрии

```sql
select * from wkr.telemetry_spindle order by ts desc limit 10;
```

### Последние 10 прогнозов

```sql
select id, ts, machine_id, tool_id, rul_minutes, alarm_level, alarm_code, state
from wkr.rul_predictions
order by ts desc
limit 10;
```

### Последние 10 тревог

```sql
select id, ts, machine_id, tool_id, rul_minutes, alarm_level, alarm_code, alarm_message
from wkr.alarm_events
order by ts desc
limit 10;
```

### Очистить тестовые данные

```sql
truncate table wkr.alarm_events, wkr.rul_predictions, wkr.telemetry_spindle restart identity;
```

---

## 21. Полезные команды PowerShell

### Проверить порт

```powershell
Test-NetConnection 127.0.0.1 -Port 8000
Test-NetConnection 127.0.0.1 -Port 8001
Test-NetConnection 127.0.0.1 -Port 8080
```

### Посмотреть процессы, слушающие порт

```powershell
netstat -ano | findstr :8000
netstat -ano | findstr :8001
netstat -ano | findstr :8080
```

### Посмотреть процесс по PID

```powershell
tasklist /FI "PID eq 1234"
```

Вместо `1234` укажи реальный PID.

### Проверить исключённые порты Windows

```powershell
netsh int ipv4 show excludedportrange protocol=tcp
```

Это полезно, если Docker пишет ошибку:

```text
ports are not available
```

---

## 22. Частые ошибки и решения

### 22.1 `services must be a mapping`

Причина: неправильные отступы в `docker-compose.yml`.

Правильно:

```yaml
services:
  db:
    image: postgres:16
```

Неправильно:

```yaml
services:
db:
  image: postgres:16
```

Проверка compose-файла:

```powershell
docker compose config
```

---

### 22.2 `failed to read dockerfile: open Dockerfile: no such file or directory`

Причина: неверный `build.context`.

Для backend должно быть примерно:

```yaml
backend:
  build:
    context: ../backend/Backend.Api
    dockerfile: Dockerfile
```

---

### 22.3 `Feature shape mismatch`

Причина: количество признаков, которые отправляет backend, не совпадает с количеством признаков модели.

Проверить признаки ML:

```powershell
docker compose exec ml python -c "import json; m=json.load(open('/app/metrics.json', encoding='utf-8')); print(len(m['features'])); print(m['features'])"
```

Проверить backend `FeatureExtractor.cs`: он должен формировать ровно те же признаки.

Ожидаемо сейчас:

```text
18 признаков
```

---

### 22.4 `Cannot load library libgssapi_krb5.so.2`

Решение: в строку подключения добавить:

```text
GSS Encryption Mode=Disable
```

Пример:

```yaml
ConnectionStrings__Pg: Host=db;Port=5432;Database=wkr;Username=wkr;Password=wkr;GSS Encryption Mode=Disable
```

---

### 22.5 `Found non-empty schema but no schema history table`

Причина: в базе уже есть объекты, но Flyway не знает историю миграций.

Для разработки проще всего пересоздать volume:

```powershell
docker compose down -v

docker compose up -d adminer
```

На промышленной базе так делать нельзя. Там нужно использовать baseline.

---

### 22.6 `ports are not available`

Причина: порт занят или попал в исключённый диапазон Windows.

Проверить порт:

```powershell
netstat -ano | findstr :5432
```

Проверить исключённые диапазоны:

```powershell
netsh int ipv4 show excludedportrange protocol=tcp
```

Для PostgreSQL лучше использовать динамический порт:

```yaml
ports:
  - "127.0.0.1::5432"
```

---

## 23. Рекомендуемые scripts

Создай папку:

```powershell
mkdir C:\Users\grib9\wkr-rul-system\scripts
```

---

### 23.1 `scripts/start-clean.ps1`

```powershell
cd "$PSScriptRoot\..\infra"

docker compose down -v

docker compose up -d adminer

docker compose up -d --build ml backend

docker compose ps
```

---

### 23.2 `scripts/start.ps1`

```powershell
cd "$PSScriptRoot\..\infra"

docker compose up -d adminer

docker compose up -d ml backend

docker compose ps
```

---

### 23.3 `scripts/check.ps1`

```powershell
cd "$PSScriptRoot\..\infra"

Write-Host "=== Docker services ==="
docker compose ps

Write-Host "=== Tables ==="
docker compose exec db psql -U wkr -d wkr -c "\dt wkr.*"

Write-Host "=== Backend health ==="
Invoke-RestMethod http://localhost:8000/health

Write-Host "=== ML health ==="
Invoke-RestMethod http://localhost:8001/health

Write-Host "=== ML features ==="
docker compose exec ml python -c "import json; m=json.load(open('/app/metrics.json', encoding='utf-8')); print(len(m['features'])); print(m['features'])"
```

---

### 23.4 `scripts/db-port.ps1`

```powershell
cd "$PSScriptRoot\..\infra"

docker compose port db 5432
```

---

### 23.5 `scripts/clear-test-data.ps1`

```powershell
cd "$PSScriptRoot\..\infra"

docker compose exec db psql -U wkr -d wkr -c "truncate table wkr.alarm_events, wkr.rul_predictions, wkr.telemetry_spindle restart identity;"
```

---

## 24. Что делать дальше по развитию проекта

### 24.1 Добавить Collector

Сейчас телеметрия отправляется вручную через PowerShell. Для завершённой архитектуры нужен сервис:

```text
collector
```

Он должен:

```text
получать данные от внешней среды / станка / эмулятора;
формировать JSON;
отправлять POST /api/telemetry;
логировать ошибки связи.
```

---

### 24.2 Добавить сглаживание RUL

Сейчас ML возвращает прямой прогноз. Позже желательно добавить:

```text
RUL_smooth
RUL_monotonic
```

Это уменьшит скачки прогноза.

---

### 24.3 Добавить защиту от дублей прогнозов

Сейчас `RulWorker` пишет прогноз каждые 5 секунд. Можно сделать правило:

```text
писать новый прогноз только если:
- изменился alarm_level;
- RUL изменился больше чем на 1 минуту;
- прошло больше N секунд.
```

---

### 24.4 Добавить операторский интерфейс

Минимальный интерфейс должен показывать:

```text
текущий инструмент;
последний RUL;
состояние OK/WARNING/STOP;
сообщение оператору;
последние тревоги;
график RUL во времени.
```

---

### 24.5 Заменить демонстрационный датасет

Сейчас `make_demo_train_features.py` создаёт демонстрационные данные.

Для более сильной ВКР лучше подготовить обучение на:

```text
открытом датасете по износу инструмента;
или реальной телеметрии;
или смешанном демонстрационном сценарии с обоснованием ограничений.
```

Честная формулировка для ВКР:

```text
На этапе разработки работоспособность программного контура проверялась на подготовленной демонстрационной выборке признаков, имитирующей рост нагрузки по мере износа инструмента. Для промышленного применения модель должна быть дополнительно обучена и откалибрована на телеметрии конкретного станка либо на открытом наборе экспериментальных данных по износу инструмента.
```

---

## 25. Быстрый чек-лист запуска

```powershell
cd C:\Users\grib9\wkr-rul-system\infra

docker compose up -d adminer

docker compose up -d ml backend

docker compose ps

Invoke-RestMethod http://localhost:8000/health

Invoke-RestMethod http://localhost:8001/health

docker compose exec db psql -U wkr -d wkr -c "\dt wkr.*"
```

Если всё успешно — проект запущен.

---

## 26. Быстрый чек-лист после изменения кода

### Если изменил backend

```powershell
cd C:\Users\grib9\wkr-rul-system\infra

docker compose up -d --build backend

docker compose logs --tail 100 backend
```

### Если изменил ML-service

```powershell
cd C:\Users\grib9\wkr-rul-system\infra

docker compose up -d --build ml

docker compose logs --tail 100 ml
```

### Если изменил SQL-миграции

```powershell
cd C:\Users\grib9\wkr-rul-system\infra

docker compose run --rm flyway

docker compose up -d --build backend
```

---

## 27. Текущий статус проекта

На текущем этапе проект уже выполняет основной цикл:

```text
приём телеметрии
→ расчёт производных параметров резания
→ сохранение в PostgreSQL
→ выбор окна резания
→ формирование 18 признаков
→ вызов XGBoost
→ получение RUL
→ запись прогноза
→ запись тревоги при warning/critical
```

Для демонстрации ВКР этого уже достаточно как рабочего программного контура. Дальнейшие улучшения: Collector, интерфейс оператора, сглаживание RUL и обучение модели на более репрезентативных данных.
