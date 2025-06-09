# **Библиотека +**

`Библиотека +` — настольное приложение на Windows Forms для ведения каталога книг и учёта перемещений литературы в небольшой библиотеке. Система хранит данные в SQLite и предоставляет удобные формы для работы с читателями, должниками и складскими операциями.

---

## Содержание
1. [Файловая структура](#файловая-структура)
2. [Схема базы данных](#схема-базы-данных)
3. [Запуск приложения](#запуск-приложения)
4. [Запуск тестов](#запуск-тестов)
5. [Скриншоты](#скриншоты)
6. [Ключевые алгоритмы](#ключевые-алгоритмы)
7. [Как пользоваться](#как-пользоваться)

---

## Файловая структура

```
.
├── Data/                   # Модели и службы доступа к БД
│   ├── DatabaseInitializer.cs   # Создание таблиц SQLite
│   ├── LibraryContext.cs        # DbContext Entity Framework
│   ├── Models/                  # Классы сущностей
│   └── Services/                # CRUD‑сервисы
├── UI/
│   ├── Forms/                   # Все WinForms формы
│   └── Helpers/                 # Вспомогательные методы для UI
├── Program.cs               # Точка входа
├── LibraryApp.csproj        # Файл проекта
├── LibraryApp.sln           # Решение Visual Studio
├── LibraryApp.Tests/        # xUnit тесты
│   ├── DatabaseTests.cs     # Проверка работы БД
│   └── UITests.cs           # Простые UI‑тесты
└── README.md
```

### Краткое описание файлов
- **DatabaseInitializer.cs** – создаёт все таблицы при первом запуске.
- **LibraryContext.cs** – контекст EF для работы с моделями.
- **Models*** – классы `Book`, `Author`, `Genre`, `Location` и др.
- **Services*** – обёртки с методами `Add`, `Update`, `Delete` и выборками.
- **Forms/** – страницы приложения (главная, книги, должники и др.).
- **Helpers/** – расширения элементов UI и обработка изображений.
- **Program.cs** – конфигурация DI и запуск `MainForm`.
- **LibraryApp.Tests/** – набор тестов для проверки логики.

---

## Схема базы данных

| Таблица | Поля и типы | Связи |
|---------|-------------|-------|
| **Publisher** | `publisher_id` INTEGER PK, `name` TEXT UNIQUE | 1 ко многим с `Book` |
| **LanguageCode** | `lang_id` INTEGER PK, `code` TEXT UNIQUE | 1 ко многим с `Book` и `BookLanguage` |
| **Genre** | `genre_id` INTEGER PK, `name` TEXT UNIQUE | многие ко многим с `Book` через `GenreBook` |
| **Author** | `author_id` INTEGER PK, `name` TEXT UNIQUE | многие ко многим с `Book` через `AuthorBook` |
| **Book** | `book_id` INTEGER PK, `ISBN` TEXT UNIQUE, `publisher_id` FK, `publish_year` INTEGER, `lang_id` FK, `title` TEXT, `description` TEXT, `pages` INTEGER, `cover_url` TEXT | связи с `AuthorBook`, `GenreBook`, `BookLanguage`, `InventoryTransactions`, `Debts` |
| **Location** | `location_id` INTEGER PK, `location_name` TEXT UNIQUE, `amount` INTEGER | 1 ко многим с `InventoryTransactions` |
| **InventoryTransactions** | `inv_trans_id` INTEGER PK, `book_id` FK, `location_id` FK, `prev_location_id` FK, `date` TEXT, `amount` INTEGER | многие к одному с `Book` и `Location` |
| **GenreBook** | `genre_book_id` INTEGER PK, `genre_id` FK, `book_id` FK | связь многие-ко-многим |
| **AuthorBook** | `author_book_id` INTEGER PK, `author_id` FK, `book_id` FK | связь многие-ко-многим |
| **BookLanguage** | `book_language_id` INTEGER PK, `book_id` FK, `lang_id` FK | связь многие-ко-многим |
| **Reader** | `reader_id` INTEGER PK, `full_name` TEXT, `email` TEXT UNIQUE, `phone` TEXT | 1 к 1 с `ReaderTicket` |
| **ReaderTicket** | `reader_id` INTEGER PK/FK, `registration_date` TEXT, `end_time` TEXT | 1 к 1 с `Reader` и 1 ко многим с `Debts` |
| **Debts** | `debt_id` INTEGER PK, `book_id` FK, `reader_ticket_id` FK, `start_time` TEXT, `end_time` TEXT | многие к одному с `Book` и `ReaderTicket` |

---

## Запуск приложения

1. Установите .NET 8 SDK.
2. Выполните в каталоге проекта:
   ```bash
   dotnet run --project LibraryApp.csproj
   ```
   При первом запуске в папке `AppData` будет создана база `library.db`.

## Запуск тестов

```bash
dotnet test
```
Тесты проверяют работу БД и базовые функции UI.

---

## Скриншоты
Разместите ваши изображения работы программы в этом разделе.

---

## Ключевые алгоритмы
1. **Загрузка и обработка обложки** – при выборе файла картинка уменьшается до 260 px по ширине, а затем обрезается или дополняется белыми полосами, чтобы высота была ровно 420 px.
2. **Создание карточек книг** – каждая книга отображается как панель с обложкой и краткой информацией. Элементы автоматически округляются и подстраиваются под размер окна.
3. **Поиск книг и читателей** – ввод текста фильтрует таблицы по нескольким полям сразу, что облегчает навигацию в большом списке.
4. **Учёт перемещений книг** – транзакции позволяют фиксировать, откуда и куда перемещена книга, с автоматическим изменением количества на складах.
5. **Автоначисление штрафов** – при просрочке возврата система считает количество дней и умножает на фиксированный размер штрафа.
6. **Инициализация базы данных** – при старте приложение проверяет наличие файла БД и при его отсутствии создаёт все таблицы согласно схеме.
7. **Работа с изображениями** – старые обложки можно заменить без блокировки файлов; файлы хранятся в `AppData/Media/Covers`.

---

## Как пользоваться
Приложение рассчитано на работу администратора библиотеки. Администратор вносит данные о книгах, читателях и местонахождении литературы. Обычные пользователи доступа к программе не имеют. Если требуется предоставить информацию читателю или сотруднику, это делает администратор, сформировав необходимые отчёты или распечатки.

