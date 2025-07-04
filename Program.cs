using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

using Iot_dashboard.Hubs;
using static Iot_dashboard.Controllers.andon.andonController;

using static Iot_dashboard.Controllers.AMR.confirmController;
using static Iot_dashboard.Controllers.AMR.L1Controller;
using static Iot_dashboard.Controllers.AMR.L2Controller;
using static Iot_dashboard.Controllers.AMR.L3Controller;
using Iot_dashboard.Models.AMR;
using static Iot_dashboard.Controllers.AMR.amr_dashController;
using static Iot_dashboard.Controllers.AMR.AMR_FG.FG1Controller;
using static Iot_dashboard.Controllers.AMR.orderController;
using static Iot_dashboard.Controllers.andon.andonCompController;
using static Iot_dashboard.Controllers.andon.andonreportController;
using static Iot_dashboard.Hubs.andonComHub;
using static Iot_dashboard.Controllers.AMR.AMR_FG.FG2Controller;
using static Iot_dashboard.Controllers.AMR.AMR_FG.FGController;
using static Iot_dashboard.Controllers.Iot.MachineController;
using static Iot_dashboard.Controllers.Iot.IoTdashController;
using static Iot_dashboard.Controllers.Iot.Iotconfig;
using static Iot_dashboard.Controllers.Iot.IoTbestController;
using static Iot_dashboard.Controllers.Iot.iotout;
using static Iot_dashboard.Controllers.Iot.IoTzone;

using static Iot_dashboard.Controllers.Iot.iotpast;
using static Iot_dashboard.Controllers.Iot.iotoutfl;
using static IotDeviceController;
using static Iot_dashboard.Controllers.Iot.iotplusminus;
using static Iot_dashboard.Controllers.Synergy.realtime;
using static Iot_dashboard.Controllers.Synergy.synout;
using static Iot_dashboard.Controllers.Iot.IoTPastVisual;
using static Iot_dashboard.Controllers.Iot.outAnalyze;
using static Iot_dashboard.Controllers.Synergy.synbest;
using static Iot_dashboard.Controllers.Synergy.ModuleAPI;
using static Iot_dashboard.Controllers.Synergy.synoutfilter;
using static Iot_dashboard.Controllers.ESL.moduleESL;
using static Iot_dashboard.Controllers.ESL.SubModuleESL;
using static Iot_dashboard.Controllers.Iot.ChangebyModule;
using static Iot_dashboard.Controllers.Synergy.DifferentBetweenCounts;
using static Iot_dashboard.Controllers.Iot.LOGIN.Register;
using static Iot_dashboard.Controllers.Synergy.sewrealtime;
using static Iot_dashboard.Controllers.UpdateDeviceController;
using static Iot_dashboard.Controllers.Synergy.bestboth;
using static Iot_dashboard.Controllers.Synergy.bestsewf;
using static Iot_dashboard.Controllers.hanger.AQL;
using static Iot_dashboard.Controllers.hanger.input;
using static Iot_dashboard.Controllers.Synergy.manualOutput;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var secondConnectionString = builder.Configuration.GetConnectionString("DefaultConnection1");
builder.Services.AddDbContext<AppDbContext1>(options =>
    options.UseSqlServer(secondConnectionString));



var amrconfString = builder.Configuration.GetConnectionString("DefaultConnection4");
builder.Services.AddDbContext<AppDbContext4>(options =>
    options.UseSqlServer(amrconfString));

var amrconString = builder.Configuration.GetConnectionString("DefaultConnection4");
builder.Services.AddDbContext<AppDbContext5>(options =>
    options.UseSqlServer(amrconString));

var L1 = builder.Configuration.GetConnectionString("DefaultConnection4");
builder.Services.AddDbContext<AppDbContext6>(options =>
    options.UseSqlServer(L1));


var L2 = builder.Configuration.GetConnectionString("DefaultConnection4");
builder.Services.AddDbContext<AppDbContext7>(options =>
    options.UseSqlServer(L2));

var L3 = builder.Configuration.GetConnectionString("DefaultConnection4");
builder.Services.AddDbContext<AppDbContext8>(options =>
    options.UseSqlServer(L3));


var L1C = builder.Configuration.GetConnectionString("DefaultConnection4");
builder.Services.AddDbContext<AppDbContext9>(options =>
    options.UseSqlServer(L1C));



var L2C = builder.Configuration.GetConnectionString("DefaultConnection4");
builder.Services.AddDbContext<AppDbContext10>(options =>
    options.UseSqlServer(L2C));


