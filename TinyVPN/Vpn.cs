using Pouyan.SingBox.Model;
using Pouyan;
using SingBoxLib.Configuration.Outbound.Abstract;
using SingBoxLib.Parsing;

namespace Pouyan
{
    public class Vpn
    {
        public static List<Task<TestResult>> TestProfiles(int index, int count, SingBox.Build singbox, List<ProfileItem> profiles)
        {
            var testResults = new List<Task<TestResult>>();
            Console.WriteLine($"Start Test Profiles...");
            var freePorts = Pouyan.Network.Tools.GetFreePorts(count + 1);
            int i = 0;
            profiles.GetRange(index, count).ForEach(p =>
            {
                i++;
                testResults.Add(singbox.UrlTestAsync(p, freePorts[i],1000));
            });
            return testResults;
        }
        
        public static TestResult[] CheckProfiles(List<Task<TestResult>> profilesResults)
        {
            var testedProfiles = Task.WhenAll(profilesResults).Result;
            Console.WriteLine("Test Result:");
            testedProfiles.ToList().ForEach(p => Console.WriteLine($"{p.Profile.Name} - Delay:{p.Result!.Delay}"));
            return testedProfiles;
        }

        public static List<ProfileItem> TakeProfiles(string url)
        {
            var profiles = SingBox.Build.GetProfilesFromSubscribe(url);

            Console.WriteLine($"{profiles.Count} Profiles Founded ");
            return profiles;
        }
    }
}
