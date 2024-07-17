using Pouyan.SingBox.Model;
using SingBoxLib.Parsing;

namespace Pouyan
{
    public class Vpn
    { 
        public void WriteTestResult(List<TestResult> profilesResults)
        {
            Console.WriteLine("Test Result:");
            profilesResults.ForEach(p => Console.WriteLine($"{p.Profile.Name} - Delay:{p.Result!.Delay}"));
        }

        public List<ProfileItem> TakeProfiles(string url)
        {
            var profiles = SingBox.GetProfile.GetProfilesFromSubscribe(url);

            Console.WriteLine($"{profiles.Count} Profiles Founded ");
            return profiles;
        }
    }
}
