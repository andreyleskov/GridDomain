using System;
using System.Runtime.ExceptionServices;
using Akka.Actor;
using Akka.DI.Core;
using GridDomain.Scheduling.Akka.Messages;
using GridDomain.Scheduling.Akka.Tasks;
using GridDomain.Scheduling.Quartz.Logging;
using Newtonsoft.Json;
using Quartz;

namespace GridDomain.Scheduling.Integration
{
    public class QuartzJob : IJob
    {
        private readonly IQuartzLogger _quartzLogger;
        private readonly ActorSystem _actorSystem;
        private const string MessageKey = "Message";
        private const string Timeout = "Timeout";
        

        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects,
            TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
        };

        public QuartzJob(
            IQuartzLogger quartzLogger,
            ActorSystem actorSystem
            )
        {
            _quartzLogger = quartzLogger;
            _actorSystem = actorSystem;
        }

        public void Execute(IJobExecutionContext context)
        {
            try
            {
                var scheduledRequest = DeserializeTaskData(context.JobDetail.JobDataMap);
                var timeout = DeserializeTimeout(context.JobDetail.JobDataMap);
                var jobStatusManager = _actorSystem.ActorOf(_actorSystem.DI().Props<MessageProcessingStatusManager>());
                var result = jobStatusManager.Ask(scheduledRequest, timeout);
                //TODO::VZ:: is there a better way to communicate with akka?
                result.Wait(timeout);
                //TODO::VZ refactor without casts
                var success = result.Result as MessageSuccessfullyProcessed;
                if (success != null)
                {
                    _quartzLogger.LogSuccess(context.JobDetail.Key.Name);
                }
                else
                {
                    var failure = result.Result as MessageProcessingFailed;
                    if (failure != null)
                    {
                        ExceptionDispatchInfo.Capture(failure.Exception).Throw();
                    }
                    throw new InvalidOperationException($"Wrong reply from task handler actor. Reply: ${result.Result}");
                }
            }
            catch (Exception e)
            {
                _quartzLogger.LogFailure(context.JobDetail.Key.Name, e);
                throw new JobExecutionException(e);
            }
        }

        private static ScheduledMessage DeserializeTaskData(JobDataMap jobDatMap)
        {
            var taskJson = jobDatMap[MessageKey] as string;
            //TODO::VZ:: use external wrapper around serializer?
            var task = JsonConvert.DeserializeObject<ScheduledMessage>(taskJson, JsonSerializerSettings);
            return task;
        }

        private static TimeSpan DeserializeTimeout(JobDataMap jobDatMap)
        {
            var timeout = TimeSpan.Parse(jobDatMap[Timeout] as string);
            return timeout;
        }

        public static JobBuilder Create(JobKey jobKey, ScheduledMessage message, TimeSpan timeout)
        {
            //TODO::VZ:: use external wrapper around serializer?
            var serialized = JsonConvert.SerializeObject(message, JsonSerializerSettings);
            var jdm = new JobDataMap { { MessageKey, serialized }, { Timeout, timeout.ToString() } };
            return JobBuilder
                        .Create<QuartzJob>()
                        .WithIdentity(jobKey)
                        .UsingJobData(jdm)
                        .StoreDurably(true)
                        .RequestRecovery(true);
        }
    }
}