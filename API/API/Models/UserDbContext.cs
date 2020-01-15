using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Models
{
    public class UserDbContext : DbContext
    {
        public UserDbContext( DbContextOptions<UserDbContext> options) : base(options)
        {
        }

        public virtual DbSet<User> Users { get; set; }
    }
}
