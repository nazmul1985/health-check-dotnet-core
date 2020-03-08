using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Sample.Health.Checks.CustomHealthChecks
{
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
}