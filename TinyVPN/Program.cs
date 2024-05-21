using Microsoft.Extensions.Configuration;
using Pouyan;
using SingBoxLib.Configuration.Outbound.Abstract;
using SingBoxLib.Parsing;

//variables
Pouyan.Model.TestResult[] testedProfiles;
List<Task<Pouyan.Model.TestResult>> profilesResults;
List<ProfileItem> profiles;
int index,counterProfile;
string url;
List<Pouyan.Model.TestResult> orderedProfiles;
//variables
var vpn = new Vpn();
var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json");
var configuration = builder.Build();
counterProfile = int.Parse(configuration["count_profile"]!);
url = configuration["subscribe_url"]!;

var singbox = new SingBox("./sing-box.exe",Pouyan.Model.Inbounds.EnumInbounds.Mixed);

var cts = new CancellationTokenSource();
Random rng = new Random();
profiles = vpn.TakeProfiles(url).OrderBy(x=> rng.Next()).ToList();
if (counterProfile > profiles.Count)
    counterProfile = profiles.Count;
index = 0;
Console.WriteLine($"doing {counterProfile} test profiles");
do
{
    profilesResults = vpn.TestProfiles(index, counterProfile, singbox, profiles);
    testedProfiles = vpn.CheckProfiles(profilesResults);
    orderedProfiles = testedProfiles.Where(p => p.Result!.Delay > 0).OrderBy(p => p.Result!.Delay).ToList();
    index += counterProfile;
    if (index > profiles.Count)
        index = profiles.Count;
} while (orderedProfiles.Count == 0 || index == profiles.Count);

if (orderedProfiles.Count == 0)
{
    Console.WriteLine("0 profiles work well with your internet connection");
    return;
}
Console.WriteLine("Profiles Work Well:");
orderedProfiles.ForEach(p => { Console.WriteLine($"Name: {p.Profile.Name} Delay: {p.Result!.Delay}"); });

singbox.OutBounds = vpn.GetOutbounds(orderedProfiles[0].Profile);

Console.WriteLine($"Connecting To {orderedProfiles[0].Profile.Name}");
var tunneling = singbox.StartTunneling(cts);
Console.WriteLine($"Connected");
Console.CancelKeyPress += new ConsoleCancelEventHandler((e, s) => OnProcessExit(cts));
AppDomain.CurrentDomain.ProcessExit += new EventHandler((s, e) => OnProcessExit(cts));
tunneling.Wait();


static void OnProcessExit(CancellationTokenSource cts)
{
    cts.Cancel();
    Network.DisableProxy();
}