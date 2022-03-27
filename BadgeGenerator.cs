using System.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;


namespace BadgeGenerator
{
    public class GetBadge
    {
        private readonly ILogger<GetBadge> _logger;
        private string _personalaccesstoken = "";


        public GetBadge(ILogger<GetBadge> log)
        {
            _logger = log;
            _personalaccesstoken = Environment.GetEnvironmentVariable("PAT");
        }

        [FunctionName("GetBadge")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiParameter(name: "organization", In = ParameterLocation.Path)]
        [OpenApiParameter(name: "project", In = ParameterLocation.Path)]
        [OpenApiParameter(name: "buildId", In = ParameterLocation.Path)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{organization}/{project}/{buildId}")] HttpRequest req, string organization, string project, string buildId)
        {
            string codeCoverageEndpoint = $"https://dev.azure.com/{organization}/{project}/_apis/test/codecoverage?buildId={buildId}&api-version=6.0-preview.1";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/octet-stream"));

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                            System.Text.ASCIIEncoding.ASCII.GetBytes(
                                string.Format("{0}:{1}", "", _personalaccesstoken))));

                    using (HttpResponseMessage response = await client.GetAsync(codeCoverageEndpoint))
                    {
                        response.EnsureSuccessStatusCode();
                        Root responseBody = await response.Content.ReadAsAsync<Root>();

                        CoverageStat x = responseBody.coverageData.First().coverageStats.First(x => x.label == "Lines");
                        double covered = ((double)x.covered / (double)x.total) * 100;


                        var rounded = Math.Round(covered);
                        var color = "red";

                        if (rounded > 30)
                            color = "yellow";
                        if (rounded > 70)
                            color = "green";
                        req.HttpContext.Response.Headers.Add("Content-Type", "image/svg+xml; charset=utf-8; api-version=7.1-preview.1");

                        using (WebClient client2 = new WebClient())
                        {
                            var scgContent = client2.DownloadString($"https://img.shields.io/badge/Coverage-{rounded}%25-{color}");
                            return new FileContentResult(System.Text.Encoding.UTF8.GetBytes(scgContent), "image/svg+xml; charset=utf-8");
                        }


                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Unable to create badge: {ex.Message}");
            }

            return new RedirectResult($"https://img.shields.io/badge/Coverage-NA-red");
        }
    }
}

