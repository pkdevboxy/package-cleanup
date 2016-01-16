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

namespace chocolatey.package.cleanup.infrastructure.app.tasks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Timers;
    using NuGetGallery;
    using configuration;
    using infrastructure.messaging;
    using infrastructure.tasks;
    using messaging;
    using registration;
    using services;

    public class CheckForPackagesTask : ITask
    {
        private readonly IConfigurationSettings _configurationSettings;
        private const double TIMER_INTERVAL = 300000;
        private const string SERVICE_ENDPOINT = "/api/v2/submitted/";
        private readonly Timer _timer = new Timer();
        private IDisposable _subscription;

        public CheckForPackagesTask(IConfigurationSettings configurationSettings)
        {
            _configurationSettings = configurationSettings;
        }

        public void initialize()
        {
            _subscription = EventManager.subscribe<StartupMessage>((message) => timer_elapsed(null, null), null, null);
            _timer.Interval = TIMER_INTERVAL;
            _timer.Elapsed += timer_elapsed;
            _timer.Start();
            this.Log().Info(() => "{0} will check for packages to validate every {1} minute(s).".format_with(GetType().Name, TIMER_INTERVAL / 60000));
        }

        public void shutdown()
        {
            if (_subscription != null) _subscription.Dispose();

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
            }
        }

        private void timer_elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();

            this.Log().Info(() => "Checking for packages to cleanup (remind/reject).");

            try
            {
                var submittedPackagesUri = NuGetService.get_service_endpoint_url(_configurationSettings.PackagesUrl, SERVICE_ENDPOINT);

                // such a fun, dynamically generated name
                var service = new FeedContext_x0060_1(submittedPackagesUri)
                {
                    Timeout = 70
                };

                var twentyDaysAgo = DateTime.UtcNow.AddDays(-20);
                
                // this only returns 30-40 results at a time but at least we'll have something to start with

                //this is not a perfect check but will capture most of the items that need cleaned up.
                IQueryable<V2FeedPackage> packageQuery =
                    service.Packages.Where(
                        p => p.Created < twentyDaysAgo
                            && p.PackageStatus == "Submitted"
                            && p.PackageSubmittedStatus == "Waiting"
                            && (p.PackageCleanupResultDate == null)
                            && ((p.PackageReviewedDate != null && p.PackageReviewedDate < twentyDaysAgo) 
                                ||
                                (p.PackageReviewedDate == null && p.PackageTestResultStatusDate < twentyDaysAgo)
                                )
                        );

                //int totalCount = packageQuery.Count();

                // specifically reduce the call to 30 results so we get back results faster from Chocolatey.org
                IList<V2FeedPackage> packages = packageQuery.Take(30).ToList();
                if (packages.Count == 0) this.Log().Info("No packages to remind.");
                else this.Log().Info("Pulled in {0} packages for reminders.".format_with(packages.Count));

                foreach (var package in packages.or_empty_list_if_null())
                {
                    this.Log().Info(() => "========== {0} v{1} ==========".format_with(package.Id, package.Version));
                    this.Log().Info("{0} v{1} found for review.".format_with(package.Title, package.Version));
                    EventManager.publish(new ReminderPackageMessage(package.Id, package.Version));
                }

                var fifteenDaysAgo = DateTime.UtcNow.AddDays(-15);
                IQueryable<V2FeedPackage> packageQueryForReject =
                     service.Packages.Where(
                         p => p.PackageCleanupResultDate < fifteenDaysAgo
                             && p.PackageStatus == "Submitted"
                             && p.PackageSubmittedStatus == "Waiting"
                         );
                
                // specifically reduce the call to 30 results so we get back results faster from Chocolatey.org
                IList<V2FeedPackage> packagesForReject = packageQueryForReject.Take(30).ToList();
                if (packagesForReject.Count == 0) this.Log().Info("No packages to reject.");
                else this.Log().Info("Pulled in {0} packages for rejection.".format_with(packages.Count));

                foreach (var package in packagesForReject.or_empty_list_if_null())
                {
                    this.Log().Info(() => "========== {0} v{1} ==========".format_with(package.Id, package.Version));
                    this.Log().Info("{0} v{1} found for review.".format_with(package.Title, package.Version));
                    EventManager.publish(new RejectPackageMessage(package.Id, package.Version));
                }
            }
            catch (Exception ex)
            {
                Bootstrap.handle_exception(ex);
            }

            this.Log().Info(() => "Finished checking for packages to validate. Sleeping for {0} minute(s).".format_with(TIMER_INTERVAL / 60000));

            _timer.Start();
        }
    }
}
