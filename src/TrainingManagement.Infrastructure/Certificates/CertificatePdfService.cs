using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using QRCoder;
using TrainingManagement.Application.Certificates;

namespace TrainingManagement.Infrastructure.Certificates;

public sealed class CertificatePdfService : ICertificatePdfService
{
    private static int initialized;

    public byte[] Generate(CertificatePdfModel model)
    {
        ConfigureFonts();
        using var document = new PdfDocument();
        document.Info.Title = $"Certificat {model.CertificateNumber}";
        var page = document.AddPage();
        page.Orientation = PdfSharp.PageOrientation.Landscape;
        using var graphics = XGraphics.FromPdfPage(page);
        var bounds = new XRect(25, 25, page.Width.Point - 50, page.Height.Point - 50);
        graphics.DrawRoundedRectangle(new XPen(XColor.FromArgb(25, 78, 140), 3), bounds, new XSize(12, 12));
        var title = new XFont("Arial", 25, XFontStyleEx.Bold);
        var heading = new XFont("Arial", 13, XFontStyleEx.Bold);
        var normal = new XFont("Arial", 11, XFontStyleEx.Regular);
        graphics.DrawString(model.PlatformName, heading, XBrushes.DarkSlateBlue,
            new XRect(0, 52, page.Width.Point, 30), XStringFormats.TopCenter);
        graphics.DrawString("CERTIFICAT DE RÉUSSITE", title, XBrushes.DarkBlue,
            new XRect(0, 92, page.Width.Point, 40), XStringFormats.TopCenter);
        graphics.DrawString("Ce certificat atteste que", normal, XBrushes.Black,
            new XRect(0, 150, page.Width.Point, 22), XStringFormats.TopCenter);
        graphics.DrawString(model.LearnerName, new XFont("Arial", 22, XFontStyleEx.Bold), XBrushes.DarkBlue,
            new XRect(0, 178, page.Width.Point, 35), XStringFormats.TopCenter);
        graphics.DrawString($"a terminé avec succès la formation « {model.TrainingTitle} »", normal,
            XBrushes.Black, new XRect(60, 225, page.Width.Point - 120, 35), XStringFormats.TopCenter);
        var trainer = string.IsNullOrWhiteSpace(model.TrainerName) ? "" : $"Formateur : {model.TrainerName}   •   ";
        graphics.DrawString($"{trainer}Réussite : {model.CompletionDate:dd/MM/yyyy}", normal, XBrushes.Black,
            new XRect(50, 275, page.Width.Point - 100, 25), XStringFormats.TopCenter);
        graphics.DrawString($"N° {model.CertificateNumber}", heading, XBrushes.Black,
            new XRect(55, 330, 430, 25), XStringFormats.TopLeft);
        graphics.DrawString($"Code : {model.VerificationCode}", normal, XBrushes.Black,
            new XRect(55, 360, 500, 25), XStringFormats.TopLeft);
        graphics.DrawString($"Vérification : {model.VerificationUrl}", new XFont("Arial", 8), XBrushes.DarkBlue,
            new XRect(55, 388, 570, 25), XStringFormats.TopLeft);
        graphics.DrawString(model.ExpiresAt is null ? "Validité permanente" :
            $"Valide jusqu’au {model.ExpiresAt:dd/MM/yyyy}", normal, XBrushes.Black,
            new XRect(55, 425, 420, 25), XStringFormats.TopLeft);
        DrawQr(graphics, model.VerificationUrl, page.Width.Point - 180, 325, 110);
        graphics.DrawLine(new XPen(XColors.Gray), 315, 470, 520, 470);
        graphics.DrawString("Signature électronique de la plateforme", new XFont("Arial", 9),
            XBrushes.Gray, new XRect(300, 476, 240, 20), XStringFormats.TopCenter);
        graphics.DrawString("Document généré électroniquement", new XFont("Arial", 8),
            XBrushes.Gray, new XRect(0, page.Height.Point - 48, page.Width.Point, 20), XStringFormats.TopCenter);
        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    private static void DrawQr(XGraphics graphics, string value, double x, double y, double size)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q);
        var modules = data.ModuleMatrix;
        var cell = size / modules.Count;
        graphics.DrawRectangle(XBrushes.White, x, y, size, size);
        for (var row = 0; row < modules.Count; row++)
            for (var col = 0; col < modules[row].Length; col++)
                if (modules[row][col])
                    graphics.DrawRectangle(XBrushes.Black, x + col * cell, y + row * cell, cell + .2, cell + .2);
    }

    private static void ConfigureFonts()
    {
        if (Interlocked.Exchange(ref initialized, 1) != 0) return;
        if (OperatingSystem.IsWindows()) GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        if (OperatingSystem.IsLinux() && File.Exists("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"))
            GlobalFontSettings.FontResolver = new DejaVuFontResolver();
    }

    private sealed class DejaVuFontResolver : IFontResolver
    {
        private const string Regular = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf";
        private const string Bold = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf";
        public byte[] GetFont(string faceName) => File.ReadAllBytes(faceName == "bold" ? Bold : Regular);
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
            new(isBold ? "bold" : "regular");
    }
}
