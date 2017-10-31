using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenHardwareMonitor;
using OpenHardwareMonitor.Hardware;
using System.Threading;
using System.IO;
using System.Diagnostics;
using Microsoft.ServiceBus.Messaging;

using Newtonsoft.Json;
using System.Net.NetworkInformation;

// note the following DLL's are expected to be on the system if the requisite hardware is installed.
// Present for all Windows systems
//   ntdll.dll - OS level hardware - all systems
//   kernel32.dll - OS level - all systems
// Present optionally
//   advapi32.dll - ACPI Hardwrare
//   atiadlxx.dll - ATI GPU Hardware
//   atiadlxy.dll - ATI GPU Hardware
//   nvapi.dll - nVidia GPU hardware, 32-bit
//   nvapi64.dll - nVidia GPU hardware, 64-bit
//   ftd2xx.dll - Fan/cooler/flow

namespace ConsoleMonitor
{
    public class ConfigurationInformation
    {
        public string recordType;
        public string recordVersion;
        public string recordID;
        public string systemID;
        public string TableKey;
        public string utcTime;
        public string locationGUID;

        public string MainboardType;
        public string CPUtype;
        public string GPUType;

        public Dictionary<string, object> details;

        public ConfigurationInformation( string id)
        {
            systemID = id;
            TableKey = id;
            recordType = "Configuration";
            recordVersion = "00002";

            details = new Dictionary<string, object>();
        }

        public void Resolve()
        {
            utcTime = DateTime.UtcNow.ToString("O");

            recordID = "C" + "|" + systemID + "|" + utcTime;
        }
    }

    public class MaintenanceInformation
    {
        public string recordType;
        public string recordVersion;
        public string recordID;
        public string systemID;
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

        public Dictionary<string, object> details;

        public static DateTime updateStartTime;

        private static DateTime monitorStartTime = DateTime.UtcNow;

