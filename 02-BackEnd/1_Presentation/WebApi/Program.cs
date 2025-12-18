using Shared.Settings;
using WebApi.Configurations;

#region Configurações WebApplicationBuilder

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
//Obtendo as configurações da API "appsettings"
SettingApp.Start(builder.Configuration, builder.Environment.WebRootPath);
//Configurações iniciais da API
builder.ConfigInitialize();
//Configurações do Swagger
builder.AddConfigSwagger();


#endregion Configurações WebApplicationBuilder

#region Configurações WebApplication

WebApplication app = builder.Build();
//Configurações iniciais da API
app.ConfigInitialize();
//Configurações do Swagger
app.AddConfigSwagger();

#endregion Configurações WebApplication

//Iniciando a aplicação por padrão ASYNC
await app.RunAsync();
