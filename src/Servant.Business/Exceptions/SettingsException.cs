using System;

namespace Servant.Business.Exceptions
{
    public class SettingsException : Exception
    {
        public SettingsException(string message) : base(message)
        {
        }
    }
}