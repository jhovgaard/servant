namespace Servant.Business.Objects
{
    public class Error
    {
        public bool IsGlobal { get; set; }
        public string Message { get; set; }
        public string PropertyName { get; set; }

        public Error(bool isGlobal, string message, string propertyName)
        {
            IsGlobal = isGlobal;
            Message = message;
            PropertyName = propertyName;
        }

        /// <summary>
        /// Creates a global message
        /// </summary>
        public Error(string message)
        {
            IsGlobal = true;
            Message = message;
        }
    }
}