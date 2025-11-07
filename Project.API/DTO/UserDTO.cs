using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Project.API.Constants;

namespace Project.API.DTO
{
    public class UserDTO
    {
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;

        public string Role { get; set; } = null!;
    }
}