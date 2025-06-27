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

        // Метод для удаления кавычек в начале и конце строки
        static string CleanInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            input = input.Trim();

            if (input.StartsWith("\"") && input.EndsWith("\"") && input.Length > 1)
            {
                input = input.Substring(1, input.Length - 2);
            }
            return input;
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
                var manualPortRaw = AnsiConsole.Ask<string>("Введите имя порта:");
                var manualPort = CleanInput(manualPortRaw);
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
            var pathRaw = AnsiConsole.Ask<string>("Укажите путь к файлу дампа (.dat) или введите [[Отмена]]:");
            var path = CleanInput(pathRaw);

            if (path.Equals("[[Отмена]]", StringComparison.OrdinalIgnoreCase))
                return;

            if (!File.Exists(path))
            {
                AnsiConsole.MarkupLine("[red]Файл не найден![/]");
                AnsiConsole.Prompt(new TextPrompt<string>("Нажмите Enter чтобы продолжить...").AllowEmpty());
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

                var inputRaw = Console.ReadLine();
                if (inputRaw == null)
                    return;

                var input = CleanInput(inputRaw);

                if (input.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                    return;

                if (!File.Exists(input))
                {
                    AnsiConsole.MarkupLine("[red]Файл не найден. Попробуйте снова.[/]");
                    AnsiConsole.Prompt(new TextPrompt<string>("Нажмите Enter чтобы продолжить...").AllowEmpty());
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
                    AnsiConsole.Prompt(new TextPrompt<string>("Нажмите Enter чтобы продолжить...").AllowEmpty());
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
                        if (lastActionWasDump)
                        {
                            // Запрос нового пути и декодирование дампа, оставаясь в этом меню
                            ReloadDump();
                        }
                        else if (!lastActionWasDump && selectedPort != null)
                        {
                            RunScannerMode();
                            return; // выход после работы сканера
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

        static void ReloadDump()
        {
            var pathRaw = AnsiConsole.Ask<string>("Укажите путь к файлу дампа (.dat) или введите [[Отмена]]:");
            var path = CleanInput(pathRaw);

            if (path.Equals("[[Отмена]]", StringComparison.OrdinalIgnoreCase))
                return;

            if (!File.Exists(path))
            {
                AnsiConsole.MarkupLine("[red]Файл не найден![/]");
                AnsiConsole.Prompt(new TextPrompt<string>("Нажмите Enter чтобы продолжить...").AllowEmpty());
                return;
            }

            lastDumpFile = path;
            lastBarcodeResult = DecodeFromFile(path);
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
                AnsiConsole.Prompt(new TextPrompt<string>("Нажмите Enter чтобы продолжить...").AllowEmpty());
                return null;
            }
        }

        static void SaveResultToFile()
        {
            var pathRaw = AnsiConsole.Ask<string>("Введите путь для сохранения результата (.txt):");
            var path = CleanInput(pathRaw);

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
            AnsiConsole.Prompt(new TextPrompt<string>("Нажмите Enter чтобы продолжить...").AllowEmpty());
        }

        static void SaveDumpToFile()
        {
            if (lastDumpFile == null)
            {
                AnsiConsole.MarkupLine("[red]Дамп отсутствует для сохранения[/]");
                AnsiConsole.Prompt(new TextPrompt<string>("Нажмите Enter чтобы продолжить...").AllowEmpty());
                return;
            }

            var pathRaw = AnsiConsole.Ask<string>("Введите путь для сохранения дампа (.dat):");
            var path = CleanInput(pathRaw);

            try
            {
                File.Copy(lastDumpFile, path, true);
                AnsiConsole.MarkupLine("[green]Дамп сохранён успешно[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Ошибка сохранения дампа: {ex.Message}[/]");
            }
            AnsiConsole.Prompt(new TextPrompt<string>("Нажмите Enter чтобы продолжить...").AllowEmpty());
        }
    }
}