var L3C = builder.Configuration.GetConnectionString("DefaultConnection4");
builder.Services.AddDbContext<AppDbContext11>(options =>
    options.UseSqlServer(L3C));



var dash = builder.Configuration.GetConnectionString("DefaultConnection4");
builder.Services.AddDbContext<AppDbContext12>(options =>
    options.UseSqlServer(dash));


var conf = builder.Configuration.GetConnectionString("DefaultConnection4");
builder.Services.AddDbContext<AppDbContext13>(options =>
    options.UseSqlServer(conf));


var order = builder.Configuration.GetConnectionString("DefaultConnection4");
builder.Services.AddDbContext<AppDbContext14>(options =>
    options.UseSqlServer(order));


var andonc = builder.Configuration.GetConnectionString("DefaultConnection4");
builder.Services.AddDbContext<AppDbContext15>(options =>
    options.UseSqlServer(andonc));


var andonch = builder.Configuration.GetConnectionString("DefaultConnection4");
builder.Services.AddDbContext<AppDbContext16>(options =>
    options.UseSqlServer(andonch));

var L1L = builder.Configuration.GetConnectionString("DefaultConnection4");
builder.Services.AddDbContext<AppDbContext17>(options =>
    options.UseSqlServer(L1L));


var L2L = builder.Configuration.GetConnectionString("DefaultConnection4");
builder.Services.AddDbContext<AppDbContext18>(options =>
    options.UseSqlServer(L2L));


var L3L = builder.Configuration.GetConnectionString("DefaultConnection4");
builder.Services.AddDbContext<AppDbContext19>(options =>
    options.UseSqlServer(L3L));



var andonre = builder.Configuration.GetConnectionString("DefaultConnection4");
builder.Services.AddDbContext<AppDbContext20>(options =>
   options.UseSqlServer(andonre));




var FG = builder.Configuration.GetConnectionString("DefaultConnection4");
builder.Services.AddDbContext<AppDbContext21>(options =>
    options.UseSqlServer(FG));



var FG1 = builder.Configuration.GetConnectionString("DefaultConnection4");
builder.Services.AddDbContext<AppDbContext22>(options =>
   options.UseSqlServer(FG1));

var FG1H = builder.Configuration.GetConnectionString("DefaultConnection2");
builder.Services.AddDbContext<AppDbContext23>(options =>
    options.UseSqlServer(FG1H));



var FG12 = builder.Configuration.GetConnectionString("DefaultConnection3");
builder.Services.AddDbContext<AppDbContext24>(options =>
   options.UseSqlServer(FG12));

var FG2H = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext25>(options =>
    options.UseSqlServer(FG1H));



var FG2 = builder.Configuration.GetConnectionString("DefaultConnection3");
builder.Services.AddDbContext<AppDbContext26>(options =>
   options.UseSqlServer(FG12));



var FGc = builder.Configuration.GetConnectionString("DefaultConnection1");
builder.Services.AddDbContext<AppDbContext27>(options =>
    options.UseSqlServer(FGc));



var FGco = builder.Configuration.GetConnectionString("DefaultConnection2");
builder.Services.AddDbContext<AppDbContext28>(options =>
   options.UseSqlServer(FGco));


var FGh1 = builder.Configuration.GetConnectionString("DefaultConnection3");
builder.Services.AddDbContext<AppDbContext29>(options =>
    options.UseSqlServer(FGh1));



var extra = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext30>(options =>
   options.UseSqlServer(extra));


var md = builder.Configuration.GetConnectionString("DefaultConnection3");
builder.Services.AddDbContext<AppDbContext31>(options =>
    options.UseSqlServer(md));



var md1 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext32>(options =>
   options.UseSqlServer(md1));


var iotda = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext33>(options =>
   options.UseSqlServer(iotda));


var iot = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext34>(options =>
   options.UseSqlServer(iot));


var best = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext35>(options =>
   options.UseSqlServer(best));

var tack = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext36>(options =>
   options.UseSqlServer(tack));

var iotout = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext37>(options =>
   options.UseSqlServer(iotout));

var tout = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext38>(options =>
   options.UseSqlServer(tout));

var usmv = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext39>(options =>
   options.UseSqlServer(usmv));

var zon = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext40>(options =>
   options.UseSqlServer(zon));

var style = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext41>(options =>
   options.UseSqlServer(style));

var ta = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext42>(options =>
   options.UseSqlServer(ta));

var ta1 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext43>(options =>
   options.UseSqlServer(ta1));

