using QRCoder;

namespace Gcmac.Registration.Services;

public static class QrCodes
{
    public static string DataUri(string text)
    {
        var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var bytes = new PngByteQRCode(data).GetGraphic(12);
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }
}
