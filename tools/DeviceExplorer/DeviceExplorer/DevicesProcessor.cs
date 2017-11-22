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
        private String iotHubConnectionString;
        private int maxCountOfDevices;
        private String protocolGatewayHostName;

        private string hostName;
        public string HostName
        {
            get
            {
                if (hostName == null)
                {
                    hostName = ExtractHostNameFromIotHubConnectionString(iotHubConnectionString);
                }
                return hostName;
            }
        }

        public DevicesProcessor(string iotHubConnenctionString, int devicesCount, string protocolGatewayName)
        {
            this.listOfDevices = new List<DeviceEntity>();
            this.iotHubConnectionString = iotHubConnenctionString;
            this.maxCountOfDevices = devicesCount;
            this.protocolGatewayHostName = protocolGatewayName;
            this.registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
        }

        public async Task<List<DeviceEntity>> GetDevices()
        {
            listOfDevices.Clear();
            IEnumerable<Device> devices = await registryManager.GetDevicesAsync(maxCountOfDevices);
            if (devices == null) return listOfDevices;

            foreach (Device device in devices)
            {
                DeviceEntity deviceEntity = MapDeviceToDeviceEntity(device);
                listOfDevices.Add(deviceEntity);
            }

            await GetAllDevices();

            return listOfDevices;
        }

        public async Task<List<DeviceEntity>> GetAllDevices()
        {
            listOfDevices.Clear();
            IQuery query = registryManager.CreateQuery("SELECT * FROM devices");

            while (query.HasMoreResults)
            {
                IEnumerable<Twin> batch = await query.GetNextAsTwinAsync();

                foreach (Twin item in batch)
                {
                    listOfDevices.Add(MapTwinToDeviceEntity(item));
                }
            }

            return listOfDevices;
        }

        public async Task<DeviceEntity> GetDeviceById(string deviceId)
        {
            Device device = await registryManager.GetDeviceAsync(deviceId);
            return MapDeviceToDeviceEntity(device);
        }

        private DeviceEntity MapTwinToDeviceEntity(Twin twin)
        {
            return new DeviceEntity
            {
                Id = twin.DeviceId,
            };
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

        private String CreateDeviceConnectionString(Device device)
        {
            StringBuilder deviceConnectionString = new StringBuilder();

            if (!String.IsNullOrWhiteSpace(hostName))
            {
                deviceConnectionString.Append(hostName);
                deviceConnectionString.AppendFormat("DeviceId={0}", device.Id);

                if (device.Authentication != null)
                {
                    if ((device.Authentication.SymmetricKey != null) && (device.Authentication.SymmetricKey.PrimaryKey != null))
                    {
                        deviceConnectionString.AppendFormat(";SharedAccessKey={0}", device.Authentication.SymmetricKey.PrimaryKey);
                    }
                    else
                    {
                        deviceConnectionString.AppendFormat(";x509=true");
                    }
                }

                if (this.protocolGatewayHostName.Length > 0)
                {
                    deviceConnectionString.AppendFormat(";GatewayHostName=ssl://{0}:8883", this.protocolGatewayHostName);
                }
            }
            
            return deviceConnectionString.ToString();
        }


        private static string ExtractHostNameFromIotHubConnectionString(string iotHubConnectionString)
        {
            string hostName = String.Empty;
            string[] tokenArray = iotHubConnectionString.Split(';');

            foreach (string t in tokenArray) {
                string[] keyValueArray = t.Split('=');
                if (keyValueArray[0] == "HostName")
                {
                    hostName = t + ';';
                    return hostName;
                }
            }

            return null;
        }



        // For testing without connecting to a live service
        static public List<DeviceEntity> GetDevicesForTest()
        {
            List<DeviceEntity> deviceList;
            deviceList = new List<DeviceEntity>();
            deviceList.Add(new DeviceEntity() { Id = "TestDevice01", PrimaryKey = "TestPrimKey01", SecondaryKey = "TestSecKey01" });
            deviceList.Add(new DeviceEntity() { Id = "TestDevice02", PrimaryKey = "TestPrimKey02", SecondaryKey = "TestSecKey02" });
            deviceList.Add(new DeviceEntity() { Id = "TestDevice03", PrimaryKey = "TestPrimKey03", SecondaryKey = "TestSecKey03" });
            deviceList.Add(new DeviceEntity() { Id = "TestDevice04", PrimaryKey = "TestPrimKey04", SecondaryKey = "TestSecKey04" });
            deviceList.Add(new DeviceEntity() { Id = "TestDevice05", PrimaryKey = "TestPrimKey05", SecondaryKey = "TestSecKey05" });
            return deviceList;
        }
    }
}
