using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Audiofingerprint.Classes
{
	public static class АudioСonversion
	{
		public static AudioFileReader ConversionForConsole(string path)
		{
			if (Path.GetExtension(path) != ".wav")
			{
				throw new Exception("File extension must be .wav");
			}
			Console.WriteLine("------------ПРЕОБРАЗОВАНИЕ АУДИО-----------");
			AudioFileReader originalAudio = new AudioFileReader(path);
			Console.WriteLine("\nИз стерео в моно>>>>");

			Console.WriteLine($"\nКоличество каналов до: {originalAudio.WaveFormat.Channels}");

			originalAudio = ConvertToMono(originalAudio);

			Console.WriteLine($"Количество каналов после: {originalAudio.WaveFormat.Channels}");

			Console.WriteLine("\nИзменение частоты дискретизации>>>>");

			Console.WriteLine($"\nЧастота дискретизации до: {originalAudio.WaveFormat.SampleRate}");

			originalAudio = ChangeTheSamplingRate(originalAudio, 44100);

			Console.WriteLine($"Частота дискретизации после: {originalAudio.WaveFormat.SampleRate}");

			Console.WriteLine("\nНормализация>>>>");

			Console.WriteLine($"\nМаксимальная амплитуда до: {FindMaxAmplitude(originalAudio)}");

			AudioFileReader normalizedAudio = NormalizationOfSignalAmplitude(originalAudio);

			Console.WriteLine($"Максимальная амплитуда после: {FindMaxAmplitude(normalizedAudio)}");

			return normalizedAudio;
		}
		public static AudioFileReader Conversion(string path)
		{
			if (Path.GetExtension(path) != ".wav")
			{
				throw new Exception("File extension must be .wav");
			}
			AudioFileReader originalAudio = new AudioFileReader(path);

			originalAudio = ConvertToMono(originalAudio);

			originalAudio = ChangeTheSamplingRate(originalAudio, 44100);

			AudioFileReader normalizedAudio = NormalizationOfSignalAmplitude(originalAudio);

			return normalizedAudio;
		}
		public static AudioFileReader ConvertToMono(AudioFileReader audio)
		{
			if (audio.WaveFormat.Channels < 2)
			{
				Console.WriteLine("Файл уже моно.");
				return audio;
			}
			else
			{
				SampleToWaveProvider monoStream = new SampleToWaveProvider(audio.ToMono());
				Console.WriteLine("Преобразование успешно завершено.");
				return ConvertToAudioFileReader(monoStream, audio.Length);
			}
		}
		public static AudioFileReader ChangeTheSamplingRate(AudioFileReader audio, int samplingRate)
		{

			var resampler = new WdlResamplingSampleProvider(audio, samplingRate);
			var waveProvider = new SampleToWaveProvider(resampler);

			return ConvertToAudioFileReader(waveProvider, audio.Length);
		}
		private static AudioFileReader ConvertToAudioFileReader(SampleToWaveProvider audio, long lenth)
		{
			string tempWavPath = Path.GetTempFileName() + ".wav";

			using (var writer = new WaveFileWriter(tempWavPath, audio.WaveFormat))
			{
				byte[] buffer = new byte[lenth * sizeof(float)];
				int bytesRead;

				while ((bytesRead = audio.Read(buffer, 0, buffer.Length)) > 0)
				{
					writer.Write(buffer, 0, bytesRead);
				}
			}

			var result = new AudioFileReader(tempWavPath);
			return result;
		}
		public static AudioFileReader NormalizationOfSignalAmplitude(AudioFileReader audio)
		{
			float[] buffer = new float[1024];

			float maxAmplitude = FindMaxAmplitude(audio);

			if (maxAmplitude == 0 || maxAmplitude == 1)
			{
				return audio;
			}
			audio.Position = 0;

			string tempWavPath = Path.ChangeExtension(Path.GetTempFileName(), ".wav");

			using (WaveFileWriter writer = new WaveFileWriter(tempWavPath, audio.WaveFormat))
			{
				while (audio.Position < audio.Length)
				{
					int count = audio.Read(buffer, 0, buffer.Length);
					if (count == 0) break;

					for (int i = 0; i < count; i++)
					{
						buffer[i] = buffer[i] / maxAmplitude;
					}

					writer.WriteSamples(buffer, 0, count);
				}
			}
			return new AudioFileReader(tempWavPath);
		}
		public static float FindMaxAmplitude(AudioFileReader audio)
		{
			float[] buffer = new float[1024];
			float maxAbsValue = 0f;

			long originalPosition = audio.Position;

			audio.Position = 0;

			while (audio.Position < audio.Length)
			{
				int count = audio.Read(buffer, 0, buffer.Length);
				if (count == 0) break;

				for (int i = 0; i < count; i++)
				{
					float absVal = Math.Abs(buffer[i]);
					if (absVal > maxAbsValue)
						maxAbsValue = absVal;
				}
			}
			audio.Position = originalPosition;

			return maxAbsValue;
		}
	}
}
