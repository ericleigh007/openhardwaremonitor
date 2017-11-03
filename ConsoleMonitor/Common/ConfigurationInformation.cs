using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raydon.CommonData.Configuration
{
    public class ConfigurationInformation
    {
        public string recordType;
        public string recordVersion;
        public string recordID;  // document ID for nosql
        public string systemID;
        public string TableKey;
        public string utcTime;
        public string venueID;
        public bool configSent;

        public string MainboardType;
        public string CPUtype;
        public string GPUType;

        public Dictionary<string, object> details;

        public ConfigurationInformation(string id, string vID)
        {
            systemID = id;
            venueID = vID;
            TableKey = id;

            recordType = "Configuration";
            recordVersion = "00003";

            details = new Dictionary<string, object>();
        }

        public void Resolve()
        {
            utcTime = DateTime.UtcNow.ToString("O");

            recordID = "C" + "|" + systemID + "|" + utcTime;
        }
    }
}
