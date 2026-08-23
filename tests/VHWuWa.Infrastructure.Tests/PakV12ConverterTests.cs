using System.Buffers.Binary;
using System.Security.Cryptography;
using VHWuWa.Infrastructure;
using Xunit;

namespace VHWuWa.Infrastructure.Tests;

public sealed class PakV12ConverterTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "VHWuWa_PakTest_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void ConvertV11ToV12_RewritesEntriesOffsetsAndFooter()
    {
        Directory.CreateDirectory(_tempDir);
        var v11 = Path.Combine(_tempDir, "font.v11.pak");
        var v12 = Path.Combine(_tempDir, "font.v12.pak");
        File.WriteAllBytes(v11, CreateMinimalV11Pak());

        PakV12Converter.ConvertV11ToV12(v11, v12);

        Assert.True(PakV12Converter.TryVerifyV12(v12, out var error), error);
        var oldData = File.ReadAllBytes(v11);
        var newData = File.ReadAllBytes(v12);
        Assert.Equal(oldData.Length + 1, newData.Length);

        var footer = newData.AsSpan(newData.Length - 221, 221);
        Assert.Equal(12u, BinaryPrimitives.ReadUInt32LittleEndian(footer.Slice(21, 4)));
        var primaryOffset = checked((int)BinaryPrimitives.ReadUInt64LittleEndian(footer.Slice(25, 8)));

        // Bỏ qua mount, số entry, seed, hai mô tả index rồi đọc blob encoded entry.
        var cursor = primaryOffset;
        var mountLength = BinaryPrimitives.ReadInt32LittleEndian(newData.AsSpan(cursor, 4));
        cursor += 4 + mountLength + 4 + 8 + 40 + 40;
        Assert.Equal(13, BinaryPrimitives.ReadInt32LittleEndian(newData.AsSpan(cursor, 4)));
        cursor += 4;
        Assert.Equal(0xE0000000u, BinaryPrimitives.ReadUInt32LittleEndian(newData.AsSpan(cursor, 4)));
        Assert.Equal(0, newData[cursor + 4]);
        Assert.Equal(456u, BinaryPrimitives.ReadUInt32LittleEndian(newData.AsSpan(cursor + 5, 4)));
        Assert.Equal(123u, BinaryPrimitives.ReadUInt32LittleEndian(newData.AsSpan(cursor + 9, 4)));
    }

    [Fact]
    public void TryVerifyV12_RejectsCorruptPrimaryIndex()
    {
        Directory.CreateDirectory(_tempDir);
        var v11 = Path.Combine(_tempDir, "font.v11.pak");
        var v12 = Path.Combine(_tempDir, "font.v12.pak");
        File.WriteAllBytes(v11, CreateMinimalV11Pak());
        PakV12Converter.ConvertV11ToV12(v11, v12);

        var data = File.ReadAllBytes(v12);
        data[64] ^= 0xFF;
        File.WriteAllBytes(v12, data);

        Assert.False(PakV12Converter.TryVerifyV12(v12, out var error));
        Assert.Contains("SHA-1", error, StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] CreateMinimalV11Pak()
    {
        const int dataPrefixSize = 64;
        var pathHashIndex = Build(writer =>
        {
            writer.Write(1);
            writer.Write(0x1122334455667788UL);
            writer.Write(0);
        });
        var fullDirectoryIndex = Build(writer =>
        {
            writer.Write(1);
            WriteFString(writer, "./");
            writer.Write(1);
            WriteFString(writer, "LaguSansBold.ufont");
            writer.Write(0);
        });

        var mount = Build(writer => WriteFString(writer, "../../../"));
        const int primarySize =
            4 + 10 + // FString mount: length + "../../../" + NUL
            4 + 8 +
            4 + 8 + 8 + 20 +
            4 + 8 + 8 + 20 +
            4 + 12;
        var pathHashOffset = dataPrefixSize + primarySize;
        var fullDirectoryOffset = pathHashOffset + pathHashIndex.Length;
        var primary = Build(writer =>
        {
            writer.Write(mount);
            writer.Write(1);
            writer.Write(0x0102030405060708UL);
            writer.Write(1);
            writer.Write((long)pathHashOffset);
            writer.Write((long)pathHashIndex.Length);
            writer.Write(SHA1.HashData(pathHashIndex));
            writer.Write(1);
            writer.Write((long)fullDirectoryOffset);
            writer.Write((long)fullDirectoryIndex.Length);
            writer.Write(SHA1.HashData(fullDirectoryIndex));
            writer.Write(12);
            writer.Write(0xE0000000u);
            writer.Write(123u);
            writer.Write(456u);
        });
        Assert.Equal(primarySize, primary.Length);

        var footer = new byte[221];
        footer[17] = 0xE1;
        footer[18] = 0x12;
        footer[19] = 0x6F;
        footer[20] = 0x5A;
        BinaryPrimitives.WriteUInt32LittleEndian(footer.AsSpan(21, 4), 11);
        BinaryPrimitives.WriteUInt64LittleEndian(footer.AsSpan(25, 8), dataPrefixSize);
        BinaryPrimitives.WriteUInt64LittleEndian(footer.AsSpan(33, 8), (ulong)primary.Length);
        SHA1.HashData(primary).CopyTo(footer, 41);

        return new byte[dataPrefixSize]
            .Concat(primary)
            .Concat(pathHashIndex)
            .Concat(fullDirectoryIndex)
            .Concat(footer)
            .ToArray();
    }

    private static void WriteFString(BinaryWriter writer, string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value + '\0');
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    private static byte[] Build(Action<BinaryWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        write(writer);
        writer.Flush();
        return stream.ToArray();
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
