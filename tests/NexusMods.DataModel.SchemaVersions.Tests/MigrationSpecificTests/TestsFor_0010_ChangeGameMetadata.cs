using FluentAssertions;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Games;
using Xunit.Abstractions;

namespace NexusMods.DataModel.SchemaVersions.Tests.MigrationSpecificTests;

public class TestsFor_0010_ChangeGameMetadata(ITestOutputHelper helper) : ALegacyDatabaseTest(helper)
{
    [Fact]
    public async Task Test()
    {
        await using var migrationHost = await ConnectionFor("Migration-10.rocksdb.zip");

        var connection = migrationHost.Connection;

        connection.TxId.Value.Should().Be(connection.DatomStore.AsOfTxId.Value);
        // connection.Db.BasisTxId.Value.Should().Be(connection.TxId.Value); // fails because DB hasn't been updated

        // var db = connection.Db;
        // db.BasisTxId.Value.Should().NotBe(72057594037927971); // fails because DB hasn't been updated

        // TODO: connection is one DB revision behind
        var db = connection.AsOf(connection.TxId);
        db.BasisTxId.Value.Should().NotBe(72057594037927971);

        // NOTE(erri120): because of our testing framework, this will only have stardew valley installations
        var entities = GameInstallMetadata.All(db);
        entities.Count.Should().Be(4);
        entities.Should().AllSatisfy(x => x.GameId.Value.Should().Be(GameId.From("StardewValley").Value));
    }
}
