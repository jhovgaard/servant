using System;
using Servant.Business.Objects;

namespace Servant.Business
{
    public interface IHost
    {
        bool Debug { get; set; }
        DateTime StartupTime { get; set; }
        void Start(ServantConfiguration configuration = null);
        void Stop();
        void Kill();
        void Update();
        void RemoveCertificateBinding(int port);
        void AddCertificateBinding(int port);
        void StartWebSocket();
    }
}