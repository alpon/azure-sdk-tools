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
    using System.Management.Automation;
    using Microsoft.WindowsAzure.Management.Utilities.CloudService;
    using Microsoft.WindowsAzure.Management.Utilities.Properties;
    using ServiceManagement;

    /// <summary>
    /// Stops the deployment of specified slot in the azure service
    /// </summary>
    [Cmdlet(VerbsLifecycle.Stop, "AzureService"), OutputType(typeof(Deployment))]
    public class StopAzureServiceCommand : DeploymentStatusManager
    {
        /// <summary>
        /// SetDeploymentStatus will handle the execution of this cmdlet
        /// </summary>
        public StopAzureServiceCommand()
        {
            Status = DeploymentStatus.Suspended;
        }

        public StopAzureServiceCommand(IServiceManagement channel) : base(channel)
        {
            Status = DeploymentStatus.Suspended;
        }

        public override void SetDeploymentStatusProcess(string rootPath, string newStatus, string slot, string subscription, string serviceName)
        {
            // Check that cloud service exists
            WriteVerboseWithTimestamp(Resources.LookingForServiceMessage, serviceName);
            bool found = !Channel.IsDNSAvailable(CurrentSubscription.SubscriptionId, serviceName).Result;

            if (found)
            {
                base.SetDeploymentStatusProcess(rootPath, newStatus, slot, subscription, serviceName);
            }
            else
            {
                WriteExceptionError(new Exception(string.Format(Resources.ServiceDoesNotExist, serviceName)));
            }
        }
    }
}
