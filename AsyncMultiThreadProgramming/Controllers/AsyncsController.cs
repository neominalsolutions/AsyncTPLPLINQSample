using AsyncMultiThreadProgramming.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AsyncMultiThreadProgramming.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class AsyncsController : ControllerBase
  {
    // iki sınıfın bir arayüz üzerinden bağımlık oluşturmadan bağlanaması prensibi DIP (Dependency Inversion Principle) SOLID
    private readonly IAsyncService asyncService;
    //private readonly AsyncService asyncService1;
    public AsyncsController(IAsyncService asyncService)
    {
      this.asyncService = asyncService;
      //this.asyncService1 = asyncService1;
    }


    [HttpGet("syncResult")]
    public IActionResult SyncRequest()
    {
      Thread.Sleep(3000);
      Console.Out.WriteLine($"Sync {Thread.CurrentThread.ManagedThreadId}");
      return Ok();
    }

    [HttpGet("asyncResult")]
    public IActionResult AsyncRequest()
    {
      Task.Run(async () =>
      {
        // burada senkron bir kod yazsak dahi task run ile sarmallandığı için non-blocking çalışır.
        await Task.Delay(3000);
        Console.Out.WriteLine($"Async {Thread.CurrentThread.ManagedThreadId}");
      });

      return Ok();
    }


    [HttpGet("continueWith")]
    public IActionResult ContinueWithRequest()
    {
      // asenkron kod blokları sıralı olarak çalışmazlar. bu sebeple birbirinde bağımsız istekleri kendi içlerinde yöntemek için ContinueWith methodunun kullanımı önemlidir. // Birbirinden bağımsız Fetch istekleri

      HttpClient httpClient = new HttpClient();
      httpClient.GetStringAsync("https://google.com")
        .ContinueWith((response) =>
      {
        if (response.IsCompletedSuccessfully)
        {
          Console.Out.WriteLine($"Google Data Size {response.Result.Length}");
        }


        if (response.IsFaulted)
        {
          Console.Out.WriteLine($"Google Hata oluştu {response.Exception.Message}");
        }

      });


      httpClient.GetStringAsync("https://neominal.com")
       .ContinueWith((response) =>
       {
         if (response.IsCompletedSuccessfully)
         {
           Console.Out.WriteLine($"Neominal Data Size {response.Result.Length}");
         }


         if (response.IsFaulted)
         {
           Console.Out.WriteLine($"Neominal Hata oluştu {response.Exception.Message}");
         }

       });


      return Ok();
    }



    [HttpGet("asyncAwait")]
    public async Task<IActionResult> AsyncAwaitRequest()
    {
      // Not: await ile uyutulan yada bekletilen requestler non-blocking çalışır. Bu sebeple thread pool dali farklı threadId üzerinden işlem gerçekleşebilir. veya aynı Main Thread bloklanmadan kullanılabilir.

      int totalSize = 0;

      //await Task.Delay(50000);

      HttpClient httpClient = new HttpClient();
      var data1 = await httpClient.GetStringAsync("https://google.com");
      await Console.Out.WriteLineAsync($"Google Data Size {data1.Length}");
      await Console.Out.WriteLineAsync($"Google ThreadId {Thread.CurrentThread.ManagedThreadId}");

      // 2.istek 1. isteğin resolve olmasını çözümlenmesini bekliyor.
      var data2 = await httpClient.GetStringAsync("https://neominal.com");
      await Console.Out.WriteLineAsync($"Neominal Data Size {data2.Length}");
      await Console.Out.WriteLineAsync($"Neominal ThreadId {Thread.CurrentThread.ManagedThreadId}");


      totalSize = data1.Length + data2.Length;

      return Ok(new { totalSize });
    }


    [HttpGet("resultsRequest")]
    public async Task<IActionResult> ResultsRequest()
    {
      // Not: await ile uyutulan yada bekletilen requestler non-blocking çalışır. Bu sebeple thread pool dali farklı threadId üzerinden işlem gerçekleşebilir. veya aynı Main Thread bloklanmadan kullanılabilir.

      int totalSize = 0;

      //await Task.Delay(50000);

      // Result await olamadan kodun bloke olmasına sebep verir. Bu sebeple senkron bir kod bloğu gibi kodu çalıştırmak için mantıklıdır fakat await deki gibi asenkron non-blocking özelliği kaybolur.
      HttpClient httpClient = new HttpClient();
      var data1 = httpClient.GetStringAsync("https://google.com").Result;
      Console.Out.WriteLine($"Google Data Size {data1.Length}");
      Console.Out.WriteLine($"Google ThreadId {Thread.CurrentThread.ManagedThreadId}");




      // 2.istek 1. isteğin resolve olmasını çözümlenmesini bekliyor.
      var data2 = httpClient.GetStringAsync("https://neominal.com").Result;
      Console.Out.WriteLine($"Neominal Data Size {data2.Length}");
      Console.Out.WriteLine($"Neominal ThreadId {Thread.CurrentThread.ManagedThreadId}");


      // Wait veya WaitAll methodları Non-Blocking çalışmazlar. Main Thread bloke ederler.

      var task3 = httpClient.GetStringAsync("https://google.com");

      task3.Wait(); // await ile aynı şey değil, Task döndürmeyen her bir method kodu bloke eder.

      //  task3.GetAwaiter().GetResult(); // bu kodu bloke eder.


      if (task3.IsCompletedSuccessfully)
      {
        Console.Out.WriteLine("Kod Başarılı" + task3.Result.Length);
      }

      var task4 = Task.Run(() =>
      {
        Console.Out.Write("Task4");
      });


      Task.WaitAll(task3, task4); // İki tane farklı taskın ikisininde çözümesini beklemek istersek kullanılabilir. Kodu Bloklar. JSdeki Promise All benzer.



      totalSize = data1.Length + data2.Length;

      return Ok(new { totalSize });
    }


    // Request İptal işlemi: Cancelation Token

    [HttpGet("requestCancelation")]
    public async Task<IActionResult> RequestCancelation(CancellationToken token)
    {
      try
      {
        await Task.Delay(50000); // simüle etmek için kullandık.


        if (token.IsCancellationRequested)
        {
          await Console.Out.WriteLineAsync("Request Cancel edildi");
        }

        token.ThrowIfCancellationRequested(); // bu cancelation işlemlerinde böyle bir durum söz konusu olduğunda istisna bir durum oluşturmak için kullanılabilir.
      }
      catch (OperationCanceledException ex)
      {

        await Console.Out.WriteLineAsync(ex.Message);
      }


      return Ok();
    }


    [HttpGet("customRequest")]
    public async Task<IActionResult> CustomAsyncRequest(CancellationToken token)
    {

      try
      {
        // kendi servisimizi tetiklemiş olduk
        var task = asyncService.HandleAsync(1, token);

        if (task.IsCanceled) // eğer istek iptal edildiyse badRequest
        {
          Console.Out.WriteLine("Bad Request");
          return BadRequest();
        }
        else // istek iptal yoksa bu durumda OK result
        {
          var response = await task;

          return Ok(response);
        }

      }
      catch (Exception ex)
      {

        await Console.Out.WriteLineAsync(ex.Message);
      }

      return Ok();

    
    }



  }
}
