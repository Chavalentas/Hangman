using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hangmantest
{
    public class App
    {
        public async Task Play()
        {
            bool wannaPlayAgain = false;
            var random = new Random();
            int promptX = 0;
            int promptY = 0;
            string invalidOptionMessage = "No valid option!";

            do
            {
                string generatorMode = ReadValueInContainer(new string[] { "L", "I" },
                    "Choose generator mode (L = Local, I = Internet, L is backup if no connection): ",
                    invalidOptionMessage, promptX, promptY,
                    Console.ForegroundColor, Console.BackgroundColor, invalidOptionMessage.Length);
                Console.Clear();
                IWordGeneratorService wordGeneratorService = GetWordGeneratorService(generatorMode);
                await DoRound(random, 60, 0, 40, 20, promptX, promptY, wordGeneratorService);
                string userInput = ReadValueInContainer(new string[] { "Y", "N" },
                    "Wanna play again? (Y/N):", invalidOptionMessage, promptX, promptY + 3, Console.ForegroundColor,
                    Console.BackgroundColor, invalidOptionMessage.Length);

                if (userInput == "Y")
                {
                    wannaPlayAgain = true;
                }
                else if (userInput == "N")
                {
                    wannaPlayAgain = false;
                }

                Console.Clear();
            }
            while (wannaPlayAgain);
        }

        private IWordGeneratorService GetWordGeneratorService(string choice)
        {
            if (string.IsNullOrEmpty(choice))
            {
                throw new ArgumentNullException(nameof(choice), "Cannot be null or empty!"); 
            }

            if (!(choice == "L" || choice == "I"))
            {
                throw new ArgumentException("Wrong choice detected!");
            }

            var randomGenerator = new Random();
            string[] words = GetWords();

            try
            {
                switch (choice)
                {
                    case "L":
                        return new LocalGeneratorService(randomGenerator, words);
                    case "I":
                        return GetInternetGeneratorService();
                }

                throw new InvalidOperationException("Invalid option detected!");
            }
            catch (Exception)
            {
                return new LocalGeneratorService(randomGenerator, words);
            }
        }

        private InternetGeneratorService GetInternetGeneratorService()
        {
            try
            {
                InternetGeneratorService internetGenerator = new InternetGeneratorService();
                string tryWord = Task.Run(() => internetGenerator.Generate()).Result;
                return internetGenerator;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private string[] GetWords()
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
                "MUG",
                "CATERPILLAR",
                "HAT"
            };
        }

        private async Task DoRound(Random randomGenerator, int userGuessX, int userGuessY, int hangmanX,
            int hangmanY, int promptX, int promptY, IWordGeneratorService generatorService)
        {
            if (randomGenerator == null)
            {
                throw new ArgumentNullException(nameof(randomGenerator), "Cannot be null!");
            }

            if (generatorService == null)
            {
                throw new ArgumentNullException(nameof(generatorService), "Cannot be null!");
            }

            double deathRate = 0;
            bool roundFinished = false;
            int numberOfMistakes = 0;
            var guessTask = GetRandomWord(generatorService);
            string wordToGuess = await guessTask;
            int numberOfTries = wordToGuess.Length;
            string usersGuess = GetRepeatedChar('-', wordToGuess.Length);
            char[] alreadyUsed = new char[0];
            Print(userGuessX, userGuessY, usersGuess, Console.ForegroundColor, Console.BackgroundColor);

            while (!roundFinished)
            {
                char guessChar = ReadCharacterExcept(
                    "Already used!", promptX, promptY, Console.ForegroundColor,
                    Console.BackgroundColor, 'A', 'Z', alreadyUsed);

                alreadyUsed = Add(alreadyUsed, guessChar);

                if (!ContainsValue(wordToGuess, guessChar))
                {
                    numberOfMistakes++;
                    deathRate = GetDeathRate(numberOfTries, numberOfMistakes);
                    char[,] hangman = GetHangman(deathRate);
                    DrawHangman(hangmanX, hangmanY, hangman, Console.ForegroundColor, Console.BackgroundColor, deathRate);
                }
                else
                {
                    int[] indexes = GetIndexes(wordToGuess, guessChar);
                    usersGuess = string.Join("", UpdateAtIndexes(usersGuess, indexes, guessChar));
                    Print(userGuessX, userGuessY, usersGuess, Console.ForegroundColor, Console.BackgroundColor);
                }

                if (wordToGuess == usersGuess)
                {
                    Print(promptX, promptY + 2, $"Congratulations! You have won the game! (Death rate: {deathRate} %, Word: {wordToGuess})", Console.ForegroundColor, Console.BackgroundColor);
                    roundFinished = true;
                }

                if (numberOfTries == numberOfMistakes)
                {
                    Print(promptX, promptY + 2, $"You lost! (Death rate: {deathRate} %, Word: {wordToGuess})", Console.ForegroundColor, Console.BackgroundColor);
                    roundFinished = true;
                }
            }
        }

        private int[] GetIndexes(string input, char toSearch)
        {
            int[] indexes = new int[0];

            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentNullException(nameof(input), "Cannot be null!");
            }

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == toSearch)
                {
                    indexes = Add(indexes, i);
                }
            }

            return indexes;
        }

        private IEnumerable<T> UpdateAtIndexes<T>(IEnumerable<T> input, int[] indexes, T toUpdateWith)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input), "Cannot be null!");
            }

            if (indexes == null)
            {
                throw new ArgumentNullException(nameof(indexes), "Cannot be null!");
            }

            if (toUpdateWith == null)
            {
                throw new ArgumentNullException(nameof(toUpdateWith), "Cannot be null!");
            }

            T[] inputArray = input.ToArray();
            int inputCount = inputArray.Length;

            for (int i = 0; i < inputCount; i++)
            {
                if (ContainsValue(indexes, i))
                {
                    yield return toUpdateWith;
                }
                else
                {
                    yield return inputArray[i];
                }
            }
        }

        private async Task<string> GetRandomWord(IWordGeneratorService wordGeneratorService)
        {
            string words = await wordGeneratorService.Generate();
            return words;
        }

        private char ReadCharacterExcept(string errorMessage, int x, int y, ConsoleColor foreground, ConsoleColor background, char min, char max, char[] except)
        {
            if (string.IsNullOrEmpty(errorMessage))
            {
                throw new ArgumentNullException(nameof(errorMessage), "Cannot be null!");
            }

            if (!(x >= 0 && x <= Console.WindowWidth - 1))
            {
                throw new ArgumentOutOfRangeException(nameof(x), "The value was out of console range!");
            }

            if (!(y >= 0 && x <= Console.WindowHeight - 1))
            {
                throw new ArgumentOutOfRangeException(nameof(y), "The value was out of console range!");
            }

            if (min > max)
            {
                throw new ArgumentOutOfRangeException(nameof(min), "Min was larger than max!");
            }

            if (except == null)
            {
                throw new ArgumentNullException(nameof(except), "Cannot be null!");
            }

            string prompt = $"Please enter a value ({min}-{max}): ";
            string error = $"Please note the valid range ({min}-{max})!";
            string noCharError = "No character entered!";
            int lengthToErase = error.Length + noCharError.Length + errorMessage.Length;
            char readChar = ReadCharacterWithinRange(prompt, error, noCharError, x, y, foreground, background, min, max, lengthToErase);

            while (ContainsValue(except, readChar))
            {
                Print(x, y + 1, GetRepeatedChar(' ', lengthToErase), foreground, background);
                Print(x, y + 1, errorMessage, foreground, background);
                readChar = ReadCharacterWithinRange(prompt,
                                error, noCharError,
                                x, y, foreground, background, min, max, lengthToErase);
            }

            return readChar;
        }

        private void DrawHangman(int x, int y, char[,] hangman, ConsoleColor foreground, ConsoleColor background, double deathRate)
        {
            if (!(x >= 0 && x <= Console.WindowWidth - 1))
            {
                throw new ArgumentOutOfRangeException(nameof(x), "The value was out of console range!");
            }

            if (!(y >= 0 && y <= Console.WindowHeight - 1))
            {
                throw new ArgumentOutOfRangeException(nameof(y), "The value was out of console range!");
            }

            if (!(x + hangman.GetLength(0) >= 0 && x + hangman.GetLength(0) <= Console.WindowWidth - 1))
            {
                throw new ArgumentOutOfRangeException(nameof(x), "The hangman was out of console range!");
            }

            if (!(y + hangman.GetLength(1) >= 0 && y + hangman.GetLength(1) <= Console.WindowHeight - 1))
            {
                throw new ArgumentOutOfRangeException(nameof(x), "The hangman was out of console range!");
            }

            if (hangman == null)
            {
                throw new ArgumentNullException(nameof(hangman), "Cannot be null!");
            }

            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;

            for (int row = 0; row < hangman.GetLength(0); row++)
            {
                for (int col = 0; col < hangman.GetLength(1); col++)
                {
                    Console.SetCursorPosition(x + col, y + row);
                    Console.Write(hangman[row, col]);
                }
            }
        }

        public static bool ContainsCharacter(string value, char character)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value), "Cannot be null!");
            }

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == character)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ContainsValue(string value, string[] container)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value), "Cannot be null!");
            }

            if (container == null)
            {
                throw new ArgumentNullException(nameof(container), "Cannot be null!");
            }

            for (int i = 0; i < container.Length; i++)
            {
                if (container[i] == value)
                {
                    return true;
                }
            }

            return false;
        }

        private char[,] FillRow(char[,] input, int rowIndex, int start, int end, char replaceChr)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input), "Cannot be null!");
            }

            if (!(rowIndex >= 0 && rowIndex < input.GetLength(0)))
            {
                throw new ArgumentOutOfRangeException(nameof(rowIndex), "Cannot be out of range!");
            }

            if (!(start >= 0 && start < input.GetLength(1)))
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Cannot be out of range!");
            }

            if (!(end >= 0 && end < input.GetLength(1)))
            {
                throw new ArgumentOutOfRangeException(nameof(end), "Cannot be out of range!");
            }

            if (end < start)
            {
                throw new ArgumentOutOfRangeException(nameof(end), $"Cannot be smaller than {start}!");
            }

            char[,] result = input;

            for (int i = start; i <= end; i++)
            {
                result[rowIndex, i] = replaceChr;
            }

            return result;
        }

        private char[,] GetHangman(double deathRate)
        {
            char[,] hangman = new char[5, 6]
            {
                {'-', '-', '-', '-','-', ' '},
                {'|', ' ', ' ', ' ','|', ' '},
                {'|', ' ', ' ', ' ','o', ' '},
                {'|', ' ', ' ', '/','|', '\\'},
                {'|', ' ', ' ', '/',' ', '\\'}
            };

            if (deathRate > 0 && deathRate <= (100 * 1.0) / 9)
            {
                hangman = FillRow(hangman, 0, 0, hangman.GetLength(1) - 1, ' ');
                hangman = FillRow(hangman, 1, 1, hangman.GetLength(1) - 1, ' ');
                hangman = FillRow(hangman, 2, 1, hangman.GetLength(1) - 1, ' ');
                hangman = FillRow(hangman, 3, 1, hangman.GetLength(1) - 1, ' ');
                hangman = FillRow(hangman, 4, 1, hangman.GetLength(1) - 1, ' ');
            }

            if (deathRate > (100 * 1.0) / 9 && deathRate <= 2 * (100 * 1.0) / 9)
            {
                hangman = FillRow(hangman, 1, 1, hangman.GetLength(1) - 1, ' ');
                hangman = FillRow(hangman, 2, 1, hangman.GetLength(1) - 1, ' ');
                hangman = FillRow(hangman, 3, 1, hangman.GetLength(1) - 1, ' ');
                hangman = FillRow(hangman, 4, 1, hangman.GetLength(1) - 1, ' ');
            }

            if (deathRate > 2 * (100 * 1.0) / 9 && deathRate <= 3 * (100 * 1.0) / 9)
            {
                hangman = FillRow(hangman, 2, 1, hangman.GetLength(1) - 1, ' ');
                hangman = FillRow(hangman, 3, 1, hangman.GetLength(1) - 1, ' ');
                hangman = FillRow(hangman, 4, 1, hangman.GetLength(1) - 1, ' ');
            }

            if (deathRate > 3 * (100 * 1.0) / 9 && deathRate <= 4 * (100 * 1.0) / 9)
            {
                hangman = FillRow(hangman, 3, 1, hangman.GetLength(1) - 1, ' ');
                hangman = FillRow(hangman, 4, 1, hangman.GetLength(1) - 1, ' ');
            }

            if (deathRate > 4 * (100 * 1.0) / 9 && deathRate <= 5 * (100 * 1.0) / 9)
            {
                hangman = FillRow(hangman, 3, 1, 3, ' ');
                hangman = FillRow(hangman, 3, hangman.GetLength(1) - 1, hangman.GetLength(1) - 1, ' ');
                hangman = FillRow(hangman, 4, 1, hangman.GetLength(1) - 1, ' ');
            }

            if (deathRate > 5 * (100 * 1.0) / 9 && deathRate <= 6 * (100 * 1.0) / 9)
            {
                hangman = FillRow(hangman, 3, hangman.GetLength(1) - 1, hangman.GetLength(1) - 1, ' ');
                hangman = FillRow(hangman, 4, 1, hangman.GetLength(1) - 1, ' ');
            }

            if (deathRate > 6 * (100 * 1.0) / 9 && deathRate <= 7 * (100 * 1.0) / 9)
            {
                hangman = FillRow(hangman, 4, 1, hangman.GetLength(1) - 1, ' ');
            }

            if (deathRate > 7 * (100 * 1.0) / 9 && deathRate <= 8 * (100 * 1.0) / 9)
            {
                hangman = FillRow(hangman, 4, 1, hangman.GetLength(1) - 1, ' ');
            }

            if (deathRate > 8 * (100 * 1.0) / 9 && deathRate <= 9 * (100 * 1.0) / 9)
            {
                return hangman;
            }

            return hangman;
        }

        private void Print(int x, int y, string value, ConsoleColor foreground, ConsoleColor background)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value), "Cannot be null!");
            }

            if (!(x >= 0 && x <= Console.WindowWidth - 1))
            {
                throw new ArgumentOutOfRangeException(nameof(x), "The value was out of console range!");
            }

            if (!(y >= 0 && y <= Console.WindowHeight - 1))
            {
                throw new ArgumentOutOfRangeException(nameof(y), "The value was out of console range!");
            }

            if (x + value.Length >= Console.WindowWidth - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The value was out of console range!");
            }

            Console.SetCursorPosition(x, y);
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;
            Console.Write(value);
        }

        private bool IsWithinRange(char character, char min, char max)
        {
            return character >= min && character <= max;
        }

        private char ReadCharacter(string promptMessage, string errorMessage,
            int x, int y, ConsoleColor foreground, ConsoleColor background, int lengthToErase)
        {
            if (string.IsNullOrEmpty(promptMessage))
            {
                throw new ArgumentNullException(nameof(promptMessage), "Cannot be null!");
            }

            if (string.IsNullOrEmpty(errorMessage))
            {
                throw new ArgumentNullException(nameof(errorMessage), "Cannot be null!");
            }

            if (!(x >= 0 && x <= Console.WindowWidth - 1))
            {
                throw new ArgumentOutOfRangeException(nameof(x), "The value was out of console range!");
            }

            if (!(y >= 0 && x <= Console.WindowHeight - 1))
            {
                throw new ArgumentOutOfRangeException(nameof(y), "The value was out of console range!");
            }

            if (lengthToErase < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lengthToErase), "Cannot be less than 0!");
            }

            Print(x, y, promptMessage, foreground, background);
            string input = Console.ReadLine();

            while (input.Length != 1)
            {
                Print(x, y, GetRepeatedChar(' ', promptMessage.Length + input.Length), foreground, background);
                Print(x, y + 1, GetRepeatedChar(' ', lengthToErase), foreground, background);
                Print(x, y + 1, errorMessage, foreground, background);
                Print(x, y, promptMessage, foreground, background);
                input = Console.ReadLine();
            }

            Print(x, y, GetRepeatedChar(' ', promptMessage.Length + input.Length), foreground, background);
            Print(x, y + 1, GetRepeatedChar(' ', lengthToErase), foreground, background);
            return input[0];
        }

        private char ReadCharacterWithinRange(string promptMessage, string errorMessage,
            string noCharErrorMessage,
            int x, int y, ConsoleColor foreground, ConsoleColor background, char min, char max, int lengthToErase)
        {
            if (string.IsNullOrEmpty(promptMessage))
            {
                throw new ArgumentNullException(nameof(promptMessage), "Cannot be null!");
            }

            if (string.IsNullOrEmpty(errorMessage))
            {
                throw new ArgumentNullException(nameof(errorMessage), "Cannot be null!");
            }

            if (string.IsNullOrEmpty(noCharErrorMessage))
            {
                throw new ArgumentNullException(nameof(noCharErrorMessage), "Cannot be null!");
            }

            if (!(x >= 0 && x <= Console.WindowWidth - 1))
            {
                throw new ArgumentOutOfRangeException(nameof(x), "The value was out of console range!");
            }

            if (!(y >= 0 && x <= Console.WindowHeight - 1))
            {
                throw new ArgumentOutOfRangeException(nameof(y), "The value was out of console range!");
            }

            if (min > max)
            {
                throw new ArgumentOutOfRangeException(nameof(min), "Min was larger than max!");
            }

            if (lengthToErase < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lengthToErase), "Cannot be less than 0!");
            }

            char readChar = ReadCharacter(promptMessage, noCharErrorMessage, x, y, foreground, background, lengthToErase);

            while (!IsWithinRange(readChar, min, max))
            {
                Print(x, y + 1, GetRepeatedChar(' ', lengthToErase), foreground, background);
                Print(x, y + 1, errorMessage, foreground, background);
                readChar = ReadCharacter(promptMessage, noCharErrorMessage, x, y, foreground, background, lengthToErase);
            }

            Print(x, y, GetRepeatedChar(' ', promptMessage.Length + readChar.ToString().Length), foreground, background);
            Print(x, y + 1, GetRepeatedChar(' ', lengthToErase), foreground, background);
            return readChar;
        }

        private string ReadValueInContainer(string[] container, string promptMessage, string errorMessage,
    int x, int y, ConsoleColor foreground, ConsoleColor background, int lengthToErase)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container), "Cannot be null!");
            }

            if (string.IsNullOrEmpty(promptMessage))
            {
                throw new ArgumentNullException(nameof(promptMessage), "Cannot be null!");
            }

            if (string.IsNullOrEmpty(errorMessage))
            {
                throw new ArgumentNullException(nameof(errorMessage), "Cannot be null!");
            }

            if (!(x >= 0 && x <= Console.WindowWidth - 1))
            {
                throw new ArgumentOutOfRangeException(nameof(x), "The value was out of console range!");
            }

            if (!(y >= 0 && y <= Console.WindowHeight - 1))
            {
                throw new ArgumentOutOfRangeException(nameof(y), "The value was out of console range!");
            }

            if (lengthToErase < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lengthToErase), "Cannot be less than 0!");
            }

            Print(x, y, promptMessage, foreground, background);
            string input = Console.ReadLine();

            while (!ContainsValue(input, container))
            {
                Print(x, y, GetRepeatedChar(' ', promptMessage.Length + input.Length), foreground, background);
                Print(x, y + 1, GetRepeatedChar(' ', lengthToErase), foreground, background);
                Print(x, y + 1, errorMessage, foreground, background);
                Print(x, y, promptMessage, foreground, background);
                input = Console.ReadLine();
            }

            Print(x, y, GetRepeatedChar(' ', promptMessage.Length + input.Length), foreground, background);
            Print(x, y + 1, GetRepeatedChar(' ', lengthToErase), foreground, background);
            return input;
        }

        private string GetRepeatedChar(char character, int count)
        {
            string result = string.Empty;

            for (int i = 0; i < count; i++)
            {
                result += character;
            }

            return result;
        }

        private double GetDeathRate(int numberTries, int numberMistakes)
        {
            double deathRate;

            deathRate = (1.0 * numberMistakes) / (1.0 * numberTries) * 100;

            return deathRate;
        }

        private T[] Add<T>(T[] elements, T elementToAdd)
        {
            if (elements == null)
            {
                throw new ArgumentNullException(nameof(elements), "Cannot be null!");
            }

            if (elementToAdd == null)
            {
                throw new ArgumentNullException(nameof(elementToAdd), "Cannot be null!");
            }

            T[] result = new T[elements.Length + 1];

            for (int i = 0; i < elements.Length; i++)
            {
                result[i] = elements[i];
            }

            result[result.Length - 1] = elementToAdd;
            return result;
        }

        private bool ContainsValue<T>(IEnumerable<T> sequence, T value)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException(nameof(sequence), "Cannot be null!");
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Cannot be null!");
            }

            foreach (T item in sequence)
            {
                if (item.Equals(value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
