using System.Linq;
using System;
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

namespace BadgeGenerator
{
    public class BadgeGenerator
    {
        private readonly ILogger<BadgeGenerator> _logger;
        private string _personalaccesstoken = "";

        public BadgeGenerator(ILogger<BadgeGenerator> log)
        {
            _logger = log;
            _personalaccesstoken = Environment.GetEnvironmentVariable("PAT");
        }

        [FunctionName("GetBadge")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiParameter(name: "organization", In = ParameterLocation.Path)]
        [OpenApiParameter(name: "project", In = ParameterLocation.Path)]
        [OpenApiParameter(name: "buildId", In = ParameterLocation.Path)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "image/svg+xml", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{organization}/{project}/{buildId}")] HttpRequest req, string organization, string project, string buildId)
        {
            string codeCoverageEndpoint = $"https://dev.azure.com/{organization}/{project}/_apis/test/codecoverage?buildId={buildId}&api-version=6.0-preview.1";

            try
            {
                var codecoverage = await GetCodeCoverageAsync(codeCoverageEndpoint);

                CoverageStat x = codecoverage.coverageData.First().coverageStats.First(x => x.label == "Lines");
                double covered = ((double)x.covered / (double)x.total) * 100;
                var rounded = Math.Round(covered);
                string color = ResolveBadgeColor(rounded);

                req.HttpContext.Response.Headers.Add("Content-Type", "image/svg+xml; charset=utf-8; api-version=7.1-preview.1");
                var svgContent = await GetBadge($"https://img.shields.io/badge/Coverage-{rounded}%25-{color}");
                return new FileContentResult(svgContent, "image/svg+xml; charset=utf-8");

            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Unable to create badge: {ex.Message}");
            }

            return new RedirectResult($"https://img.shields.io/badge/Coverage-NA-red");
        }

        private static string ResolveBadgeColor(double rounded)
        {
            var color = "red";

            if (rounded > 30)
                color = "yellow";
            if (rounded > 70)
                color = "green";
            return color;
        }

        private async Task<byte[]> GetBadge(string codeCoverageEndpoint)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/octet-stream"));

                using (HttpResponseMessage response = await client.GetAsync(codeCoverageEndpoint))
                {
                    response.EnsureSuccessStatusCode();
                    var responseBody = await response.Content.ReadAsByteArrayAsync();

                    return responseBody;
                }
            }
        }


        private async Task<Root> GetCodeCoverageAsync(string codeCoverageEndpoint)
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

                    return responseBody;
                }
            }
        }
    }
}

