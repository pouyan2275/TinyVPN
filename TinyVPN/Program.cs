using Microsoft.Extensions.Configuration;
using Pouyan;
using SingBoxLib.Configuration.Outbound.Abstract;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json");
var configuration = builder.Build();

var profileTake = int.Parse(configuration["count_profile"]!);
var url = "https://raw.githubusercontent.com/Mahdi0024/ProxyCollector/master/sub/proxies.txt";

static void OnProcessExit(CancellationTokenSource cts)
{
    cts.Cancel();
    Network.DisableProxy();
}

var singbox = new SingBox("./sing-box.exe", Pouyan.Model.Inbounds.Mixed);
var cts = new CancellationTokenSource();

var profiles = SingBox.GetProfilesFromSubscribe(url);

Console.WriteLine($"{profiles.Count} Profiles Founded");
Console.WriteLine($"Select {profileTake} Profiles From {profiles.Count} Profiles");

var freePorts = Network.GetFreePorts(profileTake + 1);
int i = 0;
var profilesResults = new List<Task<Pouyan.Model.TestResult>>();

Console.WriteLine($"Testing Profiles");
profiles.GetRange(0, profileTake).ForEach(p =>
{
    i = i + 1;
    profilesResults.Add(singbox.UrlTestAsync(p, freePorts[i]));
});

var testedProfiles = Task.WhenAll(profilesResults).Result;
var orderedProfiles = testedProfiles.Where(p => p.Result!.Delay > 0).OrderBy(p => p.Result!.Delay).ToList();
if (!orderedProfiles.Any())
{
    Console.WriteLine("0 profiles work well with your internet connection");
    return;
}
Console.WriteLine("Profiles Work Well:");
orderedProfiles.ForEach(p => { Console.WriteLine($"Name: {p.Profile.Name} Delay: {p.Result!.Delay}"); });

singbox.OutBounds = new List<OutboundConfig> { orderedProfiles[0].Profile.ToOutboundConfig() };
Console.WriteLine($"Connecting To {orderedProfiles[0].Profile.Name}");
var tunneling = singbox.StartTunneling(cts);
Console.WriteLine($"Connected");

Console.CancelKeyPress += new ConsoleCancelEventHandler((e, s) => OnProcessExit(cts));
AppDomain.CurrentDomain.ProcessExit += new EventHandler((s, e) => OnProcessExit(cts));
tunneling.Wait();

