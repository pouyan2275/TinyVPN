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

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json");
var configuration = builder.Build();

counterProfile = int.Parse(configuration["count_profile"]!);
url = configuration["subscribe_url"]!;

var singbox = new SingBox("./sing-box.exe", Pouyan.Model.Inbounds.Mixed);

var cts = new CancellationTokenSource();

profiles = TakeProfiles();
if (counterProfile > profiles.Count)
    counterProfile = profiles.Count;
index = 0;
Console.WriteLine($"doing {counterProfile} test profiles");
do
{
    profilesResults = TestProfiles(index, counterProfile, singbox, profiles);
    testedProfiles = CheckProfiles(profilesResults);
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
    return profiles;
}
static void OnProcessExit(CancellationTokenSource cts)
{
    cts.Cancel();
    Network.DisableProxy();
}

List<Task<Pouyan.Model.TestResult>> TestProfiles(int index, int count, SingBox singbox, List<ProfileItem> profiles)
{
    var testResults = new List<Task<Pouyan.Model.TestResult>>();
    Console.WriteLine($"Start Test Profiles...");
    var freePorts = Network.GetFreePorts(count + 1);
    int i = 0;
    profiles.GetRange(index, count).ForEach(p =>
    {
        i = i + 1;
        testResults.Add(singbox.UrlTestAsync(p, freePorts[i]));
    });
    return testResults;
}

Pouyan.Model.TestResult[] CheckProfiles(List<Task<Pouyan.Model.TestResult>> profilesResults)
{
    var testedProfiles = Task.WhenAll(profilesResults).Result;
    Console.WriteLine("Test Result:");
    testedProfiles.ToList().ForEach(p => Console.WriteLine($"{p.Profile.Name} - Delay:{p.Result!.Delay}"));
    return testedProfiles;
}