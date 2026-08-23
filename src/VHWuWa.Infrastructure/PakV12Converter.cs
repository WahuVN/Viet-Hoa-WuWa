using System.Buffers.Binary;
using System.Security.Cryptography;

namespace VHWuWa.Infrastructure;

/// <summary>
/// Chuyển PAK chuẩn Unreal V11 do repak tạo sang biến thể V12 mà Wuthering Waves sử dụng.
/// </summary>
public static class PakV12Converter
{
    private const int FooterSize = 221;
    private static ReadOnlySpan<byte> Magic => [0xE1, 0x12, 0x6F, 0x5A];

    public static void ConvertV11ToV12(string v11Path, string v12Path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(v11Path);
        ArgumentException.ThrowIfNullOrWhiteSpace(v12Path);

        var data = File.ReadAllBytes(v11Path);
        if (data.Length < FooterSize)
            throw new InvalidDataException("PAK V11 quá nhỏ hoặc thiếu footer 221 byte.");

        var footer = data.AsSpan(data.Length - FooterSize, FooterSize).ToArray();
        var footerMagic = footer.AsSpan().LastIndexOf(Magic);
        if (footerMagic < 17 || footerMagic + 44 > footer.Length)
            throw new InvalidDataException("Không tìm thấy footer PAK hợp lệ.");

        var version = ReadUInt32(footer, footerMagic + 4);
        if (version != 11)
            throw new InvalidDataException($"repak không tạo PAK V11 (phiên bản nhận được: {version}).");

        var indexOffset = ReadUInt64(footer, footerMagic + 8);
        var indexSize = ReadUInt64(footer, footerMagic + 16);
        var index = Slice(data, indexOffset, indexSize, "primary index");

        var cursor = 0;
        var mountLength = ReadInt32(index, ref cursor, "mount point");
        var mountByteLength = FStringByteLength(mountLength, "mount point");
        EnsureAvailable(index, cursor, mountByteLength, "mount point");
        var mount = index.AsSpan(0, checked(cursor + mountByteLength)).ToArray();
        cursor += mountByteLength;

        var entryCount = ReadInt32(index, ref cursor, "số entry");
        if (entryCount < 0)
            throw new InvalidDataException("Số entry trong PAK không hợp lệ.");

        var seed = Take(index, ref cursor, 8, "seed");
        var hasPathHashIndex = ReadInt32(index, ref cursor, "cờ PathHashIndex");
        var pathHashOffset = ReadInt64(index, ref cursor, "vị trí PathHashIndex");
        var pathHashSize = ReadInt64(index, ref cursor, "kích thước PathHashIndex");
        _ = Take(index, ref cursor, 20, "SHA-1 PathHashIndex cũ");

        var hasFullDirectoryIndex = ReadInt32(index, ref cursor, "cờ FullDirectoryIndex");
        var fullDirectoryOffset = ReadInt64(index, ref cursor, "vị trí FullDirectoryIndex");
        var fullDirectorySize = ReadInt64(index, ref cursor, "kích thước FullDirectoryIndex");
        _ = Take(index, ref cursor, 20, "SHA-1 FullDirectoryIndex cũ");

        if (hasPathHashIndex != 1 || hasFullDirectoryIndex != 1)
            throw new InvalidDataException("PAK thiếu PathHashIndex hoặc FullDirectoryIndex cần thiết.");
        if (pathHashOffset < 0 || pathHashSize < 0 || fullDirectoryOffset < 0 || fullDirectorySize < 0)
            throw new InvalidDataException("Vị trí index trong PAK không hợp lệ.");

        var encodedSize = ReadInt32(index, ref cursor, "kích thước encoded entries");
        var expectedEncodedSize = checked(entryCount * 12);
        if (encodedSize != expectedEncodedSize)
            throw new InvalidDataException(
                $"PAK có entry nén hoặc định dạng chưa hỗ trợ (encodedSize={encodedSize}, entries={entryCount}).");

        var encoded = Take(index, ref cursor, encodedSize, "encoded entries");
        var trailing = index.AsSpan(cursor).ToArray();
        var pathHashIndex = Slice(data, (ulong)pathHashOffset, (ulong)pathHashSize, "PathHashIndex");
        var fullDirectoryIndex = Slice(data, (ulong)fullDirectoryOffset, (ulong)fullDirectorySize, "FullDirectoryIndex");

        var convertedEntries = ConvertEntries(encoded, entryCount);
        RemapPathHashOffsets(pathHashIndex, encodedSize);
        RemapFullDirectoryOffsets(fullDirectoryIndex, encodedSize);

        var primaryLength = checked(
            mount.Length + 4 + seed.Length +
            4 + 8 + 8 + 20 +
            4 + 8 + 8 + 20 +
            4 + convertedEntries.Length + trailing.Length);
        var newPathHashOffset = checked((long)indexOffset + primaryLength);
        var newFullDirectoryOffset = checked(newPathHashOffset + pathHashIndex.Length);

        byte[] primary;
        using (var stream = new MemoryStream(primaryLength))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(mount);
            writer.Write(entryCount);
            writer.Write(seed);
            writer.Write(1);
            writer.Write(newPathHashOffset);
            writer.Write((long)pathHashIndex.Length);
            writer.Write(SHA1.HashData(pathHashIndex));
            writer.Write(1);
            writer.Write(newFullDirectoryOffset);
            writer.Write((long)fullDirectoryIndex.Length);
            writer.Write(SHA1.HashData(fullDirectoryIndex));
            writer.Write(convertedEntries.Length);
            writer.Write(convertedEntries);
            writer.Write(trailing);
            writer.Flush();
            primary = stream.ToArray();
        }

