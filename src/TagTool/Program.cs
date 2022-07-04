while (!(Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.F4))
{
    Console.Write("Hi");
    await Task.Delay(1000);
}

return 0;
