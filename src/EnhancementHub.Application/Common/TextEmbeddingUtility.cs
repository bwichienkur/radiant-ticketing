using System.Security.Cryptography;
using System.Text;

namespace EnhancementHub.Application.Common;

public static class TextEmbeddingUtility
{
    public static float[] CreateEmbeddingFromText(string text, int dimensions = 64)
    {
        var normalized = text.Trim().ToLowerInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        return HashToEmbedding(hash, dimensions);
    }

    public static float[] HashToEmbedding(string hashHex, int dimensions = 64)
    {
        var bytes = Convert.FromHexString(hashHex);
        var embedding = new float[dimensions];
        for (var i = 0; i < dimensions; i++)
        {
            embedding[i] = bytes[i % bytes.Length] / 255f;
        }

        return embedding;
    }
}
