using System;
using System.Text.RegularExpressions;


namespace Decodee
{
    public class IcaoReader 
    {   
        private string ICAO;

        public bool SetICAO(string ICAO)
        {
            string cyrillicRegex = @"[А-Яа-яЁё0-9\s\-]+";
            
            if (Regex.IsMatch(ICAO, cyrillicRegex) || ICAO.Length != 4)
            {
                return false;
            }
            
            this.ICAO = ICAO;
            return true;
        }

        public string GetMetar()
        {
            MetarRequest metarRequest = new MetarRequest(this.ICAO);
            return metarRequest.Metar;
        }
    }
}

