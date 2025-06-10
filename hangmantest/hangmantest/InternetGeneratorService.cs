using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace hangmantest
{
    public class InternetGeneratorService : IWordGeneratorService
    {
        private HttpClient _httpClient;

        private string _url;

        public InternetGeneratorService()
        {
            _httpClient = new HttpClient();
            _url = "https://random-word-api.herokuapp.com/word";
        }

        public async Task<string> Generate()
        {
            try
            {
                var t = FetchWords();
                string[] words = await t;
                return words[0].ToUpper();
            }
            catch(Exception)
            {
                throw;
            }
        }

        private async Task<string[]> FetchWords()
        {
            try
            {
                string[] result = await _httpClient.GetFromJsonAsync<string[]>(_url);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
