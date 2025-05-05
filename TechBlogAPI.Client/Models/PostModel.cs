using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TechBlogAPI.Client.Models
{
    public class PostModel
    {
        public int PostId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Author Author { get; set; }
        public List<Category> Categories { get; set; }
    }
}