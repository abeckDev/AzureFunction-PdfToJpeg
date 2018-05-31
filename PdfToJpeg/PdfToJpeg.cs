using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using PdfiumViewer;
using System.IO;
using System.Drawing.Imaging;

namespace PdfToJpeg
{
    public static class PdfToJpeg
    {

        private static HttpClient Client { get; } = new HttpClient();

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
            //Get PDF Document
            PdfDocument pdfDocument = await GetPdfFromUrl(pdfUrl);

            //Convert PDF to JPG
            Stream jpgStream = ConvertPdfToJpg(pdfDocument);

            //Return downloadable File
            
            return req.CreateResponse(HttpStatusCode.Accepted,jpgStream);

        }

        public static async Task<PdfDocument> GetPdfFromUrl(string pdfurl)
        {

            PdfDocument pdfDocument = PdfDocument.Load(await Client.GetStreamAsync(pdfurl));
            return pdfDocument;
        }

        public static Stream ConvertPdfToJpg(PdfDocument pdfDocument)
        {
            Stream jpgStream = new MemoryStream();
            var jpgImage = pdfDocument.Render(0, 300, 300, true);
            jpgImage.Save(jpgStream, ImageFormat.Jpeg);
            return jpgStream;
        }
    }
}
