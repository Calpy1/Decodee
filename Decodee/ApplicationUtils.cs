using System;

namespace Decodee;

public class ApplicationUtils
{
    public bool ExistChecker(string param)
    {
        return !string.IsNullOrEmpty(param);
    }

    public static void AwaitExitCommand(string error)
    {
        Console.WriteLine(error);
        Thread.Sleep(1000);
        Environment.Exit(0);
    }
}