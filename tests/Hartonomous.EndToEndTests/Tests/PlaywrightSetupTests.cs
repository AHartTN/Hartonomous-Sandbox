using Microsoft.Playwright;
using FluentAssertions;
using Hartonomous.EndToEndTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.EndToEndTests.Tests;

/// <summary>
/// E2E tests using Playwright. Inherits from a base class that handles logging.
/// </summary>
public class PlaywrightSetupTests : PlaywrightTestBase
{
    public PlaywrightSetupTests(PlaywrightFixture fixture, ITestOutputHelper output) 
        : base(fixture, output)
    {
        _output.WriteLine($"[Test] Running test in class: {nameof(PlaywrightSetupTests)}");
    }

    [Fact]
    public void Playwright_BrowserIsInitialized()
    {
        _output.WriteLine("[Test] ==> Playwright_BrowserIsInitialized");
        
        // Assert
        _fixture.Browser.Should().NotBeNull();
        _fixture.Browser!.IsConnected.Should().BeTrue();
            
        _output.WriteLine("[Test] <== Playwright_BrowserIsInitialized PASSED");
    }

    [Fact]
    public async Task Playwright_CanNavigateToUrl()
    {
        _output.WriteLine("[Test] ==> Playwright_CanNavigateToUrl");
        
        // Arrange
        _output.WriteLine("[Test] Creating browser context...");
        await using var context = await _fixture.Browser!.NewContextAsync();
        _output.WriteLine("[Test] Creating new page...");
        var page = await context.NewPageAsync();

        try
        {
            // Act
            _output.WriteLine("[Test] Navigating to example.com...");
            var response = await page.GotoAsync("https://www.example.com", new PageGotoOptions
            {
                Timeout = 15000,
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
            _output.WriteLine($"[Test] Navigation complete. Status: {response?.Status}");

            // Assert
            response.Should().NotBeNull();
            response!.Ok.Should().BeTrue();
            page.Url.Should().Contain("example.com");
            
            _output.WriteLine("[Test] <== Playwright_CanNavigateToUrl PASSED");
        }
        finally
        {
            _output.WriteLine("[Test] Cleaning up page and context...");
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Playwright_CanExtractPageContent()
    {
        _output.WriteLine("[Test] ==> Playwright_CanExtractPageContent");
        
        // Arrange
        _output.WriteLine("[Test] Creating browser context...");
        await using var context = await _fixture.Browser!.NewContextAsync();
        _output.WriteLine("[Test] Creating new page...");
        var page = await context.NewPageAsync();

        try
        {
            _output.WriteLine("[Test] Navigating to example.com...");
            await page.GotoAsync("https://www.example.com", new PageGotoOptions
            {
                Timeout = 15000,
                WaitUntil = WaitUntilState.DOMContentLoaded
            });

            // Act
            _output.WriteLine("[Test] Extracting page title...");
            var title = await page.TitleAsync();
            _output.WriteLine($"[Test] Title: '{title}'");
            
            _output.WriteLine("[Test] Extracting page content...");
            var content = await page.ContentAsync();

            // Assert
            title.Should().NotBeNullOrWhiteSpace();
            content.Should().Contain("Example Domain");
            
            _output.WriteLine("[Test] <== Playwright_CanExtractPageContent PASSED");
        }
        finally
        {
            _output.WriteLine("[Test] Cleaning up page and context...");
            await page.CloseAsync();
        }
    }
}
