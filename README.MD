Система обработки PDF-документов на базе .NET.

Проект состоит из API-сервиса и фонового worker-процесса для асинхронной обработки PDF-файлов.

### Структура проекта:

```text
Api                        # АПИ сервер, принимающий запросы от клиентов
PdfWorker                  # Воркер для обработки входящих PDF
Core                       # Общие компоненты (сообщения, дто, константы)
Database                   # Реализация работы с PostgreSQL
```

### Запуск

##### Требования

- .NET 10 SDK
- PostgreSQL
- RabbitMQ

Первичная настройка осуществляется в файле `config.json`.

Для запуска API и воркера использовать команды

`dotnet run --project .\Api\Api.csproj` и `dotnet run --project .\PdfWorker\PdfWorker.csproj` соответственно.

Спецификация OpenAPI доступна по адресу `http://localhost:5204/openapi/v1.json`.

##### Настройка RabbitMQ

Для работы воркера требуется очередь `pdf.incoming`.

Сообщения должны публиковаться в exchange `amq.topic` с `routing key`:

```text
pdf.uploaded
```

Необходимо создать binding:

```text
Exchange: amq.topic
Queue: pdf.incoming
Routing Key: pdf.uploaded
```

### Flow обработки PDF

- Клиент загружает PDF при помощи метода `POST` на эндпоинт `api/pdf`
- API принимает документ и сохраняет его во внутреннее хранилище
- Отправляется сообщение с названием файла и ссылкой брокеру
- Воркер получется сообщение из очереди брокера
- PDF обрабатывается
- Результат сохраняется в PostgreSQL

##### Пример HTTP-запроса

```http
POST /api/pdf HTTP/1.1
Host: localhost:5204
Content-Type: multipart/form-data

file=document.pdf
```
