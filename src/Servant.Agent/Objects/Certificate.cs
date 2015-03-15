namespace Servant.Agent.Objects
{
    public class Certificate
    {
        public byte[] Hash { get; set; }
        public string Thumbprint { get; set; }
        public string Name { get; set; }
    }
}