using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace WebApi.Configurations
{
    /// <summary>
    /// Classe estática responsável por configurar rate limiting (limitação de taxa de requisições) na WebApi
    /// Previne ataques de brute force, DDoS e abuso de API
    /// </summary>
    public static class ConfigWebApiRateLimiting
    {
        /// <summary>
        /// Extension method para configurar políticas de rate limiting no builder da aplicação
        /// </summary>
        /// <param name="builder">WebApplicationBuilder usado para configurar a aplicação</param>
        public static void AddConfigRateLimiting(this WebApplicationBuilder builder)
        {
            // Adiciona o serviço de rate limiting ao container de DI
            builder.Services.AddRateLimiter(options =>
            {
                // Define o comportamento quando o limite é excedido
                // 429 Too Many Requests será retornado automaticamente
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // ============================================================
                // POLÍTICA 1: Limitação Global por IP (Janela Fixa)
                // Protege contra ataques volumétricos de um único IP
                // ============================================================
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    // Obtém o IP real do cliente (considera X-Forwarded-For para proxies/load balancers)
                    var clientIp = httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                        ?? "unknown";

                    // Cria partição por IP usando algoritmo de janela fixa
                    return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                    {
                        // Permite 100 requisições por janela de tempo
                        PermitLimit = 100,

                        // Janela de 1 minuto (reseta o contador a cada minuto)
                        Window = TimeSpan.FromMinutes(1),

                        // Número de requisições que podem ser enfileiradas quando o limite é atingido
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,

                        // Limite de fila (0 = rejeita imediatamente quando exceder o limite)
                        QueueLimit = 0
                    });
                });

                // ============================================================
                // POLÍTICA 2: Autenticação/Login - Janela Deslizante (Mais Restritiva)
                // Previne brute force em endpoints de login
                // ============================================================
                options.AddPolicy("authentication", httpContext =>
                {
                    // Obtém o IP do cliente
                    var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    // Algoritmo de janela deslizante: mais sofisticado que janela fixa
                    // Distribui o limite ao longo do tempo ao invés de resetar abruptamente
                    return RateLimitPartition.GetSlidingWindowLimiter(clientIp, _ => new SlidingWindowRateLimiterOptions
                    {
                        // Permite apenas 5 tentativas de login por janela
                        PermitLimit = 5,

                        // Janela de 1 minuto
                        Window = TimeSpan.FromMinutes(1),

                        // Divide a janela em 3 segmentos para distribuir melhor o limite
                        // Ex: 5 requisições em 60s = ~1.67 req por segmento de 20s
                        SegmentsPerWindow = 3,

                        // Não permite enfileiramento - rejeita imediatamente
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
                });

                // ============================================================
                // POLÍTICA 3: Token Bucket para APIs Públicas
                // Permite bursts controlados de requisições
                // ============================================================
                options.AddPolicy("api-public", httpContext =>
                {
                    var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    // Token Bucket: Ideal para APIs que permitem bursts ocasionais
                    // Tokens são reabastecidos ao longo do tempo
                    return RateLimitPartition.GetTokenBucketLimiter(clientIp, _ => new TokenBucketRateLimiterOptions
                    {
                        // Capacidade total do bucket (tokens disponíveis)
                        TokenLimit = 50,

                        // Taxa de reabastecimento de tokens por período
                        ReplenishmentPeriod = TimeSpan.FromSeconds(10),

                        // Quantidade de tokens adicionados a cada período
                        TokensPerPeriod = 10,

                        // Habilita preenchimento automático do bucket
                        AutoReplenishment = true,

                        // Não permite enfileiramento
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
                });

                // ============================================================
                // POLÍTICA 4: Concorrência para Operações Pesadas
                // Limita requisições simultâneas para endpoints que consomem muitos recursos
                // ============================================================
                options.AddPolicy("heavy-operations", httpContext =>
                {
                    var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    // Limita requisições concorrentes (simultâneas) por IP
                    return RateLimitPartition.GetConcurrencyLimiter(clientIp, _ => new ConcurrencyLimiterOptions
                    {
                        // Permite apenas 3 requisições simultâneas por IP
                        PermitLimit = 3,

                        // Enfileira até 5 requisições extras
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    });
                });

                // ============================================================
                // POLÍTICA 5: Rate Limiting por Usuário Autenticado
                // Limita baseado no ID do usuário ao invés do IP
                // ============================================================
                options.AddPolicy("authenticated-user", httpContext =>
                {
                    // Obtém o ID do usuário autenticado (claim 'sub' ou 'nameid')
                    var userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        ?? httpContext.User?.FindFirst("sub")?.Value
                        ?? "anonymous";

                    // Janela fixa por usuário
                    return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
                    {
                        // Permite 200 requisições por minuto por usuário
                        PermitLimit = 200,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
                });

                // ============================================================
                // Configura resposta customizada quando o limite é excedido
                // ============================================================
                options.OnRejected = async (context, cancellationToken) =>
                {
                    // Define o status code 429 Too Many Requests
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/json";

                    // Calcula o tempo de espera até poder fazer nova requisição
                    var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                        ? retryAfterValue.TotalSeconds
                        : 60; // Default: 60 segundos

                    // Adiciona header Retry-After (padrão HTTP para indicar quando tentar novamente)
                    context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString();

                    // Monta resposta JSON estruturada
                    var response = new
                    {
                        message = "Limite de requisições excedido",
                        error = "Too Many Requests",
                        retryAfterSeconds = retryAfter,
                        detail = "Você excedeu o número máximo de requisições permitidas. Aguarde antes de tentar novamente."
                    };

                    // Loga evento de rate limiting para monitoramento
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning(
                        "Rate limit excedido - IP: {IP}, Endpoint: {Endpoint}, Política: {Policy}",
                        context.HttpContext.Connection.RemoteIpAddress,
                        context.HttpContext.Request.Path,
                        context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName ?? "global"
                    );

                    // Retorna resposta JSON
                    await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
                };
            });
        }

        /// <summary>
        /// Extension method para habilitar o middleware de rate limiting na pipeline de requisições
        /// </summary>
        /// <param name="app">WebApplication configurada</param>
        public static void UseConfigRateLimiting(this WebApplication app)
        {
            // Adiciona o middleware de rate limiting na pipeline
            // ⚠️ IMPORTANTE: Deve vir ANTES de UseAuthentication/UseAuthorization
            // para prevenir brute force antes mesmo de processar credenciais
            app.UseRateLimiter();
        }
    }
}