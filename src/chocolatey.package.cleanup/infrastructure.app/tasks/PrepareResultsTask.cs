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
    using System.Text;
    using infrastructure.messaging;
    using infrastructure.tasks;
    using messaging;

    public class PrepareResultsTask : ITask
    {
        private IDisposable _reminderSubscription;
        private IDisposable _rejectSubscription;

        public void initialize()
        {
            _reminderSubscription = EventManager.subscribe<ReminderPackageMessage>(process_reminder, null, null);
            this.Log().Info(() => "{0} is waiting for {1} messages.".format_with(GetType().Name, typeof(ReminderPackageMessage).Name));
            _rejectSubscription = EventManager.subscribe<RejectPackageMessage>(process_rejection, null, null);
            this.Log().Info(() => "{0} is waiting for {1} messages.".format_with(GetType().Name, typeof(RejectPackageMessage).Name));
        }

        public void shutdown()
        {
            if (_reminderSubscription != null) _reminderSubscription.Dispose();
            if (_rejectSubscription != null) _rejectSubscription.Dispose();
        }

        private void process_reminder(ReminderPackageMessage message)
        {
            var resultsMessage = new StringBuilder();


            resultsMessage.AppendLine("We've found {0} v{1} in a submitted status and waiting for your next actions. It has had no updates within 20 days after a reviewer has asked for corrections. Please note that if there is no response or fix of the package within 15 days of this message, this package version will automatically be closed (rejected) due to being stale.".format_with(message.PackageId,message.PackageVersion));
            resultsMessage.AppendFormat("{0}Take action:{0}".format_with(Environment.NewLine));
            resultsMessage.AppendLine(" * Log in to the site and respond to the review comments.");
            resultsMessage.AppendLine(" * Resubmit fixes for this version.");
            resultsMessage.AppendLine(" * If the package version is failing automated checks, you can self-reject the package.");
            resultsMessage.AppendLine("{0}If your package is failing automated testing, you can use the [chocolatey test environment](https://github.com/chocolatey/chocolatey-test-environment to manually run the verification) to determine what needs to be fixed.".format_with(Environment.NewLine));
            resultsMessage.AppendLine("{0}**Note**: We don't like to see packages automatically rejected. It doesn't mean that we don't value your contributions, just that we can not continue to hold packages versions in a waiting status that have possibly been abandoned. If you don't believe you will be able to fix up this version of the package within 15 days, we strongly urge you to log in to the site and respond to the review comments until you are able to.".format_with(Environment.NewLine));

            EventManager.publish(new FinalResultMessage(message.PackageId, message.PackageVersion, resultsMessage.to_string(), reject: false));
        }

        private void process_rejection(RejectPackageMessage message)
        {
            var resultsMessage = new StringBuilder();
            
            EventManager.publish(new FinalResultMessage(message.PackageId, message.PackageVersion, resultsMessage.to_string(), reject: true));
        }
    }
}
