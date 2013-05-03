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
    using System.IO;
    using System.Management.Automation;
    using Microsoft.WindowsAzure.Management.CloudService.Development.Scaffolding;
    using Microsoft.WindowsAzure.Management.Test.Utilities.Common;
    using Microsoft.WindowsAzure.Management.Utilities.CloudService;
    using Microsoft.WindowsAzure.Management.Utilities.Common;
    using Microsoft.WindowsAzure.Management.Utilities.Properties;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AddAzureWebRoleTests : TestBase
    {
        private MockCommandRuntime mockCommandRuntime;

        private AddAzureWebRoleCommand addWebCmdlet;

        [TestInitialize]
        public void SetupTest()
        {
            GlobalPathInfo.GlobalSettingsDirectory = Data.AzureSdkAppDir;
            CmdletSubscriptionExtensions.SessionManager = new InMemorySessionManager();
            mockCommandRuntime = new MockCommandRuntime();
        }

        [TestMethod]
        public void AddAzureWebRoleProcess()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                string roleName = "WebRole1";
                string serviceName = "AzureService";
                string rootPath = files.CreateNewService(serviceName);
                string expectedVerboseMessage = string.Format(Resources.AddRoleMessageCreate, rootPath, roleName);
                string originalDirectory = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(rootPath);
                addWebCmdlet = new AddAzureWebRoleCommand() { RootPath = rootPath, CommandRuntime = mockCommandRuntime, Name = roleName };

                addWebCmdlet.ExecuteCmdlet();

                AzureAssert.ScaffoldingExists(Path.Combine(files.RootPath, "AzureService", roleName), Path.Combine(Resources.GeneralScaffolding, Resources.WebRole));
                Assert.AreEqual<string>(roleName, ((PSObject)mockCommandRuntime.OutputPipeline[0]).GetVariableValue<string>(Parameters.RoleName));
                Assert.AreEqual<string>(expectedVerboseMessage, mockCommandRuntime.VerboseStream[0]);

                Directory.SetCurrentDirectory(originalDirectory);
            }
        }

        [TestMethod]
        public void AddAzureWebRoleWillRecreateDeploymentSettings()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                string roleName = "WebRole1";
                string serviceName = "AzureService";
                string rootPath = files.CreateNewService(serviceName);
                string expectedVerboseMessage = string.Format(Resources.AddRoleMessageCreate, rootPath, roleName);
                string settingsFilePath = Path.Combine(rootPath, Resources.SettingsFileName);
                string originalDirectory = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(rootPath);
                File.Delete(settingsFilePath);
                Assert.IsFalse(File.Exists(settingsFilePath));
                addWebCmdlet = new AddAzureWebRoleCommand() { RootPath = rootPath, CommandRuntime = mockCommandRuntime, Name = roleName };

                addWebCmdlet.ExecuteCmdlet();

                AzureAssert.ScaffoldingExists(Path.Combine(files.RootPath, "AzureService", roleName), Path.Combine(Resources.GeneralScaffolding, Resources.WebRole));
                Assert.AreEqual<string>(roleName, ((PSObject)mockCommandRuntime.OutputPipeline[0]).GetVariableValue<string>(Parameters.RoleName));
                Assert.AreEqual<string>(expectedVerboseMessage, mockCommandRuntime.VerboseStream[0]);
                Assert.IsTrue(File.Exists(settingsFilePath));

                Directory.SetCurrentDirectory(originalDirectory);
            }
        }

        [TestMethod]
        public void AddAzureWebRoleWithTemplateFolder()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                string roleName = "WebRole1";
                string serviceName = "AzureService";
                string rootPath = files.CreateNewService(serviceName);
                string expectedVerboseMessage = string.Format(Resources.AddRoleMessageCreate, rootPath, roleName);
                string originalDirectory = Directory.GetCurrentDirectory();
                string scaffoldingPath = "MyWebTemplateFolder";
                Directory.SetCurrentDirectory(rootPath);
                addWebCmdlet = new AddAzureWebRoleCommand() { RootPath = rootPath, CommandRuntime = mockCommandRuntime, Name = roleName, TemplateFolder = scaffoldingPath };

                addWebCmdlet.ExecuteCmdlet();

                AzureAssert.ScaffoldingExists(Path.Combine(rootPath, roleName), scaffoldingPath);
                Assert.AreEqual<string>(roleName, ((PSObject)mockCommandRuntime.OutputPipeline[0]).GetVariableValue<string>(Parameters.RoleName));
                Assert.AreEqual<string>(expectedVerboseMessage, mockCommandRuntime.VerboseStream[0]);

                Directory.SetCurrentDirectory(originalDirectory);
            }
        }

        [TestMethod]
        public void AddAzureWebRoleWithMissingScaffoldXmlFail()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                string roleName = "WebRole1";
                string serviceName = "AzureService";
                string rootPath = files.CreateNewService(serviceName);
                string expectedVerboseMessage = string.Format(Resources.AddRoleMessageCreate, rootPath, roleName);
                string originalDirectory = Directory.GetCurrentDirectory();
                string scaffoldingPath = "TemplateMissingScaffoldXml";
                addWebCmdlet = new AddAzureWebRoleCommand() { RootPath = rootPath, CommandRuntime = mockCommandRuntime, Name = roleName, TemplateFolder = scaffoldingPath };

                Testing.AssertThrows<FileNotFoundException>(() => addWebCmdlet.ExecuteCmdlet());
            }
        }
    }
}
