using System.IO;
using System.Text.Json;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneTooX.RestPush.Model;

namespace OneTooXRestArchiveTest.Controllers
{
    [Authorize]
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ArchiveController : ControllerBase
    {
        private readonly ILogger<ArchiveController> _logger;
        private readonly IOptions<ArchiveControllerSettings> _settings;

        public ArchiveController(ILogger<ArchiveController> logger, IOptions<ArchiveControllerSettings> settings)
        {
            _logger = logger;
            _settings = settings;
        }

        [HttpPost]
        public IActionResult Post([FromBody] ArchiveMessage archiveMessage)
        {
            _logger.LogInformation($"{nameof(Post)}: Received ArchiveMessage with JobId: {archiveMessage.JobId}");
            // Validate message
            if (!archiveMessage.ArchiveCategory.StartsWith("Category"))
            {
                return BadRequest("Bad category");
            }

            Directory.CreateDirectory(_settings.Value.ArchiveFolder);

            System.IO.File.WriteAllBytes(Path.Combine(_settings.Value.ArchiveFolder, $"archiveDoc-{archiveMessage.JobId}.pdf"), archiveMessage.MainDocument.DocumentData);

            // Don't save documents in JSON or XML
            archiveMessage.MainDocument.DocumentData = null;
            if (archiveMessage.Addendums != null) foreach (var addendum in archiveMessage.Addendums) addendum.DocumentData = null;

            System.IO.File.WriteAllText(Path.Combine(_settings.Value.ArchiveFolder, $"archiveDoc-{archiveMessage.JobId}.json"), JsonSerializer.Serialize(archiveMessage));
            using (var fs = new FileStream(Path.Combine(_settings.Value.ArchiveFolder, $"archiveDoc-{archiveMessage.JobId}.xml"), FileMode.Create))
                new XmlSerializer(typeof(ArchiveMessage)).Serialize(fs, archiveMessage);

            return Ok(new ResponseMessage { ResponseCode = 0, Message = "OK" });
        }
    }
}
