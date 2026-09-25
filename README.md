# TestJob API

REST API на .NET 10 для обработки HTML-страницы из тестового задания.

## Запуск

```bash
docker compose up --build
```

После запуска доступны:

- Swagger: http://localhost:8090/api/swagger
- pgAdmin: http://localhost:8080

pgAdmin запускается без формы входа и уже содержит подключение к PostgreSQL. Таблица с результатами находится по пути «Серверы → TestJob — PostgreSQL 18 → Базы данных → testjob → Схемы → public → Таблицы → elements».

Остановить контейнеры:

```bash
docker compose down
```

Данные PostgreSQL сохраняются в Docker volume. Для удаления данных используйте `docker compose down -v`.

## API

`POST /api/process-page`

Для проверки можно использовать `json_payload_1.txt` и `json_payload_2.txt`. Ожидаемые ответы находятся в `json_result_1.txt` и `json_result_2.txt`.

Обработчик:

1. проверяет запрос через FluentValidation;
2. декодирует URL и HTML из Base64;
3. разбирает HTML через AngleSharp и применяет CSS-селектор;
4. сохраняет найденные элементы в PostgreSQL через Dapper;
5. извлекает адреса электронной почты скомпилированным регулярным выражением;
6. расшифровывает текст алгоритмом AES-256-ECB без padding.

## Структура

- `Program.cs` — конфигурация приложения и зависимостей;
- `Controllers/PageProcessingController.cs` — HTTP-контракт;
- `Models/ProcessingModels.cs` — модели, коды ошибок и валидация;
- `Services/PageProcessingService.cs` — обработка данных и запись в БД;
- `compose.yml`, `Dockerfile`, `deploy/` — локальное развёртывание.

## Тесты

```bash
dotnet test TestJob.slnx
```

## Асинхронность

Асинхронно выполняются операции ввода-вывода: разбор документа, подключение к PostgreSQL и запись данных. Пока база данных отвечает, поток запроса может обслуживать другую работу.

Декодирование Base64, регулярное выражение, CSS-выборка и AES работают синхронно, потому что это вычисления в памяти и у используемых API нет асинхронных вариантов. `Task.Run` лишь перенёс бы работу в другой поток и добавил накладные расходы.

## Принятые решения

- По просьбе владельца образ приложения собирается через Dockerfile без монтирования исходного кода в контейнер.
- Расшифрованная строка в примерах (`AES Error: Object reference not set to an instance of an object.`) является содержимым тестового шифротекста, а не ошибкой API.
