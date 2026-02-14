// ========== КОНТРОЛЛЕР: PhotosController ==========
// MVC контроллер для работы с HTTP запросами загрузки и отображения фотографий

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PhotoHost.Models;
using PhotoHost.Services;
using System;
using System.Threading.Tasks;

namespace PhotoHost.Controllers
{
    public class PhotosController : Controller
    {
        private readonly IPhotoService _photoService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<PhotosController> _logger;

        public PhotosController(IPhotoService photoService, UserManager<AppUser> userManager, ILogger<PhotosController> logger)
        {
            _photoService = photoService ?? throw new ArgumentNullException(nameof(photoService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
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
                if (User.Identity?.IsAuthenticated == true)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        var photos = await _photoService.GetUserPhotosAsync(user.Id);
                        ViewBag.Title = "Мои фотографии";
                        ViewBag.UserName = $"{user.FirstName} {user.LastName}".Trim();
                        return View(photos);
                    }
                }

                // Показываем все фото для неавторизованных пользователей
                var allPhotos = await _photoService.GetAllPhotosAsync();
                ViewBag.Title = "Все фотографии";
                return View(allPhotos);
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
        [Authorize]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                TempData["Error"] = "Выберите файл(ы) для загрузки";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "Пользователь не найден";
                return RedirectToAction(nameof(Index));
            }

            var uploaded = 0;
            var errors = new List<string>();

            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                    continue;

                try
                {
                    await _photoService.UploadAsync(file, user.Id);
                    uploaded++;
                    _logger.LogInformation($"Файл {file.FileName} успешно загружен пользователем {user.Id}");
                }
                catch (InvalidOperationException ex)
                {
                    errors.Add($"{file.FileName}: {ex.Message}");
                    _logger.LogWarning(ex, "Ошибка валидации при загрузке файла");
                }
                catch (Exception ex)
                {
                    errors.Add($"{file.FileName}: ошибка при загрузке");
                    _logger.LogError(ex, "Неожиданная ошибка при загрузке файла");
                }
            }

            if (uploaded > 0)
                TempData["Success"] = $"Успешно загружено {uploaded} файл(ов).";

            if (errors.Any())
                TempData["Error"] = string.Join("; ", errors);

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// POST запрос: удаляет фотографию по ID
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Delete(Guid photoId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "Пользователь не найден";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _photoService.DeletePhotoAsync(photoId, user.Id);
                TempData["Success"] = "Фотография удалена!";
                _logger.LogInformation($"Фотография {photoId} удалена пользователем {user.Id}");
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Вы не можете удалять фото других пользователей";
                _logger.LogWarning($"Попытка удалить чужое фото {photoId}");
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
