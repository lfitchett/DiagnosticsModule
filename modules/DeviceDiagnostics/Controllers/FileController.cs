﻿using System;
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
        public const string SHARED_DIRECTORY = @"C:\Users\Lee\Documents\Test\From";

        public FileController()
        {
            Directory.SetCurrentDirectory(SHARED_DIRECTORY);
        }

        // GET api/file?filename=
        [HttpGet]
        public FileResult Get([FromQuery] string filename, [FromServices] IFileProvider fileProvider)
        {
            Console.WriteLine($"Sending file {filename}");
            return File(fileProvider.GetFileInfo(filename).CreateReadStream(), System.Net.Mime.MediaTypeNames.Application.Octet);
        }

        [Route("list")]
        public ActionResult<string> ListAllFiles([FromQuery] string directory = ".")
        {
            return string.Join('\n', Directory.GetFiles(directory));
        }

        [Route("test")]
        public ActionResult<string> Test()
        {
            Console.WriteLine(Directory.GetCurrentDirectory());
            return Directory.GetCurrentDirectory();
        }
    }
}