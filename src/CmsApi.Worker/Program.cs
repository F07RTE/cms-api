using CmsApi.Worker;

var builder = WorkerHost.Configure(Host.CreateApplicationBuilder(args));

var host = builder.Build();
host.Run();