        public MaintenanceInformation( string id)
        {
            systemID = id;

            recordType = "Maintenance";
            recordVersion = "00002";

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

            if ( CPUPowerCount > 0)
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

    public class Config
    {
        public bool sendConfig;
        public bool sendMaintenance;

        public int maintInterval;

        public string configHubName;
        public string configHubConnectionString;
        public string maintHubName;
        public string maintHubConnectionString;
    }

    class Program
    {
        static int Main(string[] args)
        {
            string configFileName = args[0];

            string jsonConfig = String.Empty;
            try
            {
                jsonConfig = File.ReadAllText(configFileName);
            }
            catch( Exception ex)
            {
                Console.WriteLine($"Cannot read file {configFileName}. {ex.Message}");
                return 1;
            }

            EventHubClient configHub = null;
            var config = JsonConvert.DeserializeObject<Config>(jsonConfig);

            if ( config.sendConfig )
            {
                configHub = EventHubClient.CreateFromConnectionString(config.configHubConnectionString);
            }

            EventHubClient maintHub = null;
            if ( config.sendMaintenance )
            {
                maintHub = EventHubClient.CreateFromConnectionString(config.maintHubConnectionString);
            }

            if ( config.maintInterval == 0 )
            {
                config.maintInterval = 2000;
            }

            MaintenanceInformation.updateStartTime = DateTime.UtcNow;

            var computerName = Dns.GetHostName();
            var nicAdapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach( var nic in nicAdapters)
            {
                Console.WriteLine($"Network adapter {nic.Name}, id: {nic.Id}");
                Console.WriteLine($"    Description: {nic.Description}");
                Console.WriteLine($"    Speed: {nic.Speed}  Type: {nic.NetworkInterfaceType}");
            }

            var chosenNic = nicAdapters.Where(p => p.NetworkInterfaceType.ToString() == "Ethernet").Where(p => p.OperationalStatus == OperationalStatus.Up).FirstOrDefault();
            string chosenMAC = String.Empty;

            if ( chosenNic != null )
            {
                chosenMAC = chosenNic.GetPhysicalAddress().ToString();
                Console.WriteLine($"Selected adapter {chosenNic.Name} with MAC {chosenMAC}");
            }

            computerName += $"|{chosenMAC}";

            var cp = new Computer()
            {
                FanControllerEnabled = true,
                CPUEnabled = true,
                GPUEnabled = true,
                HDDEnabled = true,
                MainboardEnabled = true,
                RAMEnabled = true
            };

            cp.Open();

            bool report = true;

            Stopwatch runTime = new Stopwatch();
            runTime.Start();

            Stopwatch updateTime = new Stopwatch();
            updateTime.Start();

            var configData = new ConfigurationInformation(computerName);
            int sentCount = 0;
            UInt64 updateSentBytes = 0;
            UInt64 totalSentBytes = 0;
            bool firstSend = true;

            while (true)
            {
                var maintData = new MaintenanceInformation(computerName);

                foreach (var hw in cp.Hardware)
                {
                    if (report) Console.WriteLine(hw.GetReport());
                    if (config.sendConfig) hw.AddHardware(configData);

                    hw.Update();

                    foreach (var hwSensor in hw.Sensors)
                    {
                        if (report) Console.WriteLine($"  HW: Sensor {hwSensor.SensorType} : {hwSensor.Name} : {hwSensor.Value}");
                        hwSensor.AddSensor(maintData.details);
                        hwSensor.UpdateGenericMaintenanceData(hw, maintData);

                        foreach (var par in hwSensor.Parameters)
                        {
                            if (report) Console.WriteLine($"    HW: Sensor: Param {par.Name} {par.Value}");
                        }

                        foreach (var val in hwSensor.Values)
                        {
                            if (report) Console.WriteLine($"    HW: Sensor: Value {val.Time} {val.Value}");
                        }
                    }

                    foreach (var subhw in hw.SubHardware)
                    {
                        if (report) Console.WriteLine(subhw.GetReport());
                        subhw.Update();

                        if ( config.sendConfig) subhw.AddHardware(configData);

                        foreach (var subhwSensor in subhw.Sensors)
                        {
                            if (report) Console.WriteLine($"  HW: Sensor {subhwSensor.SensorType} : {subhwSensor.Name} : {subhwSensor.Value}");
                            subhwSensor.AddSensor(maintData.details);
                            subhwSensor.UpdateGenericMaintenanceData(subhw, maintData);

                            foreach (var par in subhwSensor.Parameters)
                            {
                                if (report) Console.WriteLine($"    HW: Sensor: Param {par.Name} {par.Value}");
                            }

                            foreach (var val in subhwSensor.Values)
                            {
                                if (report) Console.WriteLine($"    HW: Sensor: Value {val.Time} {val.Value}");
                            }
                        }
                    }
                }

                report = false;

                string j = String.Empty;
                UInt64 maintSize = 0;
                UInt64 configSize = 0;

                if (config.sendConfig )
                {
                    configData.Resolve();

                    j = JsonConvert.SerializeObject(configData);
                    EventData data = new EventData(Encoding.UTF8.GetBytes(j)) { PartitionKey = configData.systemID };

                    try
                    {
                        configHub.Send(data);

                        sentCount++;
                        configSize = (UInt64)data.SerializedSizeInBytes;
                        updateSentBytes += configSize;
                        totalSentBytes += configSize;
                    }
                    catch ( Exception ex)
                    {
                        Console.WriteLine($"Exception sending to {config.configHubName} \n" +
                            "     {ex.Message)\n" +
                            "     skipped");
                    }

                    Console.WriteLine($"Configuration data size: {AutoFormatValue(configSize, 2)}");

                    File.WriteAllText(@".\HWconfig-information.json", j);

                    // just once
                    config.sendConfig = false;
                }
                else if ( !configHub.IsClosed )
                {
                    configHub.Close();
                }

                if (config.sendMaintenance )
                {
                    maintData.Resolve();

                    j = JsonConvert.SerializeObject(maintData);
                    EventData data = new EventData(Encoding.UTF8.GetBytes(j)) { PartitionKey = maintData.systemID };

                    try
                    {
                        maintHub.Send(data);
                    }
                    catch ( Exception ex)
                    {
                        Console.WriteLine($"Exception sending to {config.maintHubName} \n" +
                        "     {ex.Message)\n" +
                        "     skipped");
                    }

                    sentCount++;
                    maintSize = (UInt64)data.SerializedSizeInBytes;
                    updateSentBytes += maintSize;
                    totalSentBytes += maintSize;
                }
                else if ( !maintHub.IsClosed )
                {
                    maintHub.Close();
                }

                MaintenanceInformation.updateStartTime = DateTime.UtcNow;

                if ( firstSend || ((sentCount % 60 ) == 0))
                {
                    if ( firstSend )
                    {
                        Console.WriteLine($"Maintenance data size: {AutoFormatValue(maintSize, 2)}");

                        File.WriteAllText(@".\HWmaint-information.json", j);

                        firstSend = false;
                    }

                    double sinceLastUpdate = updateTime.Elapsed.TotalSeconds;

                    double bytesSecond = (double)updateSentBytes / sinceLastUpdate;
                    string autoValue = AutoFormatValue(totalSentBytes, 2);
                    string autoBytesSecond = AutoFormatValue( (ulong) bytesSecond, 2);
                    Console.WriteLine($"{computerName}: at {maintData.utcTime} : interval: {config.maintInterval} {sentCount} sends, {autoValue} ({autoBytesSecond}/Sec)");

                    updateTime.Restart();
                    updateSentBytes = 0;
                }

                Thread.Sleep(config.maintInterval);
            }

            return 0;
        }

        // stackoverflow
        static string AutoFormatValue(ulong value, int decimalPlaces = 0)
        {
            string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            if (value < 0)
            {
                throw new ArgumentException("Bytes should not be negative", "value");
            }
            var mag = (int)Math.Max(0, Math.Log(value, 1024));
            var adjustedSize = Math.Round(value / Math.Pow(1024, mag), decimalPlaces);
            return String.Format("{0} {1}", adjustedSize, SizeSuffixes[mag]);
        }
    }

    public static class extensionMethods
    {
        public static void UpdateGenericMaintenanceData( this ISensor sensor , IHardware hardware , MaintenanceInformation maintInfo)
        {
            switch( hardware.HardwareType )
            {
                case HardwareType.SuperIO:
                case HardwareType.Mainboard:
                    switch (sensor.SensorType)
                    {
                        case SensorType.Fan:
                            if (sensor.Value > maintInfo.MainboardFanSpeedMax)
                            {
                                maintInfo.MainboardFanSpeedMax = (double)sensor.Value;
                            }

                            if (sensor.Value < maintInfo.MainboardFanSpeedMin)
                            {
                                maintInfo.MainboardFanSpeedMin = (double)sensor.Value;
                            }

                            maintInfo.MainboardFanSpeedAvg += (double)sensor.Value;
                            maintInfo.MainboardFanCount++;

                            break;
                        case SensorType.Temperature:
                            if (sensor.Value > maintInfo.MainboardTemperatureMax)
                            {
                                maintInfo.MainboardTemperatureMax = (double)sensor.Value;
                            }

                            if (sensor.Value < maintInfo.MainboardTemperatureMin)
                            {
                                maintInfo.MainboardTemperatureMin = (double)sensor.Value;
                            }

                            maintInfo.MainboardTemperatureAvg += (double)sensor.Value;
                            maintInfo.MainboardTempCount++;

                            break;
                    }
                    break;
                case HardwareType.CPU:
                    switch (sensor.SensorType)
                    {
                        case SensorType.Fan:
                            if (sensor.Value > maintInfo.CPUFanSpeedMax)
                            {
                                maintInfo.CPUFanSpeedMax = (double)sensor.Value;
                            }

                            if (sensor.Value < maintInfo.CPUFanSpeedMin)
                            {
                                maintInfo.CPUFanSpeedMin = (double)sensor.Value;
                            }

                            maintInfo.CPUFanSpeedAvg += (double)sensor.Value;
                            maintInfo.CPUFanCount++;

                            break;
                        case SensorType.Temperature:
                            if (sensor.Value > maintInfo.CPUTemperatureMax)
                            {
                                maintInfo.CPUTemperatureMax = (double)sensor.Value;
                            }

                            if (sensor.Value < maintInfo.CPUTemperatureMin)
                            {
                                maintInfo.CPUTemperatureMin = (double)sensor.Value;
                            }

                            maintInfo.CPUTemperatureAvg += (double)sensor.Value;
                            maintInfo.CPUTempCount++;

                            break;
                        case SensorType.Power:
                            if (sensor.Value > maintInfo.CPUPowerMax)
                            {
                                maintInfo.CPUPowerMax = (double)sensor.Value;
                            }

                            if (sensor.Value < maintInfo.CPUPowerMin)
                            {
                                maintInfo.CPUPowerMin = (double)sensor.Value;
                            }

                            maintInfo.CPUPowerAvg += (double)sensor.Value;
                            maintInfo.CPUPowerCount++;

                            break;
                    }
                    break;
                case HardwareType.GpuAti:
                case HardwareType.GpuNvidia:
                    switch (sensor.SensorType)
                    {
                        case SensorType.Fan:
                            if (sensor.Value > maintInfo.GPUFanSpeedMax)
                            {
                                maintInfo.GPUFanSpeedMax = (double)sensor.Value;
                            }

                            if (sensor.Value < maintInfo.GPUFanSpeedMin)
                            {
                                maintInfo.GPUFanSpeedMin = (double)sensor.Value;
                            }

                            maintInfo.GPUFanSpeedAvg += (double)sensor.Value;
                            maintInfo.GPUFanCount++;

                            break;
                        case SensorType.Temperature:
                            if (sensor.Value > maintInfo.GPUTemperatureMax)
                            {
                                maintInfo.GPUTemperatureMax = (double)sensor.Value;
                            }

                            if (sensor.Value < maintInfo.GPUTemperatureMin)
                            {
                                maintInfo.GPUTemperatureMin = (double)sensor.Value;
                            }

                            maintInfo.GPUTemperatureAvg += (double)sensor.Value;
                            maintInfo.GPUTempCount++;

                            break;
                        case SensorType.Power:
                            if (sensor.Value > maintInfo.GPUPowerMax)
                            {
                                maintInfo.GPUPowerMax = (double)sensor.Value;
                            }

                            if (sensor.Value < maintInfo.GPUPowerMin)
                            {
                                maintInfo.GPUPowerMin = (double)sensor.Value;
                            }

                            maintInfo.GPUPowerAvg += (double)sensor.Value;
                            maintInfo.GPUPowerCount++;

                            break;
                    }
                    break;
            }
        }

        // Identifier is essentially the "path" to the sensor
        public static string GetID( this ISensor sensor )
        {
            var name = sensor.Identifier.ToString();
            return name;
        }

        public static string AddSensor(this ISensor sensor, IDictionary<string, object> msg)
        {
            var name = sensor.GetID();

            // Add the object to the dictionary
            msg.Add(name, sensor.Value);
            return name;
        }

        public static string GetID(this IHardware hardware)
        {
            var name = hardware.Identifier.ToString();
            return name;
        }

        public static string AddHardware(this IHardware hardware, ConfigurationInformation configInfo)
        {
            var name = hardware.GetID();
            var msg = configInfo.details;

            switch( hardware.HardwareType )
            {
                case HardwareType.CPU:
                    configInfo.CPUtype = hardware.Name;
                    break;

                case HardwareType.GpuAti:
                case HardwareType.GpuNvidia:
                    configInfo.GPUType = hardware.Name;
                    break;

                case HardwareType.Mainboard:
                    configInfo.MainboardType = hardware.Name;
                    break;

            }
            // add the object to the dictionary
            string descriptionString = String.Empty;
            descriptionString = hardware.GetReport();

            msg.Add(name, descriptionString);
            return name;
        }
    };

}
