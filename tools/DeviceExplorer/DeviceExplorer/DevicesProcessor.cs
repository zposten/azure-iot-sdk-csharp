using Microsoft.Azure.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;

namespace DeviceExplorer
{
    class DevicesProcessor
    {
        private List<DeviceEntity> listOfDevices;
        private RegistryManager registryManager;
        private string iotHubConnectionString;
        private int maxCountOfDevices;
        private string protocolGatewayHostName;
        private string hostName;

        public DevicesProcessor(string iotHubConnenctionString, int devicesCount, string protocolGatewayName)
        {
            this.listOfDevices = new List<DeviceEntity>();
            this.iotHubConnectionString = iotHubConnenctionString;
            this.maxCountOfDevices = devicesCount;
            this.protocolGatewayHostName = protocolGatewayName;
            this.registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);

            hostName = ExtractHostNameFromIotHubConnectionString(iotHubConnectionString);
        }

        /// <summary>
        /// Receive an approximation of the devices in the IoT hub, up to the 
        /// specified max.  This is not a definitive list, and seems to return
        /// only up to around 1000 devices regardless of what you specify as 
        /// the max.
        /// </summary>
        /// <returns></returns>
        public async Task<List<DeviceEntity>> GetABunchOfDevices()
        {
            listOfDevices.Clear();
            IEnumerable<Device> devices = await registryManager.GetDevicesAsync(maxCountOfDevices);
            if (devices == null) return listOfDevices;

            foreach (Device device in devices)
            {
                DeviceEntity deviceEntity = MapDeviceToDeviceEntity(device);
                listOfDevices.Add(deviceEntity);
            }

            return listOfDevices;
        }

        /// <summary>
        /// Receive the ID of every device in the IoT hub.
        /// </summary>
        /// <remarks>
        /// This method returns only the IDs of the devices because going out and 
        /// fetching the rest of the data for every of tens of thousands of devices
        /// takes too long.  Instead, this data should be fetched when the user needs
        /// it or when they're not waiting.
        /// </remarks>
        public async Task<List<string>> GetAllDeviceIds()
        {
            IQuery query = registryManager.CreateQuery("SELECT * FROM devices");
            var deviceIds = new List<string>();

            while (query.HasMoreResults)
            {
                IEnumerable<Twin> batch = await query.GetNextAsTwinAsync();
                deviceIds.AddRange(batch.Select(item => item.DeviceId));
            }

            return deviceIds;
        }

        public async Task<DeviceEntity> GetDeviceById(string deviceId)
        {
            Device device = await registryManager.GetDeviceAsync(deviceId);
            return MapDeviceToDeviceEntity(device);
        }

        private DeviceEntity MapDeviceToDeviceEntity(Device device)
        {
            return new DeviceEntity
            {
                Id = device.Id,
                PrimaryKey           = device.Authentication?.SymmetricKey.PrimaryKey,
                SecondaryKey         = device.Authentication?.SymmetricKey.SecondaryKey,
                PrimaryThumbPrint    = device.Authentication?.X509Thumbprint?.PrimaryThumbprint,
                SecondaryThumbPrint  = device.Authentication?.X509Thumbprint?.SecondaryThumbprint,
                ConnectionState      = device.ConnectionState.ToString(),
                ConnectionString     = CreateDeviceConnectionString(device),
                LastActivityTime     = device.LastActivityTime,
                LastStateUpdatedTime = device.StatusUpdatedTime,
                MessageCount         = device.CloudToDeviceMessageCount,
                State                = device.Status.ToString(),
                SuspensionReason     = device.StatusReason,
                LastConnectionStateUpdatedTime = device.ConnectionStateUpdatedTime,
            };
        }

        private string CreateDeviceConnectionString(Device device)
        {
            var deviceConnectionString = new StringBuilder();
            if (string.IsNullOrWhiteSpace(hostName)) return string.Empty;

            deviceConnectionString.Append(hostName);
            deviceConnectionString.AppendFormat("DeviceId={0}", device.Id);

            if (device.Authentication != null)
            {
                if (device.Authentication.SymmetricKey?.PrimaryKey != null)
                {
                    deviceConnectionString.AppendFormat(";SharedAccessKey={0}", device.Authentication.SymmetricKey.PrimaryKey);
                }
                else
                {
                    deviceConnectionString.AppendFormat(";x509=true");
                }
            }

            if (protocolGatewayHostName.Length > 0)
            {
                deviceConnectionString.AppendFormat(";GatewayHostName=ssl://{0}:8883", protocolGatewayHostName);
            }

            return deviceConnectionString.ToString();
        }


        private static string ExtractHostNameFromIotHubConnectionString(string iotHubConnectionString)
        {
            string[] tokenArray = iotHubConnectionString.Split(';');

            return (
                from t in tokenArray
                let keyValueArray = t.Split('=')
                where keyValueArray[0] == "HostName"
                select t + ';'
            ).FirstOrDefault();
        }



        // For testing without connecting to a live service
        public static List<DeviceEntity> GetDevicesForTest()
        {
            return new List<DeviceEntity>
            {
                new DeviceEntity { Id = "TestDevice01", PrimaryKey = "TestPrimKey01", SecondaryKey = "TestSecKey01" },
                new DeviceEntity { Id = "TestDevice02", PrimaryKey = "TestPrimKey02", SecondaryKey = "TestSecKey02" },
                new DeviceEntity { Id = "TestDevice03", PrimaryKey = "TestPrimKey03", SecondaryKey = "TestSecKey03" },
                new DeviceEntity { Id = "TestDevice04", PrimaryKey = "TestPrimKey04", SecondaryKey = "TestSecKey04" },
                new DeviceEntity { Id = "TestDevice05", PrimaryKey = "TestPrimKey05", SecondaryKey = "TestSecKey05" }
            };
        }
    }
}
