using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;

namespace Messenger;

public class FileWorker
{
    private readonly IMinioClient _minioClient;
    private const string BucketName = "messenger";

    public FileWorker(IMinioClient minioClient)
    {
        _minioClient = minioClient;
    }

    // Загрузка файла
    public async Task<string> UploadFileAsync(IFormFile file, string objectName)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Файл пустой.");

        using var stream = file.OpenReadStream();
        await _minioClient.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(BucketName)
                .WithObject(objectName) // используем переданное имя
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(file.ContentType));

        return objectName;
    }

    // Скачивание файла
    public async Task<(Stream Stream, string ContentType, string Name)> DownloadFileAsync(string objectName)
    {
        var memory = new MemoryStream();

        // Загружаем файл в память
        await _minioClient.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(BucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream =>
                {
                    stream.CopyTo(memory);
                }));

        memory.Position = 0;

        // Получаем метаданные
        ObjectStat stat = await _minioClient.StatObjectAsync(
            new StatObjectArgs()
                .WithBucket(BucketName)
                .WithObject(objectName));

        return (
            memory,
            stat.ContentType ?? "application/octet-stream",
            objectName
        );
    }

    // Удаление файла

    public async Task DeleteFileAsync(string objectName)
    {
        await _minioClient.RemoveObjectAsync(
            new RemoveObjectArgs()
                .WithBucket(BucketName)
                .WithObject(objectName));
    }
}