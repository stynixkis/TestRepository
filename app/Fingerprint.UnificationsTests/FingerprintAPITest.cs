using AudioFingerprinting;
using Fingerprint.Unifications;
using Fingerprint.Unifications.Controllers;
using Fingerprint.Unifications.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

public class AudioFilesControllerTests
{
    private readonly Mock<FingerprintDatabaseContext> _mockContext;
    private readonly AudioFilesController _controller;

    public AudioFilesControllerTests()
    {
        // ������������� mock ��������� ���� ������ � ����������� ��� ������������
        _mockContext = new Mock<FingerprintDatabaseContext>();
        _controller = new AudioFilesController(_mockContext.Object);
    }

    /// <summary>
    /// ��������� ����� GetAudioFiles �� ������� ������ �����������
    /// ��������� ���:
    /// 1. ������������ ActionResult � ���������� AudioFile
    /// 2. ��������� �������� ��������� ���������� ���������
    /// 3. ������ � ��������� ������������� �������� ������
    /// </summary>
    [Fact]
    public async Task GetAudioFiles_ReturnsListOfAudioFiles()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        var options = new DbContextOptionsBuilder<FingerprintDatabaseContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .UseInternalServiceProvider(serviceProvider)
            .Options;

        using (var context = new FingerprintDatabaseContext(options))
        {
            context.AudioFiles.Add(new AudioFile
            {
                IdAudio = 1,
                TitleAudio = "Test Audio",
                FftPrint = new byte[0],
                MfccPrint = new byte[0]
            });
            await context.SaveChangesAsync();
        }

