using System;
using System.Diagnostics.Metrics;
using System.Text;

namespace Decodee;

public class DecodedMetar
{
    public static List<string> data = new List<string>();
    public string result = string.Empty;

    public void GetStringToList(string type, string successValue)
    {
        string concattedMetar = string.Join(" ", type, successValue);
        data.Add(concattedMetar);
    }

    public string GetData()
    {
        result = string.Join('\n', data);
        data.Clear();
        return result;
    }

}