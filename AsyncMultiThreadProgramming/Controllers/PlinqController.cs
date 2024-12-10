using AsyncMultiThreadProgramming.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace AsyncMultiThreadProgramming.Controllers
{

  // Not: IO Bounded işlemler, API istek atma, Okuma yazma işlemleri,Message Brokers Queues AMQP ,Database işlemleri, PDF generate gibi işlemlerde ASYNC programalamayı daha çok tercih ederiz.

  // CPU bounded CPU tarafında maliyetli hesaplama, matematiksel, finansal algoritmalarda ve verinin milyonlarca olduğu durumlarda Paralel kodlamayı tercih ederiz.


  [Route("api/[controller]")]
  [ApiController]
  public class PlinqController : ControllerBase
  {

    // PLINQ veritabanına atılan LINQ sorgularının program tarafında paralel olarak işlenmesine olana tanır.

    [HttpGet("asParalel")]
    public IActionResult AsParalel()
    {

      // veri tabanı, db olarak düşünürsek
      ConcurrentBag<Product> plist = new();

      // Parallel.For ile yazılan kodlar yada Parallel.Foreach kodları LINQ sorguları sorgulanamaz. Fakat buda AsParalel gibi paralel çalışır.
      Parallel.For(0, 10000000, (item) =>
      {
        plist.Add(new Product { Id = item, Name = $"Product-{item}" });
      });

      plist.ToList().ForEach((item) =>
      {
        // kod senkronlaştı
      });


      // select * from products
      // sqlden dönen kayda göre biz bu kodu paralel olarak program tarafında işliyoruz.
      // AsParallel e kadar olan query ParalelQuery olarak geçer bundan sonra yazılan her şey ram üzerinde işlem olarak algılanır.
      plist.AsParallel().Where(x => x.Id > 5000).Take(50).ForAll((item) =>
      {
        // burada seçilecek olan 50 kayıt sırasız bir şekilde Threadlerin toplandığı veriler olacak.
        Console.Out.WriteLine(item.Name);
      });

      // select top 50 * from Products where Id > 50000 sql giden sorgudan gelen sonuç paralel olarak program tarafında işlenir.
      // Sql üzerinden filtrelenmiş veri üzerinden paralel bir işlem yapılacak ise doğru sorgu bul alltaki sorgudur.
      var query = plist.Where(x => x.Id > 5000).Take(50).AsQueryable().AsParallel();


      return Ok();
    }



    // PLINQ veritabanına atılan LINQ sorgularının program tarafında paralel olarak işlenmesine olana tanır.

    [HttpGet("AsOrdered")]
    public IActionResult AsOrdered()
    {

      // veri tabanı, db olarak düşünürsek
      ConcurrentBag<Product> plist = new();

      // Parallel.For ile yazılan kodlar yada Parallel.Foreach kodları LINQ sorguları sorgulanamaz. Fakat buda AsParalel gibi paralel çalışır.
      Parallel.For(0, 10000000, (item) =>
      {
        plist.Add(new Product { Id = item, Name = $"Product-{item}" });
      });


      // Not: AsOrdered kullanırsak normal AsParallel veya asunOrdered() daha daha yavaş olucaktır.
      var ordered = plist.Where(x => x.Id > 5000).Take(50).AsParallel().AsOrdered(); // Buffered Ordered Item

      foreach (var item in ordered)
      {
        Console.Out.WriteLine(item);
      }


      return Ok();
    }


    [HttpGet("withCancelation")]
    public IActionResult WithCancelation(CancellationToken token)
    {

      try
      {
        // veri tabanı, db olarak düşünürsek
        ConcurrentBag<Product> plist = new();

        // Parallel.For ile yazılan kodlar yada Parallel.Foreach kodları LINQ sorguları sorgulanamaz. Fakat buda AsParalel gibi paralel çalışır.
        Parallel.For(0, 10000000, (item) =>
        {
          plist.Add(new Product { Id = item, Name = $"Product-{item}" });
        });

        // Not: AsOrdered kullanırsak normal AsParallel veya asUnOrdered() daha daha yavaş olucaktır.
        // WithDegreeOfParallelism 4 farklı Thread üzerinde çalıştır.
        // ParallelExecutionMode.ForceParallelism bu mod kullanılırsa LINQ sorgusunun paralel'e zorlar. Default mode sorgunun maliyetine göre paralel işleme zorlayıp zorlamayacağına karar verir.Default mode çalıştırmak daha mantıklıdır.
        plist.Where(x => x.Id > 5000).Take(50).AsParallel().WithExecutionMode(ParallelExecutionMode.ForceParallelism).WithDegreeOfParallelism(4).WithCancellation(token).ForAll((item) =>
        {
          Thread.SpinWait(5000000);
          Math.Sqrt(item.Id);
          Console.Out.WriteLine("Thread Id" + Thread.CurrentThread.ManagedThreadId);

        }); // Buffered Ordered Item


        // WithCancellation method token.ThrowIfCancellationRequested(); ile operationCanceled exception veriyor.

        // aynı zamanda LINQ sorgusuda iptal eiliyor.
        if (token.IsCancellationRequested)
        {
          Console.Out.WriteLine("İstek İptal edildi");
        }

      }
      catch (OperationCanceledException ex)
      {
        Console.Out.WriteLine(ex.Message);
      }

      return Ok();
    }





    [HttpGet("withException")]
    public IActionResult WithException()
    {

      try
      {
        // veri tabanı, db olarak düşünürsek
        ConcurrentBag<Product> plist = new();

        plist.Add(new Product { Id = 54551, Name = null });
        plist.Add(null);

        // Parallel.For ile yazılan kodlar yada Parallel.Foreach kodları LINQ sorguları sorgulanamaz. Fakat buda AsParalel gibi paralel çalışır.
        Parallel.For(0, 10, (item) =>
        {
          plist.Add(new Product { Id = item, Name = $"Product-{item}" });
        });

        plist.Add(new Product { Id = 54545, Name = null });
        plist.Add(new Product { Id = 54546, Name = null });
        plist.Add(null);

        // concurent olarak aynı anda farklı threadlerde yazılan sorgudan dolayı bir istisnai durum meydana gelebilir. Bu durumda birden fazla hata exception oluşabilir. Bu exceptionların hepsi AggregateException sınıfı üzerinden toplu bir şekilde yakalanır.

        plist.Where(x => x.Name.StartsWith("Pro")).AsParallel().ForAll((item) =>
        {
          Thread.SpinWait(5000000);
          Math.Sqrt(item.Id);
          Console.Out.WriteLine("Thread Id" + Thread.CurrentThread.ManagedThreadId);

        });


      }
      catch (AggregateException ex)
      {
        // LINQ sorgusu içerisindeki tüm hataları yakalamamızı sağlar.
        // Bu hatalar Concurent eş zamanları oluşan hatayı temsil eder.
        foreach (var item in ex.InnerExceptions)
        {
          Console.Out.WriteLine(item.Message);
        }
      }

      return Ok();
    }



  }
}
