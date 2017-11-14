using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleMonitor
{
    public static class EventHubREST
    {
        private static string serviceNamespace = "raydoneventhubnamespace-01";
        private static string hubName = "raydontrainingdatahub-01";
        private static string deviceName = "device-01";
        private static Uri uri = null;
        private static int count = 0;

        public static string sas = String.Empty;

        static EventHubREST()
        {
            uri = new Uri(String.Format("https://{0}.servicebus.windows.net/{1}/publishers/{2}/messages", serviceNamespace, hubName, deviceName));
        }

        public static int Send( string jsonString )
        {
            if ( String.IsNullOrWhiteSpace(sas))
            {
                return -1;
            }

            var req = WebRequest.Create(uri);
            req.Method = "POST";
            req.Headers.Add("Authorization", sas);
            req.ContentType = "application/atom+xml;type=entry;charset=utf-8";

            using (var writer = new StreamWriter(req.GetRequestStream()))
            {
                writer.Write(jsonString);
            }

            using (var response = req.GetResponse() as HttpWebResponse)
            {
                count++;
            }

            return jsonString.Length;
        }
    }
}