var ta2 = builder.Configuration.GetConnectionString("DefaultConnection");  
builder.Services.AddDbContext<AppDbContext44>(options =>
   options.UseSqlServer(ta2));


var iotdev = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext45>(options =>
   options.UseSqlServer(iotdev));

var dev = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext46>(options =>
   options.UseSqlServer(dev));

var dev1 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext47>(options =>
   options.UseSqlServer(dev1));

var plus = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext48>(options =>
   options.UseSqlServer(plus));

var plusout = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext49>(options =>
   options.UseSqlServer(plusout));

var plusout1 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext50>(options =>
   options.UseSqlServer(plusout1));

var plusout2 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext51>(options =>
   options.UseSqlServer(plusout2));

var plus1 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext52>(options =>
   options.UseSqlServer(plus));

var plusout23 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext53>(options =>
   options.UseSqlServer(plusout23));

var plusout13 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext54>(options =>
   options.UseSqlServer(plusout13));

var plusout233 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext55>(options =>
   options.UseSqlServer(plusout233));


var plusout134 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext56>(options =>
   options.UseSqlServer(plusout134));

var plusout2333 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext57>(options =>
   options.UseSqlServer(plusout2333));


var outana = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext58>(options =>
   options.UseSqlServer(outana));

var io = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext59>(options =>
   options.UseSqlServer(io));


var outana1 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext60>(options =>
   options.UseSqlServer(outana1));

var io1 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext61>(options =>
   options.UseSqlServer(io1));


var io13 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext62>(options =>
   options.UseSqlServer(io13));

var io143 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext63>(options =>
   options.UseSqlServer(io143));


var esl = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext64>(options =>
   options.UseSqlServer(esl));

var esls = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext65>(options =>
   options.UseSqlServer(esls));


var cmodule = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext66>(options =>
   options.UseSqlServer(cmodule));

var io133 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext67>(options =>
   options.UseSqlServer(io133));


var io1333 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext68>(options =>
   options.UseSqlServer(io1333));


var io13334 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext69>(options =>
   options.UseSqlServer(io13334));


var io1333W4 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext70>(options =>
   options.UseSqlServer(io1333W4));



var io1333W48 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext71>(options =>
   options.UseSqlServer(io1333W48));



var io133a3W48 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext72>(options =>
   options.UseSqlServer(io133a3W48));


var io1333We48 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext73>(options =>
   options.UseSqlServer(io1333We48));



var io133a3eW48 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext74>(options =>
   options.UseSqlServer(io133a3eW48));




var io1333We481 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext75>(options =>
   options.UseSqlServer(io1333We481));



var io133a3eW481 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext76>(options =>
   options.UseSqlServer(io133a3eW481));





var H1 = builder.Configuration.GetConnectionString("hanger");
builder.Services.AddDbContext<AppDbContext77>(options =>
   options.UseSqlServer(H1));



var H2 = builder.Configuration.GetConnectionString("hanger");
builder.Services.AddDbContext<AppDbContext78>(options =>
   options.UseSqlServer(H2));


var H21 = builder.Configuration.GetConnectionString("hanger");
builder.Services.AddDbContext<AppDbContext79>(options =>
   options.UseSqlServer(H21));


var H221 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext80>(options =>
   options.UseSqlServer(H221));

var H2216 = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext81>(options =>
   options.UseSqlServer(H2216));


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add SignalR
builder.Services.AddSignalR();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();
app.UseSession();
app.UseStaticFiles();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();

    endpoints.MapControllerRoute(
    name: "iotmenu",
    pattern: "{controller=iotmenu}/{action=Index}/{id?}");

    endpoints.MapControllerRoute(
 name: "synout",
 pattern: "{controller=synout}/{action=Index}/{id?}");

    endpoints.MapControllerRoute(
        name: "fallback",
        pattern: "{*url}",
        defaults: new { controller = "iotmenu", action = "Index" });

    endpoints.MapHub<HistoryHub>("/HistoryHub");
    endpoints.MapHub<DashboardHub>("/dashboardHub");
    endpoints.MapHub<andonHub>("/andonHub");
    endpoints.MapHub<StatusHub>("/statusHub");
    endpoints.MapHub<L1Hub>("/l1Hub");
    endpoints.MapHub<L2Hub>("/l2Hub");
    endpoints.MapHub<L3Hub>("/l3Hub");
    endpoints.MapHub<andonComHub>("/acHub");

    
});
app.Run();
