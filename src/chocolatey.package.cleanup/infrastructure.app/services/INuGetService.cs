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

namespace chocolatey.package.cleanup.infrastructure.app.services
{
    using System.Net;
    using NuGet;
    using configuration;
    using results;

    public interface INuGetService
    {
        string ApiKeyHeader { get; }

        HttpClient get_client(string baseUrl, string path, string method, string contentType);

        /// <summary>
        ///   Ensures that success response is received.
        /// </summary>
        /// <param name="client">The client that is making the request.</param>
        /// <param name="expectedStatusCode">The exected status code.</param>
        /// <returns>
        ///   True if success response is received; false if redirection response is received.
        ///   In this case, _baseUri will be updated to be the new redirected Uri and the requrest
        ///   should be retried.
        /// </returns>
        NuGetServiceGetClientResult ensure_successful_response(HttpClient client, HttpStatusCode? expectedStatusCode = null);

        /// <summary>
        ///   Downloads a package to the specified folder and returns the package stream
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="packageVersion">The package version.</param>
        /// <param name="downloadLocation">The download location.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns></returns>
        IPackage download_package(string packageId, string packageVersion, string downloadLocation, IConfigurationSettings configuration);
    }
}
