using Fingerprint.Unifications.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Fingerprint.Unifications.Components.Pages
{
	public class HomeModel : ComponentBase
	{
		[Inject]
		private NavigationManager Navigation { get; set; }

		public async Task HandleFileSelected(InputFileChangeEventArgs e)
		{
			try
			{
				UploadedFile.Clear();
				UploadedFile.FileName = e.File.Name;
				Console.WriteLine($"Начало обработки файла: {UploadedFile.FileName}");

				if (!e.File.Name.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
				{
					Console.WriteLine("неверный формат файла! нужен .WAV файл!");
					return;
				}

				// Очистка директорий с повторными попытками
				await SafeCleanDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Pictures", "Histogram"));
				await SafeCleanDirectory(Path.Combine(Directory.GetCurrentDirectory(), "AudioFilesUser"));

				// Сохранение файла с гарантированным освобождением ресурсов
				var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "AudioFilesUser");
				Directory.CreateDirectory(uploadsDir);

				var filePath = Path.Combine(uploadsDir, "audioFile.wav");

				// Удаление старого файла с ожиданием
				await SafeDeleteFile(filePath);

				// Сохранение нового файла
				await using (var stream = e.File.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024))
				{
					await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
					{
						await stream.CopyToAsync(fileStream);
					}
				}

				Console.WriteLine($"Файл успешно сохранен: {filePath}");
				Navigation.NavigateTo("/ChosingAction");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Критическая ошибка: {ex.Message}");
			}
		}

		// Улучшенная функция очистки директории
		private async Task SafeCleanDirectory(string directoryPath, int maxAttempts = 3, int delayMs = 300)
		{
			if (!Directory.Exists(directoryPath))
			{
				Directory.CreateDirectory(directoryPath);
				return;
			}

			foreach (var file in Directory.EnumerateFiles(directoryPath))
			{
				await SafeDeleteFile(file, maxAttempts, delayMs);
			}
		}

		// Надежное удаление файла с повторными попытками
		private async Task SafeDeleteFile(string filePath, int maxAttempts = 3, int delayMs = 300)
		{
			for (int attempt = 1; attempt <= maxAttempts; attempt++)
			{
				try
				{
					if (!File.Exists(filePath)) return;

					// Освобождаем ресурсы
					GC.Collect();
					GC.WaitForPendingFinalizers();

					// Устанавливаем атрибуты для перезаписи
					File.SetAttributes(filePath, FileAttributes.Normal);
					File.Delete(filePath);
					Console.WriteLine($"Файл удален: {filePath}");
					return;
				}
				catch (IOException ex) when (attempt < maxAttempts &&
					  (ex.HResult == -2147024864 || ex.Message.Contains("used by another process")))
				{
					await Task.Delay(delayMs * attempt);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Ошибка удаления файла {filePath}: {ex.Message}");
					throw;
				}
			}
			throw new IOException($"Не удалось удалить файл после {maxAttempts} попыток: {filePath}");
		}
	}
}