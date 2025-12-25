using Microsoft.AspNetCore.Components;

namespace Fingerprint.Unifications.Components.Pages
{
	public class WorkingWithFingerprintModel : ComponentBase
	{
		[Inject]
		private NavigationManager NavigationManager { get; set; }
		public string PathFrequencyHistogramOne { get; set; } = Path.Combine("Pictures", "Histogram", "frequency_histogram_one.png");
		public string PathMFCCHistogramOne { get; set; } = Path.Combine("Pictures", "Histogram", "mfcc_histogram_one.png");

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
		}
	}
}
