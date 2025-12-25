using Audiofingerprint.Services;
using AudioFingerprinting;
using Fingerprint.Unifications.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fingerprint.Unifications.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AudioFilesController : ControllerBase
	{
		private readonly FingerprintDatabaseContext _context;
		public MfccFingerprinter _fingerprinterMFCC = new MfccFingerprinter();
		private FingerprintService _fingerprintService = new FingerprintService();
		public AudioFilesController(FingerprintDatabaseContext context)
		{
			_context = context;
		}

		// GET: api/AudioFiles
		[HttpGet]
		public async Task<ActionResult<IEnumerable<AudioFile>>> GetAudioFiles()
		{
			return await _context.AudioFiles.ToListAsync();
		}

		// GET: api/AudioFiles/5
		[HttpGet("{id}")]
		public async Task<ActionResult<AudioFile>> GetAudioFile(int id)
		{
			var audioFile = await _context.AudioFiles.FindAsync(id);

			if (audioFile == null)
			{
				return NotFound();
			}

			return audioFile;
		}

		// PUT: api/AudioFiles/5
		// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
		[HttpPut("{id}")]
		public async Task<IActionResult> PutAudioFile(int id, string title)
		{
			var entryItem = _context.AudioFiles.Where(p => p.IdAudio == id).FirstOrDefault();
			if (entryItem == null)
			{
				return BadRequest("There is no record with the entered id");
			}
			if (title == null || String.IsNullOrEmpty(title.Trim()))
			{
				return BadRequest("The audio title cannot be empty");
			}

			entryItem.TitleAudio = title;
			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!AudioFileExists(id))
				{
					return NotFound();
				}
				else
				{
					throw;
				}
			}

			return NoContent();
		}

		[HttpPost("Generation-with-storage-in-the-database")]
		public async Task<ActionResult<AudioFile>> PostAudioFileDB([FromForm] string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				return BadRequest("Path cannot be empty");

			if (!path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
				return BadRequest("Only .wav files are supported");

			if (!System.IO.File.Exists(path))
				return NotFound($"File not found: {path}");

			try
			{
				var audioFile = new AudioFile
				{
					TitleAudio = Path.GetFileNameWithoutExtension(path),
					IdAudio = _context.AudioFiles.Any()
						? _context.AudioFiles.Max(x => x.IdAudio) + 1
						: 1
				};

				var workingDir = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					"Downloads",
					"FingerprintResults");

				Directory.CreateDirectory(workingDir);

				audioFile.MfccPrint = _fingerprinterMFCC.GenerateFingerprint(path);
				var mfccFilePath = Path.Combine(workingDir, $"{audioFile.TitleAudio}_MFCC.bin");
				try
				{
					using (var writer = new BinaryWriter(System.IO.File.Open(mfccFilePath, FileMode.Create)))
					{
						foreach (uint hash in audioFile.MfccPrint)
							writer.Write(hash);
					}
				}
				catch (IOException ex)
				{
					return StatusCode(500, $"Failed to save MFCC: {ex.Message}");
				}

				var fftFilePath = Path.Combine(workingDir);
				string resultGenerateFFT = _fingerprintService.GenerateFingerprint(path, fftFilePath);

				try
				{
					audioFile.FftPrint = System.IO.File.ReadAllBytes(resultGenerateFFT);
				}
				catch (IOException ex)
				{
					return StatusCode(500, $"Failed to read FFT file: {ex.Message}");
				}

				_context.AudioFiles.Add(audioFile);
				await _context.SaveChangesAsync();

				return CreatedAtAction(nameof(GetAudioFile), new { id = audioFile.IdAudio }, audioFile);
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"Processing failed: {ex.Message}");
			}
		}
		[HttpPost("Generation-without-saving-in-the-database")]
		public async Task<ActionResult<AudioFile>> PostAudioFileNotDB([FromForm] string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				return BadRequest("Path cannot be empty");

			if (!path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
				return BadRequest("Only .wav files are supported");

			if (!System.IO.File.Exists(path))
				return NotFound($"File not found: {path}");

			try
			{
				var audioFile = new AudioFile
				{
					TitleAudio = Path.GetFileNameWithoutExtension(path),
					IdAudio = 0
				};

				var workingDir = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					"Downloads",
					"FingerprintResults");

				Directory.CreateDirectory(workingDir);

				audioFile.MfccPrint = _fingerprinterMFCC.GenerateFingerprint(path);
				var mfccFilePath = Path.Combine(workingDir, $"{audioFile.TitleAudio}_MFCC.bin");
				try
				{
					using (var writer = new BinaryWriter(System.IO.File.Open(mfccFilePath, FileMode.Create)))
					{
						foreach (uint hash in audioFile.MfccPrint)
							writer.Write(hash);
					}
				}
				catch (IOException ex)
				{
					return StatusCode(500, $"Failed to save MFCC: {ex.Message}");
				}

				var fftFilePath = Path.Combine(workingDir);
				string resultGenerateFFT = _fingerprintService.GenerateFingerprint(path, fftFilePath);

				try
				{
					audioFile.FftPrint = System.IO.File.ReadAllBytes(resultGenerateFFT);
				}
				catch (IOException ex)
				{
					return StatusCode(500, $"Failed to read FFT file: {ex.Message}");
				}

				return CreatedAtAction(nameof(GetAudioFile), new { id = audioFile.IdAudio }, audioFile);
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"Processing failed: {ex.Message}");
			}
		}

		[HttpPost("compare-mfcc")]
		public async Task<ActionResult<string>> PostAudioFilesComparisonMFCC(
			[FromForm] string pathFirst,
			[FromForm] string pathSecond)
		{
			if (string.IsNullOrWhiteSpace(pathFirst))
				return BadRequest("Path 1 cannot be empty");

			if (!pathFirst.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
				return BadRequest("Only .bin files 1 are supported");

			if (!System.IO.File.Exists(pathFirst))
				return NotFound($"File 1 not found: {pathFirst}");

			if (string.IsNullOrWhiteSpace(pathSecond))
				return BadRequest("Path 2 cannot be empty");

			if (!pathSecond.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
				return BadRequest("Only .bin files 2 are supported");

			if (!System.IO.File.Exists(pathSecond))
				return NotFound($"File 2 not found: {pathSecond}");
			try
			{
				byte[] fingerprintFirst;
				try
				{
					fingerprintFirst = System.IO.File.ReadAllBytes(pathFirst);
				}
				catch (IOException ex)
				{
					return StatusCode(500, $"Failed to read file 1: {ex.Message}");
				}
				byte[] fingerprintSecond;
				try
				{
					fingerprintSecond = System.IO.File.ReadAllBytes(pathSecond);
				}
				catch (IOException ex)
				{
					return StatusCode(500, $"Failed to read file 2: {ex.Message}");
				}

				double resultMFCC = _fingerprinterMFCC.Compare(fingerprintFirst, fingerprintSecond);

				string resultComparison = $"Процент схожести по методу MFCC: {Math.Round(resultMFCC, 2)}";
				return resultComparison;
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"Processing failed: {ex.Message}");
			}
		}

		[HttpPost("compare-fft")]
		public async Task<ActionResult<string>> PostAudioFilesComparisonFFT(
			[FromForm] string pathFirst,
			[FromForm] string pathSecond)
		{
			if (string.IsNullOrWhiteSpace(pathFirst))
				return BadRequest("Path 1 cannot be empty");

			if (!pathFirst.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
				return BadRequest("Only .bin files 1 are supported");

			if (!System.IO.File.Exists(pathFirst))
				return NotFound($"File 1 not found: {pathFirst}");

			if (string.IsNullOrWhiteSpace(pathSecond))
				return BadRequest("Path 2 cannot be empty");

			if (!pathSecond.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
				return BadRequest("Only .bin files 2 are supported");

			if (!System.IO.File.Exists(pathSecond))
				return NotFound($"File 2 not found: {pathSecond}");
			try
			{
				byte[] fingerprintFirst;
				try
				{
					fingerprintFirst = System.IO.File.ReadAllBytes(pathFirst);
				}
				catch (IOException ex)
				{
					return StatusCode(500, $"Failed to read file 1: {ex.Message}");
				}
				byte[] fingerprintSecond;
				try
				{
					fingerprintSecond = System.IO.File.ReadAllBytes(pathSecond);
				}
				catch (IOException ex)
				{
					return StatusCode(500, $"Failed to read file 2: {ex.Message}");
				}

				double resultFFT = _fingerprintService.CompareFingerprints(fingerprintFirst, fingerprintSecond);

				string resultComparison = $"Процент схожести по методу FFT: {Math.Round(resultFFT, 2)}";
				return resultComparison;
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"Processing failed: {ex.Message}");
			}
		}

		// DELETE: api/AudioFiles/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteAudioFile(int id)
		{
			var audioFile = await _context.AudioFiles.FindAsync(id);
			if (audioFile == null)
			{
				return NotFound();
			}

			_context.AudioFiles.Remove(audioFile);
			await _context.SaveChangesAsync();

			return NoContent();
		}

		private bool AudioFileExists(int id)
		{
			return _context.AudioFiles.Any(e => e.IdAudio == id);
		}
	}
}
