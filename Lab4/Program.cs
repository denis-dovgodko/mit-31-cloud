using Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

 builder.Services.
       AddAzureClients(b =>
        {
            b.AddBlobServiceClient(Environment.GetEnvironmentVariable("blobConn"));
            var endpoint = new Uri(Environment.
            GetEnvironmentVariable("textAnalyticsEndpoint"));
            var credential = new AzureKeyCredential(Environment.
            GetEnvironmentVariable("textAnalyticsKey"));
            b.AddTextAnalyticsClient(endpoint, credential);
        });

builder.Build().Run();
