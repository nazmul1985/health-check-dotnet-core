# Implementing Health Checks in ASP.NET Core
## What went wrong after implemting micro-service based system
Micro-service architecture is a latest trend and clearly a good thing to build large-scale web application. We have built a lovely distributed system based on micro-service architecture which is far easier to scale and deploy. Our system included a lerge number of .net core api, RabbitMQ, Redis, MongoDB, MS SQL Server, Socket Cluster and we have depoyed the system using docker, kubernetes, istio. 

After devoping and deploying the system we felt really proud that we have accomplished something amazing. And now are getting reported some parts of the application is not working, it means that it has sick service into the system somewhere which is failing to do its job properly, and now the race is on to find out who’s healthy and who’s not. As an example, we found out that some of the service is not working properly because file store service was not healthy and unable to upload and download file peroperly. So, it is neccesary to identify from a service that it's dependent service is not healthy.

## Health Checks as a solution
Implementing proper health-checking system can be a good solution the problem. 
We’re going to implement some basic health checking logic, so you can see how easy it can be to expose this kind of functionality.
Configure/Intergation of health is preety easy. Please follow the steps bellow:

>install-package Microsoft.AspNetCore.Diagnostics.HealthChecks

Open your startup.cs file. In here, we will add the basic health check logic to get us started. Edit (or add) your ConfigureServices / Configure methods to add the following lines.
```sh
 public void ConfigureServices(IServiceCollection services)
 {
      services.AddHealthChecks();
 }
 public void Configure(IApplicationBuilder app, IHostingEnvironment env)
 {
    //other code
    app.UseHealthChecks("/health");
    //other code
 }
 ```
 Now if you go to http://[YourService]/health, it will give an response like 
 ```sh
 RESPONSE:
 200 OK
 Healthy
  ```
 This is so simple, but of course it does nothing we want.
 
 Let's assume our application has lots of dependencies like MongoDB, SQL Server, RabbitMQ, Redis, AWS S3, Other Api services and so on. We can tell our api healthy when find all of those are healthy.
 
 GitHub contributors, particularly the team at [Xabaril/AspNetCore.Diagnostics.HealthChecks] offer a very good list of health check nuget packages to helping out.
 
 ### SQL Server Health Check
  >Install-Package AspNetCore.HealthChecks.SqlServer
 
 Add following lines in ConfigureServices method in Startup.cs
 ```sh
 services.AddHealthChecks()
                 //Configure SQL Server connectivity Health Checking
                .AddSqlServer(Configuration["SQLServerConnectionString"], name: "sql",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "db", "sql", "sqlserver" })
  ```
 ### MongoDB Health Check
  >Install-Package AspNetCore.HealthChecks.MongoDb
 
 Add following lines in ConfigureServices method in Startup.cs
 ```sh
 services.AddHealthChecks()
                //Configure MongoDb connectivity Health Checking
                .AddMongoDb(Configuration["MongodbConnectionString"], "MongoDB", HealthStatus.Unhealthy,
                    new[] { "db", "mongo", "mongodb" })
  ```
  
  ### Redis Health Check
  >Install-Package AspNetCore.HealthChecks.Redis
 
 Add following lines in ConfigureServices method in Startup.cs
 ```sh
 services.AddHealthChecks()
                 //Configure Redis connectivity Health Checking
                .AddRedis(Configuration["RedisConnectionString"], "Redis", HealthStatus.Unhealthy)
  ```
  ### RabbitMQ Health Check
  >Install-Package AspNetCore.HealthChecks.RabbitMQ
 
 Add following lines in ConfigureServices method in Startup.cs
 ```sh
 services.AddHealthChecks()
                //Configure RabbitMQ connectivity Health Checking
                .AddRabbitMQ(Configuration["RabbitMqConnectionString"], "RabbitMQ", HealthStatus.Unhealthy, 
                new[] { "Rabbit", "RabbitMQ" })
  ```
  ### AWS S3 Health Check
  
  >Install-Package AspNetCore.HealthChecks.Aws.S3
 
 Add following lines in ConfigureServices method in Startup.cs
 ```sh
 services.AddHealthChecks()
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
  ```
  ### Dependent other service Health Check
  
  The service can have dependencies on a specific API end-end point of other service. To check the health of dependent api you need to install the following nuget package.
  
  >Install-Package AspNetCore.HealthChecks.Uris
 
 Add following lines in ConfigureServices method in Startup.cs
 ```sh
 services.AddHealthChecks()
                 //Dependent service api health checking
                .AddUrlGroup(o =>
                {
                    o.AddUri(new Uri("http://DependentService/api/Health"), s =>
                    {
                        s.UseGet();
                        s.AddCustomHeader("Authorization", Configuration["Authorization"]);
                    });
                }, "DependentService", HealthStatus.Unhealthy, new[] { "Dependency", "Api" })
  ```
 ### Health Check logs in Microsoft Azure Application Insight
  
