using System;
using System.Security.Cryptography.X509Certificates;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using Microsoft.VisualBasic;
using System.Reflection;
using System.Reflection.Metadata;

namespace Decodee;

class Program
{
    static async Task Main(string[] args)
    {
        Entry programStart = new Entry();
        await programStart.ProgramStartAsync();
    }

}