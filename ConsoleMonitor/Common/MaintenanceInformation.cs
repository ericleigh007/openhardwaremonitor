using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raydon.CommonData.Maintenance
{
    public class MaintenanceInformation
    {
        public string recordType;
        public string recordVersion;
        public string recordID;
        public string systemID;
        public string venueID;
        public string TableKey;  // the partition key for CosmosDB
        public string utcTime;
        public double runTime;
        public double incrRunTime;

        public double CPUTemperatureMax;
        public double CPUTemperatureMin;
        public double CPUTemperatureAvg;

        public double GPUTemperatureMax;
        public double GPUTemperatureMin;
        public double GPUTemperatureAvg;

        public double CPUPowerMax;
        public double CPUPowerMin;
        public double CPUPowerAvg;

        public double GPUPowerMax;
        public double GPUPowerMin;
        public double GPUPowerAvg;

        public double CPUFanSpeedMax;
        public double CPUFanSpeedMin;
        public double CPUFanSpeedAvg;

        public double GPUFanSpeedMax;
        public double GPUFanSpeedMin;
        public double GPUFanSpeedAvg;

        public double MainboardFanSpeedMax;
        public double MainboardFanSpeedMin;
        public double MainboardFanSpeedAvg;

        public double MainboardTemperatureMax;
        public double MainboardTemperatureMin;
        public double MainboardTemperatureAvg;

        public double MaximumTemperature;

        public double CPULoad;
        public double GPULoad;
        public double RAMLoad;
        public double diskLoad;

        public double CPUFanCount;
        public double GPUFanCount;
        public double CPUTempCount;
        public double GPUTempCount;
        public double CPUPowerCount;
        public double GPUPowerCount;

        public double MainboardFanCount;
        public double MainboardTempCount;

        public double systemPowerWatts;

        public Dictionary<string, object> details;

        public static DateTime updateStartTime;

        private static DateTime monitorStartTime = DateTime.UtcNow;

        public MaintenanceInformation(string id, string vID)
        {
            systemID = id;
            venueID = vID;

            recordType = "Maintenance";
            recordVersion = "00003";

            CPUTemperatureMax = 0.0;
            CPUTemperatureMin = 1000.0;
            CPUTemperatureAvg = 0.0;

            GPUTemperatureMax = 0.0;
            GPUTemperatureMin = 1000.0;
            GPUTemperatureAvg = 0.0;

            CPUFanSpeedMax = 0.0;
            CPUFanSpeedMin = 10000.0;
            CPUFanSpeedAvg = 0.0;

            GPUFanSpeedMax = 0.0;
            GPUFanSpeedMin = 10000.0;
            GPUFanSpeedAvg = 0.0;

            CPUPowerMax = 0.0;
            CPUPowerMin = 10000.0;
            CPUPowerAvg = 0.0;

            GPUPowerMax = 0.0;
            GPUPowerMin = 10000.0;
            GPUPowerAvg = 0.0;

            MainboardFanSpeedMax = 0.0;
            MainboardFanSpeedMin = 10000.0;
            MainboardFanSpeedAvg = 0.0;

            MainboardTemperatureMax = 0.0;
            MainboardTemperatureMin = 1000.0;
            MainboardTemperatureAvg = 0.0;

            MaximumTemperature = 0.0;

            CPUTempCount = 0;
            GPUTempCount = 0;
            CPUFanCount = 0;
            GPUFanCount = 0;
            CPUPowerCount = 0;
            GPUPowerCount = 0;
            MainboardFanCount = 0;
            MainboardTempCount = 0;

            details = new Dictionary<string, object>();
        }

        public void Resolve()
        {
            if (CPUTempCount > 0)
            {
                CPUTemperatureAvg /= (double)CPUTempCount;
            }
            else
            {
                CPUTemperatureAvg = 0.0;
                CPUTemperatureMax = 0.0;
                CPUTemperatureMin = 0.0;
            }

            if (GPUTempCount > 0)
            {
                GPUTemperatureAvg /= (double)GPUTempCount;
            }
            else
            {
                GPUTemperatureAvg = 0.0;
                GPUTemperatureMax = 0.0;
                GPUTemperatureMin = 0.0;
            }

            if (CPUFanCount > 0)
            {
                CPUFanSpeedAvg /= (double)CPUFanCount;
            }
            else
            {
                CPUFanSpeedAvg = 0.0;
                CPUFanSpeedMax = 0.0;
                CPUFanSpeedMin = 0.0;
            }

            if (GPUFanCount > 0)
            {
                GPUFanSpeedAvg /= (double)GPUFanCount;
            }
            else
            {
                GPUFanSpeedAvg = 0.0;
                GPUFanSpeedMax = 0.0;
                GPUFanSpeedMin = 0.0;
            }

            if (CPUPowerCount > 0)
            {
                CPUPowerAvg /= (double)CPUPowerCount;
            }
            else
            {
                CPUPowerAvg = 0.0;
                CPUPowerMax = 0.0;
                CPUPowerMin = 0.0;
            }

            if (GPUPowerCount > 0)
            {
                GPUPowerAvg /= (double)GPUPowerCount;
            }
            else
            {
                GPUPowerAvg = 0.0;
                GPUPowerMax = 0.0;
                GPUPowerMin = 0.0;
            }

            if (MainboardTempCount > 0)
            {
                MainboardTemperatureAvg /= (double)MainboardTempCount;
            }
            else
            {
                MainboardTemperatureAvg = 0.0;
                MainboardTemperatureMax = 0.0;
                MainboardTemperatureMin = 0.0;
            }

            if (MainboardFanCount > 0)
            {
                MainboardFanSpeedAvg /= (double)MainboardFanCount;
            }
            else
            {
                MainboardFanSpeedAvg = 0.0;
                MainboardFanSpeedMax = 0.0;
                MainboardFanSpeedMin = 0.0;
            }

            MaximumTemperature = Math.Max(Math.Max(CPUTemperatureMax, GPUTemperatureMax), MainboardTemperatureMax);

            TableKey = systemID;

            runTime = (DateTime.UtcNow - monitorStartTime).TotalSeconds;
            utcTime = DateTime.UtcNow.ToString("O");

            incrRunTime = (DateTime.UtcNow - MaintenanceInformation.updateStartTime).TotalSeconds;

            recordID = "M" + "|" + systemID + "|" + utcTime;
        }
    }
}
