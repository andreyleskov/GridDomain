using GridDomain.Node.Akka.Configuration.Hocon;

namespace GridDomain.Node.Tests.TestJournals.Hocon
{
    internal class TestJournalConfig : IHoconConfig
    {
        public string Build()
        {
        string config =
            @"akka.persistence.journal.plugin = ""akka.persistence.journal.inmem""
              akka.persistence.journal.inmem.class = """+typeof(TestJournal).AssemblyQualifiedShortName()+@"""
              akka.persistence.query.journal.plugin = """+TestReadJournal.Identifier+@"""
              akka.persistence.query.journal.test.class = """+typeof(TestReadJournalProvider).AssemblyQualifiedShortName()+@"""";
            return config;
        }
    }
}