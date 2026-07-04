using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EnhancementHub.Infrastructure.Persistence.Converters;

public sealed class FloatArrayToBytesConverter : ValueConverter<float[]?, byte[]?>
{
    public FloatArrayToBytesConverter()
        : base(
            v => v == null ? null : FloatArrayToBytes(v),
            v => v == null ? null : BytesToFloatArray(v))
    {
    }

    private static byte[] FloatArrayToBytes(float[] values)
    {
        var bytes = new byte[values.Length * sizeof(float)];
        Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static float[] BytesToFloatArray(byte[] bytes)
    {
        var values = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
        return values;
    }
}
