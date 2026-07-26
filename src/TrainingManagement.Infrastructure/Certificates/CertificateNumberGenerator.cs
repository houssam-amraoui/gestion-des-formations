using System.Security.Cryptography;
using TrainingManagement.Application.Certificates;

namespace TrainingManagement.Infrastructure.Certificates;

public sealed class CertificateNumberGenerator : ICertificateNumberGenerator
{
    public string CreateCertificateNumber(DateTime issuedAt) =>
        $"CERT-{issuedAt.Year}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(4))}";

    public string CreateVerificationCode() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(18));
}
