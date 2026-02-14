// ========== РАСШИРЕННЫЙ ПРИМЕР: PhotoService Альтернатива ==========
// Дополнительные методы для более продвинутого функционала

/*

// Метод для получения статистики
public async Task<PhotoStatistics> GetStatisticsAsync()
{
    var photos = await _context.Photos.ToListAsync();
    return new PhotoStatistics
    {
        TotalPhotos = photos.Count,
        TotalSize = photos.Sum(p => GetFileSize(p.Path)),
        OldestPhoto = photos.OrderBy(p => p.UploadDate).FirstOrDefault(),
        NewestPhoto = photos.OrderByDescending(p => p.UploadDate).FirstOrDefault()
    };
}

// Метод для поиска фотографий по дате
public async Task<List<Photo>> SearchByDateRangeAsync(DateTime startDate, DateTime endDate)
{
    return await _context.Photos
        .Where(p => p.UploadDate >= startDate && p.UploadDate <= endDate)
        .OrderByDescending(p => p.UploadDate)
        .ToListAsync();
}

// Метод для пакетной загрузки
public async Task<List<Photo>> UploadMultipleAsync(IFormFileCollection files)
{
    var uploadedPhotos = new List<Photo>();
    
    foreach (var file in files)
    {
        try
        {
            var photo = await UploadAsync(file);
            uploadedPhotos.Add(photo);
        }
        catch (Exception ex)
        {
            // Логируем ошибку для каждого файла
            _logger.LogError(ex, $"Ошибка при загрузке {file.FileName}");
        }
    }
    
    return uploadedPhotos;
}

// Метод для очистки старых фотографий
public async Task<int> DeleteOldPhotosAsync(int daysOld)
{
    var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
    var oldPhotos = await _context.Photos
        .Where(p => p.UploadDate < cutoffDate)
        .ToListAsync();
    
    int deletedCount = 0;
    foreach (var photo in oldPhotos)
    {
        await DeletePhotoAsync(photo.Id);
        deletedCount++;
    }
    
    return deletedCount;
}

// Метод для экспорта метаданных в JSON
public async Task<string> ExportMetadataAsync()
{
    var photos = await GetAllPhotosAsync();
    return JsonConvert.SerializeObject(photos, Formatting.Indented);
}

*/
