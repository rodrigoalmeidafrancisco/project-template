using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Shared.Settings;

namespace WebApi.Configurations
{
    public static class ConfigWebApiApplicationInsights
    {
        extension(WebApplicationBuilder builder)
        {
            public void AddConfigApplicationInsights()
            {
                //Configura a utilização do Application Insights
                builder.Services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions()
                {
                    ConnectionString = SettingApp.ConnectionStrings.Default,
                    // Desabilita coleta de dependências em desenvolvimento para reduzir ruído
                    EnableAdaptiveSampling = !builder.Environment.IsDevelopment(),
                    // Habilita coleta de heartbeat para monitoramento de disponibilidade
                    EnableHeartbeat = true,
                    // Habilita coleta detalhada de dependências (SQL, HTTP, etc)
                    EnableDependencyTrackingTelemetryModule = true,
                    // Habilita coleta de requisições HTTP
                    EnableRequestTrackingTelemetryModule = true,
                    // Habilita coleta de contadores de performance
                    EnablePerformanceCounterCollectionModule = true,
                    // Habilita coleta de eventos de aplicação
                    EnableEventCounterCollectionModule = true,
                });

                // Enriquecimento de telemetria
                builder.Services.AddSingleton<ITelemetryInitializer, TelemetryInitializer>();
            }
        }

        extension(WebApplication app)
        {
            public void AddConfigApplicationInsights()
            {
                var telemetry = app.Services.GetRequiredService<TelemetryClient>();

                //Marca início da aplicação
                app.Lifetime.ApplicationStarted.Register(() =>
                {
                    telemetry.TrackTrace($"Log da aplicação '{SettingApp.Aplication.Name}' iniciada", SeverityLevel.Information);
                });
            }
        }


        #region Métodos Privados

        // Inicializador para enriquecer toda telemetria com contexto global
        private sealed class TelemetryInitializer : ITelemetryInitializer
        {
            public void Initialize(ITelemetry telemetry)
            {
                if (telemetry?.Context == null) return;

                // Define o nome da role (nome exibido no Application Map)
                telemetry.Context.Cloud.RoleName = $"[{SettingApp.Aplication._Environment}] - {SettingApp.Aplication.Name} ({SettingApp.Aplication.Type})";
                telemetry.Context.Cloud.RoleInstance = Environment.MachineName;

                // Adiciona propriedades globais para todas as telemetrias
                telemetry.Context.GlobalProperties["Ambiente"] = SettingApp.Aplication._Environment;
                telemetry.Context.GlobalProperties["Aplicacao"] = SettingApp.Aplication.Name;
                telemetry.Context.GlobalProperties["TipoAplicacao"] = SettingApp.Aplication.Type;

                // Adiciona versão do .NET
                telemetry.Context.GlobalProperties["DotNetVersion"] = Environment.Version.ToString();

                // Enriquece telemetria de requisições
                if (telemetry is RequestTelemetry requestTelemetry)
                {
                    EnrichRequestTelemetry(requestTelemetry);
                }

                // Enriquece telemetria de dependências (SQL, HTTP, etc)
                if (telemetry is DependencyTelemetry dependencyTelemetry)
                {
                    EnrichDependencyTelemetry(dependencyTelemetry);
                }

                // Enriquece telemetria de exceções
                if (telemetry is ExceptionTelemetry exceptionTelemetry)
                {
                    EnrichExceptionTelemetry(exceptionTelemetry);
                }
            }
        }

        /// <summary>
        /// Enriquece telemetria de requisições HTTP.
        /// Permite customizar o comportamento de success/failure por status code.
        /// </summary>
        private static void EnrichRequestTelemetry(RequestTelemetry requestTelemetry)
        {
            // Exemplo: Marcar requisições 400 como sucesso se for validação de negócio esperada
            if (int.TryParse(requestTelemetry.ResponseCode, out int statusCode))
            {
                // Descomente se quiser que erros 400 (Bad Request) sejam considerados sucesso
                // if (statusCode == 400)
                // {
                //     requestTelemetry.Success = true;
                //     requestTelemetry.Properties["OverriddenSuccess"] = "true";
                // }

                // Adiciona categoria de status HTTP
                requestTelemetry.Properties["StatusCategory"] = statusCode switch
                {
                    >= 200 and < 300 => "Success",
                    >= 300 and < 400 => "Redirect",
                    >= 400 and < 500 => "ClientError",
                    >= 500 => "ServerError",
                    _ => "Unknown"
                };
            }
        }

        /// <summary>
        /// Enriquece telemetria de dependências externas (SQL, HTTP, Redis, etc).
        /// </summary>
        private static void EnrichDependencyTelemetry(DependencyTelemetry dependencyTelemetry)
        {
            // Adiciona informações úteis sobre dependências
            if (dependencyTelemetry.Type == "SQL")
            {
                dependencyTelemetry.Properties["DatabaseType"] = "SqlServer";
            }
        }

        /// <summary>
        /// Enriquece telemetria de exceções com informações adicionais de contexto.
        /// </summary>
        private static void EnrichExceptionTelemetry(ExceptionTelemetry exceptionTelemetry)
        {
            // Adiciona severity level baseado no tipo de exceção
            if (exceptionTelemetry.Exception != null)
            {
                exceptionTelemetry.Properties["ExceptionType"] = exceptionTelemetry.Exception?.GetType().Name ?? "Unknown";
                exceptionTelemetry.Properties["InnerExceptionType"] = exceptionTelemetry.Exception.InnerException?.GetType().Name ?? "None";
                exceptionTelemetry.SeverityLevel ??= SeverityLevel.Error;
            }
        }

        #endregion Métodos Privados

    }
}
