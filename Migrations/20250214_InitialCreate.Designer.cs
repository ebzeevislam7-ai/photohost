// ========== ФРЕЙМ МИГРАЦИИ: MigrationBuilder.cs ==========

using Microsoft.EntityFrameworkCore.Migrations;

namespace PhotoHost.Migrations
{
    public partial class InitialCreate : Migration
    {
        public override string GetUpScript()
        {
            return @"
CREATE TABLE [Photos] (
    [Id] uniqueidentifier NOT NULL,
    [FileName] nvarchar(255) NOT NULL,
    [Path] nvarchar(500) NOT NULL,
    [UploadDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_Photos] PRIMARY KEY ([Id])
);

CREATE INDEX [IX_Photos_UploadDate] ON [Photos] ([UploadDate] DESC);
            ";
        }
    }
}
