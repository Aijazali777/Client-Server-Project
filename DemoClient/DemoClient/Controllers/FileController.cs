using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace DemoClient.Controllers
{
    public class FileController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        public FileController(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            // Create an instance of HTTPClient, that will be used to make HTTP requests
            _httpClient = httpClientFactory.CreateClient();
            _cache = cache;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> ViewFile(string fileName)
        {
            string contentType = string.Empty;
            string fileExtension = Path.GetExtension(fileName);
            if (fileExtension == ".pdf")
                contentType = "application/pdf";
            else if(fileExtension == ".jpg")
                contentType = "image/jpg";

            var content = await FetchFile(fileName);
            var fileContentResult = new FileContentResult(content, contentType);
            Response.Headers.Add("Content-Disposition", "inline; filename=fileName");
            return fileContentResult;
        }

        public async Task<IActionResult> DownloadFile(string fileName)
        {
            var content = await FetchFile(fileName);
            return File(content, "application/pdf", fileName);
        }

        public async Task<byte[]> FetchFile(string fileName)
        {
            var cacheKey = fileName;
            var lastModifiedCacheKey = fileName+".lastModified";

            var lastModified = _cache.Get<DateTime?>(lastModifiedCacheKey);
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://localhost:44348/api/file/{fileName}");
            if (lastModified.HasValue)
                request.Headers.IfModifiedSince = lastModified.Value;

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
            {
                var cachedContent = _cache.Get<byte[]>(cacheKey);
                if (cachedContent != null)
                    return cachedContent;
            }
            else if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsByteArrayAsync();
                var responseLastModified = response.Content.Headers.LastModified?.UtcDateTime;
                if (responseLastModified.HasValue)
                {
                    _cache.Set(lastModifiedCacheKey, responseLastModified.Value);
                    _cache.Set(cacheKey, content);
                }

                return content;
            }
            return Array.Empty<byte>();
        }
    }
}