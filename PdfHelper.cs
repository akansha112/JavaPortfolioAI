using System;
using System.IO;
using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace PortfolioAI.Services
{
    public static class PdfHelper
    {
        /// <summary>
        /// Extracts text from a PDF stream in-memory (no disk writes).
        /// </summary>
        public static string ExtractTextFromPdf(Stream pdfStream)
        {
            if (pdfStream == null || pdfStream.Length == 0)
                throw new ArgumentException("PDF stream is empty");

            pdfStream.Position = 0; // reset stream
            var textBuilder = new StringBuilder();

            using (var reader = new PdfReader(pdfStream))
            using (var pdfDoc = new PdfDocument(reader))
            {
                int numberOfPages = pdfDoc.GetNumberOfPages();
                for (int i = 1; i <= numberOfPages; i++)
                {
                    var page = pdfDoc.GetPage(i);
                    string pageText = PdfTextExtractor.GetTextFromPage(page);
                    if (!string.IsNullOrEmpty(pageText))
                        textBuilder.AppendLine(pageText);
                }
            }

            return textBuilder.ToString();
        }
    }
}