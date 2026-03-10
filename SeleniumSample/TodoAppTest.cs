using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace SeleniumSample;

/// <summary>
/// C# port of the Java TestNG todo-app test.
/// Runs against https://lambdatest.github.io/sample-todo-app/
/// </summary>
public class TodoAppTest
{
    private readonly IWebDriver _driver;
    private const string TestUrl = "https://lambdatest.github.io/sample-todo-app/";
    private string _status = "passed";

    public TodoAppTest(IWebDriver driver)
    {
        _driver = driver;
    }

    public void Run()
    {
        try
        {
            RunTest1_AddAndCheckItems();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FAIL] {ex.Message}");
            _status = "failed";
        }
        finally
        {
            // Mirror Java's lambda-status JS call
            ((IJavaScriptExecutor)_driver)
                .ExecuteScript($"lambda-status={_status}");
        }
    }

    private void RunTest1_AddAndCheckItems()
    {
        _driver.Navigate().GoToUrl(TestUrl);
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

        Console.WriteLine("[INFO] Opened: " + TestUrl);

        var textField = _driver.FindElement(By.Id("sampletodotext"));
        int itemCount = 5;

        // Add new items
        for (int i = 1; i <= itemCount; i++)
        {
            textField.Click();
            textField.SendKeys($"Adding a new item {i}");
            textField.SendKeys(Keys.Enter);
            Console.WriteLine($"[INFO] Added item {i}");
            Thread.Sleep(500);
        }

        // Check off items and verify remaining count
        int totalCount = itemCount + 5; // 5 pre-existing + 5 added
        int remaining  = totalCount - 1;

        for (int i = 1; i < totalCount; i++, remaining--)
        {
            string xpath = $"(//input[@type='checkbox'])[{i}]";
            _driver.FindElement(By.XPath(xpath)).Click();
            Thread.Sleep(300);

            string actualText   = _driver.FindElement(By.ClassName("ng-binding")).Text;
            string expectedText = $"{remaining} of {totalCount} tasks remaining";

            if (!actualText.Contains(expectedText))
            {
                Console.WriteLine($"[WARN] Mismatch — expected: '{expectedText}', got: '{actualText}'");
                _status = "failed";
            }
            else
            {
                Console.WriteLine($"[PASS] Item {i} checked — {actualText}");
            }

            Thread.Sleep(300);
        }
    }
}
