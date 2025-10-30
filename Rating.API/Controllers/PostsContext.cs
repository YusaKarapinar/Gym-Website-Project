using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Rating.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Rating.API.Controllers
{
    public class PostsContext : IdentityDbContext<AppUser, AppRole, int>
    {
        public DbSet<Post> Posts { get; set; }
        public PostsContext(DbContextOptions<PostsContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Additional configuration if needed
            builder.Entity<Post>().HasData(
                new Post
                {
                    PostId = 1,
                    Title = "First Post",
                    Content = "This is the content of the first post.",
                    CreatedAt = new DateTime(2023, 1, 15),
                    UpdatedAt = new DateTime(2023, 1, 15),
                    PostState = "Public"
                },
                new Post
                {
                    PostId = 2,
                    Title = "Second Post",
                    Content = "This is the content of the second post.",
                    CreatedAt = new DateTime(2023, 1, 15),
                    UpdatedAt = new DateTime(2023, 1, 15),
                    PostState = "Private"
                },
                new Post
                {
                    PostId = 3,
                    Title = "Third Post",
                    Content = "This is the content of the third post.",
                    CreatedAt = new DateTime(2023, 1, 15),
                    UpdatedAt = new DateTime(2023, 1, 15),
                    PostState = "Public"
                },
                new Post
                {
                    PostId = 4,
                    Title = "Fourth Post",
                    Content = "This is the content of the fourth post.",
                    CreatedAt = new DateTime(2023, 1, 15),
                    UpdatedAt = new DateTime(2023, 1, 15),
                    PostState = "Private"
                }
            );
        }
    }
}