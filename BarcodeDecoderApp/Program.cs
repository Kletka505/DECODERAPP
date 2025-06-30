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
        static Barcode? lastBarcodeResult = null;
        static SerialPort? serialPort = null;
        private static readonly object consoleLock = new object();

        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            while (true)
            {
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Выберите действие:")
                        .AddChoices(new[] { "Выбрать COM порт сканера", "Загрузить дамп", "Выход" }));

                if (choice == "Выход")
                    break;

                switch (choice)
                {
                    case "Выбрать COM порт сканера":
                        SelectComPortMenu();
                        break;
                    case "Загрузить дамп":
                        LoadDumpMenu();
                        break;
                }
            }
        }

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

        static void SelectComPortMenu()
        {
            var ports = SerialPort.GetPortNames().OrderBy(p => p).ToList();
            ports.Add("[[Отмена]]");

            if (ports.Count == 1) 
            {
                AnsiConsole.MarkupLine("[red]COM-порты не найдены![/]");
                AnsiConsole.Prompt(new TextPrompt<string>("Нажмите Enter чтобы продолжить...").AllowEmpty());
                return;
            }

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Выберите COM порт сканера:")
                    .AddChoices(ports));

            if (choice == "[[Отмена]]")
                return;

            selectedPort = choice;
            RunScannerMode();
        }

        static void RunScannerMode()
        {
            if (selectedPort == null)
                return;

            try
            {
                serialPort = new SerialPort(selectedPort, 9600, Parity.None, 8, StopBits.One)
                {
                    Encoding = Encoding.ASCII,
                    ReadTimeout = 5000,
                    NewLine = "\r\n"
                };

                serialPort.DataReceived += SerialPort_DataReceived;
                serialPort.Open();

                AnsiConsole.MarkupLine($"[green]Порт {selectedPort} открыт. Ожидание сканирования...[/]");
                AnsiConsole.MarkupLine("[grey]Нажмите Enter для выхода из режима сканера.[/]");

                Console.ReadLine();

                serialPort.DataReceived -= SerialPort_DataReceived;
                serialPort.Close();
                serialPort.Dispose();
                serialPort = null;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Ошибка при открытии порта: {ex.Message}[/]");
                AnsiConsole.Prompt(new TextPrompt<string>("Нажмите Enter чтобы продолжить...").AllowEmpty());
            }
        }

        private static void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (serialPort == null)
                    return;

                string data = serialPort.ReadLine(); 
                data = data.Trim();

                if (string.IsNullOrEmpty(data))
                    return;

                lock (consoleLock)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Получен штрихкод: {data}");
                    Console.ResetColor();
                }

              
            }
            catch (TimeoutException)
            {
                
            }
            catch (Exception ex)
            {
                lock (consoleLock)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Ошибка чтения с порта: {ex.Message}");
                    Console.ResetColor();
                }
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

            lastBarcodeResult = DecodeFromFile(path);
            ShowBarcodeResultMenu();
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

                var choices = new List<string> { "Повторить", "Отмена" };

                var action = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Выберите действие:")
                        .AddChoices(choices));

                switch (action)
                {
                    case "Повторить":
                        return; 
                    case "Отмена":
                        lastBarcodeResult = null;
                        return;
                }
            }
        }
    }
}
