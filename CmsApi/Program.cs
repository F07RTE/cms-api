using CmsApi.Auth;
using CmsApi.Core;
using CmsApi.Data;
using CmsApi.Dtos;
using CmsApi.Errors;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCmsCore();
builder.Services.AddCmsData(builder.Configuration);
builder.Services.AddContentEntityProjections();
builder.Services.AddCmsAuth(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CmsExceptionHandler>();
builder.Services.AddControllers();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
