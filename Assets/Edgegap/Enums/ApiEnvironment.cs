namespace Edgegap
{
    public enum ApiEnvironment
    {
        Staging,
        Production,
    }

    public static class ApiEnvironmentsExtensions
    {
        public static string GetApiUrl(this ApiEnvironment apiEnvironment)
        {
            string apiUrl;

            switch (apiEnvironment)
            {
                case ApiEnvironment.Staging:
                    apiUrl = "https://staging-api.edgegap.com";
                    break;
                case ApiEnvironment.Production:
                    apiUrl = "https://api.edgegap.com";
                    break;
                default:
                    apiUrl = null;
                    break;
            }

            return apiUrl;
        }

        public static string GetDashboardUrl(this ApiEnvironment apiEnvironment)
        {
            string apiUrl;

            switch (apiEnvironment)
            {
                case ApiEnvironment.Staging:
                    apiUrl = "https://staging-console.edgegap.com";
                    break;
                case ApiEnvironment.Production:
                    apiUrl = "https://console.edgegap.com";
                    break;
                default:
                    apiUrl = null;
                    break;
            }

            return apiUrl;
        }

        public static string GetDocumentationUrl(this ApiEnvironment apiEnvironment)
        {
            string apiUrl;

            switch (apiEnvironment)
            {
                case ApiEnvironment.Staging:
                    apiUrl = "https://staging-docs.edgegap.com";
                    break;
                case ApiEnvironment.Production:
                    apiUrl = "https://docs.edgegap.com";
                    break;
                default:
                    apiUrl = null;
                    break;
            }

            return apiUrl;
        }
    }
}