We can also push the health check response or logs into Azure Application Insights. For, that you have configure Application Insight Telemetry first. Then add AddApplicationInsightsPublisher to health checks. 

> Install-Package Microsoft.ApplicationInsights.AspNetCore

> Install-Package AspNetCore.HealthChecks.Publisher.ApplicationInsights
 
 Add following lines in ConfigureServices method in Startup.cs
 ```sh
 
 //Configure ApplicationInsight Telemetry
 services.AddApplicationInsightsTelemetry(Configuration["ApplicationInsights:InstrumentationKey"]);
 
 services.AddHealthChecks()
          .AddApplicationInsightsPublisher(Configuration["ApplicationInsights:InstrumentationKey"])
  ```
  
  ### Customized Health Check
  
  Now, we might want to write some custom health check logic in our service. Let assume we might want to check that if database contains specific records or not. Or we might want to check if the service contains specific files and specific records in it.
  
  For this we can write our custom health check service:
   ```sh
  public class HasFilesHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = new CancellationToken())
        {
            //TODO: Implement your own healthcheck logic here
            var isHealthy = true;
            if (isHealthy)
                return Task.FromResult(HealthCheckResult.Healthy("I am one healthy as I have the required files with me."));

            return Task.FromResult(HealthCheckResult.Unhealthy("I am the sad, unhealthy microservice API. Because, I don't have the files I required."));
        }
    }
  ```
  
 Then Add following lines in ConfigureServices method in Startup.cs
 ```sh
   services.AddHealthChecks()
          .AddCheck<HasFilesHealthCheck>("Api");
  ```
  
 ## Customized Health Check Response
 We can customized the response of health check as we want. Let say we want our health check response in Json format. For that we have to add function for response formatting. Like I added the following methid in Startup.cs file. You can put it in a seperate clss file if you want.
 
  ```sh
  private static Task CustomHealthCheckResponse(HttpContext httpContext, HealthReport result)
        {
            httpContext.Response.ContentType = "application / json";            
            var text = JsonConvert.SerializeObject(result, Formatting.Indented, new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = new List<JsonConverter>
                {
                    new StringEnumConverter()
                }                
            });
            return httpContext.Response.WriteAsync(text);
        }
  ```
  
  To configure the custom response formatter you have to add the following code in Configure method.
   ```sh
  app.UseHealthChecks("/Health", new HealthCheckOptions
            {
                ResponseWriter = CustomHealthCheckResponse
            });
   ```
 ## Conclusion
 There are lots of other things to consider in health check. ASP.NET Core has really added first-class support for health checks, and made it very easy to build and customise them at the same time. You can check out this [Microsoft-Documentation] for digg down more details.
 
 Docker offers a built-in HEALTHCHECK directive that can be used to check the status of an app that uses the basic health check configuration:
 ```sh
  HEALTHCHECK CMD curl --fail http://localhost:5000/health || exit
 ```
 
 Hope this helps.
 
 [Xabaril/AspNetCore.Diagnostics.HealthChecks]: <https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks>
 [Microsoft-Documentation]: <https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-2.2>

 






