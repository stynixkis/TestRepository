using Fingerprint.Unifications.Storage;
using Microsoft.AspNetCore.Components;

namespace Fingerprint.Unifications.Components.Pages
{
	public class AudioFileComparisonModel : ComponentBase
	{
		[Inject]
		private NavigationManager NavigationManager { get; set; }
		public string PathFrequencyHistogramOne { get; set; } = Path.Combine("Pictures", "Histogram", "frequency_histogram_two.png");
		public string PathMFCCHistogramOne { get; set; } = Path.Combine("Pictures", "Histogram", "mfcc_histogram_two.png");
		public string InfoFFT { get; set; }
		public string InfoMFCC { get; set; }

		protected override void OnInitialized()
		{
			string full = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", PathFrequencyHistogramOne);
			// Проверка существования файлов
			if (!File.Exists(full))
			{
				Console.WriteLine($"Файл '{PathFrequencyHistogramOne}' не найден!");
				PathFrequencyHistogramOne = "";
			}
			Console.WriteLine(full);
			full = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", PathMFCCHistogramOne);

			if (!File.Exists(full))
			{
				Console.WriteLine($"Файл '{PathMFCCHistogramOne}' не найден!");
				PathMFCCHistogramOne = "";
			}
			Console.WriteLine(full);
			InfoFFT = UploadedFile.CompareFFT;
			InfoMFCC = UploadedFile.CompareMFCC;
		}
	}
}
