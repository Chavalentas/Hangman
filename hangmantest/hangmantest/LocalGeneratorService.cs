using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hangmantest
{
    public class LocalGeneratorService : IWordGeneratorService
    {
        private Random _randomGenerator;

        private string[] _words;

        public LocalGeneratorService(Random random, string[] words)
        {
            _randomGenerator = random ?? throw new ArgumentNullException(nameof(random), "Cannot be null!");
            SetWords(words);
        }

        public async Task<string> Generate()
        {
            int index = _randomGenerator.Next(0, _words.Length);
            var t = Task.Run<string>(() => { return _words[index]; });
            string word = await t;
            return word.ToUpper();
        }

        private void SetWords(string[] words)
        {
            if (words == null)
            {
                throw new ArgumentNullException(nameof(words), "Cannot be null!");
            }

            if (words.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(words), "Length cannot be 0!");
            }

            _words = words;
        }
    }
}
