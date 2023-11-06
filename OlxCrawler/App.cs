using Command = System.CommandLine.Command;

namespace OlxCrawler;

internal sealed class App
{
    private IWebDriver _driver;
    private WebDriverWait _driverWait;
    private List<string> _offersHrefs = new List<string>();
    private List<string> _numbers = new List<string>();
    private string _baseOffersUrl = "";

    private const string OLX_BASE_URL = "https://www.olx.pl";

    public App()
        => PrepareDriver();

    public async Task Run(string[] args)
    {
        var levelSwitch = new LoggingLevelSwitch();
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .WriteTo.Console();

        var rootCommand = BuildRootCommand(
            levelSwitch, 
            loggerConfiguration
        );

        await rootCommand.InvokeAsync(args);
    }

    private RootCommand BuildRootCommand(
        LoggingLevelSwitch levelSwitch, 
        LoggerConfiguration loggerConfiguration
    )
    {
        var rootCommand = new RootCommand
        {
            BuildGetCommand(
                levelSwitch,
                loggerConfiguration
            )
        };

        return rootCommand;
    }

    private Command BuildGetCommand(
        LoggingLevelSwitch levelSwitch, 
        LoggerConfiguration loggerConfiguration
    )
    {
        Log.Logger = loggerConfiguration.CreateLogger();

        var searchPhraseOption = new Option<string?>(
            "--searchPhrase",
            () => null,
            "Search phrase that crawler will use in olx search box"
        );

        var cmd = new Command("get-numbers")
        {
            searchPhraseOption
        };

        cmd.SetHandler(
            GetNumbersOfOffersOwners,
            searchPhraseOption
        );

        return cmd;
    }

    private void GetNumbersOfOffersOwners(string searchPhrase)
    {
        FetchOffersBySearchPhrase(searchPhrase);

        foreach (var offerHref in _offersHrefs)
        {
            _driver
                .Navigate()
                .GoToUrl(offerHref);

            RevealNumber();
        }

        _driver.Close();
    }

    private void FetchOffersBySearchPhrase(string searchPhrase)
    {
        Base();

        var searchBarInput = _driver.FindElement(By.Id(ElementSelectors.HomePage.SEARCH_BAR_INPUT));
        searchBarInput.SendKeys("Praca dentysta");

        Thread.Sleep(420);

        var submitButton = _driver.FindElement(By.XPath(ElementSelectors.HomePage.SUBMIT_BUTTON));

        submitButton.Click();

        Thread.Sleep(5000);

        _baseOffersUrl = _driver.Url;

#if DEBUG
        ScrapOffersHrefs();
#else
        do
        {
            ScrapOffersHrefs();
            if (IsForwardButtonPresent())
            {
                ForwardOffersPagination(); 
                Thread.Sleep(1000);
            }
            else
            {
                break; 
            }
        }
        while (true);
#endif
    }

    private void ForwardOffersPagination()
    {
        var btn = _driver.FindElement(By.XPath(ElementSelectors.HomePage.FORWARD_BUTTON));

        btn.Click();
    }

    private bool IsForwardButtonPresent()
    {
        try
        {
            _driverWait.Until(driver =>
            {
                var element = driver.FindElement(By.XPath(ElementSelectors.HomePage.FORWARD_BUTTON));

                return 
                    element != null && 
                    element.Displayed && element.Enabled;
            });
            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    private void ScrapOffersHrefs()
    {
        var offers = _driver.FindElements(By.ClassName("css-rc5s2u"));

        foreach (var offer in offers)
        {
            var href = offer.GetAttribute("href");

            _offersHrefs.Add(href);

            Log.Information(href);
        };

        Thread.Sleep(1000);
    }

    private void RevealNumber()
    {
        Thread.Sleep(3000);

        var numberButton = _driverWait.Until(driver =>
        {
            var element = driver.FindElement(By.XPath(ElementSelectors.OfferDetailsPage.NUMBER_BUTTON));
            return (
                element is not null && 
                element.Displayed && 
                element.Enabled
            ) ? 
                element : 
                throw new NoSuchElementException();
        });

        numberButton.Click();

        var number = _driverWait.Until(driver =>
        {
            var elements = driver.FindElements(By.XPath(ElementSelectors.OfferDetailsPage.NUMBER));
            if (
                elements.Count > 0 && 
                elements[0].Displayed
            )
                return elements[0];

            return null;
        });

        if (number is not null)
        {
            Log.Information(number.Text);
            _numbers.Add(number.Text);
        }

        _driver.Navigate().GoToUrl(_baseOffersUrl);
    }

    private void Base()
    {
        _driver
            .Navigate()
            .GoToUrl($"{OLX_BASE_URL}/");

        AddRecaptchaCookieToDriver();
        AcceptCookies();

        Thread.Sleep(1000);
    }

    private void AcceptCookies()
    {
        try
        {
            var submitButton = _driver.FindElement(By.Id(ElementSelectors.HomePage.ACCEPT_COOKIES_BUTTON));

            submitButton.Click();
        }
        catch (WebDriverTimeoutException)
        {
        }
        catch (NoSuchElementException)
        {
        }
    }

    private void PrepareDriver()
    {
        var options = new ChromeOptions();
        options.AcceptInsecureCertificates = true;
#if RELEASE
        options.AddArguments("--headless");
#endif
        _driver = new ChromeDriver(options);
        _driverWait = new(
            _driver,
            TimeSpan.FromSeconds(20)
        );
        _driverWait.IgnoreExceptionTypes(typeof(NoSuchElementException));
    }

    private void AddRecaptchaCookieToDriver()
    {
        var cookie = new Cookie(
            "_GRECAPTCHA",
            "09ANjddZZZduVESn3Qf5HH-4L3jP_J6d6rJjkfvNh1jPt63tXSGa8726QkyaHOce_Xvv4Aizj3dUq8fiij_twtvhs",
            ".olx.pl",
            "/",
            null
        );

        _driver.Manage().Cookies.AddCookie(cookie);
        _driver.Navigate().Refresh();
    }
}