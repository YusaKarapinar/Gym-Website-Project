using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rating.API.DTO
{
    public class PostDTO
    {
        public int PostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}