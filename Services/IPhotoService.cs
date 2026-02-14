// ========== ИНТЕРФЕЙС СЕРВИСА: IPhotoService ==========
// Определяет контракт для сервиса работы с фотографиями

using Microsoft.AspNetCore.Http;
using PhotoHost.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhotoHost.Services
{
    public interface IPhotoService
    {
        /// <summary>
        /// Загружает файл изображения, сохраняет его в систему и добавляет запись в БД
        /// </summary>
        /// <param name="file">Загруженный файл</param>
        /// <param name="userId">ID пользователя-владельца фото</param>
        /// <returns>Информация о загруженной фотографии</returns>
        Task<Photo> UploadAsync(IFormFile file, string userId);

        /// <summary>
        /// Получает все загруженные фотографии пользователя
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns>Список фотографий пользователя</returns>
        Task<List<Photo>> GetUserPhotosAsync(string userId);

        /// <summary>
        /// Получает все загруженные фотографии (для админа)
        /// </summary>
        /// <returns>Список всех фотографий</returns>
        Task<List<Photo>> GetAllPhotosAsync();

        /// <summary>
        /// Удаляет фотографию по ID
        /// </summary>
        /// <param name="photoId">ID фотографии</param>
        /// <param name="userId">ID пользователя (для проверки прав)</param>
        Task DeletePhotoAsync(System.Guid photoId, string userId);
    }
}
