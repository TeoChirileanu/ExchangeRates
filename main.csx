#r "nuget: Newtonsoft.Json, 12.0.1"
#r "nuget: WindowsAzure.Storage, 9.3.3"

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob; 

public enum Multiplicator { ByOne = 1, ByHundred = 100 }

public class Currency {
    public string CurrencyName {get; set;}
    public Multiplicator Multiplicator {get; set;}
    public decimal ExchangeRate {get; set;}

    public override string ToString() => $"{CurrencyName}\t\t{Multiplicator:D}\t\t{ExchangeRate:0.####}";
}

public class CurrencyInfo {
    public IEnumerable<Currency> Currencies {get; set;}
    public DateTime ExchangeRateDate {get; set;}

    public override string ToString() {
        var header = "Currency\tMultiplicator\tExchange Rate";
        var currenciesAsString = string.Join("\n", Currencies);
        return $"{header}\n{currenciesAsString}\nExchange Rates for {ExchangeRateDate.ToString("dd-MM-yyyy")}";
    }
}

public async Task<JObject> GetExchangeRateInformation() {
    const string ExchangeUri = "https://api.openapi.ro/api/exchange/";
    const string ApiKey = "nzjYgxzZcvkBdkuPAFsF71LiTHqvPH9NqmmUYkdjdutwVGv8Rg";
    HttpClient client = new HttpClient();
    client.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", ApiKey);
    var jsonResponse = await client.GetStringAsync(ExchangeUri);
    return JObject.Parse(jsonResponse);
}

public async Task<string> GetCurrencyInfo() {
    var exchangeRateInformation = await GetExchangeRateInformation();
    IDictionary<string, JToken> exchangeRates = exchangeRateInformation["rates"] as JObject;
    var currencies = exchangeRates.Select(x => new Currency {
        CurrencyName = x.Key,
        Multiplicator = x.Key.IsSpecialCurrency()
            ? Multiplicator.ByHundred
            : Multiplicator.ByOne,
        ExchangeRate = x.Key.IsSpecialCurrency()
            ? 100 * (decimal) x.Value
            : (decimal) x.Value
    });
    var exchangeRateDate = DateTime.Parse(exchangeRateInformation["date"].ToString());
    var currencyInfo = new CurrencyInfo {
        Currencies = currencies,
        ExchangeRateDate = exchangeRateDate
    };
    return currencyInfo.ToString();
}

static readonly string[] specialCurrencies = new [] { "HUF", "KRW", "JPY" };
public static bool IsSpecialCurrency(this string currency) => 
    specialCurrencies.Contains(currency) ? true : false;

public static async Task UploadToAzure(string content) {
    var storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=getexchangerates;AccountKey=wkZK4HH0JWrYGErvDfywg9x309Eo8JL4aK/FJZNecfFuW9D5TPMo3TkhFbcV09/aPuwQr5lNzKBxjDY7eN2bCA==;EndpointSuffix=core.windows.net");
    var client = storageAccount.CreateCloudBlobClient();
    var container = client.GetContainerReference("exchangerates");
    var blob = container.GetBlockBlobReference("currencies.txt");
    await blob.UploadTextAsync(content);
}


try {
    var currencyInfo = await GetCurrencyInfo();
    Console.WriteLine(currencyInfo);
    // await UploadToAzure(currencyInfo);
} catch (Exception e) {
    Console.WriteLine($"Oops, something bad happened:\n{e}");
}