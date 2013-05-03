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

namespace Microsoft.WindowsAzure.Management.Test.CloudService.Development.Tests.Cmdlet
{
    using System;
    using System.Collections.Generic;
    using Microsoft.WindowsAzure.Management.CloudService.Development;
    using Microsoft.WindowsAzure.Management.Test.Utilities.Common;
    using Microsoft.WindowsAzure.Management.Utilities.CloudService;
    using Microsoft.WindowsAzure.Management.Utilities.Common;
    using Microsoft.WindowsAzure.Management.Utilities.Properties;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SetAzureServiceProjectTests : TestBase
    {
        private MockCommandRuntime mockCommandRuntime;

        private SetAzureServiceProjectCommand setServiceProjectCmdlet;

        [TestInitialize]
        public void SetupTest()
        {
            GlobalPathInfo.GlobalSettingsDirectory = Data.AzureSdkAppDir;
            CmdletSubscriptionExtensions.SessionManager = new InMemorySessionManager();
            mockCommandRuntime = new MockCommandRuntime();

            setServiceProjectCmdlet = new SetAzureServiceProjectCommand();
            setServiceProjectCmdlet.CommandRuntime = mockCommandRuntime;
            setServiceProjectCmdlet.PassThru = true;
        }

        [TestMethod]
        public void SetAzureServiceProjectTestsSubscriptionValid()
        {
            foreach (string item in Data.ValidSubscriptionName)
            {
                using (FileSystemHelper files = new FileSystemHelper(this))
                {
                    // Create new empty settings file
                    //
                    ServicePathInfo paths = new ServicePathInfo(files.RootPath);
                    ServiceSettings settings = new ServiceSettings();
                    settings.Save(paths.Settings);
                    setServiceProjectCmdlet.PassThru = false;

                    settings = setServiceProjectCmdlet.SetAzureServiceProjectProcess(null, null, null, item, paths.Settings);

                    // Assert subscription is changed
                    //
                    Assert.AreEqual<string>(item, settings.Subscription);
                    Assert.AreEqual<int>(0, mockCommandRuntime.OutputPipeline.Count);
                }
            }
        }

        [TestMethod]
        public void SetAzureServiceProjectTestsSubscriptionEmptyFail()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                // Create new empty settings file
                //
                ServicePathInfo paths = new ServicePathInfo(files.RootPath);
                ServiceSettings settings = new ServiceSettings();
                settings.Save(paths.Settings);

                Testing.AssertThrows<ArgumentException>(() => setServiceProjectCmdlet.SetAzureServiceProjectProcess(
                    null,
                    null,
                    null,
                    string.Empty,
                    paths.Settings),
                    string.Format(Resources.InvalidOrEmptyArgumentMessage, "Subscription"));
            }
        }

        [TestMethod]
        public void SetAzureServiceProjectTestsLocationValid()
        {
            string[] locations = { "West US", "East US", "East Asia", "North Europe" };
            foreach (string item in locations)
            {
                using (FileSystemHelper files = new FileSystemHelper(this))
                {
                    // Create new empty settings file
                    //
                    ServicePathInfo paths = new ServicePathInfo(files.RootPath);
                    ServiceSettings settings = new ServiceSettings();
                    mockCommandRuntime = new MockCommandRuntime();
                    setServiceProjectCmdlet.CommandRuntime = mockCommandRuntime;
                    settings.Save(paths.Settings);

                    settings = setServiceProjectCmdlet.SetAzureServiceProjectProcess(item, null, null, null, paths.Settings);

                    // Assert location is changed
                    //
                    Assert.AreEqual<string>(item, settings.Location);
                    ServiceSettings actualOutput = mockCommandRuntime.OutputPipeline[0] as ServiceSettings;
                    Assert.AreEqual<string>(item, settings.Location);
                }
            }
        }

        [TestMethod]
        public void SetAzureServiceProjectTestsLocationEmptyFail()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                // Create new empty settings file
                //
                ServicePathInfo paths = new ServicePathInfo(files.RootPath);
                ServiceSettings settings = new ServiceSettings();
                settings.Save(paths.Settings);

                Testing.AssertThrows<ArgumentException>(() => setServiceProjectCmdlet.SetAzureServiceProjectProcess(string.Empty, null, null, null, paths.Settings), string.Format(Resources.InvalidOrEmptyArgumentMessage, "Location"));
            }
        }

        [TestMethod]
        public void SetAzureServiceProjectTestsUnknownLocation()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                // Create new empty settings file
                //
                ServicePathInfo paths = new ServicePathInfo(files.RootPath);
                ServiceSettings settings = new ServiceSettings();
                settings.Save(paths.Settings);
                string unknownLocation = "Unknown Location";

                settings = setServiceProjectCmdlet.SetAzureServiceProjectProcess(unknownLocation, null, null, null, paths.Settings);

                // Assert location is changed
                //
                Assert.AreEqual<string>(unknownLocation, settings.Location);
                ServiceSettings actualOutput = mockCommandRuntime.OutputPipeline[0] as ServiceSettings;
                Assert.AreEqual<string>(unknownLocation, settings.Location);
            }
        }

        [TestMethod]
        public void SetAzureServiceProjectTestsStorageTests()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                // Create new empty settings file
                //
                ServicePathInfo paths = new ServicePathInfo(files.RootPath);
                ServiceSettings settings = new ServiceSettings();
                mockCommandRuntime = new MockCommandRuntime();
                setServiceProjectCmdlet.CommandRuntime = mockCommandRuntime;
                settings.Save(paths.Settings);

                settings = setServiceProjectCmdlet.SetAzureServiceProjectProcess(null, null, "companystore", null, paths.Settings);

                // Assert storageAccountName is changed
                //
                Assert.AreEqual<string>("companystore", settings.StorageAccountName);
                ServiceSettings actualOutput = mockCommandRuntime.OutputPipeline[0] as ServiceSettings;
                Assert.AreEqual<string>("companystore", settings.StorageAccountName);
            }
        }

        [TestMethod]
        public void SetAzureServiceProjectTestsStorageTestsEmptyFail()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                // Create new empty settings file
                //
                ServicePathInfo paths = new ServicePathInfo(files.RootPath);
                ServiceSettings settings = new ServiceSettings();
                settings.Save(paths.Settings);

                Testing.AssertThrows<ArgumentException>(() => setServiceProjectCmdlet.SetAzureServiceProjectProcess(null, null, string.Empty, null, paths.Settings), string.Format(Resources.InvalidOrEmptyArgumentMessage, "StorageAccountName"));
            }
        }

        [TestMethod]
        public void SetAzureServiceProjectTestsSlotTests()
        {
            foreach (KeyValuePair<SlotType, string> item in ArgumentConstants.Slots)
            {
                using (FileSystemHelper files = new FileSystemHelper(this))
                {
                    // Create new empty settings file
                    //
                    ServicePathInfo paths = new ServicePathInfo(files.RootPath);
                    ServiceSettings settings = new ServiceSettings();
                    settings.Save(paths.Settings);

                    setServiceProjectCmdlet.SetAzureServiceProjectProcess(null, item.Value, null, null, paths.Settings);

                    // Assert slot is changed
                    //
                    settings = ServiceSettings.Load(paths.Settings);
                    Assert.AreEqual<string>(item.Value, settings.Slot);
                }
            }
        }

        [TestMethod]
        public void SetAzureServiceProjectTestsSlotTestsEmptyFail()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                // Create new empty settings file
                //
                ServicePathInfo paths = new ServicePathInfo(files.RootPath);
                ServiceSettings settings = new ServiceSettings();
                settings.Save(paths.Settings);

                Testing.AssertThrows<ArgumentException>(() => setServiceProjectCmdlet.SetAzureServiceProjectProcess(null, string.Empty, null, null, paths.Settings), string.Format(Resources.InvalidOrEmptyArgumentMessage, "Slot"));
            }
        }

        [TestMethod]
        public void SetAzureServiceProjectTestsSlotTestsInvalidFail()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                // Create new empty settings file
                //
                ServicePathInfo paths = new ServicePathInfo(files.RootPath);
                ServiceSettings settings = new ServiceSettings();
                settings.Save(paths.Settings);

                Testing.AssertThrows<ArgumentException>(() => setServiceProjectCmdlet.SetAzureServiceProjectProcess(null, "MyHome", null, null, paths.Settings), string.Format(Resources.InvalidServiceSettingElement, "Slot"));
            }
        }
    }
}
