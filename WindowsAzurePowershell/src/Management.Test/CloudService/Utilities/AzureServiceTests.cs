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

namespace Microsoft.WindowsAzure.Management.Test.CloudService.Utilities
{
    using System;
    using System.IO;
    using System.Linq;
    using Microsoft.WindowsAzure.Management.CloudService.Development.Scaffolding;
    using Microsoft.WindowsAzure.Management.Test.Utilities.CloudService;
    using Microsoft.WindowsAzure.Management.Test.Utilities.Common;
    using Microsoft.WindowsAzure.Management.Utilities.CloudService;
    using Microsoft.WindowsAzure.Management.Utilities.Properties;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AzureServiceTests: TestBase
    {
        private const string serviceName = "AzureService";

        [TestMethod]
        public void AzureServiceCreateNew()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                AzureService service = new AzureService(files.RootPath, serviceName, null);
                AzureAssert.AzureServiceExists(Path.Combine(files.RootPath, serviceName), Resources.GeneralScaffolding, serviceName);
            }
        }

        [TestMethod]
        public void AzureServiceCreateNewEmptyParentDirectoryFail()
        {
            Testing.AssertThrows<ArgumentException>(() => new AzureService(string.Empty, serviceName, null), string.Format(Resources.InvalidOrEmptyArgumentMessage, "service parent directory"));
        }

        [TestMethod]
        public void AzureServiceCreateNewNullParentDirectoryFail()
        {
            Testing.AssertThrows<ArgumentException>(() => new AzureService(null, serviceName, null), string.Format(Resources.InvalidOrEmptyArgumentMessage, "service parent directory"));
        }

        [TestMethod]
        public void AzureServiceCreateNewInvalidParentDirectoryFail()
        {
            foreach (string invalidName in Data.InvalidFileName)
            {
                Testing.AssertThrows<ArgumentException>(() => new AzureService(string.Empty, serviceName, null), string.Format(Resources.InvalidOrEmptyArgumentMessage, "service parent directory"));
            }
        }

        [TestMethod]
        public void AzureServiceCreateNewDoesNotExistParentDirectoryFail()
        {
            Testing.AssertThrows<FileNotFoundException>(() => new AzureService("DoesNotExist", serviceName, null), string.Format(Resources.PathDoesNotExistForElement, Resources.ServiceParentDirectory, "DoesNotExist"));
        }

        [TestMethod]
        public void AzureServiceCreateNewEmptyServiceNameFail()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                Testing.AssertThrows<ArgumentException>(() => new AzureService(files.RootPath, string.Empty, null), string.Format(Resources.InvalidOrEmptyArgumentMessage, "Name"));
            }
        }

        [TestMethod]
        public void AzureServiceCreateNewNullServiceNameFail()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                Testing.AssertThrows<ArgumentException>(() => new AzureService(files.RootPath, null, null), string.Format(Resources.InvalidOrEmptyArgumentMessage, "Name"));
            }
        }

        [TestMethod]
        public void AzureServiceCreateNewInvalidServiceNameFail()
        {
            foreach (string invalidFileName in Data.InvalidFileName)
            {
                using (FileSystemHelper files = new FileSystemHelper(this))
                {
                    Testing.AssertThrows<ArgumentException>(() => new AzureService(files.RootPath, invalidFileName, null), string.Format(Resources.InvalidFileName, "Name"));
                }
            }
        }

        [TestMethod]
        public void AzureServiceCreateNewInvalidDnsServiceNameFail()
        {
            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();

            foreach (string invalidDnsName in Data.InvalidServiceNames)
            {
                using (FileSystemHelper files = new FileSystemHelper(this))
                {
                    // This case is handled in AzureServiceCreateNewInvalidDnsServiceNameFail test
                    //
                    if (invalidFileNameChars.Any(c => invalidFileNameChars.Contains<char>(c))) continue;
                    Testing.AssertThrows<ArgumentException>(() => new AzureService(files.RootPath, invalidDnsName, null), string.Format(Resources.InvalidDnsName, invalidDnsName, "Name"));
                }
            }
        }

        [TestMethod]
        public void AzureServiceCreateNewExistingServiceFail()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                new AzureService(files.RootPath, serviceName, null);
                Testing.AssertThrows<ArgumentException>(() => new AzureService(files.RootPath, serviceName, null), string.Format(Resources.ServiceAlreadyExistsOnDisk, serviceName, Path.Combine(files.RootPath, serviceName)));
            }
        }
        
        [TestMethod]
        public void AzureNodeServiceLoadExistingSimpleService()
        {
            AddNodeRoleTest(0, 0, 0);
        }

        [TestMethod]
        public void AzureNodeServiceLoadExistingOneWebRoleService()
        {
            AddNodeRoleTest(1, 0, 0);
        }

        [TestMethod]
        public void AzureNodeServiceLoadExistingOneWorkerRoleService()
        {
            AddNodeRoleTest(0, 1, 0);
        }

        [TestMethod]
        public void AzureNodeServiceLoadExistingMultipleWebRolesService()
        {
            AddNodeRoleTest(5, 0, 0);
        }

        [TestMethod]
        public void AzureNodeServiceLoadExistingMultipleWorkerRolesService()
        {
            AddNodeRoleTest(0, 5, 0);
        }

        [TestMethod]
        public void AzureNodeServiceLoadExistingMultipleWebAndOneWorkerRolesService()
        {
            int order = 0;
            AddNodeRoleTest(3, 4, order++);
            AddNodeRoleTest(2, 4, order++);
            AddNodeRoleTest(4, 2, order++);
            AddNodeRoleTest(3, 5, order++);
        }

        [TestMethod]
        public void AzurePHPServiceLoadExistingSimpleService()
        {
            AddPHPRoleTest(0, 0, 0);
        }

        [TestMethod]
        public void AzurePHPServiceLoadExistingOneWebRoleService()
        {
            AddPHPRoleTest(1, 0, 0);
        }

        [TestMethod]
        public void AzurePHPServiceLoadExistingOneWorkerRoleService()
        {
            AddPHPRoleTest(0, 1, 0);
        }

        [TestMethod]
        public void AzurePHPServiceLoadExistingMultipleWebRolesService()
        {
            AddPHPRoleTest(5, 0, 0);
        }

        [TestMethod]
        public void AzurePHPServiceLoadExistingMultipleWorkerRolesService()
        {
            AddPHPRoleTest(0, 5, 0);
        }

        [TestMethod]
        public void AzurePHPServiceLoadExistingMultipleWebAndOneWorkerRolesService()
        {
            int order = 0;
            AddPHPRoleTest(3, 4, order++);
            AddPHPRoleTest(2, 4, order++);
            AddPHPRoleTest(4, 2, order++);
            AddPHPRoleTest(3, 5, order++);
        }

        
        [TestMethod]
        public void AzureServiceAddNewNodeWebRoleTest()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                AzureService service = new AzureService(files.RootPath, serviceName, null);
                RoleInfo webRole = service.AddWebRole(Data.NodeWebRoleScaffoldingPath, "MyWebRole", 10);

                AzureAssert.AzureServiceExists(Path.Combine(files.RootPath, serviceName), Resources.GeneralScaffolding, serviceName, webRoles: new WebRoleInfo[] { (WebRoleInfo)webRole }, webScaff: Path.Combine(Resources.NodeScaffolding, Resources.WebRole), roles: new RoleInfo[] { webRole });
            }
        }

        [TestMethod]
        public void AzureServiceAddNewPHPWebRoleTest()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                AzureService service = new AzureService(files.RootPath, serviceName, null);
                RoleInfo webRole = service.AddWebRole(Data.PHPWebRoleScaffoldingPath, "MyWebRole", 10);

                AzureAssert.AzureServiceExists(Path.Combine(files.RootPath, serviceName), Resources.GeneralScaffolding, serviceName, webRoles: new WebRoleInfo[] { (WebRoleInfo)webRole }, webScaff: Path.Combine(Resources.PHPScaffolding, Resources.WebRole), roles: new RoleInfo[] { webRole });
            }
        }

        [TestMethod]
        public void AzureServiceAddNewNodeWorkerRoleTest()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                AzureService service = new AzureService(files.RootPath, serviceName, null);
                RoleInfo workerRole = service.AddWorkerRole(Data.NodeWorkerRoleScaffoldingPath, "MyWorkerRole", 10);

                AzureAssert.AzureServiceExists(Path.Combine(files.RootPath, serviceName), Resources.GeneralScaffolding, serviceName, workerRoles: new WorkerRoleInfo[] { (WorkerRoleInfo)workerRole }, workerScaff: Path.Combine(Resources.NodeScaffolding, Resources.WorkerRole), roles: new RoleInfo[] { workerRole });
            }
        }

        [TestMethod]
        public void AzureServiceAddNewPHPWorkerRoleTest()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                AzureService service = new AzureService(files.RootPath, serviceName, null);
                RoleInfo workerRole = service.AddWorkerRole(Data.PHPWorkerRoleScaffoldingPath, "MyWorkerRole", 10);

                AzureAssert.AzureServiceExists(Path.Combine(files.RootPath, serviceName), Resources.GeneralScaffolding, serviceName, workerRoles: new WorkerRoleInfo[] { (WorkerRoleInfo)workerRole }, workerScaff: Path.Combine(Resources.PHPScaffolding, Resources.WorkerRole), roles: new RoleInfo[] { workerRole });
            }
        }

        [TestMethod]
        public void AzureServiceAddNewNodeWorkerRoleWithWhiteCharFail()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                Testing.AssertThrows<ArgumentException>(() => new AzureService(files.RootPath, serviceName, null).AddWebRole(Data.NodeWebRoleScaffoldingPath, "\tRole"), string.Format(Resources.InvalidRoleNameMessage, "\tRole"));
            }
        }

        [TestMethod]
        public void AzureServiceAddNewPHPWorkerRoleWithWhiteCharFail()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                Testing.AssertThrows<ArgumentException>(() => new AzureService(files.RootPath, serviceName, null).AddWebRole(Data.PHPWebRoleScaffoldingPath, "\tRole"), string.Format(Resources.InvalidRoleNameMessage, "\tRole"));
            }
        }

        [TestMethod]
        public void AzureServiceAddExistingNodeRoleFail()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                AzureService service = new AzureService(files.RootPath, serviceName, null);
                service.AddWebRole(Data.NodeWebRoleScaffoldingPath, "WebRole");
                Testing.AssertThrows<ArgumentException>(() => service.AddWebRole(Data.NodeWebRoleScaffoldingPath, "WebRole"), string.Format(Resources.AddRoleMessageRoleExists, "WebRole"));
            }
        }

        [TestMethod]
        public void AzureServiceAddExistingPHPRoleFail()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                AzureService service = new AzureService(files.RootPath, serviceName, null);
                service.AddWebRole(Data.PHPWebRoleScaffoldingPath, "WebRole");
                Testing.AssertThrows<ArgumentException>(() => service.AddWebRole(Data.PHPWebRoleScaffoldingPath, "WebRole"), string.Format(Resources.AddRoleMessageRoleExists, "WebRole"));
            }
        }

        [TestMethod]
        public void GetServiceNameTest()
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                NewAzureServiceProjectCommand newServiceCmdlet = new NewAzureServiceProjectCommand();
                newServiceCmdlet.CommandRuntime = new MockCommandRuntime();
                newServiceCmdlet.NewAzureServiceProcess(files.RootPath, serviceName);
                Assert.AreEqual<string>(serviceName, new AzureService(Path.Combine(files.RootPath, serviceName), null).ServiceName);
            }
        }

        [TestMethod]
        public void ChangeServiceNameTest()
        {
            string newName = "NodeAppService";

            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                AzureService service = new AzureService(files.RootPath, serviceName, null);
                service.ChangeServiceName(newName, service.Paths);
                Assert.AreEqual<string>(newName, service.Components.CloudConfig.serviceName);
                Assert.AreEqual<string>(newName, service.Components.LocalConfig.serviceName);
                Assert.AreEqual<string>(newName, service.Components.Definition.name);
            }
        }

        [TestMethod]
        public void SetNodeRoleInstancesTest()
        {
            int newInstances = 10;

            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                AzureService service = new AzureService(files.RootPath, serviceName, null);
                service.AddWebRole(Data.NodeWebRoleScaffoldingPath, "WebRole", 1);
                service.SetRoleInstances(service.Paths, "WebRole", newInstances);
                Assert.AreEqual<int>(service.Components.CloudConfig.Role[0].Instances.count, newInstances);
                Assert.AreEqual<int>(service.Components.LocalConfig.Role[0].Instances.count, newInstances);
            }
        }

        [TestMethod]
        public void SetPHPRoleInstancesTest()
        {
            int newInstances = 10;

            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                AzureService service = new AzureService(files.RootPath, serviceName, null);
                service.AddWebRole(Data.PHPWebRoleScaffoldingPath, "WebRole", 1);
                service.SetRoleInstances(service.Paths, "WebRole", newInstances);
                Assert.AreEqual<int>(service.Components.CloudConfig.Role[0].Instances.count, newInstances);
                Assert.AreEqual<int>(service.Components.LocalConfig.Role[0].Instances.count, newInstances);
            }
        }

        /// <summary>
        /// This method handles most possible cases that user can do to create role
        /// </summary>
        /// <param name="webRole">Count of web roles to add</param>
        /// <param name="workerRole">Count of worker role to add</param>
        /// <param name="addWebBeforeWorker">Decides in what order to add roles. There are three options, note that value between brackets is the value to pass:
        /// 1. Web then, interleaving (0): interleave adding and start by adding web role first.
        /// 2. Worker then, interleaving (1): interleave adding and start by adding worker role first.
        /// 3. Web then worker (2): add all web roles then worker roles.
        /// 4. Worker then web (3): add all worker roles then web roles.
        /// By default this parameter is set to 0
        /// </param>
        private void AddNodeRoleTest(int webRole, int workerRole, int order = 0)
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                AzureServiceWrapper wrappedService = new AzureServiceWrapper(files.RootPath, serviceName, null);
                AzureService service = new AzureService(Path.Combine(files.RootPath, serviceName), null);
                
                WebRoleInfo[] webRoles = null;
                if (webRole > 0)
                {
                     webRoles = new WebRoleInfo[webRole];
                     for (int i = 0; i < webRoles.Length; i++)
                     {
                         webRoles[i] = new WebRoleInfo(string.Format("{0}{1}", Resources.WebRole, i + 1), 1);
                     }
                }


                WorkerRoleInfo[] workerRoles = null;
                if (workerRole > 0)
                {
                    workerRoles = new WorkerRoleInfo[workerRole];
                    for (int i = 0; i < workerRoles.Length; i++)
                    {
                        workerRoles[i] = new WorkerRoleInfo(string.Format("{0}{1}", Resources.WorkerRole, i + 1), 1);
                    }
                }

                RoleInfo[] roles = (webRole + workerRole > 0) ? new RoleInfo[webRole + workerRole] : null;
                if (order == 0)
                {
                    for (int i = 0, w = 0, wo = 0; i < webRole + workerRole;)
                    {
                        if (w++ < webRole) roles[i++] = wrappedService.AddWebRole(Data.NodeWebRoleScaffoldingPath);
                        if (wo++ < workerRole) roles[i++] = wrappedService.AddWorkerRole(Data.NodeWorkerRoleScaffoldingPath);
                    }
                }
                else if (order == 1)
                {
                    for (int i = 0, w = 0, wo = 0; i < webRole + workerRole;)
                    {
                        if (wo++ < workerRole) roles[i++] = wrappedService.AddWorkerRole(Data.NodeWorkerRoleScaffoldingPath);
                        if (w++ < webRole) roles[i++] = wrappedService.AddWebRole(Data.NodeWebRoleScaffoldingPath);
                    }
                }
                else if (order == 2)
                {
                    wrappedService.AddRole(Data.NodeWebRoleScaffoldingPath, Data.NodeWorkerRoleScaffoldingPath, webRole, workerRole);
                    webRoles.CopyTo(roles, 0);
                    Array.Copy(workerRoles, 0, roles, webRole, workerRoles.Length);
                }
                else if (order == 3)
                {
                    wrappedService.AddRole(Data.NodeWebRoleScaffoldingPath, Data.NodeWorkerRoleScaffoldingPath, 0, workerRole);
                    workerRoles.CopyTo(roles, 0);
                    wrappedService.AddRole(Data.NodeWebRoleScaffoldingPath, Data.NodeWorkerRoleScaffoldingPath, webRole, 0);
                    Array.Copy(webRoles, 0, roles, workerRole, webRoles.Length);
                }
                else
                {
                    throw new ArgumentException("value for order parameter is unknown");
                }

                AzureAssert.AzureServiceExists(Path.Combine(files.RootPath, serviceName), Resources.GeneralScaffolding, serviceName, webRoles: webRoles, workerRoles: workerRoles, webScaff: Data.NodeWebRoleScaffoldingPath, workerScaff: Data.NodeWorkerRoleScaffoldingPath, roles: roles);
            }
        }

        private void AddPHPRoleTest(int webRole, int workerRole, int order = 0)
        {
            using (FileSystemHelper files = new FileSystemHelper(this))
            {
                AzureServiceWrapper wrappedService = new AzureServiceWrapper(files.RootPath, serviceName, null);
                AzureService service = new AzureService(Path.Combine(files.RootPath, serviceName), null);

                WebRoleInfo[] webRoles = null;
                if (webRole > 0)
                {
                    webRoles = new WebRoleInfo[webRole];
                    for (int i = 0; i < webRoles.Length; i++)
                    {
                        webRoles[i] = new WebRoleInfo(string.Format("{0}{1}", Resources.WebRole, i + 1), 1);
                    }
                }


                WorkerRoleInfo[] workerRoles = null;
                if (workerRole > 0)
                {
                    workerRoles = new WorkerRoleInfo[workerRole];
                    for (int i = 0; i < workerRoles.Length; i++)
                    {
                        workerRoles[i] = new WorkerRoleInfo(string.Format("{0}{1}", Resources.WorkerRole, i + 1), 1);
                    }
                }

                RoleInfo[] roles = (webRole + workerRole > 0) ? new RoleInfo[webRole + workerRole] : null;
                if (order == 0)
                {
                    for (int i = 0, w = 0, wo = 0; i < webRole + workerRole; )
                    {
                        if (w++ < webRole) roles[i++] = wrappedService.AddWebRole(Data.PHPWebRoleScaffoldingPath);
                        if (wo++ < workerRole) roles[i++] = wrappedService.AddWorkerRole(Data.PHPWorkerRoleScaffoldingPath);
                    }
                }
                else if (order == 1)
                {
                    for (int i = 0, w = 0, wo = 0; i < webRole + workerRole; )
                    {
                        if (wo++ < workerRole) roles[i++] = wrappedService.AddWorkerRole(Data.PHPWorkerRoleScaffoldingPath);
                        if (w++ < webRole) roles[i++] = wrappedService.AddWebRole(Data.PHPWebRoleScaffoldingPath);
                    }
                }
                else if (order == 2)
                {
                    wrappedService.AddRole(Data.PHPWebRoleScaffoldingPath, Data.PHPWorkerRoleScaffoldingPath, webRole, workerRole);
                    webRoles.CopyTo(roles, 0);
                    Array.Copy(workerRoles, 0, roles, webRole, workerRoles.Length);
                }
                else if (order == 3)
                {
                    wrappedService.AddRole(Data.PHPWebRoleScaffoldingPath, Data.PHPWorkerRoleScaffoldingPath, 0, workerRole);
                    workerRoles.CopyTo(roles, 0);
                    wrappedService.AddRole(Data.PHPWebRoleScaffoldingPath, Data.PHPWorkerRoleScaffoldingPath, webRole, 0);
                    Array.Copy(webRoles, 0, roles, workerRole, webRoles.Length);
                }
                else
                {
                    throw new ArgumentException("value for order parameter is unknown");
                }

                AzureAssert.AzureServiceExists(Path.Combine(files.RootPath, serviceName), Resources.GeneralScaffolding, serviceName, webRoles: webRoles, workerRoles: workerRoles, webScaff: Data.PHPWebRoleScaffoldingPath, workerScaff: Data.PHPWorkerRoleScaffoldingPath, roles: roles);
            }
        }
    }
}