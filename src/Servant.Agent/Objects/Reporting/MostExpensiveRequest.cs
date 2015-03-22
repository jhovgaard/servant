namespace Servant.Business.Objects.Reporting
{
    public class MostExpensiveRequest
    {
        public string Uri { get; set; }
        public string Querystring { get; set; }
        public int Count { get; set; }
        public int AverageTimeTaken { get; set; }
 
    }
}