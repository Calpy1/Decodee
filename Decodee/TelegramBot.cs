using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Decodee
{
    internal class TelegramBot
    {
        private readonly string _token = "YOUR_TOKEN";
        private readonly long _adminChatId = YOUR_ID;
        private long _chatId;
        private string _chatText;
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

            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] INFO: Bot started successfully");
            Console.ReadKey();
            StopBot();
        }

        public async Task SendWelcomeMessage(Update update)
        {
            await _botClient.SendTextMessageAsync(_chatId, "Hello, Captain. Please enter an ICAO code");
        }

        private async Task OnUpdate(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message?.Text != null)
            {
                _chatId = update.Message.Chat.Id;
                _chatText = update.Message.Text;
                if (_chatText == "/start")
                {
                    await SendWelcomeMessage(update);
                }
                else if (_chatText.Length != 4 || _chatText.Contains(" "))
                {
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ERROR: Chat ID: {_chatId}: {botClient.ExceptionsParser} (INVALID ICAO or WHITESPACE INCLUDED) '{_chatText.ToUpper()}' | Internal Error #1");
                    await SendDebugMessageAsync($"[{DateTime.Now.ToString("HH:mm:ss")}] ERROR: Chat ID: {_chatId}: {botClient.ExceptionsParser} (INVALID ICAO or WHITESPACE INCLUDED) '{_chatText.ToUpper()}' | Internal Error #1");
                    await botClient.SendTextMessageAsync(_chatId, $"Please enter a valid ICAO");
                }

                else
                {
                    IcaoReader icaoReader = new IcaoReader();
                    bool result = icaoReader.SetICAO(_chatText);

                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] INFO: Chat ID: {_chatId}: '{_chatText.ToUpper()} 'Status: REQUESTED");
                    await SendDebugMessageAsync($"[{DateTime.Now.ToString("HH:mm:ss")}] INFO: Chat ID: {_chatId}: '{_chatText.ToUpper()} 'Status: REQUESTED");
                    try
                    {
                        if (result)
                        {
                            MetarRequest metarRequest = new MetarRequest(_chatText.ToUpper());
                            string metarRaw = await metarRequest.LoadMetarAsync();

                            if (metarRaw != null)
                            {
                                MetarDecoder metarDecoder = new MetarDecoder();
                                metarDecoder.Decode(metarRaw);


                                DecodedMetar decodedMetar = new DecodedMetar();
                                string resultMetar = decodedMetar.GetData();

                                string metarFinalization = string.Join("\n\n", metarRaw, resultMetar);

                                await botClient.SendTextMessageAsync(_chatId, metarFinalization);

                                if (resultMetar != null)
                                {
                                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] INFO: Chat ID: {_chatId}: '{_chatText.ToUpper()} 'Status: COMPLETED");
                                    await SendDebugMessageAsync($"[{DateTime.Now.ToString("HH:mm:ss")}] INFO: Chat ID: {_chatId}: '{_chatText.ToUpper()} 'Status: COMPLETED");
                                }
                            }
                        }
                    }
                    catch (ArgumentNullException ex)
                    {
                        Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ERROR from ArgumentNullException: Chat ID: {_chatId}: {ex.Message} | Internal Error #2");
                        await SendDebugMessageAsync($"[{DateTime.Now.ToString("HH:mm:ss")}] ERROR from ArgumentNullException: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ERROR: Chat ID: {_chatId}: Invalid ICAO '{_chatText.ToUpper()}' | Internal Error #3");
                        await SendDebugMessageAsync($"[{DateTime.Now.ToString("HH:mm:ss")}] ERROR: Chat ID: {_chatId}: {ex.Message}");
                        await botClient.SendTextMessageAsync(_chatId, $"Please enter a valid ICAO");

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
                Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ERROR: Chat ID: {_chatId}: ICAO must consist of 4 letters | Internal Error #3");
                await SendDebugMessageAsync($"[{DateTime.Now.ToString("HH:mm:ss")}] ERROR: Chat ID: {_chatId}: ICAO must consist of 4 letters | Internal Error #3");
                await botClient.SendTextMessageAsync(_chatId, "Please enter a valid ICAO");
            }
        }

        private async Task OnError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] INFO: Chat ID: {botClient.ExceptionsParser} ({exception.Message})");
            await Task.CompletedTask;
        }

        public async Task SendDebugMessageAsync(string debugInfo)
        {
            try
            {
                await _botClient.SendTextMessageAsync(_adminChatId, $"🔧 DEBUG INFO:\n{debugInfo}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send DEBUG message: {ex.Message}");
            }
        }

        public void StopBot()
        {
            _cancellationTokenSource?.Cancel();
        }
    }
}
