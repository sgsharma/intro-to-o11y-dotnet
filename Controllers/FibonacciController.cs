using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Trace;

[Route("/fib")]
[ApiController]
public class FibonacciController : ControllerBase
{
    private readonly Tracer _tracer;
    private readonly IHttpClientFactory _clientFactory;
    private readonly string _projectDomain = Environment.GetEnvironmentVariable("PROJECT_DOMAIN");

    public static (string memberName, int LineNumber, string FilePath)
        CodeInfo(
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int LineNumber = 0,
            [System.Runtime.CompilerServices.CallerFilePath] string FilePath = "")
        => (memberName, LineNumber, FilePath);

    public FibonacciController(Tracer tracer, IHttpClientFactory clientFactory)
    {
        _tracer = tracer;
        _clientFactory = clientFactory;
    }

    [HttpGet]
    public async Task<int> CalculateFibonacciAsync(int index = 0)
    {
        // CUSTOM ATTRIBUTES (2 lines of code to uncomment)
        var currentSpan = Tracer.CurrentSpan;
        currentSpan.SetAttribute("parameter.index", index);

        if (index == 0 | index == 1)
            return 0;
        if (index == 2)
            return 1;

        var resOne = await GetNext(index - 1);
        var resTwo = await GetNext(index - 2);

        // CUSTOM SPAN (3 sections of code to uncomment, 4 lines total)
        using (var span = _tracer.StartActiveSpan("calculation"))
        {
        var (function, lineno, fpath) = CodeInfo();
        span.SetAttribute("source.memberName", function);
        span.SetAttribute("source.LineNumber", lineno);
        span.SetAttribute("source.FilePath", fpath);
        var fibonacciNumber = resOne + resTwo;
           span.SetAttribute("result", fibonacciNumber);
        return fibonacciNumber;
        };
    }

    private async Task<int> GetNext(int iv)
    {
        var client = _clientFactory.CreateClient();
        var resp = await client.GetAsync($"http://localhost:5000/fib?index={iv}");
        resp.EnsureSuccessStatusCode();
        // System.Diagnostics.Activity.Current.AddTag("parameter.index", iv.ToString());
        return int.Parse(await resp.Content.ReadAsStringAsync());
    }
}
