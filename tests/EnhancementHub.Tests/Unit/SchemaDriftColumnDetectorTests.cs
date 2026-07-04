using EnhancementHub.Application.Abstractions.Models;
using EnhancementHub.Domain.Entities;
using EnhancementHub.Domain.Enums;
using EnhancementHub.Infrastructure.Services.SystemIntelligence;
using FluentAssertions;

namespace EnhancementHub.Tests.Unit;

public sealed class SchemaDriftColumnDetectorTests
{
    [Theory]
    [InlineData("int", "INTEGER", true)]
    [InlineData("string", "nvarchar(100)", true)]
    [InlineData("bool", "bit", true)]
    [InlineData("Guid", "uniqueidentifier", true)]
    [InlineData("int", "text", false)]
    public void TypesCompatible_MapsCommonClrAndDatabaseTypes(string clrType, string dbType, bool expected) =>
        SchemaDriftDetectorService.TypesCompatible(clrType, dbType).Should().Be(expected);

    [Fact]
    public void CompareColumns_FlagsNullableMismatch()
    {
        var mapping = new CodeEntityMapping
        {
            EntityClassName = "Order",
            EntityFilePath = "Data/Order.cs",
            Properties =
            [
                new CodeEntityProperty
                {
                    PropertyName = "Total",
                    ColumnName = "Total",
                    ClrType = "decimal",
                    IsNullable = false
                }
            ]
        };

        var table = new DatabaseTable
        {
            SchemaName = "dbo",
            TableName = "Orders",
            Columns =
            [
                new DatabaseColumn
                {
                    Name = "Total",
                    DataType = "decimal",
                    IsNullable = true
                }
            ]
        };

        var findings = new List<DriftFindingDto>();
        InvokeCompareColumns(mapping, table, findings);

        findings.Should().ContainSingle(f => f.DriftType == DriftType.NullableMismatch);
    }

    [Fact]
    public void CompareColumns_FlagsMissingDatabaseColumn()
    {
        var mapping = new CodeEntityMapping
        {
            EntityClassName = "Customer",
            EntityFilePath = "Data/Customer.cs",
            Properties =
            [
                new CodeEntityProperty
                {
                    PropertyName = "Email",
                    ColumnName = "Email",
                    ClrType = "string",
                    IsNullable = true
                }
            ]
        };

        var table = new DatabaseTable
        {
            SchemaName = "dbo",
            TableName = "Customers",
            Columns =
            [
                new DatabaseColumn
                {
                    Name = "Id",
                    DataType = "int",
                    IsNullable = false
                }
            ]
        };

        var findings = new List<DriftFindingDto>();
        InvokeCompareColumns(mapping, table, findings);

        findings.Should().Contain(f => f.DriftType == DriftType.MissingInDatabase);
    }

    private static void InvokeCompareColumns(
        CodeEntityMapping mapping,
        DatabaseTable table,
        List<DriftFindingDto> findings)
    {
        var method = typeof(SchemaDriftDetectorService).GetMethod(
            "CompareColumns",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method!.Invoke(null, [mapping, table, findings]);
    }
}
