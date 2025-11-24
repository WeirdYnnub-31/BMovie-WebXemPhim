using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Runtime;

namespace webxemphim.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ServerController : Controller
    {
        private readonly ILogger<ServerController> _logger;

        public ServerController(ILogger<ServerController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var process = Process.GetCurrentProcess();
            var startTime = process.StartTime;
            var uptime = DateTime.Now - startTime;

            // Memory info
            var workingSet = process.WorkingSet64;
            var privateMemory = process.PrivateMemorySize64;
            var virtualMemory = process.VirtualMemorySize64;
            var gcMemory = GC.GetTotalMemory(false);

            // CPU time
            var cpuTime = process.TotalProcessorTime;

            // Environment info
            var environment = new
            {
                MachineName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount,
                OSVersion = Environment.OSVersion.ToString(),
                FrameworkVersion = Environment.Version.ToString(),
                Is64BitProcess = Environment.Is64BitProcess,
                Is64BitOperatingSystem = Environment.Is64BitOperatingSystem
            };

            ViewBag.ProcessInfo = new
            {
                ProcessId = process.Id,
                ProcessName = process.ProcessName,
                StartTime = startTime,
                Uptime = uptime,
                WorkingSet = workingSet,
                WorkingSetMB = Math.Round(workingSet / 1024.0 / 1024.0, 2),
                PrivateMemory = privateMemory,
                PrivateMemoryMB = Math.Round(privateMemory / 1024.0 / 1024.0, 2),
                VirtualMemory = virtualMemory,
                VirtualMemoryMB = Math.Round(virtualMemory / 1024.0 / 1024.0, 2),
                GCMemory = gcMemory,
                GCMemoryMB = Math.Round(gcMemory / 1024.0 / 1024.0, 2),
                CpuTime = cpuTime,
                ThreadCount = process.Threads.Count
            };

            ViewBag.Environment = environment;

            // Disk info
            try
            {
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady)
                    .Select(d => new
                    {
                        Name = d.Name,
                        Label = d.VolumeLabel,
                        Type = d.DriveType.ToString(),
                        TotalSize = d.TotalSize,
                        TotalSizeGB = Math.Round(d.TotalSize / 1024.0 / 1024.0 / 1024.0, 2),
                        AvailableSpace = d.AvailableFreeSpace,
                        AvailableSpaceGB = Math.Round(d.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0, 2),
                        UsedSpace = d.TotalSize - d.AvailableFreeSpace,
                        UsedSpaceGB = Math.Round((d.TotalSize - d.AvailableFreeSpace) / 1024.0 / 1024.0 / 1024.0, 2),
                        PercentUsed = Math.Round((double)(d.TotalSize - d.AvailableFreeSpace) / d.TotalSize * 100, 2)
                    })
                    .ToList();

                ViewBag.Drives = drives;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting disk info");
                ViewBag.Drives = new List<object>();
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForceGC()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            TempData["Message"] = "Đã thực hiện Garbage Collection";
            return RedirectToAction(nameof(Index));
        }
    }
}

