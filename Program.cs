// ========== КОНФИГУРАЦИЯ: Program.cs ==========
// Точка входа приложения, регистрация сервисов и middleware

using Microsoft.EntityFrameworkCore;
using PhotoHost.Data;
using PhotoHost.Services;

var builder = WebApplication.CreateBuilder(args);

// ===== РЕГИСТРАЦИЯ СЕРВИСОВ =====

// Добавляем контекст БД. По умолчанию используем SQLite для простоты локального запуска.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=photohost.db"));

// Регистрируем IPhotoService
builder.Services.AddScoped<IPhotoService, PhotoService>();

// Добавляем MVC контроллеры и views
builder.Services.AddControllersWithViews();

// Добавляем сервис для работы с временем
builder.Services.AddHttpContextAccessor();

// ===== КОНФИГУРАЦИЯ MIDDLEWARE =====

var app = builder.Build();

// Создаём БД при старте приложения, если её нет (EnsureCreated лучше подходит для простых локальных запусков)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

// Обработка исключений
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Статические файлы (CSS, JS, загруженные изображения)
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// ===== РОУТИНГ =====

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Photos}/{action=Index}/{id?}");

app.Run();
