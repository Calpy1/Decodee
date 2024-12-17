using System;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Concurrent;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Decodee;

public class MetarDecoder
{
    private bool _isDataExist = false;
    private string _pattern = string.Empty;
    private string _type = string.Empty;
    private string _date = string.Empty;
    private string _hours = string.Empty;
    private int _timeAgo;
    private List<int> _time = new List<int>();
    private List<string> _processedParts = new List<string>();


    private readonly Dictionary<string, string> _acronyms = new Dictionary<string, string>
        {
            {"01","1"},
            {"02","2"},
            {"03","3"},
            {"04","4"},
            {"05","5"},
            {"06","6"},
            {"07","7"},
            {"08","8"},
            {"09","9"},
            {"MPS"," Meter(s) per second"},
            {"KT"," Knot(s)"},
            {"M1/16SM","Less than 1/16 SM"},
            {"M1/4SM","Less than 1/4 SM"},
            {"M1/2SM","Less than 1/2 SM"},
            {"M1SM","Less than 1 SM"},
            {"M2SM","Less than 2 SM"},
            {"10SM","More than 10 SM"},
            {"NM","NM"},
            {"-","Light "},
            {"+","Heavy "},
            {"VCFG","Vicinity Fog"},
            {"FZFG","Freezing Fog"},
            {"MIFG","Shallow Fog"},
            {"PRFG","Precipitation Fog"},
            {"RASN","Rain and Snow"},
            {"RADZ","Rain and Drizzle"},
            {"SNRA","Snow and Rain"},
            {"SHSN","Snow Showers"},
            {"SHRA","Rain Showers"},
            {"FZRA","Freezing Rain"},
            {"FZSHRA","Freezing Snow Showers with Rain"},
            {"FZDZ","Freezing Drizzle"},
            {"TSSN","Thunderstorm with Snow"},
            {"TSRA","Thunderstorm with Rain"},
            {"TSGR","Thunderstorm with Hail"},
            {"TSGS","Thunderstorm with Graupel"},
            {"TSRAGR","Thunderstorm with Rain and Hail"},
            {"TSDS","Thunderstorm with Dust"},
            {"VCTS","Vicinity Thunderstorm"},
            {"VCSH","Vicinity Showers"},
            {"SQ","Squall"},
            {"PO","Dust/Sand Whirl"},
            {"FC","Tornado"},
            {"BR","Mist"},
            {"HZ","Haze"},
            {"FU","Smoke"},
            {"DS","Duststorm"},
            {"SS","Sandstorm"},
            {"DU","Dust"},
            {"FG","Fog"},
            {"RA","Rain"},
            {"SN","Snow"},
            {"DZ","Drizzle"},
            {"IC","Ice Crystals"},
            {"PE","Ice Pellets"},
            {"GR","Hail"},
            {"GS","Graupel Showers"},
            {"SG","Snow Grains"},
            {"TS","Thunderstorm"},
            {"CAVOK","Ceiling And Visibility OK "},
            {"FEW","Few Clouds "},
            {"BKN","Broken Clouds "},
            {"OVC","Overcast "},
            {"SCT","Scattered Clouds "},
            {"CLR","Clear Sky "},
            {"SKC","Clear Sky "},

        };

    public Dictionary<string, string> acronyms
    {
        get { return _acronyms; }
    }

    public DecodedMetar Decode(string metarRaw)
    {
        DecodedMetar result = new DecodedMetar();
        CultureInfo.CurrentCulture = new CultureInfo("en-US");

        string[] metarParts = metarRaw.Split('\n');

        foreach (string metarPart in metarParts)
        {
            IsReportType(metarPart);
            IsIcao(metarPart);
            IsTime(metarPart);
            IsWind(metarPart);
            IsVisibility(metarPart);
            IsCeiling(metarPart);
            IsTemp(metarPart);
            IsPress(metarPart);
        }

        return result;
    }

    public void IsReportType(string metarPart)
    {
        _pattern = @"(?<repType>\bMETAR|SPECI)\b";
        Match match = Regex.Match(metarPart, _pattern);
        _type = "Report Type:";

        if (match.Success)
        {
            bool isDecodingCancelled = true;
            SuccessCheck(match.Value, isDecodingCancelled);
        }
    }

    public void IsIcao(string metarPart)
    {
        _pattern = @"([A-Z]{4}\s)(?=\d{6}Z)";
        Match match = Regex.Match(metarPart, _pattern);
        _type = "ICAO:";

        if (match.Success)
        {
            bool isDecodingCancelled = true;
            SuccessCheck(match.Value, isDecodingCancelled);
        }
    }

