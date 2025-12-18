using Microsoft.Extensions.Configuration;

namespace Shared.Settings
{
    public static class SettingApp
    {
        /*====================================================================================================================
        | ********************* Declaração da propriedade ********************                                               |                  
        | *  public static SettingsAplicacao Aplicacao { get; set; }                                                         |
        | ********************************************************************                                               |
        |                                                                                                                    |
        | -> Para obter uma seção inteira do json, usar dessa forma:                                                         |
        | Aplicacao = new SettingsAplicacao();                                                                               |
        | configuration.GetSection("Aplicacao").Bind(Aplicacao);                                                             |
        |                                                                                                                    |
        | ------------------------------------------------------------------------------------------------------------------ |
        |                                                                                                                    |
        | -> Para obter cada informação individual, usar dessa forma:                                                        |
        | Aplicacao = new SettingsAplicacao() { GuidIdAplicacaoAPI = configuration["Aplicacao:GuidIdAplicacaoAPI"] };        |
        |                                                                                                                    |
        ====================================================================================================================*/

        public static void Start(IConfiguration configuration, string webRootPath)
        {
            Aplication = new SettingAppAplication();
            configuration.GetSection("Aplication").Bind(Aplication);

            ApplicationInsights = new SettingAppApplicationInsights();
            configuration.GetSection("ApplicationInsights").Bind(ApplicationInsights);

            ConnectionStrings = new SettingAppConnectionStrings();
            configuration.GetSection("ConnectionStrings").Bind(ConnectionStrings);

            Constants = new SettingAppConstants();

            Parameters = new SettingAppParameters();
            configuration.GetSection("Parameters").Bind(Parameters);

            Services = new SettingsAppServices();
            configuration.GetSection("Services").Bind(Services);

            WebRootPath = webRootPath;
            WebRootPathImages = Path.Combine(WebRootPath, "images");
        }

        public static SettingAppAplication Aplication { get; set; }
        public static SettingAppApplicationInsights ApplicationInsights { get; set; }
        public static SettingAppConnectionStrings ConnectionStrings { get; set; }
        public static SettingAppConstants Constants { get; set; }
        public static SettingAppParameters Parameters { get; set; }
        public static SettingsAppServices Services { get; set; }
        public static string WebRootPath { get; set; }
        public static string WebRootPathImages { get; set; }

    }
}
