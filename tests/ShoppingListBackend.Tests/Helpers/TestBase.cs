using Microsoft.EntityFrameworkCore;
using ShoppingListBackend.Api.Data;
using System;

namespace ShoppingListBackend.Tests.Helpers;

public abstract class TestBase : IDisposable
{
    protected readonly AppDbContext _context;

    protected TestBase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}