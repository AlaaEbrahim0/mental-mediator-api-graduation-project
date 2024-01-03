﻿using Domain.Entities;
using Infrastructure.Configurations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;
public class AppDbContext: IdentityDbContext<AppUser> 
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {

    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        var ids = new string[20];
        for (int i = 0; i < 20; i++)
        {
            ids[i] = (Guid.NewGuid().ToString());
        }
        builder.ApplyConfiguration(new AppUserConfiguration(ids));
        builder.ApplyConfiguration(new RoleConfiguration());
        builder.ApplyConfiguration(new PostConfiguration(ids));
        builder.ApplyConfiguration(new CommentConfiguration(ids));
        builder.ApplyConfiguration(new ReplyConfiguration(ids));


        base.OnModelCreating(builder);
    }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Reply> Replies { get; set; }
}
