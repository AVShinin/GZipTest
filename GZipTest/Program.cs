using System;

namespace GZipTest
{
    class Program
    {
       static GZip zipper;

        static int Main(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelKeyPress);

            ShowInfo();

            try
            {
                args.ArgsValidation();

                switch (args[0].ToLower())
                {
                    case "compress":
                        zipper = new Compressor(args[1], args[2]);
                    break;
                    case "decompress":
                        zipper = new Decompressor(args[1], args[2]);
                    break;
                }

                zipper.Launch();
                return zipper.CallBackResult();
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Error is occured!\n Method: {ex.TargetSite}\n Error description: {ex.Message}");
                return 1;
            }

        }

       static void ShowInfo()
        {
            Console.WriteLine("████─████─███─████─███─███─███─███\n" +
                              "█──────██──█──█──█──█──█───█────█\n" +
                              "█─██──██───█──████──█──███─███──█\n" +
                              "█──█─██────█──█─────█──█─────█──█\n" +
                              "████─████─███─█─────█──███─███──█\n"+
                              "To zip or unzip files please proceed with the following pattern to type in:\n" + 
                              "Zipping: GZipTest.exe compress [Source file path] [Destination file path]\n" +
                              "Unzipping: GZipTest.exe decompress [Compressed file path] [Destination file path]\n" +
                              "To complete the program correct please use the combination CTRL + C\n");
        }


       static void CancelKeyPress(object sender, ConsoleCancelEventArgs _args)
        {
            if (_args.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                Console.WriteLine("\nCancelling...");
                _args.Cancel = true;
                zipper.Cancel();
                
            }
        }
    }
}
