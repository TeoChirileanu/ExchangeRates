#r "nuget: Newtonsoft.Json, 12.0.1"

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public enum Multiplicator { ByOne = 1, ByHundred = 100 }

public class Currency {
    public string CurrencyName {get; set;}
    public Multiplicator Multiplicator {get; set;}
    public decimal ExchangeRate {get; set;}

    public override string ToString() => $"{CurrencyName}\t{Multiplicator:D}\t{ExchangeRate:0.####}";
}

public string GetFilePath() {
    string DefaultFilePath = Path.Join(Directory.GetCurrentDirectory(), "currencies.txt");
    var filePath = string.Empty;
    try {
        filePath = Args[0];
    } catch (Exception) {
        Console.WriteLine($"No path provided, using default path {DefaultFilePath}");
        filePath = DefaultFilePath;
    }
    return filePath;
}

public async Task<JObject> GetCurrencyInfo() {
    const string ExchangeUri = "https://api.openapi.ro/api/exchange/";
    const string ApiKey = "nzjYgxzZcvkBdkuPAFsF71LiTHqvPH9NqmmUYkdjdutwVGv8Rg";
    HttpClient client = new HttpClient();
    client.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", ApiKey);
    var jsonResponse = await client.GetStringAsync(ExchangeUri);
    var currencyInfo = JObject.Parse(jsonResponse);
    return currencyInfo;
}

public async Task<string> GetProperlyFormatedCurrencies() {
    var currencyInfo = await GetCurrencyInfo();
    IDictionary<string, JToken> exchangeRates = currencyInfo["rates"] as JObject;
    var currencies = exchangeRates.Select(x => new Currency {
        CurrencyName = x.Key,
        Multiplicator = x.Key.IsSpecialCurrency()
            ? Multiplicator.ByHundred
            : Multiplicator.ByOne,
        ExchangeRate = x.Key.IsSpecialCurrency()
            ? 100 * (decimal) x.Value
            : (decimal) x.Value
    });
    return string.Join("\n", currencies);
}

static readonly string[] specialCurrencies = new [] { "HUF", "KRW", "JPY" };
public static bool IsSpecialCurrency(this string currency) => 
    specialCurrencies.Contains(currency) ? true : false;

public static async Task WriteToFile(this string content, string pathToFile) {
    try {
        using (var writer = new StreamWriter(pathToFile)) 
            await writer.WriteAsync(content);
    } catch (Exception e) {
        Console.WriteLine($"Could not write to file:\n{e}");
        throw;
    }
}

try {
    var filePath = GetFilePath();
    var currencies = await GetProperlyFormatedCurrencies();
    await currencies.WriteToFile(filePath);
} catch (Exception e) {
    Console.WriteLine($"Oops, something bad happened:\n{e}");
}