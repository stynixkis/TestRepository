using Audiofingerprint.Services;
using AudioFingerprinting;
using Fingerprint.Unifications.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Fingerprint.Unifications.Components.Pages
{
	public class UploadingAudioForComparisonModel : ComponentBase
	{
		[Inject]
		private NavigationManager Navigation { get; set; }
		private MfccFingerprinter _mfccFingerprinter = new MfccFingerprinter();
		private FingerprintService _fingerprintService = new FingerprintService();

		protected bool showLoading = false;
		public async Task HandleFileSelected(InputFileChangeEventArgs e)
		{
			showLoading = true;
			StateHasChanged();

			try
			{
				// 1. Проверка формата файла
				UploadedFile.FileName = e.File.Name;
				Console.WriteLine($"Начало обработки файла: {UploadedFile.FileName}");

				if (!e.File.Name.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
				{
					Console.WriteLine("Ошибка: Неверный формат файла");
					return;
				}

				// 2. Подготовка директорий
				var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "AudioFilesUser");
				var histogramsDir = Path.Combine("wwwroot", "Pictures", "Histogram");

				Directory.CreateDirectory(uploadsDir);
				Directory.CreateDirectory(histogramsDir);

				// 3. Удаление старых файлов
				var filesToDelete = new List<string>
				{
					Path.Combine(uploadsDir, "audioFileCompare.wav"),
					Path.Combine(histogramsDir, "mfcc_histogram_two.png"),
					Path.Combine(histogramsDir, "frequency_histogram_two.png")
				};

				foreach (var filePath in filesToDelete)
				{
					if (!await TryDeleteFile(filePath))
					{
						return;
					}
				}

				// 4. Сохранение нового файла
				var newFilePath = Path.Combine(uploadsDir, "audioFileCompare.wav");
				try
				{
					await using var stream = e.File.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024);
					await using var fileStream = new FileStream(newFilePath, FileMode.Create);
					await stream.CopyToAsync(fileStream);
					Console.WriteLine($"Файл сохранен: {newFilePath}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Ошибка сохранения: {ex.Message}");
					return;
				}

				// 5. Обработка через API
				var path = Path.Combine(uploadsDir, "audioFile.wav");
				var pathCompare = newFilePath;

				// Отправка обоих файлов на обработку
				if (!await ProcessAudioFile(path) || !await ProcessAudioFile(pathCompare))
				{
					return;
				}

				// 6. Сравнение отпечатков
				var workingDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					"Downloads", "FingerprintResults");

				var fingerprintFiles = new[]
				{
					Path.Combine(workingDir, "fingerprint_audioFileCompare.bin"),
					Path.Combine(workingDir, "fingerprint_audioFile.bin"),
					Path.Combine(workingDir, "audioFileCompare_MFCC.bin"),
					Path.Combine(workingDir, "audioFile_MFCC.bin")
				};

				// Ожидание генерации файлов
				if (!await WaitForFiles(fingerprintFiles, maxAttempts: 10, delayMs: 1000))
				{
					return;
				}

				// 7. Выполнение сравнения
				var (fftResult, mfccResult) = await CompareFingerprints(
					Path.Combine(workingDir, "fingerprint_audioFile.bin"),
					Path.Combine(workingDir, "fingerprint_audioFileCompare.bin"),
					Path.Combine(workingDir, "audioFile_MFCC.bin"),
					Path.Combine(workingDir, "audioFileCompare_MFCC.bin"));

				if (fftResult == null || mfccResult == null)
				{
					return;
				}

				UploadedFile.CompareFFT = fftResult;
				UploadedFile.CompareMFCC = mfccResult;

				// 8. Визуализация результатов
				_mfccFingerprinter.PlotAudioWaveformTwo(path, pathCompare);
				_fingerprintService.GenerateComparisonHistogram(path, pathCompare);

				Navigation.NavigateTo("/AudioFileComparison");
			}
			finally
			{
				showLoading = false;
				StateHasChanged();
			}
		}

		// Вспомогательные методы:

		private async Task<bool> TryDeleteFile(string filePath, int maxAttempts = 3, int delayMs = 300)
		{
			for (int attempt = 1; attempt <= maxAttempts; attempt++)
			{
				try
				{
					if (!File.Exists(filePath)) return true;

					// Освобождаем ресурсы
					GC.Collect();
					GC.WaitForPendingFinalizers();

					File.Delete(filePath);
					Console.WriteLine($"Файл удален: {filePath}");
					return true;
				}
				catch (IOException ex) when (ex.Message.Contains("used by another process"))
				{
					if (attempt < maxAttempts)
					{
						await Task.Delay(delayMs * attempt);
						continue;
					}
					Console.WriteLine($"Не удалось удалить файл после {maxAttempts} попыток: {filePath}");
					return false;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Ошибка при удалении файла: {ex.Message}");
					return false;
				}
			}
			return false;
		}

		private async Task<bool> ProcessAudioFile(string filePath)
		{
			using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
			try
			{
				var formData = new MultipartFormDataContent();
				formData.Add(new StringContent(filePath), "path");

				var response = await client.PostAsync(
					"https://localhost:7199/api/audiofiles/Generation-without-saving-in-the-database",
					formData);

				if (!response.IsSuccessStatusCode)
				{
					Console.WriteLine($"Ошибка API: {response.StatusCode}");
					return false;
				}

				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ошибка обработки файла: {ex.Message}");
				return false;
			}
		}

		private async Task<bool> WaitForFiles(string[] filePaths, int maxAttempts, int delayMs)
		{
			for (int attempt = 1; attempt <= maxAttempts; attempt++)
			{
				if (filePaths.All(File.Exists))
				{
					return true;
				}

				if (attempt < maxAttempts)
				{
					await Task.Delay(delayMs);
					Console.WriteLine($"Ожидание файлов... Попытка {attempt}/{maxAttempts}");
				}
			}
			return false;
		}

		private async Task<(string fftResult, string mfccResult)> CompareFingerprints(
			string fftPath1, string fftPath2, string mfccPath1, string mfccPath2)
		{
			using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
			try
			{
				// Сравнение FFT
				var fftResponse = await client.PostAsync(
					"https://localhost:7199/api/audiofiles/compare-fft",
					new MultipartFormDataContent
					{
				{ new StringContent(fftPath1), "pathFirst" },
				{ new StringContent(fftPath2), "pathSecond" }
					});

				// Сравнение MFCC
				var mfccResponse = await client.PostAsync(
					"https://localhost:7199/api/audiofiles/compare-mfcc",
					new MultipartFormDataContent
					{
				{ new StringContent(mfccPath1), "pathFirst" },
				{ new StringContent(mfccPath2), "pathSecond" }
					});

				if (!fftResponse.IsSuccessStatusCode || !mfccResponse.IsSuccessStatusCode)
				{
					Console.WriteLine($"Ошибки сравнения: FFT={fftResponse.StatusCode}, MFCC={mfccResponse.StatusCode}");
					return (null, null);
				}

				return (
					await fftResponse.Content.ReadAsStringAsync(),
					await mfccResponse.Content.ReadAsStringAsync()
				);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ошибка сравнения: {ex.Message}");
				return (null, null);
			}
		}
	}
}
