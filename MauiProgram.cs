using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Journal.Data;
using Journal.Services;
using Journal.Services.Abstractions;

namespace Journal;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		// MudBlazor
		builder.Services.AddMudServices();

		// SQLite EF Core (MAUI-safe DB path)
		var dbPath = DbPath.GetSqlitePath();
		builder.Services.AddDbContext<JournalDbContext>(options =>
			options.UseSqlite($"Data Source={dbPath}"));

		// App services (Application layer)
		builder.Services.AddMudServices();
		builder.Services.AddScoped<IJournalService, JournalService>();
		builder.Services.AddSingleton<IPinService, PinService>();

		// Ensure DB exists (simple for coursework; migrations later)
		using (var scope = builder.Services.BuildServiceProvider().CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<JournalDbContext>();
			db.Database.EnsureCreated();
		}

		return builder.Build();
	}
}
