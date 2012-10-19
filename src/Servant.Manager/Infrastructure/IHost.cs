using System;
using Servant.Business.Objects;

namespace Servant.Manager.Infrastructure
{
    public interface IHost
    {
        bool LogParsingStarted { get; set; }
        bool Debug { get; set; }
        DateTime StartupTime { get; set; }
        void Start(Settings settings = null);
        void Stop();
        void Kill();
        void StartLogParsing();
        void StopLogParsing();
        void LoadSettings();
    }
}