    public void IsTime(string metarPart)
    {
        _pattern = @"(?<date>\d{2})(?=\d{4}Z)(?<timeZulu>(?<hours>\d{2})(?<minutes>\d{2}Z))";
        Match match = Regex.Match(metarPart, _pattern);

        if (match.Success)
        {
            bool isDecodingCancelled = true;
            if (match.Groups["date"].Success && !string.IsNullOrEmpty(match.Groups["date"].Value))
            {
                _type = "Date:";
                _date = $"{match.Groups["date"].Value} {DateTime.Now.ToString("MMMM yyyy")}";
                SuccessCheck(_date, isDecodingCancelled);
            }

            if (match.Groups["timeZulu"].Success && !string.IsNullOrEmpty(match.Groups["timeZulu"].Value))
            {
                TimeCalculation(DateTime.UtcNow.ToString("HHmm"));
                TimeCalculation(match.Groups["timeZulu"].Value);

                TimeFinalization();

                _type = "Time:";
                _hours = $"{match.Groups["hours"].Value}:{match.Groups["minutes"].Value.TrimEnd('Z')} UTC | Last updated: {_timeAgo} minutes ago";

                SuccessCheck(_hours, isDecodingCancelled);
            }
        }
    }

    public void TimeCalculation(string val)
    {
        int hours = int.Parse(val.Substring(0, 2));
        int minutes = int.Parse(val.Substring(2, 2));
        int time = hours * 60 + minutes;
        _time.Add(time);
    }

    public void TimeFinalization()
    {
        if (_time.Count > 1)
        {
            if (_time[0] < _time[1])
            {
                _time[0] += 1440;
            }

            _timeAgo = _time[0] - _time[1];
        }
    }

    public void IsWind(string metarPart)
    {
        _pattern = @"(?<wind>(?<windDir>\d{3}|VRB)((?<windSpeed>(?<windSpeedFirst>\d{2})(G(?<windSpeedLast>\d{2}))?))(?<windUnit>KT|MPS))(?:\s+(?<windVarFrom>\d{3})V(?<windVarTo>\d{3}))?";
        Match match = Regex.Match(metarPart, _pattern);
        _type = "Wind:";
        if (match.Success)
        {
            bool isDecodingCancelled = true;

            string wind = string.Empty;
            string windDir = match.Groups["windDir"].Value;
            string windSpeed = match.Groups["windSpeed"].Value;
            string windUnit = string.Empty;
            string windVarFrom = match.Groups["windVarFrom"].Value;
            string windVarTo = match.Groups["windVarTo"].Value;
            string windSpeedFirst = match.Groups["windSpeedFirst"].Value;
            string windSpeedLast = match.Groups["windSpeedLast"].Value;

            if (windSpeed.StartsWith("0"))
            {
                windSpeed = match.Groups["windSpeed"].Value.TrimStart('0');
            }

            if (match.Groups["windUnit"].Value.Contains("KT"))
            {
                windUnit = "knot(s)";
            }
            else
            {
                windUnit = "meter(s) per second";
            }

            if (windDir.Contains("VRB") && string.IsNullOrEmpty(windVarFrom))
            {
                wind = $"Variable, {windSpeed} {windUnit}";
            }
            else
            {
                if (windSpeed.Contains("G"))
                {
                    windSpeedLast = $", gusting to {windSpeedLast}";
                }

                if (!string.IsNullOrEmpty(windVarFrom))
                {
                    if (windSpeed.Length > 1)
                    {
                        wind = $"{windDir}° at {windSpeedFirst}{windSpeedLast} {windUnit}, varying between {windVarFrom}° and {windVarTo}°";
                    }
                    else
                    {
                        wind = $"{windDir}° at {windSpeedFirst}{windSpeedLast} {windUnit}, varying between {windVarFrom}° and {windVarTo}°";
                    }
                }
                else
                {
                    if (windSpeed.Length > 1)
                    {
                        wind = $"{windDir}° at {windSpeedFirst}{windSpeedLast} {windUnit}";
                    }
                    else
                    {
                        wind = $"{windDir}° at {windSpeedFirst}{windSpeedLast} {windUnit}";
                    }
                }

            }

            SuccessCheck(wind, isDecodingCancelled);
        }
    }

