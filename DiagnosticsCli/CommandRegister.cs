using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace DiagnosticsCli
{
    public static class CommandRegister
    {
        /// <summary>
        ///     Registers all commands
        ///         getfile -s <targetFileName> -d <destinationFileName> : sends file from module to cli device
        ///         ls : lista files in module shared folder
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="parser"></param>
        public static void RegisterCommands(HttpClient httpClient, CommandLineParser parser, CancellationToken ct)
        {
            /* Register list file command */
            parser.RegisterCommand("ls", async () =>
            {
                HttpResponseMessage response = await httpClient.GetAsync(@"http://localhost:80/api/file/list", ct);
                Console.WriteLine(await response.Content.ReadAsStringAsync());
            });

            /* Register get file */
            string source = null;
            Action<string> setSource = (string val) => source = val;

            string destination = null;
            Action<string> setDest = (string val) => destination = val;

            Dictionary<string, Action<string>> args = new Dictionary<string, Action<string>>
                        {
                            {"--source", setSource },
                            {"-s", setSource },
                            {"--destination", setDest },
                            {"-d", setDest }
                        };

            parser.RegisterCommand("getfile", async () =>
            {
                if (source == null)
                {
                    Console.WriteLine("Please use -s to set a source");
                    return;
                }
                if (destination == null)
                {
                    Console.WriteLine("Please use -d to set a destination");
                    return;
                }

                UriBuilder uri = new UriBuilder(@"http://localhost:80/api/file");
                uri.Query = $"filename=" + Uri.EscapeUriString(source);
                HttpResponseMessage response = await httpClient.GetAsync(uri.Uri, ct);

                Console.WriteLine(response);
                if (response.IsSuccessStatusCode)
                {
                    using (FileStream file = File.OpenWrite($@"C:\Users\Lee\Documents\Test\To\{destination}"))
                    {
                        await (await response.Content.ReadAsStreamAsync()).CopyToAsync(file);
                    }
                }
            }, args);
        }
    }
}
