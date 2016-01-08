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
        private IDisposable _subscription;

        public void initialize()
        {
            _subscription = EventManager.subscribe<PackageResultMessage>(process_results, null, null);
            this.Log().Info(() => "{0} is now ready and waiting for {1}".format_with(GetType().Name, typeof(PackageResultMessage).Name));
        }

        public void shutdown()
        {
            if (_subscription != null) _subscription.Dispose();
        }

        private void process_results(PackageResultMessage message)
        {
            var resultsMessage = new StringBuilder();

            var rejectOrUnlist = false;

            EventManager.publish(new FinalResultMessage(message.PackageId, message.PackageVersion, resultsMessage.to_string(), rejectOrUnlist));
        }
    }
}
