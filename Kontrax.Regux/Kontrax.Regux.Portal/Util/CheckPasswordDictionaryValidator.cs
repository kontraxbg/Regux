using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using System.Text.RegularExpressions;

namespace Kontrax.Regux.Portal.Util
{
    /// <summary>
    /// Проверява паролата за минимална дължина и срещу речник
    /// </summary>
    public class CheckPasswordDictionaryValidator : IIdentityValidator<string>
    {
        private int _minimumLength { get; set; }
        private readonly string _passwordGoodEntropyRegex = @"^(?!.*(.)\1{2})(.*?){3,29}$";

        public CheckPasswordDictionaryValidator(int minimumLength)
        {
            _minimumLength = minimumLength;
        }

        public Task<IdentityResult> ValidateAsync(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < _minimumLength)
            {
                return Task.FromResult(IdentityResult.Failed($"Дължината на паролата трябва да бъде поне {_minimumLength} знака."));
            }
            if (Regex.Matches(password, _passwordGoodEntropyRegex).Count == 0)
            {
                return Task.FromResult(IdentityResult.Failed($"Паролата не може да съдържа един и същ знак повече от 3 пъти последователно. Моля изберете друга парола."));
            }
            var dictionary = LoadDictionary();
            if (dictionary.Contains(password))
            {
                return Task.FromResult(IdentityResult.Failed("Паролата присъства в списъка с най-често използвани пароли"));
            }
            return Task.FromResult(IdentityResult.Success);
        }

        private HashSet<string> LoadDictionary()
        {
            var serverPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            var filename = serverPath + "/Data/dictionary.txt";
            return new HashSet<string>(File.ReadLines(filename));
            // Четенето на редовете от файла би могло да се прави по async начин, но реално става по-бавно:
            // https://stackoverflow.com/questions/13167934/how-to-async-files-readalllines-and-await-for-results
        }
    }
}