using Fingerprint.Unifications.Models;

namespace Fingerprint.Unifications.Storage
{
	public static class UploadedFile
	{
		public static string? FileName;
		public static AudioFile? AudioFileInformation;
		public static string? CompareMFCC;
		public static string? CompareFFT;
		public static Dictionary<string, double> dictionaryFFT = new Dictionary<string, double>();
		public static Dictionary<string, double> dictionaryMFCC = new Dictionary<string, double>();
		public static void Clear()
		{
			FileName = "";
			AudioFileInformation = new AudioFile();
			dictionaryFFT = new Dictionary<string, double>();
			dictionaryMFCC = new Dictionary<string, double>();
			CompareMFCC = "";
			CompareFFT = "";
		}
	}
}
