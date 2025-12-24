
    using System;
    using HowFrame;

    public class DeferExample
    {
        void MyFunc()
        {
            using var defer = new Defer();

            defer.Add(() => Console.WriteLine("cleanup 1"));
            defer.Add(() => Console.WriteLine("cleanup 2"));

            Console.WriteLine("doing work");
        }
    }
