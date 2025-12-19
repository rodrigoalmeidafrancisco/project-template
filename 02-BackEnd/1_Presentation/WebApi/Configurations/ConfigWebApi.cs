using Domain.Commands._Base;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Logging;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApi.Configurations
{
    // Classe estática para configuração centralizada da Web API
    public static class ConfigWebApi
    {
        // Método de extensão para configurar serviços durante a construção da aplicação
        extension(WebApplicationBuilder builder)
        {
            // Método público que inicializa todas as configurações de serviços
            public void ConfigInitialize()
            {
                // Habilita a exibição de informações de identificação pessoal (PII) nos logs apenas em ambiente de desenvolvimento para facilitar debugging
                IdentityModelEventSource.ShowPII = builder.Environment.IsDevelopment();

                // Configura a integração com IIS (Internet Information Services) para hospedar a aplicação
                builder.WebHost.UseIISIntegration();

                // Remove provedores de log padrão e adiciona apenas o console para simplificar os logs
                builder.Logging.ClearProviders().AddConsole();

                // Adiciona os controllers MVC e configura opções de serialização JSON para respostas da API
                builder.Services.AddControllers().AddJsonOptions(options =>
                {
                    // Define que as propriedades JSON serão retornadas em camelCase (ex: "firstName" ao invés de "FirstName") seguindo convenção JavaScript
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                    // Ignora referências circulares durante a serialização para evitar loops infinitos quando objetos se referenciam mutuamente
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

                    // Não inclui propriedades com valor null no JSON de resposta para reduzir o tamanho do payload
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

                    // Formata o JSON com indentação apenas em desenvolvimento para facilitar a leitura durante debugging
                    options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();

                    // Serializa enums como strings ao invés de números para melhor legibilidade (ex: "Active" ao invés de 1)
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

                // Adiciona suporte para exploração de endpoints da API (usado pelo Swagger/OpenAPI)
                builder.Services.AddEndpointsApiExplorer();

                // Configura política de CORS (Cross-Origin Resource Sharing) para controlar quais domínios podem acessar a API
                builder.Services.AddCors(x => x.AddPolicy("AllowAll", y =>
                {
                    // Em desenvolvimento, permite qualquer origem, método e header para facilitar testes
                    if (builder.Environment.IsDevelopment())
                    {
                        y.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                    }
                    else
                    {
                        // Em produção, busca origens permitidas da configuração ou usa um domínio padrão
                        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["https://yourdomain.com"];
                        // Aplica restrições específicas de origem, permite qualquer método/header e habilita credenciais
                        y.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
                    }
                }));

                // Suprime o filtro automático de validação de ModelState para permitir tratamento customizado
                builder.Services.Configure<ApiBehaviorOptions>(options =>
                {
                    options.SuppressModelStateInvalidFilter = true;
                });

                // Configura compressão de respostas HTTP para reduzir o tamanho dos dados transmitidos
                builder.Services.AddResponseCompression(options =>
                {
                    // Adiciona provedor de compressão Gzip (formato mais compatível)
                    options.Providers.Add<GzipCompressionProvider>();
                    // Adiciona provedor de compressão Brotli (formato mais eficiente, mas menos compatível)
                    options.Providers.Add<BrotliCompressionProvider>();
                    // Define tipos MIME que serão comprimidos, incluindo JSON
                    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json"]);
                    // Habilita compressão mesmo para conexões HTTPS
                    options.EnableForHttps = true;
                });

                // Configura nível de compressão Gzip como "Fastest" para priorizar velocidade sobre tamanho
                builder.Services.Configure<GzipCompressionProviderOptions>(options =>
                {
                    options.Level = CompressionLevel.Fastest;
                });

                // Configura nível de compressão Brotli como "Fastest" para priorizar velocidade sobre tamanho
                builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
                {
                    options.Level = CompressionLevel.Fastest;
                });

                // Adiciona cache distribuído em memória para armazenar dados temporários
                builder.Services.AddDistributedMemoryCache();

                // Adiciona health checks para monitoramento da saúde da aplicação
                builder.Services.AddHealthChecks()
                    // Adiciona verificação básica que sempre retorna status saudável
                    .AddCheck("self", () => HealthCheckResult.Healthy("API está operacional"));

                builder.Services.AddApiVersioning(options =>
                {
                    // Define versão padrão da API como 1.0
                    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
                    // Assume versão padrão quando não especificada na requisição
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    // Reporta versões suportadas nos headers de resposta
                    options.ReportApiVersions = true;
                });

                //Configurar Rate Limiting
                builder.AddConfigRateLimiting();
            }
        }

        // Método de extensão para configurar o pipeline de middleware da aplicação
        extension(WebApplication app)
        {
            // Método público que inicializa todas as configurações do pipeline
            public void ConfigInitialize()
            {
                // Habilita servir arquivos padrão (index.html, default.html, etc.) automaticamente
                app.UseDefaultFiles();
                // Habilita servir arquivos estáticos (CSS, JS, imagens, etc.) da pasta wwwroot
                app.UseStaticFiles();

                // Em desenvolvimento, exibe página detalhada de exceções para facilitar debugging
                if (app.Environment.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }
                else
                {
                    // Em produção, habilita HSTS (HTTP Strict Transport Security) para forçar HTTPS
                    app.UseHsts();
                }

                // Redireciona requisições HTTP para HTTPS automaticamente
                app.UseHttpsRedirection();
                // Habilita compressão de respostas configurada anteriormente
                app.UseResponseCompression();

                // Aplica rate limiting antes do roteamento para limitar requisições o mais cedo possível
                app.UseRateLimiter();

                // Habilita roteamento de requisições para endpoints apropriados
                app.UseRouting();

                // Aplica política CORS após o roteamento para controlar acesso cross-origin
                app.UseCors("AllowAll");

                //Usar Rate Limiting (ANTES de UseAuthentication)
                app.UseConfigRateLimiting();

                // Habilita middleware de autenticação para identificar usuários
                app.UseAuthentication();
                // Habilita middleware de autorização para validar permissões de acesso
                app.UseAuthorization();

                // Mapeia endpoint de health check para monitoramento da API
                app.MapHealthChecks("/health");

                // Mapeia controllers e seus endpoints automaticamente
                app.MapControllers();

                // Define fallback para rotas não encontradas (404)
                app.MapFallback(async context =>
                {
                    // Define status HTTP 404 (Not Found)
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    // Define tipo de conteúdo como JSON
                    context.Response.ContentType = "application/json";
                    // Retorna mensagem de erro indicando que a rota não existe
                    await context.Response.WriteAsJsonAsync(new CommandResult<string>()
                    {
                        Message = $"Rota não encontrada: O caminho '{context.Request.Path}' não corresponde a nenhum endpoint válido."
                    });
                });

                // Configura tratamento global de exceções não capturadas
                app.UseExceptionHandler(c => c.Run(async context =>
                {
                    // Obtém informações sobre a exceção não tratada
                    var exceptionFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
                    // Extrai o objeto de exceção
                    var exception = exceptionFeature?.Error;

                    // Se existe uma exceção, processa e retorna resposta de erro
                    if (exception != null)
                    {
                        // Obtém logger do container de dependências
                        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                        // Registra erro no log com contexto da requisição
                        logger.LogError(exception, "Erro não tratado na requisição {Path}", context.Request.Path);

                        // Define status HTTP 500 (Internal Server Error)
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        // Define tipo de conteúdo como JSON
                        context.Response.ContentType = "application/json";

                        // Cria resposta de erro genérica
                        var errorResponse = new CommandResult<string>()
                        {
                            Message = "Ocorreu um erro interno no processamento da requisição."
                        };

                        // Em desenvolvimento, adiciona detalhes da exceção para facilitar debugging
                        if (app.Environment.IsDevelopment())
                        {
                            errorResponse.Message += $" Detalhes: {exception.Message}";
                        }

                        // Retorna resposta de erro formatada como JSON
                        await context.Response.WriteAsJsonAsync(errorResponse);
                    }
                }));
            }
        }
    }
}
