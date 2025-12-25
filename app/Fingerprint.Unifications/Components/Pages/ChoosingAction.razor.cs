using Audiofingerprint.Services;
using AudioFingerprinting;
using Fingerprint.Unifications.Models;
using Fingerprint.Unifications.Storage;
using Microsoft.AspNetCore.Components;

namespace Fingerprint.Unifications.Components.Pages
{
	public class ChoosingActionModel : ComponentBase
	{
		[Inject]
		private NavigationManager NavigationManager { get; set; }
		private readonly FingerprintDatabaseContext _context = new FingerprintDatabaseContext();
		protected bool showLoading = false;
		private FingerprintService _finServ = new FingerprintService();
		private MfccFingerprinter _finMFCC = new MfccFingerprinter();
		public string ActionInformation { get; set; } = "";
		public void NavigateToDownload()
		{
			NavigationManager.NavigateTo("/UploadingAudioForComparison");
		}

		private MfccFingerprinter _mfccFingerprinter = new MfccFingerprinter();

		public async Task NavigateToGenerate()
		{
			showLoading = true;
			StateHasChanged();
			ActionInformation = "ПОДОЖДИТЕ, ИДЕТ ГЕНЕРАЦИЯ!";
			try
			{
				var path = Path.Combine(Directory.GetCurrentDirectory(), "AudioFilesUser", "audioFile.wav");

				using var client = new HttpClient();
				var formData = new MultipartFormDataContent();
				formData.Add(new StringContent(path), "path");

				await Task.Delay(10000);

				var response = await client.PostAsync(
					"https://localhost:7199/api/audiofiles/Generation-without-saving-in-the-database",
					formData);

				if (response.IsSuccessStatusCode)
				{
					UploadedFile.AudioFileInformation = await response.Content.ReadFromJsonAsync<AudioFile>();
					Console.WriteLine($"id = {UploadedFile.AudioFileInformation.IdAudio}\n - title = {UploadedFile.AudioFileInformation.TitleAudio}\n" +
						$" - fft = {UploadedFile.AudioFileInformation.FftPrint.ToString()}\n - mfcc = {UploadedFile.AudioFileInformation.MfccPrint.ToString()}\n");
					_mfccFingerprinter.PlotAudioWaveformOne(path);
					NavigationManager.NavigateTo("/WorkingWithFingerprint");
				}
				else
				{
					Console.WriteLine($"Ошибка: {response.StatusCode}. {await response.Content.ReadAsStringAsync()}");
				}
			}
			finally
			{
				showLoading = false;
				StateHasChanged();
			}
		}
		public async Task NavigateToRelated()
		{
			showLoading = true;
			StateHasChanged();
			ActionInformation = "ПОДОЖДИТЕ, ИДЕТ ПОИСК ПОХОЖИХ АУДИО!";

			try
			{
				var path = Path.Combine(Directory.GetCurrentDirectory(), "AudioFilesUser", "audioFile.wav");

				using (var client = new HttpClient())
				{
					var formData = new MultipartFormDataContent();
					formData.Add(new StringContent(path), "path");

					await Task.Delay(10000);

					var response = await client.PostAsync(
						"https://localhost:7199/api/audiofiles/Generation-without-saving-in-the-database",
						formData);

					if (response.IsSuccessStatusCode)
					{
						UploadedFile.AudioFileInformation = await response.Content.ReadFromJsonAsync<AudioFile>();
						Console.WriteLine($"id = {UploadedFile.AudioFileInformation.IdAudio}\n - title = {UploadedFile.AudioFileInformation.TitleAudio}\n" +
							$" - fft = {UploadedFile.AudioFileInformation.FftPrint.ToString()}\n - mfcc = {UploadedFile.AudioFileInformation.MfccPrint.ToString()}\n");
					}
					else
					{
						Console.WriteLine($"Ошибка: {response.StatusCode}. {await response.Content.ReadAsStringAsync()}");
					}
				}

				foreach (var item in _context.AudioFiles)
				{
					var fftValue = _finServ.CompareFingerprints(UploadedFile.AudioFileInformation.FftPrint, item.FftPrint);
					var mfccValue = _finMFCC.Compare(UploadedFile.AudioFileInformation.MfccPrint, item.MfccPrint);

					if (fftValue > 70)
					{
						UploadedFile.dictionaryFFT.Add(item.TitleAudio, fftValue);
					}
					if (mfccValue > 66.54)
					{
						UploadedFile.dictionaryMFCC.Add(item.TitleAudio, mfccValue);
					}
				}

				NavigationManager.NavigateTo("/RelatedAudio");
			}
			finally
			{
				showLoading = false;
				StateHasChanged();
			}
		}

	}
}