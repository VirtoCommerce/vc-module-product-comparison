using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace VirtoCommerce.ProductComparison.Tests.Infrastructure;

/// <summary>
/// The in-memory SQLite databases of the platform and its modules, one per DbContext, as each module has its own.
/// A database is created when its module first uses it, with the schema of the module's EF model
/// (<c>EnsureCreated</c>), so a test pays only for the modules it reaches. Each lives as long as its connection: the
/// provider that created this instance disposes them with it. Every command they run goes to the
/// <see cref="SqlCommandLog"/>.
/// </summary>
internal sealed class ModuleDatabases(SqlCommandLog commandLog) : IDisposable
{
    private readonly Lock _lock = new();
    private readonly List<SqliteConnection> _connections = [];

    /// <summary>A new database for <typeparamref name="TContext"/>, with its schema.</summary>
    public DbContextOptions<TContext> Create<TContext>() where TContext : DbContext
    {
        var connection = SqliteTestDbContextFactory.CreateConnection();

        lock (_lock)
        {
            _connections.Add(connection);
        }

        return SqliteTestDbContextFactory.CreateOptions<TContext>(connection, options => options.AddInterceptors(commandLog));
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var connection in _connections)
            {
                connection.Dispose();
            }

            _connections.Clear();
        }
    }
}
