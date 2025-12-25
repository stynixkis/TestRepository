namespace Audiofingerprint.Interfaces
{
	public interface IFingerprintService
	{
		string GenerateFingerprint(string path, string filePathForSave);
		double CompareFingerprints(byte[] firstPath, byte[] secondPath);

	}
}
