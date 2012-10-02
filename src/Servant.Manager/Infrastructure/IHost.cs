using Servant.Business.Objects;

namespace Servant.Manager.Infrastructure
{
    public interface IHost
    {
        void Start(Settings settings = null);
        void Stop();
        void Kill();
    }
}