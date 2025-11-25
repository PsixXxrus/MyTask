using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public enum ClientBranch
{
    BankCards,
    BusinessServices,
    SecuritiesMarket,
    ProductsInfo,
    Operator,
    Unknown
}

public class WeightedKeyword
{
    public string Canonical { get; set; }
    public int Weight { get; set; }
    public List<string> Synonyms { get; set; }
}

public static class ClientRouter
{
    private static readonly Dictionary<ClientBranch, List<WeightedKeyword>> BranchKeywords =
        new()
        {
            {
                ClientBranch.BankCards,
                new List<WeightedKeyword>
                {
                    new() { Canonical = "карта", Weight = 5, Synonyms = new(){ "дебетовая", "кредитка", "кредитная", "дебетка", "карточка", "visa", "mastercard", "мир" } },
                    new() { Canonical = "платёж", Weight = 3, Synonyms = new(){ "оплата", "транзакция", "списание" } },
                    new() { Canonical = "лимит", Weight = 3, Synonyms = new(){ "ограничение", "лимиты" } }
                }
            },
            {
                ClientBranch.BusinessServices,
                new List<WeightedKeyword>
                {
                    new() { Canonical = "юридический", Weight = 5, Synonyms = new(){ "юрлицо", "юр лица", "компания", "организация", "ип", "ооо" } },
                    new() { Canonical = "рсчёт", Weight = 5, Synonyms = new(){ "расчетный", "рс", "расчетный счет", "расчётный счёт" } },
                    new() { Canonical = "эквайринг", Weight = 4, Synonyms = new(){ "терминал", "торговый эквайринг" } },
                    new() { Canonical = "зппроект", Weight = 3, Synonyms = new(){ "зарплатный проект" } }
                }
            },
            {
                ClientBranch.SecuritiesMarket,
                new List<WeightedKeyword>
                {
                    new() { Canonical = "акция", Weight = 5, Synonyms = new(){ "облигация", "ценная бумага", "ценные бумаги", "фондовый", "биржа" } },
                    new() { Canonical = "инвест", Weight = 4, Synonyms = new(){ "инвестиция", "инвестиции", "инвестирование", "портфель" } },
                    new() { Canonical = "брокер", Weight = 4, Synonyms = new(){ "брокерский", "брокерский счёт" } }
                }
            },
            {
                ClientBranch.ProductsInfo,
                new List<WeightedKeyword>
                {
                    new() { Canonical = "продукт", Weight = 3, Synonyms = new(){ "услуга", "программа", "предложение" } },
                    new() { Canonical = "кредит", Weight = 4, Synonyms = new(){ "ипотека", "займ", "потреб", "потребительский", "ставка" } },
                    new() { Canonical = "вклад", Weight = 4, Synonyms = new(){ "депозит", "накопительный", "процент", "сберегательный" } }
                }
            },
            {
                ClientBranch.Operator,
                new List<WeightedKeyword>
                {
                    new() { Canonical = "оператор", Weight = 10, Synonyms = new(){ "живой человек", "сотрудник", "переведи", "переведите", "хочу оператора", "менеджер" } },
                    new() { Canonical = "человек", Weight = 8, Synonyms = new(){ "специалист", "консультант" } }
                }
            }
        };

    private static readonly Regex WordRegex = new(@"[а-яА-ЯёЁa-zA-Z]+", RegexOptions.Compiled);

    public static ClientBranch DetectBranch(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return ClientBranch.Unknown;

        string text = message.ToLower();
        var words = WordRegex.Matches(text)
                             .Select(m => NormalizeWord(m.Value))
                             .Where(w => !string.IsNullOrWhiteSpace(w))
                             .ToList();

        var scores = Enum.GetValues(typeof(ClientBranch))
                        .Cast<ClientBranch>()
                        .ToDictionary(branch => branch, _ => 0);

        foreach (var branch in BranchKeywords)
        {
            foreach (var keyword in branch.Value)
            {
                foreach (var word in words)
                {
                    if (IsMatch(word, keyword))
                        scores[branch.Key] += keyword.Weight;
                }

                // поддержка многословных фраз ("хочу оператора")
                foreach (var syn in keyword.Synonyms)
                {
                    if (text.Contains(syn))
                        scores[branch.Key] += keyword.Weight;
                }
            }
        }

        var maxScore = scores.Max(s => s.Value);

        if (maxScore == 0)
            return ClientBranch.Unknown;

        return scores.First(s => s.Value == maxScore).Key;
    }

    private static string NormalizeWord(string word)
    {
        word = word.ToLower();

        string[] endings = { "у", "ю", "е", "а", "ы", "и", "ой", "ей", "ом", "ем", "ях", "ам", "ям", "ах", "ов", "ев" };

        foreach (var end in endings)
        {
            if (word.EndsWith(end) && word.Length > end.Length + 2)
                return word[..^end.Length];
        }

        return word;
    }

    private static bool IsMatch(string word, WeightedKeyword keyword)
    {
        var forms = new List<string>
        {
            NormalizeWord(keyword.Canonical)
        };

        forms.AddRange(keyword.Synonyms.Select(NormalizeWord));

        return forms.Any(f => f.Length > 2 && word.Contains(f));
    }
}