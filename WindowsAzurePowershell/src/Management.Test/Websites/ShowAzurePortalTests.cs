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
    using Microsoft.WindowsAzure.Management.Test.Utilities.Websites;
    using Microsoft.WindowsAzure.Management.Utilities.Common;
    using Microsoft.WindowsAzure.Management.Websites;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ShowAzurePortalTests : WebsitesTestBase
    {
        [TestMethod]
        public void ProcessGetAzurePublishSettingsTest()
        {
            ShowAzurePortalCommand showAzurePortalCommand = new ShowAzurePortalCommand { Name = null };
            showAzurePortalCommand.ProcessShowAzurePortal();
        }

        /// <summary>
        /// Happy case, user has internet connection and uri specified is valid.
        /// </summary>
        [TestMethod]
        public void ProcessShowAzurePortalTestFail()
        {
            Assert.IsFalse(string.IsNullOrEmpty(General.AzurePortalUrl));
        }
    }
}