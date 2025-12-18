using Domain.Commands._Base;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApi.Configurations
{
    public static class ConfigWebApi
    {
        extension(WebApplicationBuilder builder)
        {
            public void ConfigInitialize()
            {
                //habilitar a visualização de logs de PII
                IdentityModelEventSource.ShowPII = true;

                //Configura para utilizar o IIS, quando publicar.
                builder.WebHost.UseIISIntegration();

                //Configura para exibir os logs no console ao debugar a aplicação.
                builder.Logging.ClearProviders().AddConsole();

                //Configura os parâmetros do System.Text.Json para o Retorno da API   
                builder.Services.AddControllers().AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
                    options.JsonSerializerOptions.WriteIndented = true;
                });

                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddCors(x => x.AddPolicy("AllowAll", y => { y.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); }));

                //Permite fazer a validação do ComponentModel.Annotations
                builder.Services.Configure<ApiBehaviorOptions>(options =>
                {
                    options.SuppressModelStateInvalidFilter = true;
                });

                //Comprime o Json no Retorno da API, diminuindo o seu tamanho
                builder.Services.AddResponseCompression(options =>
                {
                    options.Providers.Add<GzipCompressionProvider>();
                    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json"]);
                });

                //Configuração para que o IMemoryCache seja distribuido entre os servidores no balance.
                builder.Services.AddDistributedMemoryCache();
            }
        }

        extension(WebApplication app)
        {
            public void ConfigInitialize()
            {
                //Informo que irei utilizar arquivos estáticos (wwwroot)
                app.UseDefaultFiles();
                app.UseStaticFiles();

                if (app.Environment.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }
                else
                {
                    app.UseHsts();
                }

                //Força a API responder apenas em HTTPS
                app.UseHttpsRedirection();

                //Padrão de rotas do MVC
                app.UseRouting();
                app.MapControllers();

                //Poder realizar chamadas localhost em tempo de desenvolvimento
                app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
                app.UseAuthentication(); // Autenticação
                app.UseAuthorization(); // Roles

                //Configura o Response para o 404 - Not Found
                app.MapFallback(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync($"Rota não encontrada: O caminho '{context.Request.Path}' não corresponde a nenhum endpoint válido.");
                });

                //Configura o Response para o 500 - Internal Server Error
                app.UseExceptionHandler(c => c.Run(async context =>
                {
                    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>()?.Error;
                    if (exception != null)
                    {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(new CommandResult<string>() { Message = "Ocorreu um erro interno no processamento da requisição." });
                    }
                }));

            }
        }

    }
}
