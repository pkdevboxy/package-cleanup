// Copyright © 2015 - Present RealDimensions Software, LLC
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// 
// You may obtain a copy of the License at
// 
// 	http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace chocolatey.package.cleanup.host.infrastructure.registration
{
    using System.Collections.Generic;
    using SimpleInjector;
    using cleanup.infrastructure.app.configuration;
    using cleanup.infrastructure.app.services;
    using cleanup.infrastructure.app.tasks;
    using cleanup.infrastructure.filesystem;
    using cleanup.infrastructure.tasks;

    /// <summary>
    ///   The inversion container binding for the application.
    ///   This is client project specific - contains items that are only available in the client project.
    ///   Look for the broader application container in the core project.
    /// </summary>
    public class ContainerBindingConsole
    {
        /// <summary>
        ///   Loads the module into the kernel.
        /// </summary>
        /// <param name="container">The container.</param>
        public void register_components(Container container)
        {
            container.Register<IEnumerable<ITask>>(
                () =>
                {
                    var list = new List<ITask>
                    {
                        new StartupTask(),
                        new CheckForPackagesTask(container.GetInstance<IConfigurationSettings>()),
                        new PrepareResultsTask(),
                        new UpdateWebsiteInformationTask(container.GetInstance<IConfigurationSettings>(), container.GetInstance<INuGetService>())
                    };

                    return list.AsReadOnly();
                },
                Lifestyle.Singleton);
        }
    }
}
