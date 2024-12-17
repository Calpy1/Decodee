using System;

namespace Decodee;

public class QueryUpdate
{
    public async Task UpdateMetar(string intext)
    {
        MetarDecoder metarDecoder = new MetarDecoder ();
        while(true)
        {
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo _key = Console.ReadKey(true);
                if (_key.Key == ConsoleKey.Y)
                {
                    Thread.Sleep(200);
                    Console.Clear();
                    MetarRequest metarRequest = new MetarRequest(intext);
                    string metarRaw = await metarRequest.LoadMetarAsync();
                    metarDecoder.Decode(metarRaw);
                }
                else if (_key.Key == ConsoleKey.N)
                {
                    ApplicationUtils.AwaitExitCommand("Status Code OK");
                }
            }  
        }
    }
}