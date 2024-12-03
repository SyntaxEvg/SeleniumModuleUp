using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

class Program
{
    private static string? loginUrl;
    private static string? uploadUrlPage;
    private static string? username;
    private static string? password;
    private static string? folderPath;

    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
           .Build();

        string GetFileAssemb = Assembly.GetEntryAssembly().Location;
        string AppFolder = GetFileAssemb.Substring(0, GetFileAssemb.LastIndexOf('\\') + 1);
        var Loggerpath = Path.Combine(AppFolder, "logs", "logfile.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.File(Loggerpath, rollingInterval: RollingInterval.Day)
            .CreateLogger();
        loginUrl = configuration["loginUrl"];
        uploadUrlPage = configuration["uploadUrlPage"];
        username = configuration["username"];
        password = configuration["password"];
        folderPath = configuration["folderPath"];

        try
        {
            await ChromeDriverModule();
        }
        catch (Exception ex)
        {
            Log.Error($"err: {ex.Message}");
            Console.WriteLine(ex.Message);
        }
        

        

        Log.Information("Все файлы отправлены.");
        Console.WriteLine("Все файлы отправлены.");
    }

    private static async Task ChromeDriverModule()
    {
        using (var driver = new ChromeDriver())
        {
            // driver.Navigate().GoToUrl(StartUrl);
            driver.Navigate().GoToUrl(loginUrl);


            driver.FindElement(By.Id("UserName")).SendKeys(username);
            //IWebElement element = driver.FindElement(By.Id("show_hide_password"));
            //if (element.Enabled && element.Displayed)
            //{
            //    element.Click();
            //}
            driver.FindElement(By.Id("Password")).SendKeys(password);
            driver.FindElement(By.Id("btnSubmit")).Click();
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.Url != loginUrl);

                var files = Directory.GetFiles(folderPath).ToList();
               
                List<string> ErrModule = await Method(driver,files);
                List<string> ErrModule2 =await  Method(driver,ErrModule);
                Log.Error($"ErrModule2: {string.Join(",", ErrModule2)}");
                Console.WriteLine();
            
        }
    }

    private async static Task<List<string>> Method(ChromeDriver driver, List<string> files)
    {
        List<string> ErrModule = new List<string>();
        foreach (var filePath in files)
        {
            driver.Navigate().GoToUrl(uploadUrlPage);
            try
            {
                var antiForgeryToken = driver.FindElement(By.Name("__RequestVerificationToken")).GetAttribute("value");
            }
            catch (Exception ex)
            {
                Log.Error($"err1: {ex.Message}");
                Console.WriteLine(ex.Message);
            }
            try
            {
                // Найти элемент загрузки файла
                var uploadButton = driver.FindElement(By.CssSelector(".btn.btn-labeled.btn-outline-success"));
                uploadButton.Click();
                await Task.Delay(1000);
                // Найти элемент input для файла
                IWebElement fileInput = driver.FindElement(By.Id("file"));

                // Отправить путь к файлу
                fileInput.SendKeys(filePath);
                await Task.Delay(1000);
                // Ожидание, чтобы убедиться, что файл выбран
                //WebDriverWait wait1 = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                //wait.Until(d => d.FindElement(By.CssSelector(".custom-file-label")).Text.Contains("chatgpt.bpmnimport"));

                // Найти кнопку отправки#installModule > div > div > form > div.modal-footer > button.btn.btn-primary
                // IWebElement submitButton = driver.FindElement(By.CssSelector("button[type='submit']"));
                IWebElement submitButton = driver.FindElement(By.CssSelector("#installModule > div > div > form > div.modal-footer > button.btn.btn-primary"));
                if (submitButton != null)
                {
                    // Нажать на кнопку отправки
                    submitButton.Click();
                    // await Task.Delay(5000);
                }

                //Console.WriteLine($"Файл {Path.GetFileName(filePath)} отправлен.");
            }
            catch (Exception ex)
            {
                ErrModule.Add(filePath);
                Console.WriteLine(ex.Message);
                Log.Error(ex.Message);
            }
            if (ElementExists(driver))
            {
                Log.Error("errorElementExists: " + filePath);
                Console.WriteLine("errorUpdate: " + filePath);
                ErrModule.Add(filePath);
            }
        }
        return ErrModule;
    }

    public static bool ElementExists(ChromeDriver driver)
    {
        try
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(4));
            var t = wait.Until(d => d.FindElements(By.CssSelector(".alert.alert-danger.my-2"))
                .FirstOrDefault(e => e.Text.StartsWith("Upload module failed")));
            if (t == null)
            {
                return false;
            }
            Console.WriteLine(t.Text);
            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    //static async Task UploadFile(HttpClient httpClient, string uploadUrl, string filePath, string antiForgeryToken, System.Collections.ObjectModel.ReadOnlyCollection<OpenQA.Selenium.Cookie> cookies)
    //{
    //    using (var content = new MultipartFormDataContent())
    //    {
    //        // Добавление файла
    //        using (var fileStream = File.OpenRead(filePath))
    //        {
    //            content.Add(new StreamContent(fileStream), "file", Path.GetFileName(filePath));
    //        }

    //        // Добавление anti-forgery токена
    //        content.Add(new StringContent(antiForgeryToken), "__RequestVerificationToken");

    //        // Настройка заголовков
    //        httpClient.DefaultRequestHeaders.Clear();
    //        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

    //        // Добавление куки
    //        var cookieString = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
    //        httpClient.DefaultRequestHeaders.Add("Cookie", cookieString);

    //        // Отправка запроса
    //        var response = await httpClient.PostAsync(uploadUrl, content);
    //        response.EnsureSuccessStatusCode();
    //    }
    //}
}