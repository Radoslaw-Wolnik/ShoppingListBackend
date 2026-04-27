using Microsoft.EntityFrameworkCore;
using ShoppingListBackend.Api.Models;

namespace ShoppingListBackend.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Device> Devices { get; set; }
    public DbSet<DeviceFriend> DeviceFriends { get; set; }
    public DbSet<ShoppingList> ShoppingLists { get; set; }
    public DbSet<ShoppingListCategory> ShoppingListCategories { get; set; }
    public DbSet<ShoppingListItem> ShoppingListItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => d.ApiKeyHash).IsUnique();
            entity.HasIndex(d => d.ApiKeySha256);
        });

        modelBuilder.Entity<DeviceFriend>(entity =>
        {
            entity.ToTable("DeviceFriends");
            entity.HasKey(df => new { df.DeviceId, df.FriendId });

            entity.HasOne(df => df.Device)
                  .WithMany(d => d.DeviceFriends)
                  .HasForeignKey(df => df.DeviceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(df => df.Friend)
                  .WithMany(d => d.FriendOf)
                  .HasForeignKey(df => df.FriendId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ShoppingList>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Title).IsRequired().HasMaxLength(200);
            entity.HasIndex(s => s.OwnerDeviceId);
            entity.Property(s => s.IsPrivate).HasDefaultValue(false);

            entity.HasMany(s => s.Categories)
                  .WithOne(c => c.ShoppingList)
                  .HasForeignKey(c => c.ShoppingListId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(s => s.Editors)
                  .WithMany(d => d.EditableShoppingLists)
                  .UsingEntity<Dictionary<string, object>>(
                      "ShoppingListEditor",
                      j => j.HasOne<Device>()
                            .WithMany()
                            .HasForeignKey("DeviceId")
                            .OnDelete(DeleteBehavior.Cascade),
                      j => j.HasOne<ShoppingList>()
                            .WithMany()
                            .HasForeignKey("ShoppingListId")
                            .OnDelete(DeleteBehavior.Cascade),
                      j =>
                      {
                          j.HasKey("DeviceId", "ShoppingListId");
                          j.ToTable("ShoppingListEditors");
                      });
        });

        modelBuilder.Entity<ShoppingListCategory>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(c => new { c.ShoppingListId, c.Position });

            entity.HasMany(c => c.Items)
                  .WithOne(i => i.ShoppingListCategory)
                  .HasForeignKey(i => i.ShoppingListCategoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ShoppingListItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Description).IsRequired().HasMaxLength(500);
            entity.HasIndex(i => new { i.ShoppingListCategoryId, i.Position });
            entity.Property(i => i.IsChecked).HasDefaultValue(false);
        });
    }
}