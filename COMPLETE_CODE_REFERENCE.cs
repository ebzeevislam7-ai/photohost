// ════════════════════════════════════════════════════════════
// 📸 ПОЛНЫЙ КОД ФОТОХОСТИНГА ASP.NET CORE MVC
// Все компоненты в одном файле для справки
// ════════════════════════════════════════════════════════════

// ========== 1️⃣ МОДЕЛЬ: Photo.cs ==========
using System;
using System.ComponentModel.DataAnnotations;

namespace PhotoHost.Models
{
    /// <summary>
    /// Модель для представления фотографии в системе
    /// </summary>
    public class Photo
    {
        /// <summary>
        /// Уникальный идентификатор фотографии
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Оригинальное имя файла
        /// </summary>
        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        /// <summary>
        /// Путь к сохранённому файлу относительно wwwroot
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Path { get; set; }

        /// <summary>
        /// Дата и время загрузки (UTC)
        /// </summary>
        [Required]
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    }
}


// ========== 2️⃣ БД КОНТЕКСТ: AppDbContext.cs ==========
using Microsoft.EntityFrameworkCore;

namespace PhotoHost.Data
{
    /// <summary>
    /// Entity Framework Core контекст для работы с БД
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) 
            : base(options)
        {
        }

        /// <summary>
        /// Таблица фотографий
        /// </summary>
        public DbSet<Photo> Photos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Конфигурация таблицы Photos
            modelBuilder.Entity<Photo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName)
                    .IsRequired()
                    .HasMaxLength(255);
                entity.Property(e => e.Path)
                    .IsRequired()
                    .HasMaxLength(500);
                entity.Property(e => e.UploadDate)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Индекс для быстрого поиска по дате
                entity.HasIndex(e => e.UploadDate)
                    .IsDescending();
            });
        }
    }
}


// ========== 3️⃣ ИНТЕРФЕЙС СЕРВИСА: IPhotoService.cs ==========
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhotoHost.Services
{
    /// <summary>
    /// Контракт для сервиса управления фотографиями
    /// </summary>
    public interface IPhotoService
    {
        /// <summary>
        /// Загружает файл изображения, сохраняет его и добавляет в БД
        /// </summary>
        /// <param name="file">Загруженный файл от пользователя</param>
        /// <returns>Объект Photo с информацией о загруженной фотографии</returns>
        /// <exception cref="ArgumentException">Если файл пуст или null</exception>
        /// <exception cref="InvalidOperationException">Если расширение не разрешено</exception>
        Task<Photo> UploadAsync(IFormFile file);

        /// <summary>
        /// Получает список всех загруженных фотографий отсортированных по дате
        /// </summary>
        /// <returns>Список Photo отсортированный по убыванию даты</returns>
        Task<List<Photo>> GetAllPhotosAsync();

        /// <summary>
        /// Удаляет фотографию по ID (удаляет файл и запись из БД)
        /// </summary>
        /// <param name="photoId">ID фотографии для удаления</param>
        /// <exception cref="InvalidOperationException">Если фотография не найдена</exception>
        Task DeletePhotoAsync(Guid photoId);
    }
}


