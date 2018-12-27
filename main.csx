#r "nuget: Newtonsoft.Json, 12.0.1"
#r "nuget: WindowsAzure.Storage, 9.3.3"

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File; 

public enum Multiplicator { ByOne = 1, ByHundred = 100 }

public class Currency {
    public string CurrencyName {get; set;}
    public Multiplicator Multiplicator {get; set;}
    public decimal ExchangeRate {get; set;}

    public override string ToString() => $"{CurrencyName}\t{Multiplicator:D}\t{ExchangeRate:0.####}";
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

public static async Task UploadToAzure(string content) {
    CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=getexchangerates;AccountKey=wkZK4HH0JWrYGErvDfywg9x309Eo8JL4aK/FJZNecfFuW9D5TPMo3TkhFbcV09/aPuwQr5lNzKBxjDY7eN2bCA==;EndpointSuffix=core.windows.net");
    var cloudFile = new CloudFile(new Uri("https://getexchangerates.file.core.windows.net/rates/CurrencyInfo.txt"), storageAccount.Credentials);
    await cloudFile.UploadTextAsync(content);
}

try {
    var currencyInfo = await GetProperlyFormatedCurrencies();
    await UploadToAzure(currencyInfo);
} catch (Exception e) {
    Console.WriteLine($"Oops, something bad happened:\n{e}");
}