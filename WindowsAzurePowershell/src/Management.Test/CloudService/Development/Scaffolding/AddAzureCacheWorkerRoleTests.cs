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

namespace Microsoft.WindowsAzure.Management.Test.CloudService.Development.Scaffolding
{
    using System;
    using System.IO;
    using System.Management.Automation;
    using Microsoft.WindowsAzure.Management.CloudService.Development.Scaffolding;
    using Microsoft.WindowsAzure.Management.Test.Utilities.Common;
    using Microsoft.WindowsAzure.Management.Utilities.CloudService;
    using Microsoft.WindowsAzure.Management.Utilities.Common;
    using Microsoft.WindowsAzure.Management.Utilities.Common.XmlSchema.ServiceConfigurationSchema;
    using Microsoft.WindowsAzure.Management.Utilities.Common.XmlSchema.ServiceDefinitionSchema;
    using Microsoft.WindowsAzure.Management.Utilities.Properties;
    using VisualStudio.TestTools.UnitTesting;
    using ConfigConfigurationSetting = Microsoft.WindowsAzure.Management.Utilities.Common.XmlSchema.ServiceConfigurationSchema.ConfigurationSetting;
    using MockCommandRuntime = Microsoft.WindowsAzure.Management.Test.Utilities.Common.MockCommandRuntime;
    using TestBase = Microsoft.WindowsAzure.Management.Test.Utilities.Common.TestBase;
    using Testing = Microsoft.WindowsAzure.Management.Test.Utilities.Common.Testing;

    [TestClass]
    public class AddAzureCacheWorkerRoleTests : TestBase
    {
        private MockCommandRuntime mockCommandRuntime;

        private NewAzureServiceProjectCommand newServiceCmdlet;

        private AddAzureCacheWorkerRoleCommand addCacheRoleCmdlet;

        [TestInitialize]
        public void SetupTest()
        {
            GlobalPathInfo.GlobalSettingsDirectory = Data.AzureSdkAppDir;
            CmdletSubscriptionExtensions.SessionManager = new InMemorySessionManager();
            mockCommandRuntime = new MockCommandRuntime();

            newServiceCmdlet = new NewAzureServiceProjectCommand();
            addCacheRoleCmdlet = new AddAzureCacheWorkerRoleCommand();

            newServiceCmdlet.CommandRuntime = mockCommandRuntime;
            addCacheRoleCmdlet.CommandRuntime = mockCommandRuntime;
        }

        [TestMethod]
        public void AddNewCacheWorkerRoleSuccessful()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                string rootPath = Path.Combine(files.RootPath, "AzureService");
                string roleName = "WorkerRole";
                int expectedInstanceCount = 10;
                newServiceCmdlet.NewAzureServiceProcess(files.RootPath, "AzureService");
                WorkerRole cacheWorkerRole = addCacheRoleCmdlet.AddAzureCacheWorkerRoleProcess(roleName, expectedInstanceCount, rootPath);

                AzureAssert.ScaffoldingExists(Path.Combine(files.RootPath, "AzureService", "WorkerRole"), Path.Combine(Resources.GeneralScaffolding, Resources.WorkerRole));

                AzureAssert.WorkerRoleImportsExists(new Import { moduleName = Resources.CachingModuleName }, cacheWorkerRole);

                AzureAssert.LocalResourcesLocalStoreExists(new LocalStore { name = Resources.CacheDiagnosticStoreName, cleanOnRoleRecycle = false }, 
                    cacheWorkerRole.LocalResources);

                Assert.IsNull(cacheWorkerRole.Endpoints.InputEndpoint);

                AssertConfigExists(Testing.GetCloudRole(rootPath, roleName));
                AssertConfigExists(Testing.GetLocalRole(rootPath, roleName), Resources.EmulatorConnectionString);

                PSObject actualOutput = mockCommandRuntime.OutputPipeline[1] as PSObject;
                Assert.AreEqual<string>(roleName, actualOutput.Members[Parameters.CacheWorkerRoleName].Value.ToString());
                Assert.AreEqual<int>(expectedInstanceCount, int.Parse(actualOutput.Members[Parameters.Instances].Value.ToString()));
            }
        }

        private static void AssertConfigExists(RoleSettings role, string connectionString = "")
        {
            AzureAssert.ConfigurationSettingExist(new ConfigConfigurationSetting { name = Resources.NamedCacheSettingName, value = Resources.NamedCacheSettingValue }, role.ConfigurationSettings);
            AzureAssert.ConfigurationSettingExist(new ConfigConfigurationSetting { name = Resources.DiagnosticLevelName, value = Resources.DiagnosticLevelValue }, role.ConfigurationSettings);
            AzureAssert.ConfigurationSettingExist(new ConfigConfigurationSetting { name = Resources.CachingCacheSizePercentageSettingName, value = string.Empty }, role.ConfigurationSettings);
            AzureAssert.ConfigurationSettingExist(new ConfigConfigurationSetting { name = Resources.CachingConfigStoreConnectionStringSettingName, value = connectionString }, role.ConfigurationSettings);
        }

        [TestMethod]
        public void AddNewCacheWorkerRoleWithInvalidNamesFail()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                string rootPath = Path.Combine(files.RootPath, "AzureService");
                newServiceCmdlet.NewAzureServiceProcess(files.RootPath, "AzureService");

                foreach (string invalidName in Data.InvalidRoleNames)
                {
                    Testing.AssertThrows<ArgumentException>(() => addCacheRoleCmdlet.AddAzureCacheWorkerRoleProcess(invalidName, 1, rootPath));
                }
            }
        }

        [TestMethod]
        public void AddNewCacheWorkerRoleDoesNotHaveAnyRuntime()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                string rootPath = Path.Combine(files.RootPath, "AzureService");
                string roleName = "WorkerRole";
                int expectedInstanceCount = 10;
                newServiceCmdlet.NewAzureServiceProcess(files.RootPath, "AzureService");
                
                WorkerRole cacheWorkerRole = addCacheRoleCmdlet.AddAzureCacheWorkerRoleProcess(roleName, expectedInstanceCount, rootPath);

                Variable runtimeId = Array.Find<Variable>(cacheWorkerRole.Startup.Task[0].Environment, v => v.name.Equals(Resources.RuntimeTypeKey));
                Assert.AreEqual<string>(string.Empty, runtimeId.value);
            }
        }
    }
}
