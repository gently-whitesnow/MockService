using System.Net;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;

namespace MockService.Models
{
    public record MockDto
    {
        public string Id { get; set; }
        public string FilterName { get; set; }
        public bool IsPdf { get; set; }

        public string Description { get; set; }
        public bool IsActive { get; set; }

        public object Body { get; set; }

        public HttpStatusCode StatusCode { get; set; }

        public string Path { get; set; }
        
        public string Domain { get; set; }

        public HttpMethod Method { get; set; }
    }
}