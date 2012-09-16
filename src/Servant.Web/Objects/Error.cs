namespace Servant.Web.Objects
{
    public class Error
    {
        public bool IsGlobal { get; set; }
        public string Message { get; set; }
        public string PropertyName { get; set; }

    }
}