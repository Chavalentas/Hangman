using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace hangmantest
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                App app = new App();
                var t = app.Play();
                await t;
            }
            catch (Exception)
            {
            }
        }

        public static IWordGeneratorService GetWordGeneratorService()
        {
            var randomGenerator = new Random();
            string[] words = GetWords();

            try
            {
                InternetGeneratorService internetGenerator = new InternetGeneratorService();
                string tryWord = Task.Run(() => internetGenerator.Generate()).Result;
                return internetGenerator;
            }
            catch (Exception)
            {
                return new LocalGeneratorService(randomGenerator, words);
            }
        }

        public static string[] GetWords()
        {
            return new string[]
            {
                "APPLE",
                "BANANA",
                "ORANGE",
                "OBADIAH",
                "JOCHANAN",
                "MILLENIUM",
                "CUP",
                "MUG"
            };
        }
    }
}
