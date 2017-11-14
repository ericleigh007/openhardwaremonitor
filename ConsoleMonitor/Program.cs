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

using Raydon.CommonData.Configuration;
using Raydon.CommonData.Maintenance;

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
    public class Config
    {
        public string venueName;
        public string venueID;

        public bool sendConfig;
        public bool sendMaintenance;
        public bool useREST;

        public int maintInterval;

        public string maintHubName;
        public string maintHubConnectionString;

        public string sas;
    }

    class Program
    {
        static EventHubClient maintHub;
        static int sentCount = 0;
        static UInt64 updateSentBytes = 0;
        static UInt64 totalSentBytes = 0;
        static UInt64 maintSize = 0;
        static UInt64 configSize = 0;
        private static string sendMethod = String.Empty;

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

            var config = JsonConvert.DeserializeObject<Config>(jsonConfig);

            if (config.useREST && !String.IsNullOrWhiteSpace(config.sas))
            {
                EventHubREST.sas = config.sas;
                Console.WriteLine($"sending updates via REST");
                sendMethod = "REST";
            }
            else
            {
                sendMethod = "AMQP";
                if (config.sendMaintenance || config.sendConfig)
                {
                    maintHub = EventHubClient.CreateFromConnectionString(config.maintHubConnectionString);
                }
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

            var configData = new ConfigurationInformation(computerName, config.venueName);
            bool firstSend = true;

            while (true)
            {
                var maintData = new MaintenanceInformation(computerName, config.venueName);

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

                if (config.sendConfig )
                {
                    configData.Resolve();

                    j = JsonConvert.SerializeObject(configData);

                    configSize = SendToMaintHub(j, configData.systemID, config.maintHubName, configData.recordType);

                    Console.WriteLine($"{configData.recordType} data size: {AutoFormatValue(configSize, 2)}");

                    File.WriteAllText(@".\HWconfig-information.json", j);

                    // just once
                    config.sendConfig = false;
                }

                if (config.sendMaintenance )
                {
                    maintData.Resolve();

                    j = JsonConvert.SerializeObject(maintData);
                    maintSize = SendToMaintHub(j, maintData.systemID, config.maintHubName, maintData.recordType);
                }

                MaintenanceInformation.updateStartTime = DateTime.UtcNow;

                double sinceLastUpdate = updateTime.Elapsed.TotalSeconds;

                if ( firstSend || (sinceLastUpdate > 60.0 ))
                {
                    if ( firstSend )
                    {
                        Console.WriteLine($"{maintData.recordType} data size: {AutoFormatValue(maintSize, 2)}");

                        File.WriteAllText(@".\HWmaint-information.json", j);

                        firstSend = false;
                    }

                    double bytesSecond = (double)updateSentBytes / sinceLastUpdate;
                    string autoValue = AutoFormatValue(totalSentBytes, 2);
                    string autoBytesSecond = AutoFormatValue( (ulong) bytesSecond, 2);
                    Console.WriteLine($"{computerName}: at {maintData.utcTime} : (via {sendMethod}) interval: {config.maintInterval} {sentCount} sends, {autoValue} ({autoBytesSecond}/Sec)");

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

        static UInt64 SendToMaintHub( string jsonString , string partitionKey, string name, string type )
        {
            UInt64 sendSize = 0;
            if (maintHub != null)
            {
                EventData data = new EventData(Encoding.UTF8.GetBytes(jsonString)) { PartitionKey = partitionKey };
                try
                {
                    maintHub.Send(data);
                    sendSize = (UInt64)data.SerializedSizeInBytes;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception sending {type} data to {name} \n" +
                        "     {ex.Message)\n" +
                        "     skipped");
                }
            }
            else
            {
                sendSize = (UInt64) EventHubREST.Send(jsonString);
            }

            sentCount++;
            updateSentBytes += sendSize;
            totalSentBytes += sendSize;
            return sendSize;
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
