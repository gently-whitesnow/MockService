using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;

namespace MockService.Models
{
    [BsonIgnoreExtraElements]
    public class Mock
    {
        [BsonId] public string Id { get; set; }
        [Required]
        public string FilterName { get; set; }
        [Required]
        public bool IsPdf { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public bool IsActive { get; set; }

        [JsonIgnore] public BsonValue BodyForMongo { get; set; }

        [BsonIgnore] public JContainer Body { get; set; }
        [Required]
        public HttpStatusCode StatusCode { get; set; }
        [Required]
        public string Path { get; set; }
        [Required]
        public HttpMethod Method { get; set; }
        public DateTime CreateDate { get; set; }
    }
}