using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TechBlogAPI.Entity
{
    public class LoginAttempt
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string IpAddress { get; set; }
        public bool IsSuccessful { get; set; }
        public DateTime AttemptTime { get; set; }
    }
}