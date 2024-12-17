using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Decodee
{
    internal class TelegramBot
    {
        private readonly string _token = "TOKEN_HERE";
        private readonly TelegramBotClient _botClient;
        private CancellationTokenSource _cancellationTokenSource;

        string resultMetar = string.Empty;

        public TelegramBot()
        {
            _botClient = new TelegramBotClient(_token);
        }

        public async Task Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message }
            };

            _botClient.StartReceiving(OnUpdate, OnError, receiverOptions, _cancellationTokenSource.Token);

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} INFO: Bot started successfully");
            Console.ReadKey();
            StopBot();
        }

        public async Task SendWelcomeMessage(Update update)
        {
            var chatId = update.Message.Chat.Id;
            await _botClient.SendTextMessageAsync(chatId, "Hello, Captain. Please enter an ICAO code");
        }

        private async Task OnUpdate(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message?.Text != null)
            {
                if (update.Message.Text == "/start")
                {
                    await SendWelcomeMessage(update);
                }

                if (update.Message.Text.Length != 4)
                {
                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} ERROR: Chat ID: {update.Message.Chat.Id}: Invalid ICAO '{update.Message.Text.ToUpper()}' | Internal Error #1");
                    await botClient.SendTextMessageAsync(update.Message.Chat.Id, $"Please enter a valid ICAO");
                }

                else
                {
                    IcaoReader icaoReader = new IcaoReader();
                    bool result = icaoReader.SetICAO(update.Message.Text);
                    string ICAO = update.Message.Text;

                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} INFO: Chat ID: {update.Message.Chat.Id}: {ICAO}");
                    try
                    {
                        if (result)
                        {
                            MetarRequest metarRequest = new MetarRequest(ICAO);
                            string metarRaw = await metarRequest.LoadMetarAsync();

                            if (metarRaw != null)
                            {
                                MetarDecoder metarDecoder = new MetarDecoder();
                                metarDecoder.Decode(metarRaw);

                                await botClient.SendTextMessageAsync(update.Message.Chat.Id, metarRaw);

                                DecodedMetar decodedMetar = new DecodedMetar();
                                string resultMetar = decodedMetar.GetData();

                                if (resultMetar != null)
                                {
                                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} INFO: Chat ID: {update.Message.Chat.Id}: '{ICAO.ToUpper()}' - OK");
                                }

                                await botClient.SendTextMessageAsync(update.Message.Chat.Id, resultMetar);
                            }
                        }
                    }
                    catch (ArgumentNullException ex)
                    {
                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} ERROR: Chat ID: {update.Message.Chat.Id}: Null Exception: {ex.Message} | Internal Error #2");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} ERROR: Chat ID: {update.Message.Chat.Id}: Invalid ICAO '{ICAO.ToUpper()}' | Internal Error #3");
                        await botClient.SendTextMessageAsync(update.Message.Chat.Id, $"Please enter a valid ICAO");

                        //Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} ERROR: Chat ID: {update.Message.Chat.Id}: Stack Trace:");
                        //var stackTraceLines = ex.StackTrace.Split('\n');
                        //foreach (var line in stackTraceLines)
                        //{
                        //    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} ERROR: Chat ID: {update.Message.Chat.Id}: {ex.Message}");
                        //    Console.WriteLine(line.Trim());
                        //}
                    }

                }
            }
            else
            {
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} ERROR: Chat ID: {update.Message.Chat.Id}: ICAO must consist of 4 letters | Internal Error #3");
                await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Please enter a valid ICAO");
            }
        }

        private async Task OnError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} INFO: Chat ID: {botClient.ExceptionsParser}: {exception.Message}");
            await Task.CompletedTask;
        }

        public void StopBot()
        {
            _cancellationTokenSource?.Cancel();
        }
    }
}
