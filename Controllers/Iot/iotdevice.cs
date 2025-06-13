using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Iot_dashboard.Models;
using Microsoft.EntityFrameworkCore;

public class IotDeviceController : Controller
{
    private readonly RegistryManager _registryManager;
    private readonly AppDbContext45 _dbContext;
    private readonly AppDbContext46 _dbContext1;

    public class AppDbContext45 : DbContext
    {
        public DbSet<device> KreedaIoTMachineDetails { get; set; }

        public AppDbContext45(DbContextOptions<AppDbContext45> options) : base(options)
        {
        }
    }

    public class AppDbContext46 : DbContext
    {
        public DbSet<KreedaIotTestNew> KreedaIotTestNew { get; set; }

        public AppDbContext46(DbContextOptions<AppDbContext46> options) : base(options)
        {
        }
    }

    public IotDeviceController(AppDbContext45 dbContext, AppDbContext46 dbContext1)
    {
        _dbContext = dbContext;
        _registryManager = RegistryManager.CreateFromConnectionString("HostName=KRE-SEA-KRE-IOT-HUB.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=FYsdT36eyqnN6nhcKu/0X72ISo2DccPc7VIKvhh3Oj0=");
        _dbContext1 = dbContext1;
    }

    [HttpGet]
    public async Task<IActionResult> GetIotData()
    {
        var devices = await _registryManager.GetDevicesAsync(100);

        var deviceList = new List<DeviceViewModel>();

        foreach (var device in devices)
        {
            var connectionStatus = device.ConnectionState == DeviceConnectionState.Connected ? "Connected" : "Disconnected";

            // Query the database for additional device details
            var dbDevice = await _dbContext.KreedaIoTMachineDetails.FirstOrDefaultAsync(d => d.KreedIotDeviceID == device.Id);

            if (dbDevice != null)
            {
                // Query dbContext1 for the most recent data for the given ChipID
                var recentData = await _dbContext1.KreedaIotTestNew
                    .Where(d => d.ChipID == dbDevice.ChipID)
                    .OrderByDescending(d => d.ID)
                    .FirstOrDefaultAsync();

                if (recentData != null)
                {
                    deviceList.Add(new DeviceViewModel
                    {
                        DeviceId = device.Id,
                        ConnectionState = connectionStatus,
                        Module = dbDevice.Module,
                        Operation = dbDevice.Operation,
                        Machine = dbDevice.MachineType,
                        User = dbDevice.UserLogged,
                        MachineSerialNumber = dbDevice.ChipID,
                        Status = connectionStatus == "Connected" ? "Success" : "Danger",
                        RecentDate = recentData.Date.ToString("yyyy-MM-dd"),
                        RecentTime = recentData.Time.ToString(@"hh\:mm\:ss")
                    });
                }
                else
                {
                    deviceList.Add(new DeviceViewModel
                    {
                        DeviceId = device.Id,
                        ConnectionState = connectionStatus,
                        Module = dbDevice.Module,
                        Operation = dbDevice.Operation,
                        Machine = dbDevice.MachineType,
                        User = dbDevice.UserLogged,
                        MachineSerialNumber = dbDevice.ChipID,
                        Status = connectionStatus == "Connected" ? "Success" : "Danger"
                    });
                }
            }
        }

        return Json(deviceList);
    }

    public IActionResult Index()
    {
        return View("~/Views/Iotdevice.cshtml");
    }
}
public class DeviceViewModel
{
    public string DeviceId { get; set; }
    public string ConnectionState { get; set; }
    public string Module { get; set; }
    public string Operation { get; set; }
    public string Machine { get; set; }
    public string User { get; set; }
    public string MachineSerialNumber { get; set; }
    public string Status { get; set; }
    public string RecentData { get; set; }
    public string RecentDate { get; set; }
    public string RecentTime { get; set; }
}
