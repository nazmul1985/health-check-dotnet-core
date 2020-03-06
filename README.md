# Implementing Health Checks in ASP.NET Core
# What went wrong after implemting micro-service based system
Micro-service architecture is a latest trend and clearly a good thing to build large-scale web application. We have built a lovely distributed system based on micro-service architecture which is far easier to scale and deploy. Our system included a lerge number of .net core api, RabbitMQ, Redis, MongoDB, MS SQL Server, Socket Cluster and we have depoyed the system using docker, kubernetes, istio. 

After devoping and deploying the system we felt really proud that we have accomplished something amazing. And now are getting reported some parts of the application is not working, it means that it has sick service into the system somewhere which is failing to do its job properly, and now the race is on to find out who’s healthy and who’s not. As an example, we found out that some of the service is not working properly because file store service was not healthy and unable to upload and download file peroperly. So, it is neccesary to identify from a service that it's dependent service is not healthy.

# Health Checks as a solution
Implementing proper health-checking system can be a good solution the problem. 
We’re going to implement some basic health checking logic, so you can see how easy it can be to expose this kind of functionality.

>install-package Microsoft.AspNetCore.Diagnostics.HealthChecks







