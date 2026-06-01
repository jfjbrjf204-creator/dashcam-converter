namespace DashcamConverter;

static class Program
{
    public const string Version = "1.0.3";

    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
        else if (args[0] == "--version")
        {
            Console.WriteLine($"DashcamConverter {Version}");
        }
        else if (args[0] == "remux" && args.Length >= 2)
        {
            var input = args[1];
            var output = args.Length >= 4 && args[2] == "-o" ? args[3] : null;
            var useMkv = Array.IndexOf(args, "--mkv") >= 0;
            if (useMkv && output != null)
            {
                output = Path.ChangeExtension(output, ".mkv");
            }
            else if (useMkv && output == null)
            {
                output = Path.ChangeExtension(input, ".mkv");
            }
            try
            {
                var result = Converter.Remux(input, output, p =>
                    Console.Write($"\rКонвертация: {p:F0}%"));
                Console.WriteLine($"\nГотово: {result}");
            }
            catch (ConversionException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Ошибка: {ex.Message}");
                Environment.Exit(1);
            }
        }
        else
        {
            Console.WriteLine("DashcamConverter remux <input> [-o output] [--mkv]");
            Console.WriteLine("DashcamConverter --version");
        }
    }
}
