using BepInEx.Logging;

namespace DeveloperConsole
{
    public partial class DeveloperConsole
    {
        private sealed class LogListener : ILogListener
        {
            public void Dispose() { }

            public void LogEvent(object sender, LogEventArgs eventArgs)
            {
                    OnEntryLogged(eventArgs);
            }
        }
    }
}
