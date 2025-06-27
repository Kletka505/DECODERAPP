using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Spectre.Console;
using Signatec.BarcodeScaning;

namespace BarcodeDecoderApp
{
    internal class Program
    {
        static string? selectedPort = null;
        static string? lastDumpFile = null;
        static Barcode? lastBarcodeResult = null;
        static bool lastActionWasDump = false; // чтобы понять откуда повторить

        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            while (true)
            {
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Выберите действие:")
                        .AddChoices(new[] { "Выбрать порт сканера", "Загрузить дамп", "Выход" }));

                if (choice == "Выход")
                    break;

                switch (choice)
                {
                    case "Выбрать порт сканера":
                        SelectPortMenu();
                        break;
                    case "Загрузить дамп":
                        LoadDumpMenu();
                        break;
                }
            }
        }

        static void SelectPortMenu()
        {
            var ports = SerialPort.GetPortNames().OrderBy(p => p).ToList();
            ports.Add("[[Ввести вручную]]");
            ports.Add("[[Отмена]]");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Выберите порт сканера:")
                    .AddChoices(ports));

            if (choice == "[[Отмена]]")
                return;

            if (choice == "[[Ввести вручную]]")
            {
                var manualPort = AnsiConsole.Ask<string>("Введите имя порта:");
                if (!string.IsNullOrWhiteSpace(manualPort))
                {
                    selectedPort = manualPort;
                    lastActionWasDump = false;
                    RunScannerMode();
                }
            }
            else
            {
                selectedPort = choice;
                lastActionWasDump = false;
                RunScannerMode();
            }
        }

        static void LoadDumpMenu()
        {
            var path = AnsiConsole.Ask<string>("Укажите путь к файлу дампа (.dat) или введите [[Отмена]]:");

            if (path.Equals("[[Отмена]]", StringComparison.OrdinalIgnoreCase))
                return;

            if (!File.Exists(path))
            {
                AnsiConsole.MarkupLine("[red]Файл не найден![/]");
                return;
            }

            lastDumpFile = path;
            lastBarcodeResult = DecodeFromFile(path);
            lastActionWasDump = true;
            ShowBarcodeResultMenu();
        }

        static void RunScannerMode()
        {
            while (true)
            {
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine($"Порт сканера: [green]{selectedPort}[/]");
                AnsiConsole.MarkupLine("[grey]Считайте штрихкод (введите путь к файлу или 'cancel' для отмены):[/]");

                var input = Console.ReadLine();
                if (input == null || input.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                    return;

                if (!File.Exists(input))
                {
                    AnsiConsole.MarkupLine("[red]Файл не найден. Попробуйте снова.[/]");
                    AnsiConsole.Prompt(new TextPrompt<string>("Нажмите Enter чтобы продолжить..."));
                    continue;
                }

                try
                {
                    var bytes = File.ReadAllBytes(input);
                    var barBuffer = new BarBuffer(bytes.ToList());
                    var inputBuffer = barBuffer.GetPureBuffer();

                    var res = HiginaCompressor.TryDecompress(inputBuffer);
                    if (res == null)
                        res = TaskCompressor.DeCompress(inputBuffer);

                    lastBarcodeResult = res;
                    lastActionWasDump = false;
                    ShowBarcodeResultMenu();
                    return; // после показа результата вернемся в главное меню
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Ошибка: {ex.Message}[/]");
                    AnsiConsole.Prompt(new TextPrompt<string>("Нажмите Enter чтобы продолжить..."));
                }
            }
        }

        static void ShowBarcodeResultMenu()
        {
            while (true)
            {
                AnsiConsole.Clear();

                if (lastBarcodeResult == null)
                {
                    AnsiConsole.MarkupLine("[red]Штрихкод не распознан[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[green]Штрихкод распознан[/]");
                    AnsiConsole.MarkupLine($"Формат: {lastBarcodeResult.SourceType}");
                    AnsiConsole.MarkupLine($"Количество полей: {lastBarcodeResult.Fields?.Length ?? 0}");

                    var table = new Table();
                    table.AddColumn("Ключ");
                    table.AddColumn("Значение");

                    if (lastBarcodeResult.Fields != null)
                    {
                        foreach (var f in lastBarcodeResult.Fields)
                        {
                            table.AddRow(f.Name, f.Value);
                        }
                    }

                    AnsiConsole.Write(table);
                }

                var choices = new List<string> { "Повторить", "Сохранить дамп", "Отмена" };
                if (lastBarcodeResult != null)
                    choices.Insert(1, "Сохранить результат");

                var action = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Выберите действие:")
                        .AddChoices(choices));

                switch (action)
                {
                    case "Повторить":
                        if (lastActionWasDump && lastDumpFile != null)
                        {
                            lastBarcodeResult = DecodeFromFile(lastDumpFile);
                        }
                        else if (!lastActionWasDump && selectedPort != null)
                        {
                            RunScannerMode();
                            return; // exit after returning from scanner mode
                        }
                        break;

                    case "Сохранить результат":
                        SaveResultToFile();
                        break;

                    case "Сохранить дамп":
                        SaveDumpToFile();
                        break;

                    case "Отмена":
                        lastBarcodeResult = null;
                        lastDumpFile = null;
                        return;
                }
            }
        }

        static Barcode? DecodeFromFile(string path)
        {
            try
            {
                byte[] data = File.ReadAllBytes(path);
                var barBuffer = new BarBuffer(data.ToList());
                var inputBuffer = barBuffer.GetPureBuffer();

                var res = HiginaCompressor.TryDecompress(inputBuffer);
                if (res == null)
                    res = TaskCompressor.DeCompress(inputBuffer);

                return res;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Ошибка при декодировании: {ex.Message}[/]");
                AnsiConsole.Prompt(new TextPrompt<string>("Нажмите Enter чтобы продолжить..."));
                return null;
            }
        }

        static void SaveResultToFile()
        {
            var path = AnsiConsole.Ask<string>("Введите путь для сохранения результата (.txt):");
            try
            {
                using var writer = new StreamWriter(path);
                writer.WriteLine($"Формат: {lastBarcodeResult!.SourceType}");
                writer.WriteLine($"Количество полей: {lastBarcodeResult.Fields?.Length ?? 0}");
                writer.WriteLine();

                if (lastBarcodeResult.Fields != null)
                {
                    foreach (var field in lastBarcodeResult.Fields)
                    {
                        writer.WriteLine($"{field.Name}: {field.Value}");
                    }
                }

                AnsiConsole.MarkupLine("[green]Результат сохранён успешно[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Ошибка сохранения: {ex.Message}[/]");
            }
            AnsiConsole.Prompt(new TextPrompt<string>("Нажмите Enter чтобы продолжить..."));
        }

        static void SaveDumpToFile()
        {
            if (lastDumpFile == null)
            {
                AnsiConsole.MarkupLine("[red]Дамп отсутствует для сохранения[/]");
                AnsiConsole.Prompt(new TextPrompt<string>("Нажмите Enter чтобы продолжить..."));
                return;
            }

            var path = AnsiConsole.Ask<string>("Введите путь для сохранения дампа (.dat):");
            try
            {
                File.Copy(lastDumpFile, path, true);
                AnsiConsole.MarkupLine("[green]Дамп сохранён успешно[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Ошибка сохранения дампа: {ex.Message}[/]");
            }
            AnsiConsole.Prompt(new TextPrompt<string>("Нажмите Enter чтобы продолжить..."));
        }
    }
}
