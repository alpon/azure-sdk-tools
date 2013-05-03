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

namespace Microsoft.WindowsAzure.Management.Test.Utilities.Common
{
    using System.Collections.Generic;
    using System.Management.Automation;
    using Microsoft.WindowsAzure.Management.Utilities.Common;

    public class InMemorySessionManager : ISessionManager
    {
        private readonly IDictionary<string, object> _variables = new Dictionary<string, object>();

        public object GetVariable(PSCmdlet cmdlet, string name)
        {
            if (_variables.ContainsKey(name))
            {
                return _variables[name];   
            }

            return null;
        }

        public void SetVariable(PSCmdlet cmdlet, string name, object value)
        {
            _variables[name] = value;
        }

        public void ClearVariable(PSCmdlet cmdlet, string name)
        {
            _variables.Remove(name);
        }
    }
}