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

namespace Microsoft.WindowsAzure.Management.Test.Websites.Services
{
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.WindowsAzure.Management.Utilities.Common;
    using Microsoft.WindowsAzure.Management.Utilities.Websites.Services;
    using Microsoft.WindowsAzure.Management.Utilities.Websites.Services.WebEntities;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CacheTests
    {
        public static string SubscriptionName = "fakename";

        public static string WebSpacesFile;

        public static string SitesFile;

        [TestInitialize]
        public void SetupTest()
        {
            GlobalPathInfo.AzureAppDir = Path.Combine(Directory.GetCurrentDirectory(), "Windows Azure Powershell");

            WebSpacesFile =  Path.Combine(GlobalPathInfo.AzureAppDir,
                                                          string.Format("spaces.{0}.json", SubscriptionName));

            SitesFile = Path.Combine(GlobalPathInfo.AzureAppDir,
                                                          string.Format("sites.{0}.json", SubscriptionName));
            
            if (File.Exists(WebSpacesFile))
            {
                File.Delete(WebSpacesFile);
            }

            if (File.Exists(SitesFile))
            {
                File.Delete(SitesFile);
            }
        }

        [TestCleanup]
        public void CleanupTest()
        {
            if (File.Exists(WebSpacesFile))
            {
                File.Delete(WebSpacesFile);
            }

            if (File.Exists(SitesFile))
            {
                File.Delete(SitesFile);
            }
        }

        [TestMethod]
        public void GetEmptyWebspaceTest()
        {
            WebSpaces getWebSpaces = Cache.GetWebSpaces("NotExisting");
            Assert.IsNotNull(getWebSpaces);
            Assert.AreEqual<int>(0, getWebSpaces.Count);
        }

        [TestMethod]
        public void AddWebSpaceTest()
        {
            WebSpace webSpace = new WebSpace {Name = "newwebspace"};
            // Add without any cache from before
            Cache.AddWebSpace(SubscriptionName, webSpace);

            WebSpaces getWebSpaces = Cache.GetWebSpaces(SubscriptionName);
            Assert.IsNotNull(getWebSpaces.Find(ws => ws.Name.Equals("newwebspace")));
        }

        [TestMethod]
        public void RemoveWebSpaceTest()
        {
            WebSpace webSpace = new WebSpace { Name = "newwebspace" };
            // Add without any cache from before
            Cache.AddWebSpace(SubscriptionName, webSpace);

            WebSpaces getWebSpaces = Cache.GetWebSpaces(SubscriptionName);
            Assert.IsNotNull(getWebSpaces.Find(ws => ws.Name.Equals("newwebspace")));

            // Now remove it
            Cache.RemoveWebSpace(SubscriptionName, webSpace);
            getWebSpaces = Cache.GetWebSpaces(SubscriptionName);
            Assert.IsNull(getWebSpaces.Find(ws => ws.Name.Equals("newwebspace")));
        }

        [TestMethod]
        public void GetSetWebSpacesTest()
        {
            // Test no webspaces
            Assert.AreEqual<int>(0, Cache.GetWebSpaces(SubscriptionName).Count);

            // Test valid webspaces
            WebSpaces webSpaces = new WebSpaces(new List<WebSpace> { new WebSpace { Name = "webspace1" }, new WebSpace { Name = "webspace2" }});
            Cache.SaveSpaces(SubscriptionName, webSpaces);

            WebSpaces getWebSpaces = Cache.GetWebSpaces(SubscriptionName);
            Assert.IsNotNull(getWebSpaces.Find(ws => ws.Name.Equals("webspace1")));
            Assert.IsNotNull(getWebSpaces.Find(ws => ws.Name.Equals("webspace2")));
        }

        [TestMethod]
        public void AddSiteTest()
        {
            Site site = new Site { Name = "newsite" };
            // Add without any cache from before
            Cache.AddSite(SubscriptionName, site);

            Sites getSites = Cache.GetSites(SubscriptionName);
            Assert.IsNotNull(getSites.Find(ws => ws.Name.Equals("newsite")));
        }

        [TestMethod]
        public void RemoveSiteTest()
        {
            Site site = new Site { Name = "newsite" };
            // Add without any cache from before
            Cache.AddSite(SubscriptionName, site);

            Sites getSites = Cache.GetSites(SubscriptionName);
            Assert.IsNotNull(getSites.Find(ws => ws.Name.Equals("newsite")));

            // Now remove it
            Cache.RemoveSite(SubscriptionName, site);
            getSites = Cache.GetSites(SubscriptionName);
            Assert.IsNull(getSites.Find(ws => ws.Name.Equals("newsite")));
        }

        [TestMethod]
        public void GetSetSitesTest()
        {
            Assert.IsNull(Cache.GetSites(SubscriptionName));

            Sites sites = new Sites(new List<Site> { new Site { Name = "site1" }, new Site { Name = "site2" }});
            Cache.SaveSites(SubscriptionName, sites);

            Sites getSites = Cache.GetSites(SubscriptionName);
            Assert.IsNotNull(getSites.Find(s => s.Name.Equals("site1")));
            Assert.IsNotNull(getSites.Find(s => s.Name.Equals("site2")));
        }
    }
}
