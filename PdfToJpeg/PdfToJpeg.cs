using ImageMagick;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace PdfToJpeg
{
    public static class PdfToJpeg
    {
        private static HttpClient Client { get; } = new HttpClient();
        public static string AppFolder
        {
            get
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
        }

        [FunctionName("PdfToJpg")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            if (req.Method != HttpMethod.Get)
            {
                //Send an Error if wrong HTTP method is used
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, "This function currently only supports GET requests");
            }

            // parse query parameter
            string pdfUrl = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "pdfurl", true) == 0)
                .Value;

            if (pdfUrl == null)
            {
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, "You need to provide a url via pdfurl option");
            }

            log.Info("Got PDF Option. Downloading: " + pdfUrl);
            //Get PDF Document
            WebClient webClient = new WebClient();
            Stream pdfStream = new MemoryStream(webClient.DownloadData(pdfUrl));

            //SetUp ImageMagick
            string ghostPath = $@"{AppFolder}\".Replace('\\', '/');
            var debug = Directory.GetFiles(ghostPath);
            log.Info("Setting up ImageMagick");

            foreach (var item in debug)
            {
                log.Info("Found file: " + item);
            }
            
            MagickNET.SetGhostscriptDirectory(ghostPath);
            MagickReadSettings settings = new MagickReadSettings();
            settings.Density = new Density(300, 300);

            //Convert PDF to JPG
            log.Info("Start converting");
            MemoryStream jpegStream = new MemoryStream();
            using (MagickImageCollection images = new MagickImageCollection())
            {
                images.Read(pdfStream,settings);
                foreach (MagickImage image in images)
                {
                    image.Format = MagickFormat.Jpeg;
                    image.Write(jpegStream);
                }
            }
            log.Info("Finished converting");

            //Create HTTP response
            log.Info("Creating response");
            var result = req.CreateResponse(HttpStatusCode.OK);
            result.Content = new ByteArrayContent(jpegStream.ToArray());
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            return result;

        }
    }
}