        WriteUInt32(footer, footerMagic + 4, 12);
        WriteUInt64(footer, footerMagic + 8, indexOffset);
        WriteUInt64(footer, footerMagic + 16, (ulong)primary.Length);
        SHA1.HashData(primary).CopyTo(footer, footerMagic + 24);

        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(v12Path));
        if (!string.IsNullOrWhiteSpace(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        using var output = new FileStream(v12Path, FileMode.Create, FileAccess.Write, FileShare.None);
        output.Write(data, 0, checked((int)indexOffset));
        output.Write(primary);
        output.Write(pathHashIndex);
        output.Write(fullDirectoryIndex);
        output.Write(footer);
    }

    public static bool TryVerifyV12(string pakPath, out string error)
    {
        try
        {
            var data = File.ReadAllBytes(pakPath);
            if (data.Length < FooterSize)
                throw new InvalidDataException("PAK quá nhỏ hoặc thiếu footer 221 byte.");

            var footer = data.AsSpan(data.Length - FooterSize, FooterSize);
            var footerMagic = footer.LastIndexOf(Magic);
            if (footerMagic < 17 || footerMagic + 44 > footer.Length)
                throw new InvalidDataException("Không tìm thấy footer PAK hợp lệ.");
            if (ReadUInt32(footer, footerMagic + 4) != 12)
                throw new InvalidDataException("PAK không phải phiên bản V12.");

            var indexOffset = ReadUInt64(footer, footerMagic + 8);
            var indexSize = ReadUInt64(footer, footerMagic + 16);
            var primary = Slice(data, indexOffset, indexSize, "primary index");
            var expectedHash = footer.Slice(footerMagic + 24, 20);
            if (!CryptographicOperations.FixedTimeEquals(SHA1.HashData(primary), expectedHash))
                throw new InvalidDataException("SHA-1 của primary index không khớp.");
            if (!footer.Slice(footerMagic - 17, 17).SequenceEqual(new byte[17]))
                throw new InvalidDataException("Footer V12 thiếu 17 byte GUID/mã hóa bắt buộc.");

            error = string.Empty;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
        {
            error = ex.Message;
            return false;
        }
    }

    private static byte[] ConvertEntries(byte[] encoded, int entryCount)
    {
        var converted = new byte[checked(entryCount * 13)];
        for (var entry = 0; entry < entryCount; entry++)
        {
            var oldOffset = entry * 12;
            var newOffset = entry * 13;
            var flags = ReadUInt32(encoded, oldOffset);
            var fileOffset = ReadUInt32(encoded, oldOffset + 4);
            var fileSize = ReadUInt32(encoded, oldOffset + 8);
            WriteUInt32(converted, newOffset, flags);
            converted[newOffset + 4] = 0;
            WriteUInt32(converted, newOffset + 5, fileSize);
            WriteUInt32(converted, newOffset + 9, fileOffset);
        }
        return converted;
    }

    private static void RemapPathHashOffsets(byte[] index, int encodedSize)
    {
        var cursor = 0;
        var count = ReadInt32(index, ref cursor, "số phần tử PathHashIndex");
        if (count < 0)
            throw new InvalidDataException("Số phần tử PathHashIndex không hợp lệ.");
        for (var i = 0; i < count; i++)
        {
            _ = Take(index, ref cursor, 8, "hash đường dẫn");
            var offsetPosition = cursor;
            var oldOffset = ReadInt32(index, ref cursor, "offset PathHashIndex");
            WriteInt32(index, offsetPosition, RemapEntryOffset(oldOffset, encodedSize));
        }
    }

    private static void RemapFullDirectoryOffsets(byte[] index, int encodedSize)
    {
        var cursor = 0;
        var directoryCount = ReadInt32(index, ref cursor, "số thư mục FullDirectoryIndex");
        if (directoryCount < 0)
            throw new InvalidDataException("Số thư mục FullDirectoryIndex không hợp lệ.");

        for (var directory = 0; directory < directoryCount; directory++)
        {
            SkipFString(index, ref cursor, "tên thư mục");
            var fileCount = ReadInt32(index, ref cursor, "số file trong thư mục");
            if (fileCount < 0)
                throw new InvalidDataException("Số file trong FullDirectoryIndex không hợp lệ.");
            for (var file = 0; file < fileCount; file++)
            {
                SkipFString(index, ref cursor, "tên file");
                var offsetPosition = cursor;
                var oldOffset = ReadInt32(index, ref cursor, "offset FullDirectoryIndex");
                WriteInt32(index, offsetPosition, RemapEntryOffset(oldOffset, encodedSize));
            }
        }
    }

    private static int RemapEntryOffset(int oldOffset, int encodedSize)
    {
        if (oldOffset < 0 || oldOffset >= encodedSize || oldOffset % 12 != 0)
            throw new InvalidDataException($"Offset encoded entry không hợp lệ: {oldOffset}.");
        return checked(oldOffset / 12 * 13);
    }

    private static void SkipFString(byte[] data, ref int cursor, string label)
    {
        var length = ReadInt32(data, ref cursor, label);
        var bytes = FStringByteLength(length, label);
        EnsureAvailable(data, cursor, bytes, label);
        cursor += bytes;
    }

    private static int FStringByteLength(int length, string label)
    {
        try
        {
            return length >= 0 ? length : checked(-length * 2);
        }
        catch (OverflowException)
        {
            throw new InvalidDataException($"Độ dài FString {label} không hợp lệ.");
        }
    }

    private static byte[] Slice(byte[] data, ulong offset, ulong size, string label)
    {
        if (offset > int.MaxValue || size > int.MaxValue || offset + size > (ulong)data.Length)
            throw new InvalidDataException($"{label} nằm ngoài phạm vi PAK.");
        return data.AsSpan((int)offset, (int)size).ToArray();
    }

    private static byte[] Take(byte[] data, ref int cursor, int count, string label)
    {
        EnsureAvailable(data, cursor, count, label);
        var result = data.AsSpan(cursor, count).ToArray();
        cursor += count;
        return result;
    }

    private static void EnsureAvailable(byte[] data, int cursor, int count, string label)
    {
        if (cursor < 0 || count < 0 || cursor > data.Length - count)
            throw new InvalidDataException($"Dữ liệu {label} bị thiếu hoặc hỏng.");
    }

    private static int ReadInt32(byte[] data, ref int cursor, string label)
    {
        EnsureAvailable(data, cursor, 4, label);
        var value = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(cursor, 4));
        cursor += 4;
        return value;
    }

    private static long ReadInt64(byte[] data, ref int cursor, string label)
    {
        EnsureAvailable(data, cursor, 8, label);
        var value = BinaryPrimitives.ReadInt64LittleEndian(data.AsSpan(cursor, 8));
        cursor += 8;
        return value;
    }

    private static uint ReadUInt32(byte[] data, int offset)
    {
        EnsureAvailable(data, offset, 4, "số nguyên 32-bit");
        return BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset)
    {
        if (offset < 0 || offset > data.Length - 4)
            throw new InvalidDataException("Thiếu số nguyên 32-bit trong PAK.");
        return BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
    }

    private static ulong ReadUInt64(byte[] data, int offset)
    {
        EnsureAvailable(data, offset, 8, "số nguyên 64-bit");
        return BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset, 8));
    }

    private static ulong ReadUInt64(ReadOnlySpan<byte> data, int offset)
    {
        if (offset < 0 || offset > data.Length - 8)
            throw new InvalidDataException("Thiếu số nguyên 64-bit trong PAK.");
        return BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(offset, 8));
    }

    private static void WriteInt32(byte[] data, int offset, int value)
        => BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(offset, 4), value);

    private static void WriteUInt32(byte[] data, int offset, uint value)
        => BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset, 4), value);

    private static void WriteUInt64(byte[] data, int offset, ulong value)
        => BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(offset, 8), value);
}
