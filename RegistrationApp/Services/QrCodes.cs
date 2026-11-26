using QRCoder;

namespace Gcmac.Registration.Services;

public static class QrCodes
{
    public static byte[] PngBytes(string text)
    {
        var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(data).GetGraphic(12);
    }

    public static string DataUri(string text) =>
        $"data:image/png;base64,{Convert.ToBase64String(PngBytes(text))}";
}
