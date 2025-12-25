using Audiofingerprint.Classes;
using Audiofingerprint.Interfaces;
using MathNet.Numerics.IntegralTransforms;
using NAudio.Wave;
using System.Drawing;
using System.Text;

namespace Audiofingerprint.Services
{
	public class FingerprintService : IFingerprintService
	{
		//ГЕНЕРАЦИЯ -- ПУТЬ ДО ФАЙЛА, ДИРЕКТОРИЯ ДЛЯ СОХРАНЕНИЯ
		public string GenerateFingerprint(string path, string directorySavePath)
		{
			try
			{
				AudioFileReader audio = АudioСonversion.ConversionForConsole(path);
				string name = Path.GetFileNameWithoutExtension(path);
				List<System.Numerics.Complex> complexArray = SpectralAnalysis(audio, name);
				return SaveFingerprintToBinaryFile(GroupPeaksIntoHashes(complexArray), directorySavePath, name);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ошибка: {ex.Message}");
			}
			return "................................";
		}

		//СРАВНЕНИЕ ОТПЕЧАТКОВ -- ПУТИ, ПРОЦЕНТ СХОЖЕСТИ
		public double CompareFingerprints(byte[] file1Bytes, byte[] file2Bytes)
		{
			double lenth = file1Bytes.Length < file2Bytes.Length ? file1Bytes.Length : file2Bytes.Length;
			int step = (int)lenth / 30;
			double persantage = 0;
			int numDifrent = 0;
			double max = 0;
			double maxInBlock = 0;
			double summax = 0;
			persantage = 0;
			numDifrent = 0;

			for (int i = 0; i < lenth; i++)
			{

				if (file1Bytes[i] != file2Bytes[i])
				{
					numDifrent++;
				}
			}

			double diff = numDifrent / (lenth / 100);
			persantage = 100 - diff;

			//Console.WriteLine($"Процент совпадения:{(persantage + summax / 30) / 2}");

			return persantage;
		}
		private static double CountRanges(double frequency)
		{
			double num = Math.Floor(frequency / 100) * 100;
			double result = frequency - num;

			//return (result < 50) ? num : num + 100;
			return result >= 25 && result <= 50 ? 50 + num : result < 25 ? num : 100 + num;
		}
		private static List<System.Numerics.Complex> SpectralAnalysis(AudioFileReader audio, string name)
		{
			float[] sample = new float[audio.WaveFormat.SampleRate * (int)audio.TotalTime.TotalSeconds];
			audio.Read(sample, 0, sample.Length);

			int frameSize = 8192;
			int overlap = frameSize / 2;
			int stepSize = frameSize - overlap;

			List<System.Numerics.Complex> allPeaks = new List<System.Numerics.Complex>();
			Dictionary<double, double> frequencyAmplitudeMap = new Dictionary<double, double>();

			for (int i = 0; i < sample.Length - frameSize; i += stepSize)
			{

				float[] frame = new float[frameSize];
				Array.Copy(sample, i, frame, 0, frameSize);
				frame = ApplyHannWindow(frame);

				System.Numerics.Complex[] fftForWindow = FFT(frame);

				double[] amplitude = fftForWindow.Select(c => c.Magnitude).ToArray();
				double[] frequency = Enumerable.Range(0, frameSize)
											  .Select(k => (double)k * audio.WaveFormat.SampleRate / frameSize)
											  .Take(frameSize / 2)
											  .ToArray();

				double meanAmplitude = amplitude.Max();
				double threshold = 0.3 * meanAmplitude;

				var peaks = frequency.Zip(amplitude, (freq, amp) => new { Frequency = freq, Amplitude = amp })
									 .Where(x => x.Amplitude > threshold)
									 .OrderByDescending(x => x.Amplitude)
									 .Take(5)
									 .ToList();

				foreach (var peak in peaks)
				{
					allPeaks.Add(new System.Numerics.Complex(peak.Frequency, peak.Amplitude));

					if (frequencyAmplitudeMap.ContainsKey(peak.Frequency))
					{
						frequencyAmplitudeMap[peak.Frequency] += peak.Amplitude;
					}
					else
					{
						frequencyAmplitudeMap[peak.Frequency] = peak.Amplitude;
					}
				}
			}
			GenerateHistogram(frequencyAmplitudeMap, name);

			return allPeaks;
		}
		private static void GenerateHistogram(Dictionary<double, double> frequencyAmplitudeMap, string name)
		{
			int width = 600;
			int height = 400;
			int margin = 50;

			using (Bitmap bitmap = new Bitmap(width, height))
			using (Graphics g = Graphics.FromImage(bitmap))
			{
				g.Clear(Color.White);

				Pen axisPen = new Pen(Color.Black, 2);
				g.DrawLine(axisPen, margin, height - margin, width - margin, height - margin);
				g.DrawLine(axisPen, margin, margin, margin, height - margin);


				var groupedData = frequencyAmplitudeMap
					.GroupBy(kvp => (int)(kvp.Key / 100) * 100)
					.ToDictionary(
						group => group.Key,
						group => group.Sum(kvp => kvp.Value)
					);

				double maxAmplitude = groupedData.Values.Max();
				int barWidth = 40;
				int x = margin;

				foreach (var kvp in groupedData.OrderBy(kvp => kvp.Key))
				{
					int barHeight = (int)(kvp.Value / maxAmplitude * (height - 2 * margin));
					Color barColor = Color.FromArgb(0, 0, (int)(255 * (kvp.Value / maxAmplitude)));
					g.FillRectangle(new SolidBrush(barColor), x, height - margin - barHeight, barWidth, barHeight);
					x += barWidth + 10;
				}

				Font font = new Font("Arial", 10);
				g.DrawString("Частота (Гц)", font, Brushes.Black, width / 2 - 50, height - margin + 20);
				g.DrawString("Амплитуда", font, Brushes.Black, margin - 50, margin - 20);
				g.DrawString("Гистограмма частот", new Font("Arial", 14), Brushes.Black, width / 2 - 100, 10);

				x = margin;
				foreach (var kvp in groupedData.OrderBy(kvp => kvp.Key))
				{
					string label = $"{kvp.Key} Гц";
					SizeF labelSize = g.MeasureString(label, font);
					g.DrawString(label, font, Brushes.Black, x + (barWidth - labelSize.Width) / 2, height - margin + 5);
					x += barWidth + 10;
				}

				string fullPath = Path.Combine("wwwroot", "Pictures", "Histogram", $"frequency_histogram_one.png");
				bitmap.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);
			}
		}
		//ГЕНЕРАЦИЯ 2-Х ГИСТОГРАММ - 1 АУДИО, 2 АУДИО, ИМЯ ФАЙЛА
		public void GenerateComparisonHistogram(string firstPath, string secondPath)
		{
			var firstFrequencies = GetFrequencyData(firstPath);
			var secondFrequencies = GetFrequencyData(secondPath);

			GenerateDualHistogram(firstFrequencies, secondFrequencies);
		}

