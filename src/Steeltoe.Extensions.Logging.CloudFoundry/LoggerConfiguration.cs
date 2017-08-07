using Microsoft.Extensions.Logging;


namespace Steeltoe.Extensions.Logging.CloudFoundry
{
    public class LoggerConfiguration : ILoggerConfiguration
    {
        public LoggerConfiguration(string name, LogLevel? configured, LogLevel effective)
        {
            Name = name;
            ConfiguredLevel = configured;
            EffectiveLevel = effective;
        }

        public string Name { get; }

        public LogLevel? ConfiguredLevel { get; }

        public LogLevel EffectiveLevel { get; }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            LoggerConfiguration lc = obj as LoggerConfiguration;
            if (lc == null)
            {
                return false;
            }
            return this.Name == lc.Name && 
                this.ConfiguredLevel == lc.ConfiguredLevel && 
                this.EffectiveLevel == lc.EffectiveLevel;
        }
        public override string ToString()
        {
            return "[" + Name + "," + ConfiguredLevel + "," + EffectiveLevel + "]";
        }
    }
}
