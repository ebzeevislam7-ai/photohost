// ========== СЕРВИС: PhotoService ==========
// Реализация логики загрузки, сохранения и управления фотографиями

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
    public class PhotoService : IPhotoService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png" };
        private const string UploadFolder = "uploads";

        public PhotoService(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// Загружает файл изображения с проверкой расширения, сохраняет и добавляет в БД
        /// </summary>
        public async Task<Photo> UploadAsync(IFormFile file, string userId)
        {
            // Проверка наличия файла
            if (file == null || file.Length == 0)
                throw new ArgumentException("Файл не выбран или пуст", nameof(file));

            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("UserId не может быть пусто", nameof(userId));

            // Получаем расширение файла
            var extension = Path.GetExtension(file.FileName).ToLower();

            // Проверяем разрешённые расширения
            if (!_allowedExtensions.Contains(extension))
                throw new InvalidOperationException(
                    $"Расширение '{extension}' не разрешено. Используйте .jpg, .jpeg, .png");

            // Генерируем уникальное имя файла
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";

            // Определяем путь для сохранения файла
            var uploadsFolderPath = Path.Combine(_environment.WebRootPath, UploadFolder);

            // Создаём директорию, если её не существует
            if (!Directory.Exists(uploadsFolderPath))
                Directory.CreateDirectory(uploadsFolderPath);

            // Полный путь к файлу
            var filePath = Path.Combine(uploadsFolderPath, uniqueFileName);

            // Сохраняем файл на диск
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Ошибка при сохранении файла на диск", ex);
            }

            // Создаём объект Photo для БД
            var photo = new Photo
            {
                Id = Guid.NewGuid(),
                FileName = file.FileName,
                Path = $"/{UploadFolder}/{uniqueFileName}",
                UploadDate = DateTime.UtcNow,
                UserId = userId
            };

            // Добавляем запись в БД
            _context.Photos.Add(photo);
            await _context.SaveChangesAsync();

            return photo;
        }

        /// <summary>
        /// Получает все фотографии пользователя из БД
        /// </summary>
        public async Task<List<Photo>> GetUserPhotosAsync(string userId)
        {
            return await _context.Photos
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.UploadDate)
                .ToListAsync();
        }

        /// <summary>
        /// Получает все фотографии из БД
        /// </summary>
        public async Task<List<Photo>> GetAllPhotosAsync()
        {
            return await _context.Photos
                .OrderByDescending(p => p.UploadDate)
                .ToListAsync();
        }

        /// <summary>
        /// Удаляет фотографию по ID (удаляет файл и запись из БД)
        /// </summary>
        public async Task DeletePhotoAsync(Guid photoId, string userId)
        {
            var photo = await _context.Photos.FindAsync(photoId);

            if (photo == null)
                throw new InvalidOperationException($"Фотография с ID {photoId} не найдена");

            if (photo.UserId != userId)
                throw new UnauthorizedAccessException("Вы не можете удалять фото других пользователей");

            try
            {
                // Удаляем файл с диска
                var filePath = Path.Combine(_environment.WebRootPath, 
                    photo.Path.TrimStart('/'));

                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Ошибка при удалении файла", ex);
            }

            // Удаляем запись из БД
            _context.Photos.Remove(photo);
            await _context.SaveChangesAsync();
        }
    }
}
