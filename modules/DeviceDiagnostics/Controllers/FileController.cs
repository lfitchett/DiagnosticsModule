using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace DeviceDiagnostics.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        // GET api/file?filename=
        [HttpGet]
        public ActionResult Get([FromQuery] string filename)
        {
            Console.WriteLine($"Sending file {filename}");
            var temp = System.IO.File.OpenRead(filename);
            return File(temp, System.Net.Mime.MediaTypeNames.Application.Octet);
        }
    }
}
