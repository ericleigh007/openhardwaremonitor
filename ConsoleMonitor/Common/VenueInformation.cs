using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raydon.CommonData.Venue
{
    public class VenueInformation
    {
        public string recordType;
        public string recordVersion;

        public string recordID;  // document id for nosql.

        // required to start the venue
        public string venueID;
        public string TableKey;   // partition key for DocumentDB
        public string name;
        public string address;
        public string country;
        public string countryCode;
        public string postalCode;
        public string city;
        public string state;
        public string stateCode;
        public string county;
        public string MTC;
        public int systemCount;
        public double lat;
        public double lon;

        // realtime data
        public string utcTime;

        public double externalTemp;
        public double externalHumidity;
        public double externalHeatLoad;

        public double internalTemp;
        public double internalHumdity;
        public double externalTempChangePerHour;

        public string envSysMode;
        public double envSysCompressorPower;
        public double envSysFanSpeed;

        public string powerSysMode;
        public double powerSysVoltage;
        public double powerSysAmperage;
        public double powerSysFrequency;

        public static readonly double optimumInternalTemp = 20.0;
        public static readonly double optimumTemperatureRange = 2.0;  // within +/- this many degrees, the AC system doesn't run

        public static readonly double optimumSysVoltage = 117.0;
        public static readonly double optimumSysAmperagePerSystem = 4.0;
        public static readonly double optimumSysFrequency = 60.0;

        public VenueInformation(string id, int sysCount)
        {
            venueID = id;
            recordType = "Venue";
            recordVersion = "00002";

            systemCount = sysCount;
        }

        public void Resolve()
        {
            utcTime = DateTime.UtcNow.ToString("O");
            recordID = $"V|{venueID}|{utcTime}";
        }

        public string GetPartitionKey()
        {
            var partKey = $"Venue|{venueID}";
            TableKey = partKey;
            return partKey;
        }
    }

    public enum envSysModes
    {
        Off,
        Cooling,
        Heating
    }

    public enum powerSysModes
    {
        Off,
        ShorePower,
        GeneratorPower
    }
}