		private Dictionary<double, double> GetFrequencyData(string audioPath)
		{
			using (var audio = АudioСonversion.ConversionForConsole(audioPath))
			{
				float[] sample = new float[audio.WaveFormat.SampleRate * (int)audio.TotalTime.TotalSeconds];
				audio.Read(sample, 0, sample.Length);

				int frameSize = 8192;
				Dictionary<double, double> frequencyAmplitudeMap = new Dictionary<double, double>();

				for (int i = 0; i < sample.Length - frameSize; i += frameSize / 2)
				{
					float[] frame = new float[frameSize];
					Array.Copy(sample, i, frame, 0, frameSize);
					frame = ApplyHannWindow(frame);

					System.Numerics.Complex[] fftForWindow = FFT(frame);
					double[] amplitude = fftForWindow.Select(c => c.Magnitude).ToArray();
					double[] frequency = Enumerable.Range(0, frameSize)
												.Select(k => (double)k * audio.WaveFormat.SampleRate / frameSize)
												.Take(frameSize / 2)
												.ToArray();

					double meanAmplitude = amplitude.Max();
					double threshold = 0.3 * meanAmplitude;

					var peaks = frequency.Zip(amplitude, (freq, amp) => new { Frequency = freq, Amplitude = amp })
										 .Where(x => x.Amplitude > threshold)
										 .OrderByDescending(x => x.Amplitude)
										 .Take(5);

					foreach (var peak in peaks)
					{
						if (frequencyAmplitudeMap.ContainsKey(peak.Frequency))
						{
							frequencyAmplitudeMap[peak.Frequency] += peak.Amplitude;
						}
						else
						{
							frequencyAmplitudeMap[peak.Frequency] = peak.Amplitude;
						}
					}
				}

				return frequencyAmplitudeMap;
			}
		}

