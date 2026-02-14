// ========== КОНТРОЛЛЕР: PhotosController ==========
// MVC контроллер для работы с HTTP запросами загрузки и отображения фотографий

using Microsoft.AspNetCore.Mvc;
using PhotoHost.Services;
using System;
using System.Threading.Tasks;

namespace PhotoHost.Controllers
{
    public class PhotosController : Controller
    {
        private readonly IPhotoService _photoService;
        private readonly ILogger<PhotosController> _logger;

        public PhotosController(IPhotoService photoService, ILogger<PhotosController> logger)
        {
            _photoService = photoService ?? throw new ArgumentNullException(nameof(photoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET запрос: отображает страницу со списком всех загруженных фотографий
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var photos = await _photoService.GetAllPhotosAsync();
                ViewBag.Title = "Фотохостинг";
                return View(photos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка фотографий");
                ModelState.AddModelError("", "Ошибка при загрузке фотографий");
                return View(new List<Models.Photo>());
            }
        }

        /// <summary>
        /// POST запрос: загружает файл изображения на сервер
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Выберите файл для загрузки");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _photoService.UploadAsync(file);
                TempData["Success"] = "Фотография успешно загружена!";
                _logger.LogInformation($"Файл {file.FileName} успешно загружен");
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                _logger.LogWarning(ex, "Ошибка валидации при загрузке файла");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Произошла ошибка при загрузке файла";
                _logger.LogError(ex, "Неожиданная ошибка при загрузке файла");
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// POST запрос: удаляет фотографию по ID
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(Guid photoId)
        {
            try
            {
                await _photoService.DeletePhotoAsync(photoId);
                TempData["Success"] = "Фотография удалена!";
                _logger.LogInformation($"Фотография {photoId} удалена");
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
