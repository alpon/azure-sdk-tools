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

namespace Microsoft.WindowsAzure.Management.Test.Websites
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Management.Test.Utilities.Common;
    using Microsoft.WindowsAzure.Management.Test.Utilities.Websites;
    using Microsoft.WindowsAzure.Management.Utilities.Common;
    using Microsoft.WindowsAzure.Management.Utilities.Websites;
    using Microsoft.WindowsAzure.Management.Utilities.Websites.Services.DeploymentEntities;
    using Microsoft.WindowsAzure.Management.Utilities.Websites.Services.WebEntities;
    using Microsoft.WindowsAzure.Management.Websites;
    using Moq;

    [TestClass]
    public class EnableAzureWebsiteDiagnosticTests : WebsitesTestBase
    {
        private const string websiteName = "website1";

        private Mock<IWebsitesClient> websitesClientMock = new Mock<IWebsitesClient>();

        private EnableAzureWebsiteDiagnosticCommand enableAzureWebsiteDiagnosticCommand;

        private Mock<ICommandRuntime> commandRuntimeMock;

        private Dictionary<DiagnosticProperties, object> properties;

        [TestInitialize]
        public override void SetupTest()
        {
            websitesClientMock = new Mock<IWebsitesClient>();
            commandRuntimeMock = new Mock<ICommandRuntime>();
            properties = new Dictionary<DiagnosticProperties, object>();
            properties[DiagnosticProperties.LogLevel] = LogEntryType.Information;
        }

        [TestMethod]
        public void EnableAzureWebsiteDiagnosticSite()
        {
            // Setup
            websitesClientMock.Setup(f => f.EnableSiteDiagnostic(
                websiteName,
                true,
                true,
                true));

            enableAzureWebsiteDiagnosticCommand = new EnableAzureWebsiteDiagnosticCommand()
            {
                ShareChannel = true,
                CommandRuntime = commandRuntimeMock.Object,
                Name = websiteName,
                CurrentSubscription = new SubscriptionData { SubscriptionId = base.subscriptionId },
                WebsitesClient = websitesClientMock.Object,
                WebServerLogging = true,
                DetailedErrorMessages = true,
                FailedRequestTracing = true,
                Type = WebsiteDiagnosticType.Site
            };

            // Test
            enableAzureWebsiteDiagnosticCommand.ExecuteCmdlet();

            // Assert
            websitesClientMock.Verify(f => f.EnableSiteDiagnostic(
                websiteName,
                true,
                true,
                true), Times.Once());

            commandRuntimeMock.Verify(f => f.WriteObject(true), Times.Never());
        }

        [TestMethod]
        public void EnableAzureWebsiteDiagnosticPassThru()
        {
            // Setup
            websitesClientMock.Setup(f => f.EnableSiteDiagnostic(
                websiteName,
                true,
                true,
                true));

            enableAzureWebsiteDiagnosticCommand = new EnableAzureWebsiteDiagnosticCommand()
            {
                ShareChannel = true,
                CommandRuntime = commandRuntimeMock.Object,
                Name = websiteName,
                CurrentSubscription = new SubscriptionData { SubscriptionId = base.subscriptionId },
                WebsitesClient = websitesClientMock.Object,
                WebServerLogging = true,
                DetailedErrorMessages = true,
                FailedRequestTracing = true,
                Type = WebsiteDiagnosticType.Site,
                PassThru = true
            };

            // Test
            enableAzureWebsiteDiagnosticCommand.ExecuteCmdlet();

            // Assert
            websitesClientMock.Verify(f => f.EnableSiteDiagnostic(
                websiteName,
                true,
                true,
                true), Times.Once());

            commandRuntimeMock.Verify(f => f.WriteObject(true), Times.Once());
        }

        [TestMethod]
        public void EnableAzureWebsiteDiagnosticSiteIgnoreSetting()
        {
            // Setup
            websitesClientMock.Setup(f => f.EnableSiteDiagnostic(
                websiteName,
                true,
                false,
                true));

            enableAzureWebsiteDiagnosticCommand = new EnableAzureWebsiteDiagnosticCommand()
            {
                ShareChannel = true,
                CommandRuntime = commandRuntimeMock.Object,
                Name = websiteName,
                CurrentSubscription = new SubscriptionData { SubscriptionId = base.subscriptionId },
                WebsitesClient = websitesClientMock.Object,
                WebServerLogging = true,
                FailedRequestTracing = true,
                Type = WebsiteDiagnosticType.Site
            };

            // Test
            enableAzureWebsiteDiagnosticCommand.ExecuteCmdlet();

            // Assert
            websitesClientMock.Verify(f => f.EnableSiteDiagnostic(
                websiteName,
                true,
                false,
                true), Times.Once());

            commandRuntimeMock.Verify(f => f.WriteObject(true), Times.Never());
        }

        [TestMethod]
        public void EnableAzureWebsiteDiagnosticApplication()
        {
            // Setup
            websitesClientMock.Setup(f => f.EnableApplicationDiagnostic(
                websiteName,
                WebsiteDiagnosticOutput.FileSystem,
                properties));

            enableAzureWebsiteDiagnosticCommand = new EnableAzureWebsiteDiagnosticCommand()
            {
                ShareChannel = true,
                CommandRuntime = commandRuntimeMock.Object,
                Name = websiteName,
                CurrentSubscription = new SubscriptionData { SubscriptionId = base.subscriptionId },
                WebsitesClient = websitesClientMock.Object,
                Type = WebsiteDiagnosticType.Application,
                Output = WebsiteDiagnosticOutput.FileSystem,
                LogLevel = LogEntryType.Information
            };

            // Test
            enableAzureWebsiteDiagnosticCommand.ExecuteCmdlet();

            // Assert
            websitesClientMock.Verify(f => f.EnableApplicationDiagnostic(
                websiteName,
                WebsiteDiagnosticOutput.FileSystem,
                properties), Times.Once());

            commandRuntimeMock.Verify(f => f.WriteObject(true), Times.Never());
        }

        [TestMethod]
        public void EnableAzureWebsiteDiagnosticApplicationTableLog()
        {
            // Setup
            string storageName = "MyStorage";
            properties[DiagnosticProperties.StorageAccountName] = storageName;
            websitesClientMock.Setup(f => f.EnableApplicationDiagnostic(
                websiteName,
                WebsiteDiagnosticOutput.StorageTable,
                properties));

            enableAzureWebsiteDiagnosticCommand = new EnableAzureWebsiteDiagnosticCommand()
            {
                ShareChannel = true,
                CommandRuntime = commandRuntimeMock.Object,
                Name = websiteName,
                CurrentSubscription = new SubscriptionData { SubscriptionId = base.subscriptionId },
                WebsitesClient = websitesClientMock.Object,
                Type = WebsiteDiagnosticType.Application,
                Output = WebsiteDiagnosticOutput.StorageTable,
                LogLevel = LogEntryType.Information,
                StorageAccountName = storageName
            };

            // Test
            enableAzureWebsiteDiagnosticCommand.ExecuteCmdlet();

            // Assert
            websitesClientMock.Verify(f => f.EnableApplicationDiagnostic(
                websiteName,
                WebsiteDiagnosticOutput.StorageTable,
                properties), Times.Once());

            commandRuntimeMock.Verify(f => f.WriteObject(true), Times.Never());
        }

        [TestMethod]
        public void EnableAzureWebsiteDiagnosticApplicationTableLogUseCurrentStorageAccount()
        {
            // Setup
            string storageName = "MyStorage";
            properties[DiagnosticProperties.StorageAccountName] = storageName;
            websitesClientMock.Setup(f => f.EnableApplicationDiagnostic(
                websiteName,
                WebsiteDiagnosticOutput.StorageTable,
                properties));

            enableAzureWebsiteDiagnosticCommand = new EnableAzureWebsiteDiagnosticCommand()
            {
                ShareChannel = true,
                CommandRuntime = commandRuntimeMock.Object,
                Name = websiteName,
                CurrentSubscription = new SubscriptionData
                {
                    SubscriptionId = base.subscriptionId,
                    CurrentStorageAccount = storageName
                },
                WebsitesClient = websitesClientMock.Object,
                Type = WebsiteDiagnosticType.Application,
                Output = WebsiteDiagnosticOutput.StorageTable,
                LogLevel = LogEntryType.Information,
            };

            // Test
            enableAzureWebsiteDiagnosticCommand.ExecuteCmdlet();

            // Assert
            websitesClientMock.Verify(f => f.EnableApplicationDiagnostic(
                websiteName,
                WebsiteDiagnosticOutput.StorageTable,
                properties), Times.Once());

            commandRuntimeMock.Verify(f => f.WriteObject(true), Times.Never());
        }
    }
}
