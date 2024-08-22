using Microsoft.Extensions.Configuration;
using Pouyan;
using Pouyan.Network;
using Pouyan.SingBox;
using Pouyan.SingBox.Model;
using SingBoxLib.Parsing;
using System.Diagnostics;

//variables
List<TestResult> profilesResults;
int index, counterProfile;
string url;
List<TestResult> orderedProfiles;
Vpn vpn = new Vpn();
string singboxPath = "./singbox/sing-box.exe";
var profileTester = new ProfileTester(singboxPath);
var cts = new CancellationTokenSource();
var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json").Build();
var setSystemProxy = bool.Parse(builder["system_proxy_on_start"]!);
var inbounds = Inbound.CreateMixedInbound(
    listen:"127.0.0.1",
    listenPort: 3080,
    setSystemProxy
    );
Random rng = new();
//variables

Console.WriteLine($"Mixed: {inbounds.Listen}:{inbounds.ListenPort}");
Console.Title = "Connecting";

counterProfile = int.Parse(builder["count_profile"]!);
url = builder["subscribe_url"]!;

var singbox = new Tunnel(singboxPath, [inbounds]);

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
string titleMessage = $"Connected - {orderedProfiles[0].Profile.Name} - ";
titleMessage = setSystemProxy ? titleMessage + "Enable" : titleMessage + "Disable";
Console.Title = titleMessage;


Console.WriteLine($"Connected - {orderedProfiles[0].Profile.Name} - proxy is enable");

Console.CancelKeyPress += new ConsoleCancelEventHandler((e, s) => OnProcessExit(cts));
AppDomain.CurrentDomain.ProcessExit += new EventHandler((s, e) => OnProcessExit(cts));

singbox.StartAsync(orderedProfiles[0].Profile, cts);
while (true)
{
    Console.WriteLine("Tip: \n" +
        "\t1- Press 1 For Disable Proxy \n" +
        "\t2- Press 2 For Enable Proxy \n" +
        "\t3- Q for Exit");
    var keyPressed = Console.ReadKey().Key;
    switch (keyPressed)
    {
        case ConsoleKey.D1 :
            Proxy.DisableProxy();
            Console.WriteLine($"\nSytem Proxy is Disable");
            Console.Title = $"Connected - {orderedProfiles[0].Profile.Name} - Disable";
            break;
        case ConsoleKey.D2 :
            Proxy.EnableProxy(inbounds.Listen! , inbounds.ListenPort ?? 0);
            Console.WriteLine($"\nsystem proxy is enable");
            Console.Title = $"Connected - {orderedProfiles[0].Profile.Name} - Enable";
            break;
        case ConsoleKey.Q:
            cts.Cancel();
            return;
        default:
            Console.WriteLine();
            break;
    }
}


static void OnProcessExit(CancellationTokenSource cts)
{
    Tunnel.CloseTunnel();
    cts.Cancel();
    Proxy.DisableProxy();
}