using Amazon;
using Amazon.S3;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sample.Health.Checks.CustomHealthChecks;

namespace Sample.Health.Checks
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            //Configure ApplicationInsight Telemetry
            services.AddApplicationInsightsTelemetry(Configuration["ApplicationInsights:InstrumentationKey"]);
            ConfigureHealthCheck(services);
        }

        private void ConfigureHealthCheck(IServiceCollection services)
        {
            services.AddHealthChecks()
                //Configure SQL Server connectivity Health Checking
                .AddSqlServer(Configuration["SQLServerConnectionString"], name: "sql",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] {"db", "sql", "sqlserver"})
                //Configure MongoDb connectivity Health Checking
                .AddMongoDb(Configuration["MongodbConnectionString"], "MongoDB", HealthStatus.Unhealthy,
                    new[] {"db", "mongo", "mongodb"})
                //Configure Redis connectivity Health Checking
                .AddRedis(Configuration["RedisConnectionString"], "Redis", HealthStatus.Unhealthy)
                //Configure RabbitMQ connectivity Health Checking
                .AddRabbitMQ(Configuration["RabbitMqConnectionString"], "RabbitMQ", HealthStatus.Unhealthy,
                    new[] {"Rabbit", "RabbitMQ"})
                //Dependent service api health checking
                .AddUrlGroup(o =>
                {
                    o.AddUri(new Uri("http://DependentService/api/Health"), s =>
                    {
                        s.UseGet();
                        s.AddCustomHeader("Authorization", Configuration["Authorization"]);
                    });
                }, "DependentService", HealthStatus.Unhealthy, new[] {"Dependency", "Api"})
                .AddS3(s3 =>
                {
                    s3.AccessKey = Configuration["Aws:S3:AccessKey"];
                    s3.BucketName = Configuration["Aws:S3:BucketName"];
                    s3.SecretKey = Configuration["Aws:S3:SecretKey"];
                    s3.S3Config = new AmazonS3Config
                    {
                        RegionEndpoint = RegionEndpoint.GetBySystemName(Configuration["Aws:S3:Region"])
                    };
                })
                .AddApplicationInsightsPublisher(Configuration["ApplicationInsights:InstrumentationKey"])
                //.AddIdentityServer(new Uri(""), "Identity", HealthStatus.Unhealthy, new[] { "Identity" })
                //.AddDiskStorageHealthCheck(d => { d.AddDrive("", 1000); })
                //.AddKubernetes(k =>
                //{
                //    k.WithConfiguration(new KubernetesClientConfiguration
                //    {
                //        AccessToken = "",
                //        ClientCertificateData = "",
                //        ClientCertificateFilePath = "",
                //        ClientCertificateKeyData = "",
                //        ClientKeyFilePath = "",
                //        Host = "",
                //        Namespace = "",
                //        Password = "",
                //        SkipTlsVerify = true,
                //        SslCaCert = new X509Certificate2(),
                //        UserAgent = "",
                //        Username = ""
                //    });
                //})
                .AddPingHealthCheck(p => { p.AddHost("google.com", 500); })
                .AddCheck<HasFilesHealthCheck>("Api");
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            app.UseMvc();
            app.UseMvcWithDefaultRoute();
            app.UseHealthChecks("/Health", new HealthCheckOptions
            {
                ResponseWriter = CustomHealthCheckResponse
            });
            app.UseHealthChecksUI();

        }

        private static Task CustomHealthCheckResponse(HttpContext httpContext, HealthReport result)
        {
            httpContext.Response.ContentType = "application / json";
            //var json = new JObject(
            //new JProperty("status", result.Status.ToString()),
            //new JProperty("results", new JObject(result.Entries.Select(pair =>
            //    new JProperty(pair.Key, new JObject(
            //        new JProperty("status", pair.Value.Status.ToString()),
            //new JProperty("description", pair.Value.Description),
            //new JProperty("data", new JObject(pair.Value.Data.Select(
            //    p => new JProperty(p.Key, p.Value))))))))));
            //var text = json.ToString(Formatting.Indented);
            var text = JsonConvert.SerializeObject(result, Formatting.Indented, new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = new List<JsonConverter>
                {
                    new StringEnumConverter()
                },
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
            });
            return httpContext.Response.WriteAsync(text);
        }
    }
}