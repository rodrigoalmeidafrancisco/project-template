using InversionOfControl;
using Shared.Settings;
using WebApi.Configurations;

#region Configurações WebApplicationBuilder

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

//Obtendo as configurações da API "appsettings"
SettingApp.Start(builder.Configuration, builder.Environment.WebRootPath);
//Injetando as dependências
Dependencies.Start(builder.Services);

//Configurações iniciais da API
builder.ConfigInitialize();
//Configurações do Swagger
builder.AddConfigSwagger();
//Configurações de Autenticação JWT
builder.AddAuthentication();
//Configurações do Application Insights
builder.AddConfigApplicationInsights();

#endregion Configurações WebApplicationBuilder

#region Configurações WebApplication

WebApplication app = builder.Build();
//Configurações iniciais da API
app.ConfigInitialize();
//Configurações do Swagger
app.AddConfigSwagger();
//Configurações do Application Insights
app.AddConfigApplicationInsights();

#endregion Configurações WebApplication

//Iniciando a aplicação por padrão ASYNC
await app.RunAsync();
