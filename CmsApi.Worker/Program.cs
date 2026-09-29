using CmsApi.Core;
using CmsApi.Data;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCmsCore();
builder.Services.AddCmsWriteData(builder.Configuration);

var host = builder.Build();
host.Run();