// ========== 4️⃣ РЕАЛИЗАЦИЯ СЕРВИСА: PhotoService.cs ==========
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PhotoHost.Data;
using PhotoHost.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoHost.Services
{
    /// <summary>
    /// Реализация сервиса управления фотографиями
    /// Содержит логику загрузки, сохранения, удаления фотографий
    /// </summary>
    public class PhotoService : IPhotoService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png" };
        private const string UploadFolder = "uploads";

        // Максимальный размер файла (25 МБ)
        private const long MaxFileSize = 25 * 1024 * 1024;

        public PhotoService(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// Загружает файл изображения с полной валидацией и обработкой ошибок
        /// 
        /// Процесс:
        /// 1. Проверяет наличие и размер файла
        /// 2. Валидирует расширение (.jpg, .jpeg, .png)
        /// 3. Генерирует уникальное имя через Guid.NewGuid()
        /// 4. Сохраняет файл в wwwroot/uploads/
        /// 5. Добавляет запись в БД через Entity Framework
        /// </summary>
        public async Task<Photo> UploadAsync(IFormFile file)
        {
            // ✅ Проверка наличия файла
            if (file == null || file.Length == 0)
                throw new ArgumentException(
                    "Файл не выбран или пуст", nameof(file));

            // ✅ Проверка размера файла
            if (file.Length > MaxFileSize)
                throw new InvalidOperationException(
                    $"Размер файла превышает максимально допустимый ({MaxFileSize / (1024 * 1024)} МБ)");

            // ✅ Получаем расширение файла и приводим к нижнему регистру
            var extension = Path.GetExtension(file.FileName).ToLower();

            // ✅ Проверяем разрешённые расширения
            if (!_allowedExtensions.Contains(extension))
                throw new InvalidOperationException(
                    $"Расширение '{extension}' не разрешено. " +
                    $"Используйте: {string.Join(", ", _allowedExtensions)}");

            // ✅ Генерируем уникальное имя файла с помощью GUID
            // Формат: {GUID}.{расширение}, например: 550e8400-e29b-41d4-a716-446655440000.jpg
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";

            // ✅ Определяем полный путь для папки uploads
            var uploadsFolderPath = Path.Combine(_environment.WebRootPath, UploadFolder);

            // ✅ Создаём директорию если её не существует
            if (!Directory.Exists(uploadsFolderPath))
                Directory.CreateDirectory(uploadsFolderPath);

            // ✅ Полный путь к файлу на диске
            var filePath = Path.Combine(uploadsFolderPath, uniqueFileName);

            // ✅ Сохраняем файл на диск асинхронно
            try
            {
                // Используем using для автоматического закрытия потока
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    // Копируем содержимое загруженного файла в поток
                    await file.CopyToAsync(stream);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException(
                    "Нет прав доступа для сохранения файла", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new InvalidOperationException(
                    "Папка для загрузок не найдена", ex);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException(
                    "Ошибка при записи файла на диск", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Неизвестная ошибка при сохранении файла", ex);
            }

            // ✅ Создаём объект Photo для БД
            var photo = new Photo
            {
                Id = Guid.NewGuid(),
                FileName = file.FileName,  // Оригинальное имя от пользователя
                Path = $"/{UploadFolder}/{uniqueFileName}",  // Путь для веб-доступа
                UploadDate = DateTime.UtcNow
            };

            // ✅ Добавляем запись в БД
            _context.Photos.Add(photo);

            // ✅ Сохраняем изменения
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Если ошибка БД, удаляем сохранённый файл
                try
                {
                    File.Delete(filePath);
                }
                catch { /* Игнорируем ошибку удаления */ }

                throw new InvalidOperationException(
                    "Ошибка при сохранении информации в БД", ex);
            }

            return photo;
        }

        /// <summary>
        /// Получает все фотографии из БД, отсортированные по дате (новые первыми)
        /// </summary>
        public async Task<List<Photo>> GetAllPhotosAsync()
        {
            return await _context.Photos
                .OrderByDescending(p => p.UploadDate)  // Новые фотографии первыми
                .ToListAsync();
        }

        /// <summary>
        /// Удаляет фотографию: удаляет файл с диска и запись из БД
        /// </summary>
        public async Task DeletePhotoAsync(Guid photoId)
        {
            // ✅ Ищем фотографию в БД
            var photo = await _context.Photos.FindAsync(photoId);

            // ✅ Проверяем есть ли такая фотография
            if (photo == null)
                throw new InvalidOperationException(
                    $"Фотография с ID {photoId} не найдена в БД");

            // ✅ Удаляем файл с диска
            try
            {
                var filePath = Path.Combine(_environment.WebRootPath, 
                    photo.Path.TrimStart('/'));  // Удаляем ведущий слеш

                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException(
                    "Нет прав для удаления файла", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Ошибка при удалении физического файла", ex);
            }

            // ✅ Удаляем запись из БД
            _context.Photos.Remove(photo);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException(
                    "Ошибка при удалении записи из БД", ex);
            }
        }
    }
}


// ========== 5️⃣ КОНТРОЛЛЕР: PhotosController.cs ==========
using Microsoft.AspNetCore.Mvc;
using PhotoHost.Services;
using System;
using System.Threading.Tasks;

namespace PhotoHost.Controllers
{
    /// <summary>
    /// MVC контроллер для обработки HTTP запросов фотохостинга
    /// </summary>
    public class PhotosController : Controller
    {
        private readonly IPhotoService _photoService;
        private readonly ILogger<PhotosController> _logger;

        public PhotosController(IPhotoService photoService, ILogger<PhotosController> logger)
        {
            _photoService = photoService 
                ?? throw new ArgumentNullException(nameof(photoService));
            _logger = logger 
                ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET: /Photos/Index
        /// Отображает главную страницу со списком всех загруженных фотографий
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Получаем все фотографии из сервиса
                var photos = await _photoService.GetAllPhotosAsync();
                
                // Устанавливаем заголовок страницы
                ViewBag.Title = "Фотохостинг";
                
                // Возвращаем представление со списком фотографий
                return View(photos);
            }
            catch (Exception ex)
            {
                // Логируем ошибку
                _logger.LogError(ex, "Ошибка при получении списка фотографий");
                
                // Показываем сообщение об ошибке
                ModelState.AddModelError("", "Ошибка при загрузке фотографий");
                
                // Возвращаем пустой список
                return View(new List<Models.Photo>());
            }
        }

        /// <summary>
        /// POST: /Photos/Upload
        /// Загружает файл изображения на сервер и добавляет в БД
        /// 
        /// Параметры:
        /// - file: Загруженный файл (multipart/form-data)
        /// 
        /// Возвращает: Перенаправление на Index с сообщением об успехе/ошибке
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            // Проверяем выбран ли файл
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Выберите файл для загрузки");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Вызываем сервис для загрузки файла
                await _photoService.UploadAsync(file);
                
                // Устанавливаем сообщение об успехе
                TempData["Success"] = "Фотография успешно загружена!";
                
                // Логируем успешную загрузку
                _logger.LogInformation($"Файл {file.FileName} успешно загружен");
            }
            catch (InvalidOperationException ex)
            {
                // Ошибки валидации (расширение, размер и т.д.)
                TempData["Error"] = ex.Message;
                _logger.LogWarning(ex, "Ошибка валидации при загрузке файла");
            }
            catch (ArgumentException ex)
            {
                // Ошибки аргументов
                TempData["Error"] = ex.Message;
                _logger.LogWarning(ex, "Ошибка аргумента при загрузке файла");
            }
            catch (Exception ex)
            {
                // Неожиданные ошибки
                TempData["Error"] = "Произошла ошибка при загрузке файла";
                _logger.LogError(ex, "Неожиданная ошибка при загрузке файла");
            }

            // Перенаправляем обратно на главную страницу
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// POST: /Photos/Delete
        /// Удаляет фотографию по ID
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(Guid photoId)
        {
            try
            {
                // Вызываем сервис для удаления
                await _photoService.DeletePhotoAsync(photoId);
                
                TempData["Success"] = "Фотография удалена!";
                _logger.LogInformation($"Фотография {photoId} успешно удалена");
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                _logger.LogWarning(ex, "Ошибка при удалении фотографии");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Произошла ошибка при удалении";
                _logger.LogError(ex, "Неожиданная ошибка при удалении фотографии");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}


// ========== 6️⃣ ПРЕДСТАВЛЕНИЕ: Index.cshtml ==========
@model List<PhotoHost.Models.Photo>

@{
    ViewData["Title"] = ViewBag.Title ?? "Фотохостинг";
}

<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>@ViewData["Title"]</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            padding: 20px;
        }

        .container {
            max-width: 1200px;
            margin: 0 auto;
        }

        header {
            text-align: center;
            color: white;
            margin-bottom: 40px;
        }

        header h1 {
            font-size: 2.5rem;
            margin-bottom: 10px;
            text-shadow: 2px 2px 4px rgba(0,0,0,0.3);
        }

        .upload-section {
            background: white;
            padding: 30px;
            border-radius: 12px;
            box-shadow: 0 8px 32px rgba(0,0,0,0.1);
            margin-bottom: 30px;
        }

        .upload-section h2 {
            color: #333;
            margin-bottom: 20px;
            font-size: 1.5rem;
        }

        .form-group {
            margin-bottom: 15px;
        }

        .form-group label {
            display: block;
            margin-bottom: 8px;
            color: #333;
            font-weight: 600;
        }

        .form-group input[type="file"] {
            width: 100%;
            padding: 10px;
            border: 2px dashed #667eea;
            border-radius: 8px;
            cursor: pointer;
            font-size: 1rem;
        }

        .form-group input[type="file"]:hover {
            border-color: #764ba2;
            background-color: #f8f9ff;
        }

        .submit-btn {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 12px 30px;
            border: none;
            border-radius: 8px;
            font-size: 1rem;
            font-weight: 600;
            cursor: pointer;
            transition: transform 0.2s, box-shadow 0.2s;
        }

        .submit-btn:hover {
            transform: translateY(-2px);
            box-shadow: 0 8px 16px rgba(102, 126, 234, 0.4);
        }

        .alert {
            padding: 15px 20px;
            margin-bottom: 20px;
            border-radius: 8px;
            font-weight: 600;
        }

        .alert-success {
            background-color: #d4edda;
            color: #155724;
            border: 1px solid #c3e6cb;
        }

        .alert-danger {
            background-color: #f8d7da;
            color: #721c24;
            border: 1px solid #f5c6cb;
        }

        .photos-section {
            margin-top: 40px;
        }

        .photos-section h2 {
            color: white;
            margin-bottom: 25px;
            font-size: 1.5rem;
            text-shadow: 2px 2px 4px rgba(0,0,0,0.3);
        }

        .photos-grid {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
            gap: 20px;
        }

        .photo-card {
            background: white;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 8px 32px rgba(0,0,0,0.1);
            transition: transform 0.3s, box-shadow 0.3s;
        }

        .photo-card:hover {
            transform: translateY(-8px);
            box-shadow: 0 16px 48px rgba(0,0,0,0.15);
        }

        .photo-image {
            width: 100%;
            height: 200px;
            object-fit: cover;
            display: block;
        }

        .photo-info {
            padding: 15px;
        }

        .photo-name {
            color: #333;
            font-weight: 600;
            margin-bottom: 8px;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
            font-size: 0.9rem;
        }

        .photo-date {
            color: #666;
            font-size: 0.85rem;
            margin-bottom: 15px;
        }

        .delete-btn {
            width: 100%;
            background-color: #dc3545;
            color: white;
            border: none;
            padding: 10px;
            border-radius: 6px;
            cursor: pointer;
            font-weight: 600;
            transition: background-color 0.2s;
        }

        .delete-btn:hover {
            background-color: #c82333;
        }

        .empty-state {
            text-align: center;
            color: white;
            padding: 50px 20px;
            font-size: 1.2rem;
        }
    </style>
</head>
<body>
    <div class="container">
        <header>
            <h1>📸 Фотохостинг</h1>
            <p>Загружайте и делитесь своими фотографиями</p>
        </header>

        <!-- Сообщения об успехе и ошибке -->
        @if (TempData["Success"] != null)
        {
            <div class="alert alert-success">
                ✅ @TempData["Success"]
            </div>
        }

        @if (TempData["Error"] != null)
        {
            <div class="alert alert-danger">
                ❌ @TempData["Error"]
            </div>
        }

        <!-- Форма загрузки фотографии -->
        <div class="upload-section">
            <h2>Загрузить фотографию</h2>
            <form method="post" action="@Url.Action("Upload")" enctype="multipart/form-data">
                <div class="form-group">
                    <label for="file">Выберите изображение (.jpg, .jpeg, .png):</label>
                    <input type="file" 
                           id="file" 
                           name="file" 
                           accept=".jpg,.jpeg,.png" 
                           required>
                </div>
                <button type="submit" class="submit-btn">📤 Загрузить</button>
            </form>
        </div>

        <!-- Сетка фотографий -->
        @if (Model != null && Model.Count > 0)
        {
            <div class="photos-section">
                <h2>Все фотографии (@Model.Count)</h2>
                <div class="photos-grid">
                    @foreach (var photo in Model)
                    {
                        <div class="photo-card">
                            <img src="@photo.Path" alt="@photo.FileName" class="photo-image">
                            <div class="photo-info">
                                <div class="photo-name" title="@photo.FileName">
                                    @photo.FileName
                                </div>
                                <div class="photo-date">
                                    📅 @photo.UploadDate.ToString("dd.MM.yyyy HH:mm")
                                </div>
                                <form method="post" 
                                      action="@Url.Action("Delete")" 
                                      onsubmit="return confirm('Вы уверены, что хотите удалить эту фотографию?');">
                                    <input type="hidden" name="photoId" value="@photo.Id">
                                    <button type="submit" class="delete-btn">🗑️ Удалить</button>
                                </form>
                            </div>
                        </div>
                    }
                </div>
            </div>
        }
        else
        {
            <div class="empty-state">
                <p>📁 Нет загруженных фотографий</p>
                <p style="font-size: 0.9rem; opacity: 0.8; margin-top: 10px;">
                    Загрузите первую фотографию выше
                </p>
            </div>
        }
    </div>
</body>
</html>


// ========== 7️⃣ КОНФИГУРАЦИЯ ПРИЛОЖЕНИЯ: Program.cs ==========
using Microsoft.EntityFrameworkCore;
using PhotoHost.Data;
using PhotoHost.Services;

var builder = WebApplication.CreateBuilder(args);

// ========== РЕГИСТРАЦИЯ СЕРВИСОВ ==========

// 🔌 Добавляем DbContext для Entity Framework Core
// Используем SQL Server LocalDB (можно изменить на другую БД)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Server=(localdb)\\mssqllocaldb;Database=PhotoHostDb;Trusted_Connection=true;"));

// 📦 Регистрируем сервис фотографий (Dependency Injection)
// Scoped = новый экземпляр для каждого HTTP запроса
builder.Services.AddScoped<IPhotoService, PhotoService>();

// 🎮 Добавляем MVC контроллеры и Razor представления
builder.Services.AddControllersWithViews();

// 🌐 Добавляем доступ к HttpContext для получения WebRootPath
builder.Services.AddHttpContextAccessor();

// ========== СОЗДАНИЕ И КОНФИГУРАЦИЯ ПРИЛОЖЕНИЯ ==========

var app = builder.Build();

// 📊 Автоматическая миграция БД при запуске приложения
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // Создаём БД если её нет, применяем миграции
    dbContext.Database.Migrate();
}

