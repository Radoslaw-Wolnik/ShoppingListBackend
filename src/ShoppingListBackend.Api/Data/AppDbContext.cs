namespace ShoppingListBackend.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Device> Devices { get; set; }
    public DbSet<DeviceFriend> Friends { get; set; }
    public DbSet<ShoppingList> ShoppingLists { get; set; }
    public DbSet<ShoppingListCategory> ShoppingListCategories { get; set; }
    public DbSet<ShoppingListItem> ShoppingListItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ========== Device ==========
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => d.ApiKeyHash).IsUnique(); // Ensure API keys are unique
            entity.HasIndex(d => d.ApiKeySha256); // not unique; BCrypt collisions possible but unlikely

            // Owned shopping lists (one-to-many)
            entity.HasMany(d => d.OwnedShoppingLists)
                .WithOne(s => s.Owner)
                .HasForeignKey(s => s.OwnerDeviceId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting device while owning lists

            // Many-to-many: editable lists
            entity.HasMany(d => d.EditableShoppingLists)
                .WithMany(s => s.Editors)
                .UsingEntity<Dictionary<string, object>>(
                    "ShoppingListEditor",
                    j => j.HasOne<ShoppingList>().WithMany().HasForeignKey("ShoppingListId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<Device>().WithMany().HasForeignKey("DeviceId").OnDelete(DeleteBehavior.Cascade)
                );

            // Self many-to-many: friends
            entity.HasMany(d => d.Friends)
                .WithMany()
                .UsingEntity<DeviceFriend>(
                    j => j.HasOne(df => df.Friend).WithMany().HasForeignKey(df => df.FriendId),
                    j => j.HasOne(df => df.Device).WithMany().HasForeignKey(df => df.DeviceId),
                    j =>
                    {
                        j.HasKey(df => new { df.DeviceId, df.FriendId });
                        j.ToTable("DeviceFriends");
                    });
        });

        // ========== ShoppingList ==========
        modelBuilder.Entity<ShoppingList>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Title).IsRequired().HasMaxLength(200);
            entity.HasIndex(s => s.OwnerDeviceId); // For queries by owner
            entity.Property(s => s.IsPrivate).HasDefaultValue(false);

            // One-to-many: Categories
            entity.HasMany(s => s.Categories)
                .WithOne(c => c.ShoppingList)
                .HasForeignKey(c => c.ShoppingListId)
                .OnDelete(DeleteBehavior.Cascade); // Delete categories when list is deleted

            // Note: Editors is configured via Device's many-to-many above
        });

        // ========== ShoppingListCategory ==========
        modelBuilder.Entity<ShoppingListCategory>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(c => new { c.ShoppingListId, c.Position }) // Ordering index
                .IsUnique(false); // Unique not required because position can repeat if user reorders, but index helps

            // One-to-many: Items
            entity.HasMany(c => c.Items)
                .WithOne(i => i.ShoppingListCategory)
                .HasForeignKey(i => i.ShoppingListCategoryId)
                .OnDelete(DeleteBehavior.Cascade); // Delete items when category is deleted
        });

        // ========== ShoppingListItem ==========
        modelBuilder.Entity<ShoppingListItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Description).IsRequired().HasMaxLength(500);
            entity.HasIndex(i => new { i.ShoppingListCategoryId, i.Position })
                .IsUnique(false); // Index for ordering
            entity.Property(i => i.IsChecked).HasDefaultValue(false);
        });
    }
}