using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

namespace DeviceDiagnostics.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        public static readonly Lazy<string> SHARED_DIRECTORY = new Lazy<string>(() => Environment.GetEnvironmentVariable("STORAGE_DIRECTORY"));

        // GET api/file?filename=
        [HttpGet]
        public FileResult Get([FromQuery] string filename, [FromServices] IFileProvider fileProvider)
        {
            Console.WriteLine($"Sending file {filename}");
            return File(fileProvider.GetFileInfo(filename).CreateReadStream(), System.Net.Mime.MediaTypeNames.Application.Octet);
        }

        [Route("list")]
        public ActionResult<string> ListAllFiles([FromServices] IFileProvider fileProvider, [FromQuery] string directory = "")
        {
            return string.Join('\n', fileProvider.GetDirectoryContents(directory).Select(f => $"{f.Name.PadRight(30)}{(f.IsDirectory ? "Directory" : $"{f.Length}").PadRight(16)}{f.LastModified}"));
        }
    }
}
