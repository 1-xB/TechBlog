using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TechBlogAPI.Client.Models;

public class Author
{
    public int AuthorId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}