// 🛡️ Обработка исключений (только в Production)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 🔒 Перенаправление на HTTPS
app.UseHttpsRedirection();

// 📄 Включаем раздачу статических файлов (CSS, JS, изображения)
app.UseStaticFiles();

// 🛣️ Включаем маршрутизацию
app.UseRouting();

// 🔐 Включаем авторизацию (если нужна)
app.UseAuthorization();

// ========== МАРШРУТЫ ==========

// Стандартный маршрут: /Photos/Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Photos}/{action=Index}/{id?}");

// 🚀 Запуск приложения
app.Run();


// ========== 8️⃣ КОНФИГ БД: appsettings.json ==========
/*
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PhotoHostDb;Trusted_Connection=true;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
*/


// ========== 9️⃣ ФАЙЛ ПРОЕКТА: PhotoHost.csproj ==========
/*
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>PhotoHost</RootNamespace>
    <AssemblyName>PhotoHost</AssemblyName>
  </PropertyGroup>

  <!-- NuGet пакеты -->
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

</Project>
*/


// ════════════════════════════════════════════════════════════
// ✅ ПОЛНЫЙ КОД ГОТОВ К ЗАПУСКУ!
// ════════════════════════════════════════════════════════════
// 
// 📋 СТРУКТУРА ПРОЕКТА:
// project/
// ├── Models/Photo.cs
// ├── Data/AppDbContext.cs
// ├── Services/IPhotoService.cs
// ├── Services/PhotoService.cs
// ├── Controllers/PhotosController.cs
// ├── Views/Photos/Index.cshtml
// ├── wwwroot/uploads/     (автоматически создаётся)
// ├── Program.cs
// ├── appsettings.json
// └── PhotoHost.csproj
//
// 🚀 ДЛЯ ЗАПУСКА:
// 1. dotnet new globaljson -> выберите .NET 8.0
// 2. dotnet new mvc -n PhotoHost
// 3. Замените файлы на содержимое выше
// 4. dotnet ef migrations add InitialCreate
// 5. dotnet ef database update
// 6. dotnet run
//
// 🌐 ОТКРОЙТЕ: https://localhost:5001
//
// ════════════════════════════════════════════════════════════
