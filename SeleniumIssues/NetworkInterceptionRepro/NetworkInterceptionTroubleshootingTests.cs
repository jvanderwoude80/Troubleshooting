using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace NetworkInterceptionTroubleshootingTests;

[TestFixture]
public class NetworkInterceptionTroubleshootingTests
{
    private IWebDriver _driver = null!;

    [SetUp]
    public async Task SetUp()
    {
        var options = new ChromeOptions();
        _driver = new ChromeDriver(options);
    }

    [TearDown]
    public void TearDown()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }

    [Test]
    public async Task Intercepting_network_call_results_in_second_navigation_action_not_succeeding_due_to_timeout()
    {
        bool someCondition = false;

        // Setup network interception the same way we do it in our failings test
        // Of course now with different URL and different JSON parsing, but the same approach
        INetwork networkInterceptor = _driver.Manage().Network;
        networkInterceptor.NetworkResponseReceived += (_, e) =>
        {
            if (e.ResponseUrl.Contains("delivery"))
            {
                var someValue = JToken.Parse(e.ResponseBody).SelectToken("$..client").Value<string>();
                someCondition = someValue == "microsoftmscompoc";
            }
        };

        await networkInterceptor.StartMonitoring();

        await _driver.Navigate().GoToUrlAsync("https://www.microsoft.com/");

        await networkInterceptor.StopMonitoring();

        // Navigate to a second website after intercepting a network call on the first website.
        // This navigation action does not succeed and we get a timeout exception instead, which is the same behavior we have in our failings test.
        // Exception: OpenQA.Selenium.WebDriverException : The HTTP request to the remote WebDriver server for URL http://localhost:54030/session/77610db131ee87a0d4dcd28ae6e149b5/url timed out after 60 seconds.
        await _driver.Navigate().GoToUrlAsync("https://github.com");

        Assert.That(_driver.Title, Does.Contain("GitHub").IgnoreCase);
    }

    [Test]
    public async Task Navigation_to_another_website_also_fails_when_no_url_is_intercepted()
    {
        bool someCondition = false;

        // Setup network interception the same way we do it in our failings test
        // Of course now with different URL and different JSON parsing, but the same approach
        INetwork networkInterceptor = _driver.Manage().Network;
        networkInterceptor.NetworkResponseReceived += (_, e) =>
        {
            if (e.ResponseUrl.Contains("notexistingresponse"))
            {
                var someValue = JToken.Parse(e.ResponseBody).SelectToken("$..client").Value<string>();
                someCondition = someValue == "microsoftmscompoc";
            }
        };

        await networkInterceptor.StartMonitoring();

        await _driver.Navigate().GoToUrlAsync("https://www.microsoft.com/");

        await networkInterceptor.StopMonitoring();

        // Navigate to a second website after intercepting a network call on the first website.
        // Even though we are not actually intercepting any URL (because the URL we are looking for in the interception logic does not exist), this navigation action also does not succeed.
        // Exception: OpenQA.Selenium.WebDriverException : The HTTP request to the remote WebDriver server for URL http://localhost:54030/session/77610db131ee87a0d4dcd28ae6e149b5/url timed out after 60 seconds.
        await _driver.Navigate().GoToUrlAsync("https://github.com");

        Assert.That(_driver.Title, Does.Contain("GitHub").IgnoreCase);
    }

    [Test]
    public async Task Navigation_to_another_website_succeeds_when_not_doing_any_network_interception_at_all()
    {
        bool someCondition = false;

        // Setup network interception the same way we do it in our failings test
        // Of course now with different URL and different JSON parsing, but the same approach
        INetwork networkInterceptor = _driver.Manage().Network;
        networkInterceptor.NetworkResponseReceived += (_, e) =>
        {
            if (e.ResponseUrl.Contains("blaaaaa"))
            {
                var someValue = JToken.Parse(e.ResponseBody).SelectToken("$..client").Value<string>();
                someCondition = someValue == "microsoftmscompoc";
            }
        };

        // NOT starting (and stopping) the network interception, so no network interception is happening at all.
        //await networkInterceptor.StartMonitoring();

        await _driver.Navigate().GoToUrlAsync("https://www.microsoft.com/");

        //await networkInterceptor.StopMonitoring();

        // Now this second navigation action DOES succeed (and pretty fast).
        await _driver.Navigate().GoToUrlAsync("https://github.com");

        Assert.That(_driver.Title, Does.Contain("GitHub").IgnoreCase);
    }
}