        using (var context = new FingerprintDatabaseContext(options))
        {
            var controller = new AudioFilesController(context);

            // Act
            var result = await controller.GetAudioFiles();

            // Assert
            var actionResult = Assert.IsType<ActionResult<IEnumerable<AudioFile>>>(result);
            var returnValue = Assert.IsType<List<AudioFile>>(actionResult.Value);
            Assert.Single(returnValue);
            Assert.Equal("Test Audio", returnValue.First().TitleAudio);
        }
    }

    /// <summary>
    /// ��������� ����� GetAudioFile �� �������� ��������� ���������� �� ������������� ID
    /// ��������� ���:
    /// 1. ��� �������������/��������������� ID ������������ ���������� AudioFile
    /// 2. ������������ ������ �������� ��������� ��������
    /// </summary>
    [Theory]
    [InlineData(1)]  // ������������ ID
    [InlineData(999)] // �������������� ID
    public async Task GetAudioFile_ReturnsAudioFile_WhenIdExists(int id)
    {
        // Arrange
        var testAudioFile = new AudioFile
        {
            IdAudio = 1,
            TitleAudio = "Test Audio File",
            FftPrint = new byte[] { 0x01, 0x02, 0x03 },
            MfccPrint = new byte[] { 0x07, 0x04, 0x03 }
        };

        _mockContext.Setup(c => c.AudioFiles.FindAsync(id))
            .ReturnsAsync(testAudioFile);

        // Act
        var result = await _controller.GetAudioFile(id);

        // Assert

        if (result != null)
        {
            var actionResult = Assert.IsType<ActionResult<AudioFile>>(result);
            var returnValue = Assert.IsType<AudioFile>(actionResult.Value);

            Assert.Equal(testAudioFile.IdAudio, returnValue.IdAudio);
            Assert.Equal(testAudioFile.TitleAudio, returnValue.TitleAudio);
            Assert.Equal(testAudioFile.FftPrint, returnValue.FftPrint);
            Assert.Equal(testAudioFile.MfccPrint, returnValue.MfccPrint);
        }
        else
            Assert.IsType<NotFoundResult>(result.Result);
    }

    /// <summary>
    /// ��������� ����� PostAudioFileDB �� ��������� ������� ����
    /// ��������� ���:
    /// 1. ��� �������� ������� ���� ������������ BadRequestObjectResult
    /// </summary>
    [Fact]
    public async Task PostAudioFileDB_ReturnsBadRequest_WhenPathEmpty()
    {
        // Act
        var result = await _controller.PostAudioFileDB("");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// ��������� ����� PostAudioFilesComparisonMFCC �� ���������� ��������� ������
    /// ��������� ���:
    /// 1. ������������ ActionResult � ����������� ���������
    /// 2. ��������� �������� ��������� ����� � ��������� ��������
    /// </summary>

  /*  [Fact]
    public async Task CompareMFCC_ReturnsssComparisonResult()
    {
        // Объявляем переменные в начале метода
        string testFile1 = null;
        string testFile2 = null;

        try
        {
            // Arrange
            testFile1 = Path.Combine(Path.GetTempPath(), "test1.bin");
            testFile2 = Path.Combine(Path.GetTempPath(), "test2.bin");

            var testData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            await File.WriteAllBytesAsync(testFile1, testData);
            await File.WriteAllBytesAsync(testFile2, testData);

            // Проверяем что файлы создались
            Assert.True(File.Exists(testFile1), $"File {testFile1} does not exist");
            Assert.True(File.Exists(testFile2), $"File {testFile2} does not exist");

            var fingerprinter = new TestableMfccFingerprinter();

            var controller = new AudioFilesController(_mockContext.Object)
            {
                _fingerprinterMFCC = fingerprinter
            };

            // Act
            var result = await controller.PostAudioFilesComparisonMFCC(testFile1, testFile2);
            
            // Отладочная информация
            Console.WriteLine($"Result type: {result?.GetType().Name}");
            Console.WriteLine($"Result is null: {result == null}");

            // Assert
            Assert.NotNull(result);

            // Исправление ошибки CS8121 - правильная проверка типа
            if (result is ActionResult<string> actionResult)
            {
                Assert.NotNull(actionResult.Result);
                var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
                var resultString = Assert.IsType<string>(okResult.Value);
                Assert.Contains("MFCC", resultString);
            }
            else
            {
                Assert.Fail($"Unexpected result type: {result?.GetType().Name}");
            }
        }
        catch (Exception ex)
        {
            // Выводим детальную информацию об ошибке
            Console.WriteLine($"Test failed with exception: {ex}");
            throw;
        }
        finally
        {
            // Очистка временных файлов - теперь переменные доступны
            try
            {
                if (testFile1 != null && File.Exists(testFile1)) 
                    File.Delete(testFile1);
                if (testFile2 != null && File.Exists(testFile2)) 
                    File.Delete(testFile2);
            }
            catch {  }
        }
    } */

    /// <summary>
    /// ��������� ����� DeleteAudioFile �� �������� ��������
    /// ��������� ���:
    /// 1. ��� ������������� ID ������������ NoContentResult
    /// 2. ����� Remove ���������� ����� ���� ���
    /// </summary>
    [Fact]
    public async Task DeleteAudioFile_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        var testFile = new AudioFile { IdAudio = 1 };
        _mockContext.Setup(c => c.AudioFiles.FindAsync(1))
            .ReturnsAsync(testFile);
        _mockContext.Setup(c => c.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _controller.DeleteAudioFile(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockContext.Verify(c => c.AudioFiles.Remove(testFile), Times.Once());
    }

    // ��������������� ������

    /// <summary>
    /// ��������������� ����� ��� �������� mock DbSet
    /// </summary>
    public static class MockDbSetHelper
    {
        public static DbSet<T> CreateMockDbSet<T>(List<T> data) where T : class
        {
            var queryable = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

            mockSet.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

            return mockSet.Object;
        }
    }

    /// <summary>
    /// �������� ���������� IAsyncEnumerator ��� ��������� async ������
    /// </summary>
    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
        public T Current => _inner.Current;
        public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());
        public ValueTask DisposeAsync() => new ValueTask();
    }

    /// <summary>
    /// �������� ���������� MfccFingerprinter � ������������� ����������� ���������
    /// </summary>
    public class TestableMfccFingerprinter : MfccFingerprinter
    {
        public virtual double Compare(byte[] first, byte[] second)
        {
            return 0.85;
        }
    }
}