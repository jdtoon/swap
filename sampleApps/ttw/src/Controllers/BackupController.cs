using Microsoft.AspNetCore.Mvc;
using ttw.Data.Backup;

namespace ttw.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BackupController : ControllerBase
    {
        private readonly DatabaseBackupService _backupService;

        public BackupController(DatabaseBackupService backupService)
        {
            _backupService = backupService;
        }

        [HttpPost("run")]
        public async Task<IActionResult> RunBackup()
        {
            await _backupService.RunOnceAsync();
            return Ok("Backup started");
        }
    }

}
