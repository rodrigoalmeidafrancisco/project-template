using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Shared.Settings;

namespace WebApi.Configurations
{
    /// <summary>
    /// Classe estática responsável por configurar o Application Insights na aplicação WebApi
    /// </summary>
    public static class ConfigWebApiApplicationInsights
    {
        /// <summary>
        /// Extension method para configurar serviços do Application Insights no builder da aplicação
        /// </summary>
        /// <param name="builder">WebApplicationBuilder usado para configurar a aplicação</param>
        public static void AddConfigApplicationInsights(this WebApplicationBuilder builder)
        {
            // Adiciona o serviço de telemetria do Application Insights ao container de DI
            builder.Services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions()
            {
                // Define a connection string para envio de telemetria ao Azure Application Insights
                ConnectionString = SettingApp.ConnectionStrings.Default,

                // Desabilita amostragem adaptativa em desenvolvimento para capturar 100% dos eventos (facilita debug)
                // Em produção, habilita para reduzir custos e volume de dados
                EnableAdaptiveSampling = !builder.Environment.IsDevelopment(),

                // Habilita envio periódico de heartbeat para monitorar disponibilidade da aplicação
                EnableHeartbeat = true,

                // Habilita rastreamento automático de chamadas a dependências externas (SQL Server, HTTP, Redis, etc)
                EnableDependencyTrackingTelemetryModule = true,

                // Habilita captura automática de todas as requisições HTTP recebidas pela API
                EnableRequestTrackingTelemetryModule = true,

                // Habilita coleta de contadores de performance do sistema (CPU, memória, threads, etc)
                EnablePerformanceCounterCollectionModule = true,

                // Habilita coleta de eventos de contadores customizados da aplicação
                EnableEventCounterCollectionModule = true,
            });

            // Registra o inicializador de telemetria como singleton para enriquecer todos os eventos
            builder.Services.AddSingleton<ITelemetryInitializer, TelemetryInitializer>();
        }

        /// <summary>
        /// Extension method para configurar middlewares e eventos do Application Insights na aplicação iniciada
        /// </summary>
        /// <param name="app">WebApplication configurada e pronta para execução</param>
        public static void AddConfigApplicationInsights(this WebApplication app)
        {
            // Obtém instância do TelemetryClient do container de DI para enviar telemetria customizada
            var telemetry = app.Services.GetRequiredService<TelemetryClient>();

            // Registra callback para ser executado quando a aplicação iniciar com sucesso
            app.Lifetime.ApplicationStarted.Register(() =>
            {
                // Envia trace informativo registrando o momento de inicialização da aplicação
                telemetry.TrackTrace($"Log da aplicação '{SettingApp.Aplication.Name}' iniciada", SeverityLevel.Information);
            });
        }

        #region Métodos Privados

        /// <summary>
        /// Inicializador customizado que enriquece automaticamente toda telemetria antes do envio ao Application Insights
        /// Implementa ITelemetryInitializer para interceptar e modificar eventos de telemetria
        /// </summary>
        private sealed class TelemetryInitializer : ITelemetryInitializer
        {
            /// <summary>
            /// Método chamado automaticamente para cada evento de telemetria antes do envio
            /// </summary>
            /// <param name="telemetry">Objeto de telemetria a ser enriquecido</param>
            public void Initialize(ITelemetry telemetry)
            {
                // Valida se o objeto de telemetria e seu contexto são válidos antes de processar
                if (telemetry?.Context == null) return;

                // Define o nome da role exibido no Application Map do Azure (identifica o serviço visualmente)
                telemetry.Context.Cloud.RoleName = $"[{SettingApp.Aplication._Environment}] - {SettingApp.Aplication.Name} ({SettingApp.Aplication.Type})";

                // Define a instância da role (identifica a máquina/container específico onde o serviço está rodando)
                telemetry.Context.Cloud.RoleInstance = Environment.MachineName;

                // Adiciona propriedade global de ambiente para filtros e queries no portal do Azure
                telemetry.Context.GlobalProperties["Ambiente"] = SettingApp.Aplication._Environment;

                // Adiciona propriedade global com nome da aplicação para identificação em queries
                telemetry.Context.GlobalProperties["NomeAplicacao"] = SettingApp.Aplication.Name;

                // Adiciona propriedade global com tipo da aplicação (WebApi, Worker, Console, etc)
                telemetry.Context.GlobalProperties["TipoAplicacao"] = SettingApp.Aplication.Type;

                // Adiciona versão do runtime .NET para troubleshooting de compatibilidade
                telemetry.Context.GlobalProperties["DotNetVersion"] = Environment.Version.ToString();

                // Enriquece especificamente telemetria de requisições HTTP (padrão switch pattern matching)
                if (telemetry is RequestTelemetry requestTelemetry)
                {
                    EnrichRequestTelemetry(requestTelemetry);
                }

                // Enriquece especificamente telemetria de chamadas a dependências externas
                if (telemetry is DependencyTelemetry dependencyTelemetry)
                {
                    EnrichDependencyTelemetry(dependencyTelemetry);
                }

                // Enriquece especificamente telemetria de exceções/erros capturados
                if (telemetry is ExceptionTelemetry exceptionTelemetry)
                {
                    EnrichExceptionTelemetry(exceptionTelemetry);
                }
            }
        }