		private void GenerateDualHistogram(
			Dictionary<double, double> firstData,
			Dictionary<double, double> secondData)
		{
			int width = 800;
			int height = 500;
			int margin = 50;

			using (Bitmap bitmap = new Bitmap(width, height))
			using (Graphics g = Graphics.FromImage(bitmap))
			{
				g.Clear(Color.White);

				Pen axisPen = new Pen(Color.Black, 2);
				g.DrawLine(axisPen, margin, height - margin, width - margin, height - margin);
				g.DrawLine(axisPen, margin, margin, margin, height - margin);

				var firstGrouped = firstData
					.GroupBy(kvp => (int)(kvp.Key / 100) * 100)
					.ToDictionary(
						group => group.Key,
						group => group.Sum(kvp => kvp.Value)
					);

				var secondGrouped = secondData
					.GroupBy(kvp => (int)(kvp.Key / 100) * 100)
					.ToDictionary(
						group => group.Key,
						group => group.Sum(kvp => kvp.Value)
					);

				var allBands = firstGrouped.Keys.Union(secondGrouped.Keys).OrderBy(k => k).ToList();

				double maxAmplitude = Math.Max(
					firstGrouped.Values.DefaultIfEmpty().Max(),
					secondGrouped.Values.DefaultIfEmpty().Max());

				int barWidth = 30;
				int spacing = 10;
				int x = margin;

				foreach (var band in allBands)
				{
					double firstValue = firstGrouped.TryGetValue(band, out var fv) ? fv : 0;
					double secondValue = secondGrouped.TryGetValue(band, out var sv) ? sv : 0;

					int firstHeight = (int)(firstValue / maxAmplitude * (height - 2 * margin));
					int secondHeight = (int)(secondValue / maxAmplitude * (height - 2 * margin));

					g.FillRectangle(Brushes.Blue, x, height - margin - firstHeight, barWidth, firstHeight);

					g.FillRectangle(Brushes.DarkBlue, x + barWidth + 2, height - margin - secondHeight, barWidth, secondHeight);

					string label = $"{band} Гц";
					SizeF labelSize = g.MeasureString(label, SystemFonts.DefaultFont);
					g.DrawString(label, SystemFonts.DefaultFont, Brushes.Black,
						x + (barWidth * 2 + 2 - labelSize.Width) / 2, height - margin + 5);

					x += barWidth * 2 + 2 + spacing;
				}

				g.FillRectangle(Brushes.Blue, width - 150, margin, 20, 20);
				g.DrawString("Первое аудио", SystemFonts.DefaultFont, Brushes.Black, width - 125, margin);
				g.FillRectangle(Brushes.DarkBlue, width - 150, margin + 25, 20, 20);
				g.DrawString("Второе аудио", SystemFonts.DefaultFont, Brushes.Black, width - 125, margin + 25);

				g.DrawString("Сравнение частот", new Font("Arial", 14), Brushes.Black, width / 2 - 100, 10);
				g.DrawString("Частота (Гц)", SystemFonts.DefaultFont, Brushes.Black, width / 2 - 50, height - margin + 20);
				g.DrawString("Амплитуда", SystemFonts.DefaultFont, Brushes.Black, margin - 40, margin);

				string fullPath = Path.Combine("wwwroot", "Pictures", "Histogram", $"frequency_histogram_two.png");
				bitmap.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);
			}
		}
		private static List<uint> GroupPeaksIntoHashes(List<System.Numerics.Complex> peaks)
		{
			List<uint> fingerprint = new List<uint>();

			for (int i = 0; i < peaks.Count - 2; i++)
			{
				double freq1 = CountRanges(peaks[i].Real);

				string hashInput = $"{freq1:F2}";

				uint hash = CalculateFrequencyHash(hashInput);

				fingerprint.Add(hash);
			}

			return fingerprint;
		}
		private static uint CalculateFrequencyHash(string frequency)
		{
			if (string.IsNullOrEmpty(frequency))
				throw new ArgumentException("Неверное значение частоты.");

			byte[] frequencyBytes = Encoding.UTF8.GetBytes(frequency);

			uint hash = 0;
			foreach (byte b in frequencyBytes)
			{
				hash = (hash << 5) + hash ^ b;
			}
			return hash;
		}
		private static string SaveFingerprintToBinaryFile(List<uint> fingerprint, string filePath, string name)
		{
			Random random = new Random();
			int num = random.Next(0, 100000);
			filePath += $"\\fingerprint_{name}.bin";
			using (BinaryWriter writer = new BinaryWriter(File.Open(filePath, FileMode.Create)))
			{
				foreach (uint hash in fingerprint)
				{
					writer.Write(hash);
				}
			}
			return filePath;
		}
		private static float[] ApplyHannWindow(float[] frame)
		{
			int N = frame.Length;
			for (int i = 0; i < N; i++)
			{
				float windowValue = 0.5f * (1 - (float)Math.Cos(2 * Math.PI * i / (N - 1)));
				frame[i] *= windowValue;
			}

			return frame;
		}
		private static System.Numerics.Complex[] FFT(float[] signal)
		{
			System.Numerics.Complex[] complexSignal = new System.Numerics.Complex[signal.Length];
			for (int i = 0; i < signal.Length; i++)
			{
				complexSignal[i] = new System.Numerics.Complex(signal[i], 0);
			}
			Fourier.Forward(complexSignal);
			return complexSignal;
		}
	}
}
