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

        /// <summary>
        /// Post the archive message.
        /// </summary>
        /// <remarks>
        /// The Content-Type header must be set and correspond to the content of the body.
        /// <br/>
        /// Use the Accept header to control the format of the response. Possible values are application/json (default) and application/xml.
        /// <br/><br/>
        /// If the invocation succeeds HTTP status code 200 is returned.
        /// <br/>
        /// If the invocation fails a problem details object is returned. The problem details object follows the guidelines in <a href="https://tools.ietf.org/html/rfc7807">RFC-7807</a>.
        /// The following problem types are explicitly supported by the archive client:
        /// <dl>
        /// <li>
        /// <dt>https://onetoox.dk/unknown-receiver</dt>
        /// <dd>The receiver is not known in the receiving system</dd>
        /// </li>
        /// <li>
        /// <dt>https://onetoox.dk/invalid-archive-category</dt>
        /// <dd>The archive category is not valid</dd>
        /// </li>
        /// <li>
        /// <dt>https://onetoox.dk/validation-error</dt>
        /// <dd>Validation error. The specific error is described by the title and detail attributes</dd>
        /// </li>
        /// </dl>
        /// </remarks>
        /// <param name="archiveMessage"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Post([FromBody] ArchiveMessage archiveMessage)
        {
            _logger.LogInformation($"{nameof(Post)}: Received ArchiveMessage with JobId: {archiveMessage.JobId}");

            // Example validation
            if (archiveMessage.Receiver?.Length != 10)
                return BadRequest(new ProblemDetails
                {
                    Type = "https://onetoox.dk/validation-error",
                    Detail = $"The format of the receiver '{archiveMessage.Receiver}' is not valid",
                    Title = "Invalid receiver format"
                });
            if ("1234560000" != archiveMessage.Receiver && "1122334455" != archiveMessage.ArchiveCategory)
                return BadRequest(new ProblemDetails
                {
                    Type = "https://onetoox.dk/unknown-receiver",
                    Detail = $"The receiver '{archiveMessage.Receiver}' is unknown",
                    Title = "Unknown receiver"
                });
            if (!archiveMessage.ArchiveCategory?.StartsWith("Category") ?? true)
                return BadRequest(new ProblemDetails
                {
                    Type = "https://onetoox.dk/invalid-archive-category",
                    Detail = $"The category '{archiveMessage.ArchiveCategory}' is not valid",
                    Title = "Invalid category"
                });

            Directory.CreateDirectory(_settings.Value.ArchiveFolder);

            System.IO.File.WriteAllBytes(Path.Combine(_settings.Value.ArchiveFolder, $"archiveDoc-{archiveMessage.JobId}.pdf"), archiveMessage.MainDocument.DocumentData);

            // Don't save document data in JSON or XML
            archiveMessage.MainDocument.DocumentData = null;
            if (archiveMessage.Addendums != null) foreach (var addendum in archiveMessage.Addendums) addendum.DocumentData = null;

            System.IO.File.WriteAllText(Path.Combine(_settings.Value.ArchiveFolder, $"archiveDoc-{archiveMessage.JobId}.json"), JsonSerializer.Serialize(archiveMessage));
            using (var fs = new FileStream(Path.Combine(_settings.Value.ArchiveFolder, $"archiveDoc-{archiveMessage.JobId}.xml"), FileMode.Create))
                new XmlSerializer(typeof(ArchiveMessage)).Serialize(fs, archiveMessage);

            return Ok();
        }
    }
}
