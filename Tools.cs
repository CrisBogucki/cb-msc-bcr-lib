using Microsoft.Extensions.Configuration;

namespace CBMscBrcLib
{
    public static class Tools
    {
        private static ConfigurationBuilder _configurationBuilder;

        public static string GetVersionString()
        {
            var v = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
            return $"{v?.Major}.{v?.Minor}.{v?.Build}";
        }

        public static string GetAppSettingsValueString(string section, string field)
        {
            _configurationBuilder = new ConfigurationBuilder();
            _configurationBuilder.AddJsonFile("appsettings.json");
            var res =  _configurationBuilder.Build().GetSection(section)[field];
            return res;
        }

    }
}
