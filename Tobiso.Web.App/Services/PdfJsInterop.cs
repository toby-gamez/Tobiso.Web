using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Tobiso.Web.App.Services
{
    // Minimal JS interop wrapper around window.pdfUtils
    public class PdfJsInterop
    {
        private readonly IJSRuntime _js;

        public PdfJsInterop(IJSRuntime js)
        {
            _js = js;
        }

        public ValueTask GenerateTextPdfAsync(string selector, string filename = "document.pdf")
        {
            return _js.InvokeVoidAsync("pdfBlazorInterop.generateTextPdf", selector, filename);
        }

        public ValueTask GeneratePdfAsync(string selector, object options = null)
        {
            return _js.InvokeVoidAsync("pdfBlazorInterop.generatePdf", selector, options ?? new { });
        }

        public ValueTask<string> SanitizeFileNameAsync(string name)
        {
            return _js.InvokeAsync<string>("pdfBlazorInterop.sanitizeFileName", name);
        }
    }
}
