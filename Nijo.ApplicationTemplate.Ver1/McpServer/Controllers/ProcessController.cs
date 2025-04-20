using McpServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class ProcessController : ControllerBase {
        private readonly IProcessManager _processManager;
        private readonly ILogger<ProcessController> _logger;

        public ProcessController(IProcessManager processManager, ILogger<ProcessController> logger) {
            _processManager = processManager;
            _logger = logger;
        }

        [HttpGet("status")]
        public ActionResult<ProcessStatus> GetStatus() {
            _logger.LogInformation("Getting process status");
            return Ok(_processManager.GetStatus());
        }

        [HttpPost("start")]
        public async Task<ActionResult<bool>> Start() {
            _logger.LogInformation("Starting processes");
            var result = await _processManager.StartAsync();
            return Ok(result);
        }

        [HttpPost("rebuild")]
        public async Task<ActionResult<bool>> Rebuild() {
            _logger.LogInformation("Rebuilding WebApi");
            var result = await _processManager.RebuildWebApiAsync();
            return Ok(result);
        }

        [HttpPost("stop")]
        public async Task<ActionResult<bool>> Stop() {
            _logger.LogInformation("Stopping all processes");
            var result = await _processManager.StopAsync();
            return Ok(result);
        }
    }
}
