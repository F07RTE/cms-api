using CmsApi.Core;
using CmsApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCmsCore();
builder.Services.AddCmsData(builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
