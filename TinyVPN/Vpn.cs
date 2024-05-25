using SingBoxLib.Configuration.Outbound.Abstract;
using SingBoxLib.Parsing;

namespace Pouyan
{
    public class Vpn
    {
        public static List<Task<Model.TestResult>> TestProfiles(int index, int count, SingBox singbox, List<ProfileItem> profiles)
        {
            var testResults = new List<Task<Pouyan.Model.TestResult>>();
            Console.WriteLine($"Start Test Profiles...");
            var freePorts = Network.GetFreePorts(count + 1);
            int i = 0;
            profiles.GetRange(index, count).ForEach(p =>
            {
                i++;
                testResults.Add(singbox.UrlTestAsync(p, freePorts[i],1000));
            });
            return testResults;
        }
        
        public static Model.TestResult[] CheckProfiles(List<Task<Pouyan.Model.TestResult>> profilesResults)
        {
            var testedProfiles = Task.WhenAll(profilesResults).Result;
            Console.WriteLine("Test Result:");
            testedProfiles.ToList().ForEach(p => Console.WriteLine($"{p.Profile.Name} - Delay:{p.Result!.Delay}"));
            return testedProfiles;
        }
        public static OutboundConfig GetOutbounds(ProfileItem profile)
        {
            return  profile.ToOutboundConfig();
        }

        public static List<ProfileItem> TakeProfiles(string url)
        {
            var profiles = SingBox.GetProfilesFromSubscribe(url);

            Console.WriteLine($"{profiles.Count} Profiles Founded ");
            return profiles;
        }
    }
}
