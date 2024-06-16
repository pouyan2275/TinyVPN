using Microsoft.Extensions.Configuration;
using Pouyan;
using Pouyan.SingBox.Model;
using SingBoxLib.Parsing;

//variables
TestResult[] testedProfiles;
List<Task<TestResult>> profilesResults;
List<ProfileItem> profiles;
int index, counterProfile;
string url;
List<TestResult> orderedProfiles;
//variables

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json");
var configuration = builder.Build();
counterProfile = int.Parse(configuration["count_profile"]!);
url = configuration["subscribe_url"]!;

var singbox = new Pouyan.SingBox.Build("./sing-box.exe", Types.Inbounds.Http);

var cts = new CancellationTokenSource();
Random rng = new();

profiles = [.. Vpn.TakeProfiles(url).OrderBy(x => rng.Next())];

if (counterProfile > profiles.Count)
    counterProfile = profiles.Count;
index = 0;
Console.WriteLine($"doing {counterProfile} test profiles");
do
{
    profilesResults = Vpn.TestProfiles(index, counterProfile, singbox, profiles);
    testedProfiles = Vpn.CheckProfiles(profilesResults);
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

singbox.OutBounds = orderedProfiles[0].Profile;

Console.WriteLine($"Connecting To {orderedProfiles[0].Profile.Name}");
var tunneling = singbox.StartTunneling(cts);
Console.WriteLine($"Connected");
Console.CancelKeyPress += new ConsoleCancelEventHandler((e, s) => OnProcessExit(cts));
AppDomain.CurrentDomain.ProcessExit += new EventHandler((s, e) => OnProcessExit(cts));
tunneling.Wait();


static void OnProcessExit(CancellationTokenSource cts)
{
    cts.Cancel();
    Pouyan.Network.Tools.DisableProxy();
}