using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public enum ClientBranch
{
    Purchase,
    Complaint,
    Question,
    Unknown
}

public class WeightedKeyword
{
    public string Canonical { get; set; }          // Основная форма слова
    public int Weight { get; set; }               // Вес ключа
    public List<string> Synonyms { get; set; }    // Синонимы
}

public static class ClientRouter
{
    private static readonly Dictionary<ClientBranch, List<WeightedKeyword>> BranchKeywords =
        new()
        {
            {
                ClientBranch.Purchase, new List<WeightedKeyword>
                {
                    new() { Canonical = "купить", Weight = 5, Synonyms = new() { "покупка", "куплю", "приобрести", "заказать", "оплатить" } },
                    new() { Canonical = "цена", Weight = 3, Synonyms = new() { "стоимость", "скидка" } }
                }
            },
            {
                ClientBranch.Complaint, new List<WeightedKeyword>
                {
                    new() { Canonical = "проблема", Weight = 5, Synonyms = new() { "ошибка", "не работает", "сломалось", "не могу", "не получается" } },
                    new() { Canonical = "жалоба", Weight = 4, Synonyms = new() { "недовольство", "претензия" } }
                }
            },
            {
                ClientBranch.Question, new List<WeightedKeyword>
                {
                    new() { Canonical = "как", Weight = 3, Synonyms = new() { "каким образом" } },
                    new() { Canonical = "что", Weight = 2, Synonyms = new() { "какой", "какая", "какие" } },
                    new() { Canonical = "почему", Weight = 3, Synonyms = new() { "зачем", "по какой причине" } },
                    new() { Canonical = "подскажите", Weight = 4, Synonyms = new() { "интересует", "объясните", "хочу узнать" } }
                }
            }
        };

    private static readonly Regex WordRegex = new(@"[а-яА-ЯёЁa-zA-Z]+", RegexOptions.Compiled);

    public static ClientBranch DetectBranch(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return ClientBranch.Unknown;

        string text = message.ToLower();

        // Разбиваем на слова
        var words = WordRegex.Matches(text)
                             .Select(m => NormalizeWord(m.Value))
                             .Where(w => !string.IsNullOrWhiteSpace(w))
                             .ToList();

        var scores = new Dictionary<ClientBranch, int>
        {
            { ClientBranch.Purchase, 0 },
            { ClientBranch.Complaint, 0 },
            { ClientBranch.Question, 0 }
        };

        // Считаем веса
        foreach (var branch in BranchKeywords)
        {
            foreach (var keyword in branch.Value)
            {
                foreach (var w in words)
                {
                    if (IsMatch(w, keyword))
                        scores[branch.Key] += keyword.Weight;
                }
            }
        }

        // Определяем ветку с максимальным весом
        var maxScore = scores.Max(s => s.Value);

        if (maxScore == 0)
            return ClientBranch.Unknown;

        return scores.First(s => s.Value == maxScore).Key;
    }

    // Простая нормализация слова (морфология)
    private static string NormalizeWord(string word)
    {
        word = word.ToLower();

        // Убираем окончания (упрощённая лемматизация)
        string[] endings = { "у", "ю", "е", "а", "ы", "и", "ой", "ей", "ом", "ем", "ях", "ам", "ям", "ах" };

        foreach (var end in endings)
        {
            if (word.EndsWith(end) && word.Length > end.Length + 2)
                return word.Substring(0, word.Length - end.Length);
        }

        return word;
    }

    // Проверяем совпадение с ключом или синонимами
    private static bool IsMatch(string word, WeightedKeyword keyword)
    {
        // Приводим все слова к канонической форме
        var allForms = new List<string>
        {
            NormalizeWord(keyword.Canonical)
        };
        allForms.AddRange(keyword.Synonyms.Select(NormalizeWord));

        // Совпадение по вхождению
        return allForms.Any(form => word.Contains(form));
    }
}