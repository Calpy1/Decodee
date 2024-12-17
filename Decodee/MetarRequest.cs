using System;
using System.Net;
using Decodee.Properties;
using HtmlAgilityPack;

namespace Decodee;

public class MetarRequest
{
    private readonly string _url;

    public string Metar { get; private set; }

    public MetarRequest(string metar)
    {
        _url = $"https://metar-taf.com/{metar}";
    }

    public async Task<string> LoadMetarAsync()
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", Resources.UserAgent);
                HttpResponseMessage response = await client.GetAsync(_url);

                if (response.StatusCode == HttpStatusCode.OK)
                {   
                    string htmlContent = await response.Content.ReadAsStringAsync();
                    HtmlDocument document = new HtmlDocument();
                    document.LoadHtml(htmlContent);
                    string metar = document.DocumentNode.SelectSingleNode("/html/body/div[2]/div/div/div[4]/div[2]/div[2]/code").InnerText;
                    return metar;
                }
                else
                {
                    //string error = response.StatusCode.ToString();
                    return string.Empty;
                }
            }
        }
        catch (Exception ex)
        {
            return ex.Message.ToString();
        }
    }
}