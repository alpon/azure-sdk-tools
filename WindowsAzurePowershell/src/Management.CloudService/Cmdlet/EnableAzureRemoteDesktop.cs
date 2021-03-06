﻿// ----------------------------------------------------------------------------------
//
// Copyright 2011 Microsoft Corporation
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

namespace Microsoft.WindowsAzure.Management.CloudService.Cmdlet
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Cryptography.Pkcs;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Permissions;
    using System.Text;
    using AzureTools;
    using Common;
    using Model;
    using ServiceConfigurationSchema;
    using ServiceDefinitionSchema;
    using Services;
    using Utilities;
    using Microsoft.Samples.WindowsAzure.ServiceManagement;

    /// <summary>
    /// Enable Remote Desktop by adding appropriate imports and settings to
    /// ServiceDefinition.csdef and ServiceConfiguration.*.cscfg
    /// </summary>
    [Cmdlet(VerbsLifecycle.Enable, "AzureServiceProjectRemoteDesktop")]
    public class EnableAzureServiceProjectRemoteDesktopCommand : CloudCmdlet<IServiceManagement>
    {
        [Parameter(Position = 0, Mandatory = true)]
        [Alias("user")]
        public string Username { get; set; }

        [Parameter(Position = 1, Mandatory = true)]
        [Alias("pwd")]
        public SecureString Password { get; set; }

        public EnableAzureServiceProjectRemoteDesktopCommand()
        {
            SkipChannelInit = true;
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public override void ExecuteCmdlet()
        {
            base.ExecuteCmdlet();

            AzureTool.Validate();
            EnableRemoteDesktop();
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void EnableRemoteDesktop()
        {
            Validate.ValidateStringIsNullOrEmpty(Username, "Username");
            if (Password == null)
            {
                throw new ArgumentNullException("Password");
            }
            
            string plainPassword = GetPlainPassword();
            if (!IsPasswordComplex(plainPassword))
            {
                throw new ArgumentException(Properties.Resources.EnableAzureRemoteDesktopCommand_Enable_NeedComplexPassword);
            }

            AzureService service = new AzureService(GetServiceRootPath(), null);
            WebRole[] webRoles = service.Components.Definition.WebRole ?? new WebRole[0];
            WorkerRole[] workerRoles = service.Components.Definition.WorkerRole ?? new WorkerRole[0];

            string forwarderName = GetForwarderName(webRoles, workerRoles);
            RemoveOtherRemoteForwarders(webRoles, workerRoles, forwarderName);
            AddRemoteAccess(webRoles, workerRoles);

            X509Certificate2 cert = ChooseCertificate();
            ServiceConfigurationSchema.Certificate certElement = new ServiceConfigurationSchema.Certificate
            {
                name = "Microsoft.WindowsAzure.Plugins.RemoteAccess.PasswordEncryption",
                thumbprintAlgorithm = ThumbprintAlgorithmTypes.sha1,
                thumbprint = cert.Thumbprint
            };
            string encryptedPassword = Encrypt(plainPassword, cert);
            
            UpdateServiceConfigurations(service, forwarderName, certElement, encryptedPassword);
            service.Components.Save(service.Paths);
        }

        private X509Certificate2 FindCertificate()
        {
            X509Store store = new X509Store(StoreName.My, System.Security.Cryptography.X509Certificates.StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            return
                store
                .Certificates
                .Cast<X509Certificate2>()
                .Where(c => c.FriendlyName == Properties.Resources.EnableRemoteDesktop_FriendlyCertificateName)
                .FirstOrDefault();
        }

        private X509Certificate2 ChooseCertificate()
        {
            X509Certificate2 cert = FindCertificate();
            if (cert == null)
            {
                new CsEncrypt().CreateCertificate();
                cert = FindCertificate();
            }
            return cert;
        }

        private static bool IsPasswordComplex(string password)
        {
            return (Convert.ToInt32(password.Any(char.IsUpper)) +
                    Convert.ToInt32(password.Any(char.IsLower)) +
                    Convert.ToInt32(password.Any(char.IsDigit)) +
                    Convert.ToInt32(password.Any(char.IsLetterOrDigit)) >= 3)
                   && password.Length >= 6;
        }

        private string Encrypt(string password, X509Certificate2 cert)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            EnvelopedCms envelopedCms = new EnvelopedCms(new ContentInfo(bytes));
            envelopedCms.Encrypt(new CmsRecipient(cert));
            return Convert.ToBase64String(envelopedCms.Encode());
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private string GetPlainPassword()
        {
            IntPtr ptr = IntPtr.Zero;
            string plainPassword = null;
            try
            {
                ptr = Marshal.SecureStringToBSTR(Password);
                plainPassword = Marshal.PtrToStringBSTR(ptr);
            }
            finally
            {
                Marshal.ZeroFreeBSTR(ptr);
            }
            
            return plainPassword;
        }
        
        private static string GetForwarderName(WebRole[] webRoles, WorkerRole[] workerRoles)
        {
            string forwarderName = null;
            WorkerRole workerForwarder = workerRoles.FirstOrDefault(r => r.Imports != null && r.Imports.Any(i => i.moduleName == "RemoteForwarder"));
            if (workerForwarder != null)
            {
                // a worker role has the forwarder
                forwarderName = workerForwarder.name;
            }
            else
            {
                WebRole webForwarder = webRoles.FirstOrDefault(r => r.Imports != null && r.Imports.Any(i => i.moduleName == "RemoteForwarder"));
                if (webForwarder != null)
                {
                    // a web role has the forwarder
                    forwarderName = webForwarder.name;
                }
                else
                {
                    // no role has the forwarder yet
                    WorkerRole firstWorkerRole = workerRoles.FirstOrDefault();
                    if (firstWorkerRole != null)
                    {
                        firstWorkerRole.Imports = General.Append(firstWorkerRole.Imports, new Import { moduleName = "RemoteForwarder" });
                        forwarderName = firstWorkerRole.name;
                    }
                    else // no worker role, use a web role
                    {
                        WebRole firstWebRole = webRoles.FirstOrDefault();
                        if (firstWebRole != null)
                        {
                            firstWebRole.Imports = General.Append(firstWebRole.Imports, new Import { moduleName = "RemoteForwarder" });
                            forwarderName = firstWebRole.name;
                        }
                        else
                        {
                            throw new InvalidOperationException(Properties.Resources.EnableAzureRemoteDesktop_Enable_NoRoles);
                        }
                    }
                }
            }
            return forwarderName;
        }

        private static void RemoveOtherRemoteForwarders(WebRole[] webRoles, WorkerRole[] workerRoles, string forwarderName)
        {
            // Remove RemoteForwarder from all but the chosen role
            foreach (WebRole webRole in webRoles)
            {
                if (webRole.name != forwarderName &&
                    webRole.Imports != null &&
                    webRole.Imports.Any(i => i.moduleName == "RemoteForwarder"))
                {
                    webRole.Imports = webRole.Imports.Where(i => i.moduleName != "RemoteForwarder").ToArray();
                }
            }
            foreach (WorkerRole workerRole in workerRoles)
            {
                if (workerRole.name != forwarderName &&
                    workerRole.Imports != null &&
                    workerRole.Imports.Any(i => i.moduleName == "RemoteForwarder"))
                {
                    workerRole.Imports = workerRole.Imports.Where(i => i.moduleName != "RemoteForwarder").ToArray();
                }
            }
        }

        private static void AddRemoteAccess(WebRole[] webRoles, WorkerRole[] workerRoles)
        {
            // Add RemoteAccess to all roles
            foreach (WebRole webRole in webRoles.Where(r => r.Imports == null || !r.Imports.Any(i => i.moduleName == "RemoteAccess")))
            {
                webRole.Imports = General.Append(webRole.Imports, new Import { moduleName = "RemoteAccess" });
            }
            foreach (WorkerRole workerRole in workerRoles.Where(r => r.Imports == null || !r.Imports.Any(i => i.moduleName == "RemoteAccess")))
            {
                workerRole.Imports = General.Append(workerRole.Imports, new Import { moduleName = "RemoteAccess" });
            }
        }

        private void UpdateServiceConfigurations(AzureService service, string forwarderName, ServiceConfigurationSchema.Certificate certElement, string encryptedPassword)
        {
            foreach (ServiceConfiguration config in new[] { service.Components.LocalConfig, service.Components.CloudConfig })
            {
                foreach (ServiceConfigurationSchema.RoleSettings role in config.Role)
                {
                    if (role.Certificates == null)
                    {
                        role.Certificates = new ServiceConfigurationSchema.Certificate[0];
                    }

                    ServiceConfigurationSchema.Certificate existingCert = role.Certificates.FirstOrDefault(c => c.name == certElement.name);
                    if (existingCert != null)
                    {
                        // ensure we're referencing the right cert
                        existingCert.thumbprint = certElement.thumbprint;
                    }
                    else
                    {
                        role.Certificates = role.Certificates.Concat(new[] { certElement }).ToArray();
                    }

                    Dictionary<string, string> settings = new Dictionary<string, string>();
                    foreach (ServiceConfigurationSchema.ConfigurationSetting setting in role.ConfigurationSettings)
                    {
                        settings[setting.name] = setting.value;
                    }
                    settings["Microsoft.WindowsAzure.Plugins.RemoteAccess.Enabled"] = "true";
                    settings["Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountUsername"] = Username;
                    settings["Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountEncryptedPassword"] = encryptedPassword;
                    settings["Microsoft.WindowsAzure.Plugins.RemoteAccess.AccountExpiration"] = (DateTime.Now + TimeSpan.FromDays(365)).ToString("o");

                    if (role.name == forwarderName)
                    {
                        settings["Microsoft.WindowsAzure.Plugins.RemoteForwarder.Enabled"] = "true";
                    }

                    role.ConfigurationSettings = settings.Select(pair => new ServiceConfigurationSchema.ConfigurationSetting { name = pair.Key, value = pair.Value }).ToArray();
                }
            }
        }   
    }
}
