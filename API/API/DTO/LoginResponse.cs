using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTO
{
    public class LoginResponse
    {
        public string  AccessToken { get; set; }
        public string ExpiryDateTime { get; set; }
    }
}
