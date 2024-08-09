using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DemoServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        [HttpGet("{fileName}")]
        public IActionResult GetFile(string fileName)
        {
            string filePath = filePath = Path.Combine(Directory.GetCurrentDirectory(), "Files", fileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            DateTime fileLastModified = System.IO.File.GetLastWriteTimeUtc(filePath);
            var ifModifiedSince = Request.Headers["If-Modified-Since"];

            if (!string.IsNullOrEmpty(ifModifiedSince) && DateTime.TryParse(ifModifiedSince, out DateTime ifModifiedSinceDate) && fileLastModified <= ifModifiedSinceDate)
                return StatusCode(304); // Not Modified

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            Response.Headers.Add("Last-Modified", fileLastModified.ToString("R"));
            return File(fileBytes, "application/pdf", fileName);
        }
    }
}