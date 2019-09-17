using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace DeviceDiagnostics.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        // GET api/values
        [HttpGet]
        public ActionResult Get()
        {
            var temp = System.IO.File.OpenRead(@"C:\Users\Lee\Documents\Test\From\New Text Document.txt");
            return File(temp, System.Net.Mime.MediaTypeNames.Application.Octet);
        }
    }
}
