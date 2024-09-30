using Pouyan;
using Pouyan.Network;

var vpn = new Vpn();
CancellationTokenSource cts = new CancellationTokenSource();
await vpn.Start(cts);

while (true)
{
    Console.WriteLine("Tip: \n" +
        "\t1- Press D For Disable Proxy \n" +
        "\t2- Press E For Enable Proxy \n" +
        "\t2- Press R For Restart Proxy \n" +
        "\t3- Q for Exit");
    var keyPressed = Console.ReadKey().Key;
    switch (keyPressed)
    {
        case ConsoleKey.D :
            Proxy.DisableProxy();
            Console.WriteLine($"\nSytem Proxy is Disable");
            Console.Title = $"Connected - Disable";
            break;
        case ConsoleKey.E :
            Proxy.EnableProxy(vpn.inbounds.Listen! , vpn.inbounds.ListenPort ?? 0);
            Console.WriteLine($"\nsystem proxy is enable");
            Console.Title = $"Connected - {vpn.orderedProfiles[0].Profile.Name} - Enable";
            break;
        case ConsoleKey.R :
            Console.Clear();
            Vpn.OnProcessExit(cts);
            vpn = new();
            await vpn.Start(cts);
            break;
        case ConsoleKey.Q:
            cts.Cancel();
            return;
        default:
            Console.WriteLine();
            break;
    }
}


