using Microsoft.Extensions.Configuration;
using Pouyan;
using Pouyan.SingBox;
using Pouyan.SingBox.Model;
using SingBoxLib.Parsing;

//variables
List<TestResult> profilesResults;
int index, counterProfile;
string url;
List<TestResult> orderedProfiles;
Vpn vpn = new Vpn();
string singboxPath = "./singbox/sing-box.exe";
var profileTester = new ProfileTester(singboxPath);
var cts = new CancellationTokenSource();
var inbounds = Pouyan.SingBox.Inbound.CreateMixedInbound(
    listen:"127.0.0.1",
    listenPort: 3080,
    true
    );
Random rng = new();
//variables


Console.Title = "Connecting";
var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json");

var configuration = builder.Build();
counterProfile = int.Parse(configuration["count_profile"]!);
url = configuration["subscribe_url"]!;


var singbox = new Pouyan.SingBox.Tunnel(singboxPath, [inbounds]);


List<ProfileItem> profiles = [.. vpn.TakeProfiles(url).OrderBy(x => rng.Next())];

if (counterProfile > profiles.Count)
    counterProfile = profiles.Count;
index = 0;
Console.WriteLine($"doing {counterProfile} test profiles");
do
{
    Console.WriteLine($"Start Test Profiles...");
    profilesResults = profileTester!.UrlTestAsync(profiles.GetRange(index, counterProfile)).Result.ToList();
    vpn.WriteTestResult(profilesResults);
    orderedProfiles = profilesResults.Where(p => p.Result!.Delay > 0).OrderBy(p => p.Result!.Delay).ToList();
    index += counterProfile;
    if (index > profiles.Count)
        index = profiles.Count;
} while (orderedProfiles.Count == 0 || index == profiles.Count);

if (orderedProfiles.Count == 0)
{
    Console.WriteLine("0 profiles work well with your internet connection");
    Console.ReadLine();
    return;
}
Console.WriteLine("Profiles Work Well:");
orderedProfiles.ForEach(p => { Console.WriteLine($"Name: {p.Profile.Name} Delay: {p.Result!.Delay}"); });

Console.WriteLine($"Connecting To {orderedProfiles[0].Profile.Name}");

Console.Title = $"Connected - {orderedProfiles[0].Profile.Name}";


Console.WriteLine($"Connected");


Console.CancelKeyPress += new ConsoleCancelEventHandler((e, s) => OnProcessExit(cts));
AppDomain.CurrentDomain.ProcessExit += new EventHandler((s, e) => OnProcessExit(cts));
Console.ReadLine();


static void OnProcessExit(CancellationTokenSource cts)
{
    Pouyan.SingBox.Tunnel.CloseSingBox();
    cts.Cancel();
    Pouyan.Network.Proxy.DisableProxy();
}