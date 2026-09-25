using Dapper;
using Npgsql;
using TestJob.Api.Models;

namespace TestJob.Api.Data;

public interface IElementRepository
{
    Task InsertAsync(IReadOnlyCollection<DiscoveredElement> elements, CancellationToken cancellationToken);
}

public sealed class ElementRepository(NpgsqlDataSource dataSource) : IElementRepository
{
    private const string CreateTableSql = """
        CREATE TABLE IF NOT EXISTS elements
        (
            id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            attribute_value TEXT NOT NULL,
            html TEXT NOT NULL
        );
        """;

    private const string InsertSql = """
        INSERT INTO elements (attribute_value, html)
        VALUES (@AttributeValue, @Html);
        """;

    public async Task InsertAsync(
        IReadOnlyCollection<DiscoveredElement> elements,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            CreateTableSql,
            cancellationToken: cancellationToken));

        if (elements.Count == 0)
        {
            return;
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            InsertSql,
            elements,
            transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
    }
}
