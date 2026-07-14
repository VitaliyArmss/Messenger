using Microsoft.EntityFrameworkCore;
using Messenger.Entities;

namespace Messenger.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {

    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatMember>()
            .HasKey(x => new
            {
                x.UserId,
                x.ChatId
            });
    }

    public DbSet<User> Users { get; set; }

    public DbSet<Chat> Chats { get; set; }

    public DbSet<Message> Messages { get; set; }

    public DbSet<ChatMember> ChatMembers { get; set; }
}