    public void IsVisibility(string metarPart)
    {
        _pattern = @"(?<visibility>\b(?:\d{4}|\d+(?:\.\d+)?SM|\d{1,2}\/\d{1,2}SM|M\d{1,2}\/\d{1,2}SM|\d{4}V\d{4}|M\d+SM)\b)";
        Match match = Regex.Match(metarPart, _pattern);
        _type = "Visibility:";
        if (match.Success)
        {
            bool isDecodingCancelled = false;
            string units = string.Empty;
            string vis = string.Empty;
            if (double.TryParse(match.Value, out double number))
            {
                if (match.Value.Contains("9999"))
                {
                    vis = "Over 10 Kilometers";
                }
                else
                {
                    units = "Kilometer(s)";
                    number = Math.Round(number / 1000);
                    vis = $"{number.ToString()} {units}";
                }
            }
            else
            {
                units = "Statute Mile(s)";
                string pattern = @"-?\d+(\.\d+)?";
                Match matchMiles = Regex.Match(match.Value, pattern);
                if (match.Value.StartsWith('M'))
                {
                    string startsWith = "Less than";
                    vis = $"{startsWith} {matchMiles.Value} {units}";
                }
                else
                {
                    if (match.Value.Contains("10SM"))
                    {
                        vis = $"More than 10 {units}";
                    }
                    else
                    {
                        vis = $"{matchMiles.Value} {units}";
                    }
                }
            }
            SuccessCheck(vis, isDecodingCancelled);
        }

    }

    public void IsCeiling(string metarPart)
    {
        _pattern = @"(FEW|SCT|BKN|OVC)(\d{3})";
        MatchCollection matches = Regex.Matches(metarPart, _pattern);
        _type = "Ceilings:";
        if (matches.Count > 0)
        {
            foreach (Match match in matches)
            {
                string cloudType = match.Groups[1].Value;
                string height = match.Groups[2].Value;
                if (height.StartsWith('0'))
                {
                    height = height.TrimStart('0');
                }
                _processedParts.Add($"{cloudType}at {height}00 ft.");
            }
            ProcessedParts();
        }
    }

    public void IsTemp(string metarPart)
    {
        _pattern = @"(?<temp>M?\d{2})\/(?<dew>M?\d{2})";
        Match match = Regex.Match(metarPart, _pattern);
        _type = "Temperature:";
        if (match.Success)
        {
            bool isDecodingCancelled = true;
            string temp = match.Groups["temp"].Value;
            string dew = match.Groups["dew"].Value;
            if (match.Value.Contains('M'))
            {
                temp = Trim(temp);
                dew = Trim(dew);
            }
            else if (temp.StartsWith('0') || dew.StartsWith('0'))
            {
                if (temp.StartsWith('0'))
                {
                    temp = Trim(temp);
                }
                if (dew.StartsWith('0'))
                {
                    dew = Trim(dew); ;
                }
            }
            string res = $"{temp}°C, Dew point: {dew}°C";
            SuccessCheck(res, isDecodingCancelled);
        }
    }

    public void IsPress(string metarPart)
    {
        _pattern = @"(A|Q)\d{4}";
        Match match = Regex.Match(metarPart, _pattern);
        _type = "Pressure:";
        if (match.Success)
        {
            bool isDecodingCancelled = true;
            string pressType = string.Empty;
            if (match.Value.Contains('Q'))
            {
                pressType = "hPa";
            }
            else
            {
                pressType = "inHg";
            }
            string res = $"{match.Value.Substring(1)} {pressType}";
            SuccessCheck(res, isDecodingCancelled);
        }
    }

    public string Trim(string val)
    {
        if (val.StartsWith('M'))
        {
            val = val.TrimStart('M');

            if (val.StartsWith('0') && val.Substring(1) != "0")
            {
                val = $"-{val.TrimStart('0')}";
            }
            else if (val.StartsWith("0") && val.Substring(1) == "0")
            {
                val = $"0";
            }
            else
            {
                val = $"-{val}";
            }
        }
        else if (val.StartsWith('0') && val.Substring(1) != "0")
        {
            val = val.TrimStart('0');
        }
        else if (val.StartsWith("0") && val.Substring(1) == "0")
        {
            val = $"0";
        }
        else
        {
            return val;
        }
        return val;
    }

    public void ProcessedParts()
    {
        if (_processedParts.Count > 0)
        {
            bool isDecodingCancelled = false;
            SuccessCheck(string.Join(", ", _processedParts), isDecodingCancelled);
            _processedParts.Clear();
        }
        else
        {
            _isDataExist = false;
        }
    }

    public void SuccessCheck(string successValue, bool isDecodingCancelled)
    {
        if (!isDecodingCancelled)
        {
            successValue = AcronymsDecoder(successValue);
        }
        _isDataExist = true;
        Print(_type, successValue);
    }

    public void Print(string type, string successValue)
    {
        DecodedMetar decodedMetar = new DecodedMetar();
        if (_isDataExist)
        {
            decodedMetar.GetStringToList(type, successValue);
        }
    }

    public string AcronymsDecoder(string val)
    {
        MetarDecoder decoder = new MetarDecoder();
        Dictionary<string, string> acronyms = decoder.acronyms;
        foreach (KeyValuePair<string, string> pair in acronyms)
        {
            val = val.Replace(pair.Key, pair.Value);
        }
        return val;
    }

}