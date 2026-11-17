using System.Security.Cryptography;
using System.Text;
using VHWuWa.Core.Services;
using Xunit;

namespace VHWuWa.Core.Tests;

public sealed class PakContentBatchPublisherTests : IDisposable
{
    private const string English = "WuWaVH_EN_99_P.pak";
    private const string HanViet = "WuWaVH_HanViet_99_P.pak";
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "WuWaPakBatchTests_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Publish_UpdatesBothPaks_WithoutTouchingUnrelatedMod()
    {
        var target = Path.Combine(_root, "content");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, English), "old English");
        File.WriteAllText(Path.Combine(target, HanViet), "old HanViet");
        File.WriteAllText(Path.Combine(target, "other-mod.pak"), "must survive");

        PakContentBatchPublisher.Publish(target, Payloads());

        Assert.Equal("new English", File.ReadAllText(Path.Combine(target, English)));
        Assert.Equal("new HanViet", File.ReadAllText(Path.Combine(target, HanViet)));
        Assert.Equal("must survive", File.ReadAllText(Path.Combine(target, "other-mod.pak")));
        Assert.DoesNotContain(Directory.GetDirectories(target),
            name => Path.GetFileName(name).StartsWith(".wuwa-pak-txn-", StringComparison.Ordinal));
    }

    [Fact]
    public void Publish_RejectsCorruptSecondPak_WithoutChangingFirst()
    {
        var target = Path.Combine(_root, "content");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, English), "old English");
        File.WriteAllText(Path.Combine(target, HanViet), "old HanViet");
        var files = Payloads();
        files[1] = files[1] with { ExpectedSha256 = new string('0', 64) };

        Assert.Throws<InvalidDataException>(() => PakContentBatchPublisher.Publish(target, files));

        Assert.Equal("old English", File.ReadAllText(Path.Combine(target, English)));
        Assert.Equal("old HanViet", File.ReadAllText(Path.Combine(target, HanViet)));
        Assert.DoesNotContain(Directory.GetDirectories(target),
            name => Path.GetFileName(name).StartsWith(".wuwa-pak-txn-", StringComparison.Ordinal));
    }

    [Fact]
    public void Publish_SimulatedFailureAfterFirstFile_RollsBothBack()
    {
        var target = Path.Combine(_root, "content");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, English), "old English");
        File.WriteAllText(Path.Combine(target, HanViet), "old HanViet");

        Assert.Throws<InvalidOperationException>(() => PakContentBatchPublisher.Publish(
            target, Payloads(), afterActivation: n =>
            {
                if (n == 1) throw new InvalidOperationException("simulated IO interruption");
            }));

        Assert.Equal("old English", File.ReadAllText(Path.Combine(target, English)));
        Assert.Equal("old HanViet", File.ReadAllText(Path.Combine(target, HanViet)));
    }

    [Fact]
    public void RecoverIncomplete_RestoresInterruptedTransactionOnNextStart()
    {
        var target = Path.Combine(_root, "content");
        Directory.CreateDirectory(target);
        var transaction = Path.Combine(target, ".wuwa-pak-txn-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(transaction);
        File.WriteAllText(Path.Combine(target, English), "partially updated");
        File.WriteAllText(Path.Combine(target, HanViet), "old HanViet");
        File.WriteAllText(Path.Combine(transaction, English + ".bak"), "old English");
        File.WriteAllText(Path.Combine(transaction, HanViet + ".bak"), "old HanViet");
        File.WriteAllText(Path.Combine(transaction, "journal.json"),
            "{\"Files\":[{\"Name\":\"WuWaVH_EN_99_P.pak\",\"Existed\":true}," +
            "{\"Name\":\"WuWaVH_HanViet_99_P.pak\",\"Existed\":true}]}");

        PakContentBatchPublisher.RecoverIncomplete(target);

        Assert.Equal("old English", File.ReadAllText(Path.Combine(target, English)));
        Assert.Equal("old HanViet", File.ReadAllText(Path.Combine(target, HanViet)));
        Assert.False(Directory.Exists(transaction));
    }

    [Fact]
    public void Publish_RejectsForeignModPathBeforeAnyFileIsWritten()
    {
        var target = Path.Combine(_root, "content");
        var file = CreateStaged("foreign mod");
        var payload = new PakContentBatchPublisher.Payload(
            "../other-mod.pak", file, Sha(file));

        Assert.Throws<InvalidDataException>(() =>
            PakContentBatchPublisher.Publish(target, new[] { payload }));
        Assert.False(Directory.Exists(target));
    }

    private List<PakContentBatchPublisher.Payload> Payloads()
    {
        var en = CreateStaged("new English");
        var hv = CreateStaged("new HanViet");
        return new List<PakContentBatchPublisher.Payload>
        {
            new(English, en, Sha(en)),
            new(HanViet, hv, Sha(hv)),
        };
    }

    private string CreateStaged(string data)
    {
        var dir = Path.Combine(_root, "source");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, Guid.NewGuid().ToString("N") + ".pak");
        File.WriteAllText(path, data, Encoding.UTF8);
        return path;
    }

    private static string Sha(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
