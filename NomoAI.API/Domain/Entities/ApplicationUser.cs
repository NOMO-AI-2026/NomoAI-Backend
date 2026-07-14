using Microsoft.AspNetCore.Identity;
using NomoAI.API.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NomoAI.API.Domain.Entities
{
    public class ApplicationUser:IdentityUser
    {
        public string Fullname { get; set; } = string.Empty;

        public int Age { get; set; }
        public Gender Gender { get; set; }
       // public UserRole Role { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
