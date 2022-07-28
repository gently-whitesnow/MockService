namespace MockService.Models
{
    public class MongoOptions
    {
        public string[] Servers { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Database { get; set; }
        public string MocksCollectionName { get; set; }
        public string ReplicaSetName { get; set; }
    }
}