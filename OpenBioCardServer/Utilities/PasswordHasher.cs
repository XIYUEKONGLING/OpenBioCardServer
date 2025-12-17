using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;
using System.Text;

namespace OpenBioCardServer.Utilities;

public class PasswordHasher
{
    // Argon2 参数配置
    private const int SaltSize = 16; // 盐值长度（字节）
    private const int HashSize = 32; // 哈希长度（字节）
    private const int Iterations = 3; // 迭代次数
    private const int MemorySize = 65536; // 内存使用 (KB) - 64MB
    private const int Parallelism = 4; // 并行度

    /// <summary>
    /// 生成密码哈希和盐值
    /// </summary>
    public static (string hash, string salt) HashPassword(string password)
    {
        // 生成随机盐值
        byte[] salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        var argon2 = new Argon2BytesGenerator();
        argon2.Init(new Argon2Parameters.Builder(Argon2Parameters.Argon2id)
            .WithVersion(Argon2Parameters.Version13)
            .WithIterations(Iterations)
            .WithMemoryAsKB(MemorySize)
            .WithParallelism(Parallelism)
            .WithSalt(salt)
            .Build());

        byte[] hash = new byte[HashSize];
        argon2.GenerateBytes(Encoding.UTF8.GetBytes(password), hash);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    public static bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        byte[] salt = Convert.FromBase64String(storedSalt);
        byte[] expectedHash = Convert.FromBase64String(storedHash);

        var argon2 = new Argon2BytesGenerator();
        argon2.Init(new Argon2Parameters.Builder(Argon2Parameters.Argon2id)
            .WithVersion(Argon2Parameters.Version13)
            .WithIterations(Iterations)
            .WithMemoryAsKB(MemorySize)
            .WithParallelism(Parallelism)
            .WithSalt(salt)
            .Build());

        byte[] hash = new byte[HashSize];
        argon2.GenerateBytes(Encoding.UTF8.GetBytes(password), hash);

        // 使用恒定时间比较防止时序攻击
        return CryptographicOperations.FixedTimeEquals(hash, expectedHash);
    }
}
