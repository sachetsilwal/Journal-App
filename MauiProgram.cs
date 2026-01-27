using Microsoft.Extensions.Logging;
using Journal.Data;
using Journal.Services;

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
			});

		builder.Services.AddMauiBlazorWebView();

		// Register database service as singleton (one instance for the app)
		builder.Services.AddSingleton<JournalDatabase>();

		// Register authentication service as singleton
		builder.Services.AddSingleton<AuthService>();

		// Register journal service as singleton
		builder.Services.AddSingleton<JournalService>();

		// Register tag service as singleton
		builder.Services.AddSingleton<TagService>();

		// Register mood service as singleton
		builder.Services.AddSingleton<MoodService>();

		// Register streak service as singleton
		builder.Services.AddSingleton<StreakService>();

		// Register settings service as singleton
		builder.Services.AddSingleton<SettingsService>();

		// Register export service as singleton
		builder.Services.AddSingleton<ExportService>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
