using Microsoft.Extensions.Configuration;
using Pouyan;
using SingBoxLib.Configuration.Outbound.Abstract;
using SingBoxLib.Parsing;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json");
var configuration = builder.Build();

var profileTake = int.Parse(configuration["count_profile"]!);

var url = "https://raw.githubusercontent.com/Mahdi0024/ProxyCollector/master/sub/proxies.txt";

var singbox = new SingBox("./sing-box.exe", Pouyan.Model.Inbounds.Mixed);

var cts = new CancellationTokenSource();

var profiles = TakeProfiles();
if (profileTake > profiles.Count)
    profileTake = profiles.Count;

var profilesResults = new List<Task<Pouyan.Model.TestResult>>();
TestProfiles(profileTake,singbox,profiles,profilesResults);

var testedProfiles = Task.WhenAll(profilesResults).Result;
testedProfiles.ToList().ForEach(p => Console.WriteLine($"{p.Profile.Name} - Delay:{p.Result!.Delay}"));

var orderedProfiles = testedProfiles.Where(p => p.Result!.Delay > 0).OrderBy(p => p.Result!.Delay).ToList();
if (!orderedProfiles.Any())
{
    Console.WriteLine("0 profiles work well with your internet connection");
    Console.ReadLine();
    return;
}
Console.WriteLine("Profiles Work Well:");
orderedProfiles.ForEach(p => { Console.WriteLine($"Name: {p.Profile.Name} Delay: {p.Result!.Delay}"); });

singbox.OutBounds = GetOutbounds(orderedProfiles[0].Profile);

Console.WriteLine($"Connecting To {orderedProfiles[0].Profile.Name}");
var tunneling = singbox.StartTunneling(cts);
Console.WriteLine($"Connected");

Console.CancelKeyPress += new ConsoleCancelEventHandler((e, s) => OnProcessExit(cts));
AppDomain.CurrentDomain.ProcessExit += new EventHandler((s, e) => OnProcessExit(cts));
tunneling.Wait();




List<OutboundConfig> GetOutbounds(ProfileItem profile)
{
    return new List<OutboundConfig> { profile.ToOutboundConfig() };
}

List<ProfileItem> TakeProfiles()
{
    var profiles = SingBox.GetProfilesFromSubscribe(url);

    Console.WriteLine($"{profiles.Count} Profiles Founded ");
    Console.WriteLine($"Select {profileTake} Profiles From {profiles.Count} Profiles");
    return profiles;
}
static void OnProcessExit(CancellationTokenSource cts)
{
    cts.Cancel();
    Network.DisableProxy();
}

void TestProfiles(int count, SingBox singbox, List<ProfileItem> profiles, List<Task<Pouyan.Model.TestResult>> testResults)
{
    Console.WriteLine($"Testing Profiles");
    var freePorts = Network.GetFreePorts(count + 1);
    int i = 0;
    profiles.GetRange(0, count).ForEach(p =>
    {
        i = i + 1;
        Console.WriteLine($"Start Test {i}.{p.Name}");
        testResults.Add(singbox.UrlTestAsync(p, freePorts[i]));
    });
}