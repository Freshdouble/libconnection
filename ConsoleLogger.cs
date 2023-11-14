using System;
using System.Threading;

internal class ConsoleLogger
{
    private static ConsoleLogger instance = null;
    public static ConsoleLogger GetInstance()
    {
        if (instance == null)
        {
            instance = new ConsoleLogger();
        }
        return instance;
    }

    private object monitorLock = new();
    private ConsoleLogger()
    {

    }

    public void Write(string message)
    {
        lock (monitorLock)
        {
            Console.Write(message);
        }
    }

    public void WriteLine(string message)
    {
        Write(message + Environment.NewLine);
    }
}