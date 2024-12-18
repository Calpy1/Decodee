using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decodee
{
    public class Entry
    {
        public async Task ProgramStartAsync()
        {
            TelegramBot bot = new TelegramBot();
            await bot.Start();
        }

    }
}