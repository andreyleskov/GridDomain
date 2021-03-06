using System.Linq;
using GridDomain.CQRS;
using GridDomain.EventSourcing;
using GridDomain.ProcessManagers.DomainBind;
using GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain;
using GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain.Commands;
using GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain.Events;
using Xunit;

namespace GridDomain.Tests.Unit.ProcessManagers
{
    public class Given_process_When_extracting_descriptor
    {
        private IProcessDescriptor Descriptor { get; } =  ProcessDescriptor.ScanByConvention(typeof(SoftwareProgrammingProcess));

        [Fact]
        public void Descriptor_can_be_created_from_process()
        {
            Assert.NotNull(Descriptor);
        }

        [Fact]
        public void Descriptor_contains_all_domain_event_types_from_process()
        {
            var expectedEvents = new[]
                                 {
                                     typeof(GotTiredEvent),
                                     typeof(CoffeMadeEvent),
                                     typeof(SleptWellEvent),
                                     typeof(Fault<GoSleepCommand>),
                                     typeof(CoffeMakeFailedEvent)
                                 };

            Assert.Equal(expectedEvents, Descriptor.AcceptMessages.Select(m => m.MessageType).ToArray());
        }

        [Fact]
        public void Descriptor_contains_process_correlation_field_by_default()
        {
            var expectedEvents = new[]
                                 {
                                     nameof(DomainEvent.ProcessId),
                                     nameof(DomainEvent.ProcessId),
                                     nameof(DomainEvent.ProcessId),
                                     nameof(DomainEvent.ProcessId),
                                     nameof(Fault.ProcessId)
                                 };

            Assert.Equal(expectedEvents, Descriptor.AcceptMessages.Select(m => m.CorrelationField));
        }

        [Fact]
        public void Descriptor_contains_process_data_type()
        {
            Assert.Equal(typeof(SoftwareProgrammingState), Descriptor.StateType);
        }

        [Fact]
        public void Descriptor_contains_process_machine_type()
        {
            Assert.Equal(typeof(SoftwareProgrammingProcess), Descriptor.ProcessType);
        }
    }
}