﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Management.Utilities.CloudService
{
    using System;
    using Microsoft.WindowsAzure.Management.Utilities.Common.XmlSchema.ServiceConfigurationSchema;
    using Microsoft.WindowsAzure.Management.Utilities.Properties;
    using Microsoft.WindowsAzure.ServiceManagement;
    using ConfigConfigurationSetting = Microsoft.WindowsAzure.Management.Utilities.Common.XmlSchema.ServiceConfigurationSchema.ConfigurationSetting;


    class CachingStorageConnectionStringUpdater : IPublishListener
    {
        public void OnPublish(IServiceManagement channel, AzureService service, ServiceSettings publishSettings, string subscriptionId)
        {
            StorageService storageService = channel.GetStorageKeys(subscriptionId, publishSettings.StorageAccountName);
            string name = publishSettings.StorageAccountName;
            string key = storageService.StorageServiceKeys.Primary;

            ConfigConfigurationSetting connectionStringConfig = new ConfigConfigurationSetting { name = Resources.CachingConfigStoreConnectionStringSettingName, value = string.Empty };
            service.Components.ForEachRoleSettings(
            r => Array.Exists<ConfigConfigurationSetting>(r.ConfigurationSettings, c => c.Equals(connectionStringConfig)),
            delegate(RoleSettings r)
            {
                int index = Array.IndexOf<ConfigConfigurationSetting>(r.ConfigurationSettings, connectionStringConfig);
                r.ConfigurationSettings[index] = new ConfigConfigurationSetting
                {
                    name = Resources.CachingConfigStoreConnectionStringSettingName,
                    value = string.Format(Resources.CachingConfigStoreConnectionStringSettingValue, name, key)
                };
            });

            service.Components.Save(service.Paths);
        }
    }
}
