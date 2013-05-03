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

namespace Microsoft.WindowsAzure.Management.CloudService
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Permissions;
    using System.Text;
    using System.Threading;
    using Microsoft.WindowsAzure.Management.Utilities.CloudService;
    using Microsoft.WindowsAzure.Management.Utilities.CloudService.AzureTools;
    using Microsoft.WindowsAzure.Management.Utilities.Common;
    using Microsoft.WindowsAzure.Management.Utilities.Common.XmlSchema.ServiceDefinitionSchema;
    using Microsoft.WindowsAzure.Management.Utilities.Properties;
    using Microsoft.WindowsAzure.ServiceManagement;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Certificate = Microsoft.WindowsAzure.Management.Utilities.Common.XmlSchema.ServiceConfigurationSchema.Certificate;
    using DeploymentStatus = Microsoft.WindowsAzure.ServiceManagement.DeploymentStatus;
    using RoleInstanceStatus = Microsoft.WindowsAzure.ServiceManagement.RoleInstanceStatus;
    using UpgradeType = Microsoft.WindowsAzure.ServiceManagement.UpgradeType;

    /// <summary>
    /// Create a new deployment. Note that there shouldn't be a deployment 
    /// of the same name or in the same slot when executing this command.
    /// </summary>
    [Cmdlet(VerbsData.Publish, "AzureServiceProject", SupportsShouldProcess = true), OutputType(typeof(Deployment))]
    public class PublishAzureServiceProjectCommand : ServiceManagementBaseCmdlet
    {
        private DeploymentSettings _deploymentSettings;
        private AzureService _azureService;
        private string _hostedServiceName;
        private List<IPublishListener> _listeners;

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        [Alias("sn")]
        public string Subscription { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        [Alias("sv")]
        public string ServiceName { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        [Alias("st")]
        public string StorageAccountName { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        [Alias("l")]
        public string Location { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        [Alias("sl")]
        public string Slot { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        [Alias("ln")]
        public SwitchParameter Launch { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public int? TimeoutSeconds { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public SwitchParameter Force
        {
            get { return force; }
            set { force = value; }
        }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        [Alias("ag")]
        public string AffinityGroup { get; set; }

        private bool force;
        /// <summary>
        /// Gets or sets a flag indicating whether publishing should skip the
        /// upload step.  This is only used for testing.
        /// </summary>
        internal bool SkipUpload { get; set; }

        /// <summary>
        /// Initializes a new instance of the PublishAzureServiceCommand class.
        /// </summary>
        public PublishAzureServiceProjectCommand()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PublishAzureServiceCommand class.
        /// </summary>
        /// <param name="channel">
        /// Channel used for communication with Azure's service management APIs.
        /// </param>
        public PublishAzureServiceProjectCommand(IServiceManagement channel)
        {
            Channel = channel;

            _listeners = new List<IPublishListener>();
            _listeners.Add(new CachingStorageConnectionStringUpdater());
        }

        /// <summary>
        /// Execute the command.
        /// </summary>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public override void ExecuteCmdlet()
        {
            PublishService(General.GetServiceRootPath(CurrentPath()));
        }

        /// <summary>
        /// Publish an Azure service defined at the given path. Publish flow is as following:
        /// * Checks is service is available.
        ///     * Exists:
        ///         * Checks if deployment slot is available.
        ///             * Exists: update the deployment with new package.
        ///             * Does not exist: create new deployment for this slot.
        ///     * Does not exist:
        ///         1. Create new service.
        ///         2. Create new deployment for slot.
        /// * Verifies the deployment until its published.
        /// </summary>
        /// <param name="serviceRootPath">
        /// Path where the service exists.
        /// </param>
        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public void PublishService(string serviceRootPath)
        {
            AzureTool.Validate();
            WriteVerbose(string.Format(Resources.PublishServiceStartMessage, _hostedServiceName));

            // Package the service and all of its roles up in the open package format used by Azure
            if (InitializeSettingsAndCreatePackage(serviceRootPath))
            {
                if (ServiceExists())
                {
                    var deploymentStatusCommand = new GetDeploymentStatus(Channel, CommandRuntime)
                    { ShareChannel = ShareChannel, CurrentSubscription = CurrentSubscription };
                    bool deploymentExists = deploymentStatusCommand.DeploymentExists(
                        _azureService.Paths.RootPath,
                        _hostedServiceName,
                        _deploymentSettings.ServiceSettings.Slot,
                        _deploymentSettings.ServiceSettings.Subscription);
                    if (deploymentExists)
                    {
                        UpgradeDeployment();
                    }
                    else
                    {
                        CreateNewDeployment();
                    }
                }
                else
                {
                    CreateHostedService();
                    CreateNewDeployment();
                }

                // Verify the deployment succeeded by checking that each of the
                // roles are running
                VerifyDeployment();

                // After we've finished deploying, optionally launch a browser pointed at the service
                if (Launch && CanGenerateUrlForDeploymentSlot())
                {
                    LaunchService();
                }

                Deployment deployment = this.RetryCall<Deployment>(s => this.Channel.GetDeploymentBySlot(
                    s, 
                    _hostedServiceName, 
                    _deploymentSettings.ServiceSettings.Slot
                ));
                WriteObject(deployment);
            }
            else
            {
                WriteVerbose(Resources.PublishAbortedAtUserRequest);
            }

            WriteVerboseWithTimestamp(Resources.PublishCompleteMessage);
        }

        /// <summary>
        /// Gets a value indicating whether we'll be able to automatically get
        /// the deployment URL for the given deployment slot.
        /// </summary>
        /// <returns>
        /// A value indicating whether we'll be able to automatically get
        /// the deployed URL for the given deployment slot.
        /// </returns>
        private bool CanGenerateUrlForDeploymentSlot()
        {
            return string.Equals(_deploymentSettings.ServiceSettings.Slot, ArgumentConstants.Slots[SlotType.Production]);
        }

        /// <summary>
        /// Initialize our model of the AzureService located at the given
        /// path along with its DeploymentSettings and SubscriptionId.
        /// </summary>
        /// <param name="rootPath">Root path of the Azure service.</param>
        /// <param name="manifest">External runtime manifest to use, mainly for testing purposes</param>
        internal bool InitializeSettingsAndCreatePackage(string rootPath, string manifest = null)
        {
            Debug.Assert(!string.IsNullOrEmpty(rootPath), "rootPath cannot be null or empty.");
            Debug.Assert(Directory.Exists(rootPath), "rootPath does not exist.");

            _azureService = new AzureService(rootPath, null);

            // If user provided a service name, change current service name to use it.
            //
            if (!string.IsNullOrEmpty(ServiceName))
            {
                _azureService.ChangeServiceName(ServiceName, _azureService.Paths);
            }

            ServiceSettings.DefaultLocation = Channel.ListLocations(CurrentSubscription.SubscriptionId).First().Name;
            ServiceSettings defaultSettings = ServiceSettings.LoadDefault(
                _azureService.Paths.Settings,
                Slot,
                Location,
                AffinityGroup,
                Subscription,
                StorageAccountName,
                ServiceName,
                _azureService.ServiceName,
                out _hostedServiceName);

            if (!string.IsNullOrEmpty(defaultSettings.Subscription))
            {
                var globalComponents = GlobalComponents.Load(GlobalPathInfo.GlobalSettingsDirectory);
                CurrentSubscription = globalComponents.Subscriptions.Values.First(
                    subscription => subscription.SubscriptionName == defaultSettings.Subscription);
            }
            else
            {
                defaultSettings.Subscription = CurrentSubscription.SubscriptionName;
            }

            WriteVerboseWithTimestamp(String.Format(Resources.RuntimeDeploymentStart,
                _hostedServiceName));
            if (PrepareRuntimeDeploymentInfo(_azureService, defaultSettings, manifest))
            {

                WriteVerboseWithTimestamp(String.Format(Resources.PublishPreparingDeploymentMessage,
                    _hostedServiceName, CurrentSubscription.SubscriptionId));

                // Caching worker roles require update to their service configuration settings with 
                // the storage service credentials. Before creating the package verify that the storage
                // service exists and fetch its credentials.
                if (!StorageAccountExists(defaultSettings.StorageAccountName))
                {
                    CreateStorageAccount(
                        defaultSettings.StorageAccountName,
                        _hostedServiceName,
                        defaultSettings.Location,
                        defaultSettings.AffinityGroup);
                }

                // Initiate call to all publish listeners.
                this._listeners.ForEach<IPublishListener>(l => l.OnPublish(Channel, _azureService, defaultSettings, CurrentSubscription.SubscriptionId));

                CreatePackage();
                
                _deploymentSettings = new DeploymentSettings(
                    defaultSettings,
                    _azureService.Paths.CloudPackage,
                    _azureService.Paths.CloudConfiguration,
                    _hostedServiceName,
                    string.Format(Resources.ServiceDeploymentName, defaultSettings.Slot));

                return true;
            }

            return false;
        }

        /// <summary>
        /// Create the package that we'll upload to Azure blob storage before
        /// deploying.
        /// </summary>
        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        private void CreatePackage()
        {
            Debug.Assert(_azureService != null);

            string unused = null;
            _azureService.CreatePackage(DevEnv.Cloud, out unused, out unused);
        }

        /// <summary>
        /// Set up runtime deployment info for each role in the service - after this method is called, 
        /// each role will have its startup configured with the URI of a runtime package to install at 
        /// role start
        /// </summary>
        /// <param name="service">The service to prepare</param>
        /// <param name="settings">The runtime settings to use to determine location</param>
        /// <param name="manifest"> </param>
        /// <returns>True if requested runtimes were successfully resolved, otherwise false</returns>
        internal bool PrepareRuntimeDeploymentInfo(AzureService service, ServiceSettings settings, string manifest = null)
        {
            CloudRuntimeCollection availableRuntimePackages;
            if (!CloudRuntimeCollection.CreateCloudRuntimeCollection(out availableRuntimePackages, manifestFile: manifest))
            {
                throw new ArgumentException(string.Format(Resources.ErrorRetrievingRuntimesForLocation,
                    settings.Location));
            }

            ServiceDefinition definition = service.Components.Definition;
            StringBuilder warningText = new StringBuilder();
            bool shouldWarn = false;
            List<CloudRuntimeApplicator> applicators = new List<CloudRuntimeApplicator>();
            if (definition.WebRole != null)
            {
                foreach (WebRole role in definition.WebRole.Where(role => role.Startup != null && CloudRuntime.GetRuntimeStartupTask(role.Startup) != null))
                {
                    CloudRuntime.ClearRuntime(role);
                    string rolePath = Path.Combine(service.Paths.RootPath, role.name);
                    foreach (CloudRuntime runtime in CloudRuntime.CreateRuntime(role, rolePath))
                    {
                        CloudRuntimePackage package;
                        if (!availableRuntimePackages.TryFindMatch(runtime, out package))
                        {
                            string warning;
                            if (!runtime.ValidateMatch(package, out warning))
                            {
                                shouldWarn = true;
                                warningText.AppendFormat("{0}\r\n", warning);
                            }
                        }

                        applicators.Add(CloudRuntimeApplicator.CreateCloudRuntimeApplicator(runtime,
                            package, role));
                    }
                }
            }

            if (definition.WorkerRole != null)
            {
                foreach (WorkerRole role in definition.WorkerRole.Where(role => role.Startup != null && CloudRuntime.GetRuntimeStartupTask(role.Startup) != null))
                {
                    string rolePath = Path.Combine(service.Paths.RootPath, role.name);
                    CloudRuntime.ClearRuntime(role);
                    foreach (CloudRuntime runtime in CloudRuntime.CreateRuntime(role, rolePath))
                    {
                        CloudRuntimePackage package;
                        if (!availableRuntimePackages.TryFindMatch(runtime, out package))
                        {
                            string warning;
                            if (!runtime.ValidateMatch(package, out warning))
                            {
                                shouldWarn = true;
                                warningText.AppendFormat("{0}\r\n", warning);
                            }
                        }
                        applicators.Add(CloudRuntimeApplicator.CreateCloudRuntimeApplicator(runtime,
                            package, role));
                    }
                }
            }

            if (!shouldWarn || ShouldProcess(string.Format(Resources.RuntimeMismatchWarning,
                _azureService.ServiceName)))
            {
                if (!shouldWarn || Force || ShouldContinue(warningText.ToString(),
                    string.Format(Resources.RuntimeMismatchWarning, _azureService.ServiceName)))
                {
                    applicators.ForEach<CloudRuntimeApplicator>(a => a.Apply());
                    service.Components.Save(service.Paths);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determine if a service already exists.
        /// </summary>
        /// <returns>
        /// A value indicating whether the service already exists.
        /// </returns>
        /// <remarks>
        /// Service names are unique across Azure.
        /// </remarks>
        private bool ServiceExists()
        {
            Debug.Assert(
                !string.IsNullOrEmpty(_hostedServiceName),
                "_hostedServiceName cannot be null or empty.");
            Debug.Assert(
                !string.IsNullOrEmpty(CurrentSubscription.SubscriptionId),
                "subscriptionId cannot be null or empty.");
            Debug.Assert(
                !string.IsNullOrEmpty(_deploymentSettings.ServiceSettings.Slot),
                "Slot cannot be null.");

            WriteVerboseWithTimestamp(Resources.PublishConnectingMessage);

            // Check whether there's an existing service with the desired
            // name in the desired slot accessible from our subscription

            try
            {
                RetryCall(subscription => Channel.GetHostedServiceWithDetails(subscription, _hostedServiceName, true));
            }
            catch (ServiceManagementClientException)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Create a hosted Azure service.
        /// </summary>
        private void CreateHostedService()
        {
            Debug.Assert(
                !string.IsNullOrEmpty(_hostedServiceName),
                "_hostedServiceName cannot be null or empty.");
            Debug.Assert(
                !string.IsNullOrEmpty(_deploymentSettings.Label),
                "Label cannot be null or empty.");
            Debug.Assert(
                !string.IsNullOrEmpty(_deploymentSettings.ServiceSettings.Location) |
                !string.IsNullOrEmpty(_deploymentSettings.ServiceSettings.AffinityGroup),
                "either Location or AffinityGroup should be set.");

            WriteVerboseWithTimestamp(Resources.PublishCreatingServiceMessage);

            CreateHostedServiceInput hostedServiceInput = new CreateHostedServiceInput
            {
                ServiceName = _hostedServiceName,
                Label = _deploymentSettings.Label,
            };

            if (!string.IsNullOrEmpty(_deploymentSettings.ServiceSettings.AffinityGroup))
            {
                hostedServiceInput.AffinityGroup = _deploymentSettings.ServiceSettings.AffinityGroup;
            }
            else
            {
                hostedServiceInput.Location = _deploymentSettings.ServiceSettings.Location;
            }

            InvokeInOperationContext(() =>
            {
                RetryCall(subscription =>
                    Channel.CreateHostedService(subscription, hostedServiceInput));
                WriteVerboseWithTimestamp(String.Format(Resources.PublishCreatedServiceMessage,
                    hostedServiceInput.ServiceName));
            });
        }

        /// <summary>
        /// Upload the package to our blob storage account.
        /// </summary>
        /// <returns>Uri to the uploaded package.</returns>
        private Uri UploadPackage()
        {
            Debug.Assert(
                !string.IsNullOrEmpty(_azureService.Paths.CloudPackage),
                "CloudPackage cannot be null or empty.");
            Debug.Assert(
                !string.IsNullOrEmpty(_deploymentSettings.ServiceSettings.StorageAccountName),
                "StorageAccountName cannot be null or empty.");

            string packagePath = _azureService.Paths.CloudPackage;
            Validate.ValidateFileFull(packagePath, Resources.Package);

            Uri packageUri = null;
            if (packagePath.StartsWith(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                packagePath.StartsWith(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                packageUri = new Uri(packagePath);
            }
            else
            {
                WriteVerboseWithTimestamp(
                    Resources.PublishUploadingPackageMessage,
                    _deploymentSettings.ServiceSettings.StorageAccountName);

                if (!SkipUpload)
                {
                    BlobRequestOptions blobRequestOptions = null;
                    if (TimeoutSeconds != null)
                    {
                        blobRequestOptions = new BlobRequestOptions { ServerTimeout = new TimeSpan(0, 0, 0, (int)TimeoutSeconds) };
                    }

                    packageUri = RetryCall(subscription =>
                        AzureBlob.UploadPackageToBlob(
                            CreateChannel(),
                            _deploymentSettings.ServiceSettings.StorageAccountName,
                            subscription,
                            this.ResolvePath(packagePath),
                            blobRequestOptions));
                }
                else
                {
                    packageUri = new Uri(packagePath);
                }
            }
            return packageUri;
        }

        /// <summary>
        /// Check if the service's storage account already exists.
        /// </summary>
        /// <returns>
        /// A value indicating whether the service's storage account already
        /// exists.
        /// </returns>
        private bool StorageAccountExists(string name)
        {
            StorageService storageService = null;
            try
            {
                storageService = RetryCall<StorageService>(subscription =>
                    Channel.GetStorageService(subscription, name));
            }
            catch (ServiceManagementClientException)
            {
                // Don't write error message.  This catch block is used to
                // detect that there's no such endpoint which indicates that
                // the storage account doesn't exist.
                return false;
            }

            return storageService != null;
        }

        /// <summary>
        /// Create an Azure storage account that we can use to upload our
        /// package when creating and deploying a service.
        /// </summary>
        private void CreateStorageAccount(string name, string label, string location, string affinityGroup)
        {
            CreateStorageServiceInput storageServiceInput = new CreateStorageServiceInput
            {
                ServiceName = name,
                Label = label,
            };

            if (!string.IsNullOrEmpty(affinityGroup))
            {
                storageServiceInput.AffinityGroup = affinityGroup;
            }
            else
            {
                storageServiceInput.Location = location;
            }

            WriteVerboseWithTimestamp(String.Format(Resources.PublishVerifyingStorageMessage, name));

            InvokeInOperationContext(() =>
            {
                RetryCall(subscription =>
                    Channel.CreateStorageService(subscription, storageServiceInput));

                StorageService storageService = null;
                do
                {
                    try
                    {
                        storageService = RetryCall<StorageService>(subscription =>
                        Channel.GetStorageService(subscription, storageServiceInput.ServiceName));
                    }
                    catch (Exception)
                    {
                        // Ignore the exception, keep getting the status.
                    }
                }
                while (storageService.StorageServiceProperties.Status != StorageServiceStatus.Created);
            });
        }

        /// <summary>
        /// Get the configuration.
        /// </summary>
        /// <returns>Configuration.</returns>
        private string GetConfiguration()
        {
            string configurationPath = _azureService.Paths.CloudConfiguration;
            Validate.ValidateFileFull(configurationPath, Resources.ServiceConfiguration);
            string fullPath = this.ResolvePath(configurationPath);
            string configuration = General.GetConfiguration(fullPath);
            return configuration;
        }

        /// <summary>
        /// Create a new deployment for the service.
        /// </summary>
        private void CreateNewDeployment()
        {
            Debug.Assert(
                !string.IsNullOrEmpty(_hostedServiceName),
                "_hostedServiceName cannot be null or empty.");
            Debug.Assert(
                !string.IsNullOrEmpty(_deploymentSettings.ServiceSettings.Slot),
                "Slot cannot be null or empty.");

            CreateDeploymentInput deploymentInput = new CreateDeploymentInput
            {
                PackageUrl = UploadPackage(),
                Configuration = GetConfiguration(),
                Label = _deploymentSettings.Label,
                Name = _deploymentSettings.DeploymentName,
                StartDeployment = true,
            };

            CertificateList uploadedCertificates = RetryCall<CertificateList>(subscription => Channel.ListCertificates(subscription, _hostedServiceName));
            AddCertificates(uploadedCertificates);
            InvokeInOperationContext(() =>
                {

                    RetryCall(subscription =>
                        Channel.CreateOrUpdateDeployment(
                            subscription,
                            _hostedServiceName,
                            _deploymentSettings.ServiceSettings.Slot,
                            deploymentInput));
                    WaitForDeploymentToStart();
                });
        }

        /// <summary>
        /// Upgrade the deployment for the service.
        /// </summary>
        private void UpgradeDeployment()
        {
            Debug.Assert(
                !string.IsNullOrEmpty(_hostedServiceName),
                "_hostedServiceName cannot be null or empty.");
            Debug.Assert(
                !string.IsNullOrEmpty(_deploymentSettings.Label),
                "Label cannot be null or empty.");
            Debug.Assert(
                !string.IsNullOrEmpty(_deploymentSettings.DeploymentName),
                "DeploymentName cannot be null or empty.");

            UpgradeDeploymentInput deploymentInput = new UpgradeDeploymentInput
            {
                PackageUrl = UploadPackage(),
                Configuration = GetConfiguration(),
                Label = _deploymentSettings.Label,
                Mode = UpgradeType.Auto
            };

            CertificateList uploadedCertificates = RetryCall<CertificateList>(subscription => Channel.ListCertificates(subscription, _hostedServiceName));
            AddCertificates(uploadedCertificates);
            InvokeInOperationContext(() =>
            {
                WriteVerboseWithTimestamp(Resources.PublishUpgradingMessage);
                RetryCall(subscription =>
                    Channel.UpgradeDeployment(
                        subscription,
                        _hostedServiceName,
                        _deploymentSettings.DeploymentName,
                        deploymentInput));
                WaitForDeploymentToStart();
            });
        }

        /// <summary>
        /// Wait until a certificate has been added to a hosted service.
        /// </summary>
        private void WaitForCertificateToBeAdded(Certificate certificate)
        {
            Debug.Assert(
                !string.IsNullOrEmpty(_hostedServiceName),
                "_hostedServiceName cannot be null or empty.");

            CertificateList certificates = null;
            do
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
                certificates = RetryCall(subscription =>
                    Channel.ListCertificates(subscription, _hostedServiceName));
            }
            while (certificates == null || certificates.Count(c => c.Thumbprint.Equals(
                certificate.thumbprint, StringComparison.OrdinalIgnoreCase)) < 1);
        }

        /// <summary>
        /// Wait until a deployment has started.
        /// </summary>
        private void WaitForDeploymentToStart()
        {
            Debug.Assert(
                !string.IsNullOrEmpty(_hostedServiceName),
                "_hostedServiceName cannot be null or empty.");
            Debug.Assert(
                !string.IsNullOrEmpty(_deploymentSettings.ServiceSettings.Slot),
                "Slot cannot be null or empty.");

            Deployment deployment = null;
            do
            {
                deployment = RetryCall<Deployment>(subscription =>
                    Channel.GetDeploymentBySlot(
                        subscription,
                        _hostedServiceName,
                        _deploymentSettings.ServiceSettings.Slot));

                // If a deployment has many roles to initialize, this
                // thread must throttle requests so the Azure portal
                // doesn't reply with a "too many requests" error
                Thread.Sleep(int.Parse(Resources.StandardRetryDelayInMs));
            }
            while (deployment.Status != DeploymentStatus.Starting &&
                deployment.Status != DeploymentStatus.Running);

            WriteVerboseWithTimestamp(string.Format(Resources.PublishCreatedDeploymentMessage,
               deployment.PrivateID));

        }

        /// <summary>
        /// Verify a deployment exists
        /// </summary>
        private void VerifyDeployment()
        {
            try
            {
                WriteVerboseWithTimestamp(Resources.PublishStartingMessage);
                WriteVerboseWithTimestamp(Resources.PublishInitializingMessage);

                Dictionary<string, RoleInstance> roleInstanceSnapshot = new Dictionary<string, RoleInstance>();

                // Continue polling for deployment until all of the roles
                // indicate they're ready
                Deployment deployment = new Deployment();
                do
                {
                    deployment = RetryCall<Deployment>(subscription =>
                        Channel.GetDeploymentBySlot(
                            subscription,
                            _hostedServiceName,
                            _deploymentSettings.ServiceSettings.Slot));

                    // The goal of this loop is to output a message whenever the status of a role 
                    // instance CHANGES. To do that, we have to remember the last status of all role instances
                    // and that's what the roleInstanceSnapshot array is for
                    foreach (RoleInstance currentInstance in deployment.RoleInstanceList)
                    {
                        // We only care about these three statuses, ignore other intermediate statuses
                        if (String.Equals(currentInstance.InstanceStatus, RoleInstanceStatus.BusyRole) ||
                            String.Equals(currentInstance.InstanceStatus, RoleInstanceStatus.ReadyRole) ||
                            String.Equals(currentInstance.InstanceStatus, RoleInstanceStatus.CreatingRole))
                        {
                            bool createdOrChanged = false;

                            // InstanceName is unique and concatenates the role name and instance name
                            if (roleInstanceSnapshot.ContainsKey(currentInstance.InstanceName))
                            {
                                // If we already have a snapshot of that role instance, update it
                                RoleInstance previousInstance = roleInstanceSnapshot[currentInstance.InstanceName];
                                if (!String.Equals(previousInstance.InstanceStatus, currentInstance.InstanceStatus))
                                {
                                    // If the instance status changed, we need to output a message
                                    previousInstance.InstanceStatus = currentInstance.InstanceStatus;
                                    createdOrChanged = true;
                                }
                            }
                            else
                            {
                                // If this is the first time we run through, we also need to output a message
                                roleInstanceSnapshot[currentInstance.InstanceName] = currentInstance;
                                createdOrChanged = true;
                            }


                            if (createdOrChanged)
                            {
                                string statusResource;
                                switch (currentInstance.InstanceStatus)
                                {
                                    case RoleInstanceStatus.BusyRole:
                                        statusResource = Resources.PublishInstanceStatusBusy;
                                        break;

                                    case RoleInstanceStatus.ReadyRole:
                                        statusResource = Resources.PublishInstanceStatusReady;
                                        break;

                                    default:
                                        statusResource = Resources.PublishInstanceStatusCreating;
                                        break;
                                }

                                WriteVerboseWithTimestamp(String.Format(Resources.PublishInstanceStatusMessage,
                                    currentInstance.InstanceName, currentInstance.RoleName, statusResource));

                            }
                        }
                    }

                    // If a deployment has many roles to initialize, this
                    // thread must throttle requests so the Azure portal
                    // doesn't reply with a "too many requests" error
                    Thread.Sleep(int.Parse(Resources.StandardRetryDelayInMs));
                }
                while (deployment.RoleInstanceList.Any(
                    r => r.InstanceStatus != RoleInstanceStatus.ReadyRole));

                if (CanGenerateUrlForDeploymentSlot())
                {
                    WriteVerboseWithTimestamp(
                        Resources.PublishCreatedWebsiteMessage,
                        string.Format(Resources.ServiceUrl, _hostedServiceName));
                }
                else
                {
                    WriteVerboseWithTimestamp(
                        Resources.PublishCreatedWebsiteLaunchNotSupportedMessage);
                }

            }
            catch (ServiceManagementClientException)
            {
                throw new InvalidOperationException(
                    string.Format(
                        Resources.CannotFindDeployment,
                        _hostedServiceName,
                        _deploymentSettings.ServiceSettings.Slot));
            }
        }

        private void AddCertificates(CertificateList uploadedCertificates)
        {
            if (_azureService.Components.CloudConfig.Role != null)
            {
                foreach (Certificate certElement in _azureService.Components.CloudConfig.Role.
                    SelectMany(r => r.Certificates ?? new Certificate[0]).Distinct())
                {
                    if (uploadedCertificates == null || (uploadedCertificates.Count(c => c.Thumbprint.Equals(
                        certElement.thumbprint, StringComparison.OrdinalIgnoreCase)) < 1))
                    {
                        X509Certificate2 cert = General.GetCertificateFromStore(certElement.thumbprint);
                        CertificateFile certFile = null;
                        try
                        {
                            certFile = new CertificateFile
                            {
                                Data = Convert.ToBase64String(cert.Export(X509ContentType.Pfx, string.Empty)),
                                Password = string.Empty,
                                CertificateFormat = "pfx"
                            };
                        }
                        catch (CryptographicException exception)
                        {
                            throw new ArgumentException(string.Format(Resources.CertificatePrivateKeyAccessError, certElement.name), exception);
                        }

                        RetryCall(subscription => Channel.AddCertificates(subscription, _hostedServiceName, certFile));
                        WaitForCertificateToBeAdded(certElement);
                    }
                }
            }
        }

        /// <summary>
        /// Launch a browser pointed at the service after deploying.
        /// </summary>
        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        private void LaunchService()
        {
            Debug.Assert(!string.IsNullOrEmpty(_hostedServiceName), "_hostedServiceName cannot be null or empty.");
            General.LaunchWebPage(string.Format(Resources.ServiceUrl, _hostedServiceName));
        }
    }
}
