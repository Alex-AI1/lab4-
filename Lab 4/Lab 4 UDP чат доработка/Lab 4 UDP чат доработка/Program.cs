using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

class UDPMessage
{
    public int Code { get; set; } // Код сообщения
    public int Length { get; set; } // Длина сообщения
    public string Message { get; set; } = ""; // Сообщение
    public string Sender { get; set; } = ""; // Имя отправителя
}

class Program
{
    private static CancellationTokenSource _cts = new();
    private static string _username;
    private const int MaxMessageLength = 25; // Ограничение на длину сообщения

    static async Task Main()
    {
        try
        {
            Console.Write("Введите ваше имя: ");
            _username = Console.ReadLine()?.Trim() ?? "Неизвестный";

            Console.Write("Введите порт для приема сообщений: ");
            if (!int.TryParse(Console.ReadLine(), out var localPort))
            {
                Console.WriteLine("Ошибка: Некорректный порт!");
                return;
            }

            Console.Write("Введите порт для отправки сообщений: ");
            if (!int.TryParse(Console.ReadLine(), out var remotePort))
            {
                Console.WriteLine("Ошибка: Некорректный порт!");
                return;
            }

            var receiveTask = ReceiveMessageAsync(localPort, _cts.Token);
            await MenuAsync(remotePort);

            _cts.Cancel();
            await receiveTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }

    static async Task MenuAsync(int remotePort)
    {
        while (true)
        {
            Console.WriteLine("\nМеню:");
            Console.WriteLine("1 - Отправить сообщение");
            Console.WriteLine("2 - Проверка соединения (true)");
            Console.WriteLine("3 - Проверка соединения (false)");
            Console.WriteLine("5 - Установить соединение");
            Console.WriteLine("6 - Проверка результата соединения");
            Console.Write("Выберите пункт: ");

            string choice = Console.ReadLine()?.Trim() ?? "";
            switch (choice)
            {
                case "1":
                    await SendMessageAsync(remotePort);
                    break;
                case "2":
                    await SendCheckMessageAsync(remotePort, 1);
                    break;
                case "3":
                    await SendCheckMessageAsync(remotePort, 2);
                    break;
                case "5":
                    await SendCheckMessageAsync(remotePort, 3);
                    break;
                case "6":
                    await SendCheckMessageAsync(remotePort, 4);
                    break;
                default:
                    Console.WriteLine("Ошибка: Неверный пункт меню!");
                    break;
            }
        }
    }

    static async Task SendMessageAsync(int remotePort)
    {
        using UdpClient sender = new();
        Console.Write("Введите сообщение (максимум 25 символов): ");
        string messageText = Console.ReadLine()?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(messageText)) return;

        if (messageText.Length > MaxMessageLength)
        {
            Console.WriteLine("Ошибка: сообщение слишком длинное! (максимум 25 символов)");
            return;
        }

        var udpMessage = new UDPMessage
        {
            Code = 0,
            Message = messageText,
            Length = messageText.Length,
            Sender = _username
        };

        byte[] data = JsonSerializer.SerializeToUtf8Bytes(udpMessage);
        await sender.SendAsync(data, new IPEndPoint(IPAddress.Parse("127.0.0.1"), remotePort));
        Console.WriteLine($"[{_username}]: {messageText}");
    }

    static async Task SendCheckMessageAsync(int remotePort, int checkCode)
    {
        using UdpClient sender = new();
        string checkMessage = checkCode == 1 ? "true" : checkCode == 2 ? "false" : "check";

        var udpMessage = new UDPMessage
        {
            Code = checkCode,
            Message = checkMessage,
            Length = checkMessage.Length,
            Sender = _username
        };

        byte[] data = JsonSerializer.SerializeToUtf8Bytes(udpMessage);
        await sender.SendAsync(data, new IPEndPoint(IPAddress.Parse("127.0.0.1"), remotePort));
        Console.WriteLine($"[{_username}]: {checkMessage}");
    }

    static async Task ReceiveMessageAsync(int localPort, CancellationToken token)
    {
        using UdpClient receiver = new(new IPEndPoint(IPAddress.Any, localPort));
        Console.WriteLine($"Ожидание сообщений на порту {localPort}...");

        try
        {
            while (!token.IsCancellationRequested)
            {
                var result = await receiver.ReceiveAsync();
                ProcessReceivedMessage(result.Buffer);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Прием сообщений остановлен.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при приеме сообщения: {ex.Message}");
        }
    }

    static void ProcessReceivedMessage(byte[] data)
    {
        try
        {
            var udpMessage = JsonSerializer.Deserialize<UDPMessage>(data);
            if (udpMessage == null) return;

            switch (udpMessage.Code)
            {
                case 0:
                    Console.WriteLine($"[{udpMessage.Sender}]: {udpMessage.Message}");
                    break;
                case 1:
                    Console.WriteLine("Проверка соединения: true");
                    break;
                case 2:
                    Console.WriteLine("Проверка соединения: false");
                    break;
                case 3:
                    Console.WriteLine("Установлено соединение");
                    break;
                case 4:
                    Console.WriteLine("Соединение не прошло");
                    break;
                default:
                    Console.WriteLine("Неизвестный код сообщения.");
                    break;
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Ошибка десериализации JSON: {ex.Message}");
        }
    }
}









