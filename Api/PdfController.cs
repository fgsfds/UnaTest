using System.Text.Json;
using Core;
using Core.DTOs;
using Core.Messages;
using Database;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

namespace Api;

[ApiController]
[Route("api/pdf")]
public class PdfController : ControllerBase
{
    /// <summary>
    /// Возвращает список PDF.
    /// </summary>
    [HttpGet("list")]
    public async Task<Ok<IEnumerable<PdfInfo>>> GetAsync([FromServices] IDbContextFactory<DatabaseContext> dbFactory)
    {
        using var db = await dbFactory.CreateDbContextAsync();
        return TypedResults.Ok(db.Pdfs.AsNoTracking().Select(x =>
            new PdfInfo()
            {
                Name = x.FileName
            }
            ).AsEnumerable());
    }

    /// <summary>
    /// Возвращает содержимое PDF.
    /// </summary>
    /// <param name="fileName">Имя PDF файла.</param>
    [HttpGet]
    public async Task<Results<Ok<PdfContent>, NotFound>> GetContentAsync(
        [FromQuery] string fileName,
        [FromServices] IDbContextFactory<DatabaseContext> dbFactory
        )
    {
        using var db = await dbFactory.CreateDbContextAsync();
        var pdf = await db.Pdfs.AsNoTracking().FirstOrDefaultAsync(x => x.FileName.Equals(fileName));

        if (pdf is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(
            new PdfContent()
            {
                Name = pdf.FileName,
                Content = pdf.Content
            });
    }

    /// <summary>
    /// Принимает загруженный PDF. 
    /// </summary>
    /// <param name="file">Файл.</param>
    [HttpPost]
    public async Task<Results<Ok, InternalServerError>> UploadAsync(
        IFormFile file,
        [FromServices] RabbitConnectionManager connectionManager,
        [FromServices] IHttpClientFactory httpFactory
        )
    {
        using var httpClient = httpFactory.CreateClient();

        using var fileStream = file.OpenReadStream();
        var content = new StreamContent(fileStream);

        Uri uploadUrl = new(Consts.FileStorageUrl + file.FileName);

        // загружаем полученный файл в хранилище
        var response = await httpClient.PutAsync(uploadUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            return TypedResults.InternalServerError();
        }

        // брокеру отправляем только ссылку на загруженный файл
        UploadedFileMessage json = new()
        {
            FileName = file.FileName,
            DownloadUrl = uploadUrl
        };
        var body = JsonSerializer.SerializeToUtf8Bytes(json);

        await using var channel = await connectionManager.GetChannelAsync();

        await channel.BasicPublishAsync(
            Consts.ExchangeTopicAmq,
            Consts.RoutingKeyPdfUploaded,
            true,
            new BasicProperties() { Persistent = true },
            body
            );

        return TypedResults.Ok();
    }
}