        /// <summary>
        /// Enriquece telemetria de requisições HTTP com informações customizadas
        /// Permite categorizar e customizar o comportamento de success/failure
        /// </summary>
        /// <param name="requestTelemetry">Objeto de telemetria da requisição HTTP</param>
        private static void EnrichRequestTelemetry(RequestTelemetry requestTelemetry)
        {
            // Tenta fazer parse do código de resposta HTTP de string para inteiro
            if (int.TryParse(requestTelemetry.ResponseCode, out int statusCode))
            {
                // Adiciona categoria legível do status HTTP usando switch expression para facilitar análises
                // Permite filtros no Azure como "StatusCategory = ClientError"
                requestTelemetry.Properties["StatusCategory"] = statusCode switch
                {
                    // Status 2xx: Requisição processada com sucesso
                    >= 200 and < 300 => "Success",

                    // Status 3xx: Redirecionamentos (MovedPermanently, Found, etc)
                    >= 300 and < 400 => "Redirect",

                    // Status 4xx: Erros do cliente (BadRequest, NotFound, Unauthorized, etc)
                    >= 400 and < 500 => "ClientError",

                    // Status 5xx: Erros do servidor (InternalServerError, ServiceUnavailable, etc)
                    >= 500 => "ServerError",

                    // Códigos não mapeados ou inválidos
                    _ => "Unknown"
                };
            }
        }

        /// <summary>
        /// Enriquece telemetria de dependências externas (bancos de dados, APIs HTTP, cache, etc)
        /// </summary>
        /// <param name="dependencyTelemetry">Objeto de telemetria da dependência externa</param>
        private static void EnrichDependencyTelemetry(DependencyTelemetry dependencyTelemetry)
        {
            // Identifica chamadas ao SQL Server e adiciona metadado adicional
            // Útil para distinguir entre diferentes tipos de bancos de dados
            if (dependencyTelemetry.Type == "SQL")
            {
                // Adiciona tipo específico do banco de dados para análises granulares
                dependencyTelemetry.Properties["DatabaseType"] = "SqlServer";
            }
        }

        /// <summary>
        /// Enriquece telemetria de exceções com informações adicionais de diagnóstico
        /// </summary>
        /// <param name="exceptionTelemetry">Objeto de telemetria da exceção capturada</param>
        private static void EnrichExceptionTelemetry(ExceptionTelemetry exceptionTelemetry)
        {
            // Valida se a exceção existe antes de processar
            if (exceptionTelemetry.Exception != null)
            {
                // Adiciona nome do tipo da exceção principal (ex: "ArgumentNullException", "InvalidOperationException")
                exceptionTelemetry.Properties["ExceptionType"] = exceptionTelemetry.Exception?.GetType().Name ?? "Unknown";

                // Adiciona nome do tipo da exceção interna (útil para diagnosticar causa raiz de erros encadeados)
                exceptionTelemetry.Properties["InnerExceptionType"] = exceptionTelemetry.Exception.InnerException?.GetType().Name ?? "None";

                // Define severity level como Error se ainda não estiver definido (null-coalescing assignment)
                exceptionTelemetry.SeverityLevel ??= SeverityLevel.Error;
            }
        }

        #endregion Métodos Privados
    }
}
