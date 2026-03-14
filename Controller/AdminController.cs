using Microsoft.AspNetCore.Mvc;
using PortfolioAI.Services;
using PortfolioAI.Data; // Make sure ResumeDataSeeder is in this namespace

namespace PortfolioAI.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly RagService _ragService;
        private readonly ResumeDataSeeder _resumeSeeder; // Add Seeder

        // Inject both RagService and ResumeDataSeeder
        public AdminController(RagService ragService, ResumeDataSeeder resumeSeeder)
        {
            _ragService = ragService;
            _resumeSeeder = resumeSeeder;
        }

        [HttpPost("resume")]
        public async Task<IActionResult> UploadResume(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            // Extract text in-memory
            var resumeText = PdfHelper.ExtractTextFromPdf(stream);

            // Index the resume
            await _ragService.IndexResumeAsync(resumeText);

            return Ok(new { message = "Resume indexed successfully" });
        }
    }
}