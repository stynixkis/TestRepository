using Fingerprint.Unifications.Models;
using Microsoft.EntityFrameworkCore;

namespace Fingerprint.Unifications;

public partial class FingerprintDatabaseContext : DbContext
{
	public FingerprintDatabaseContext()
	{
	}

	public FingerprintDatabaseContext(DbContextOptions<FingerprintDatabaseContext> options)
		: base(options)
	{
	}

	public virtual DbSet<AudioFile> AudioFiles { get; set; }

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
		=> optionsBuilder.UseSqlite("Data Source=C:\\Users\\aagor\\source\\repos\\Fingerprint.Unifications\\FingerprintDatabase.db");

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<AudioFile>(entity =>
		{
			entity.HasKey(e => e.IdAudio);

			entity.ToTable("AudioFile");

			entity.HasIndex(e => e.IdAudio, "IX_AudioFile_ID_audio").IsUnique();

			entity.Property(e => e.IdAudio)
				.ValueGeneratedNever()
				.HasColumnName("ID_audio");
			entity.Property(e => e.FftPrint).HasColumnName("FFT_print");
			entity.Property(e => e.MfccPrint).HasColumnName("MFCC_print");
		});

		OnModelCreatingPartial(modelBuilder);
	}

	partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
