using Akka.Configuration;

namespace GridDomain.Node.Configuration.Hocon {
    public class CustomConfig : IHoconConfig
    {
        private readonly Config _config;

        public CustomConfig(Config cfg)
        {
            _config = cfg;
        }

        public string Build()
        {
            return _config.ToString();
        }
    }
}