using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace AsyncMultiThreadProgramming.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class ParalelsController : ControllerBase
  {
    // TPL => Task Paralel Library kütüphanesi içerisindeki ParalelFor ve ParalelForeach sınıfları ile yönetiyoruz. Async işlemleri sağlamak için içerisinde ParalelForAsync ve ParalelForeachAsync methodlarını barındırıyor.

    // 1000 birim işimiz var
    // ParalelFor veya ParalelForeach 1000 birimlik işi kendi algoritmasına göre 100 adet Thread böldüğünü düşünelim. 10 farklı Thread 'de işlemlerimiz paralelde çalışacak ama ParalelForeach veya ParalelFor içindeki kodlar döngünün içinin çalışması multi thread olacak.ParalelForeach Bloke eden bir kapsayıcı olup Main Thread de çalışır.  ParalelForeachAsync bu genel kapsayıcı ise non-bloking çalışır.

    [HttpGet("paralelForAndParalelForeachV1")]
    public IActionResult ParalelForAndParalelForeachV1()
    {
      // Sonuç: sıralı senkron bir şekilde birbirlerini bekleyerek çalıştırlar. Fakat foreach yada for içerisindeki kodlar ise paralelde birden fazla thread bölünererek çalıştı.
      Parallel.ForEach(Enumerable.Range(0, 1000000),(item) =>
      {
        Console.Out.Write("item 1" + item);
        Console.Out.WriteLine("ThreadId Foreach:" + Thread.CurrentThread.ManagedThreadId);
      });


      Parallel.For(100, 200, (item) =>
      {
        Console.Out.Write("item 2" + item);
        Console.Out.WriteLine("ThreadId For :" + Thread.CurrentThread.ManagedThreadId);
      });

      return Ok();

    }



   


    [HttpGet("paralelForAndParalelForeachV2")]
    public  Task<OkResult> ParalelForAndParalelForeachV2(CancellationToken cancellationToken)
    {
      // Sonuç: sıralı senkron bir şekilde birbirlerini bekleyerek çalıştırlar. Fakat foreach yada for içerisindeki kodlar ise paralelde birden fazla thread bölünererek çalıştı.
      Parallel.ForEachAsync(Enumerable.Range(0, 1000000), async (item, cancellationToken) =>
     {
       await Console.Out.WriteLineAsync("item 1" + item);
       await Console.Out.WriteLineAsync("ThreadId Foreach:" + Thread.CurrentThread.ManagedThreadId);
     });


     Parallel.ForAsync(100, 200, async (item, cancellationToken) =>
      {
        await Console.Out.WriteLineAsync("item 2" + item);
        await Console.Out.WriteLineAsync("ThreadId For :" + Thread.CurrentThread.ManagedThreadId);
      });


      return Task.FromResult(new OkResult());

    }


    [HttpGet("ParalelForeachBenchMark")]
    public IActionResult ParalelForeachBenchMark()
    {
      // Sonuç: sıralı senkron bir şekilde birbirlerini bekleyerek çalıştırlar. Fakat foreach yada for içerisindeki kodlar ise paralelde birden fazla thread bölünererek çalıştı.

      Stopwatch sp = new Stopwatch();

      ConcurrentBag<double> list = new();

      sp.Start();
      Parallel.ForEach(Enumerable.Range(0, 100000000), (item) =>
      {
        //Console.Out.Write("item 1" + item);
        //Console.Out.WriteLine("ThreadId Foreach:" + Thread.CurrentThread.ManagedThreadId);
        // bu kod blogunda ne kadar maaliyetli işlem olursa o kadar paralel kullanımından sonuç alırız. yada üzerinde çalıştığımız veri seti ne kadar büyükse burda minimum 1 Milyon veri üzerinde artık performas alırız.
        double z = Math.Pow(item,2);
        double y = Math.Sqrt(item);

        list.Add(z * y);
      });
      sp.Stop();
      Console.Out.WriteLine("ParalelForeachBenchMark : " + sp.ElapsedMilliseconds);



      return Ok();

    }

    [HttpGet("ForeachBenchMark")]
    public IActionResult ForeachBenchMark()
    {
      // Sonuç: sıralı senkron bir şekilde birbirlerini bekleyerek çalıştırlar. Fakat foreach yada for içerisindeki kodlar ise paralelde birden fazla thread bölünererek çalıştı.

      Stopwatch sp = new Stopwatch();

      // Uyarı: Normal List sınfıını kullandığımızda milyonluk veri setlerinde bu sınıfın bu kadar veriyi tutamadığı durumlar ile karşılaşabiliriz böyle bir durum olursa Concurency Collectionlar senkron kodlarda bile tercih edilebilir. 
      List<double> list = new();

      sp.Start(); // 100 Milyon veri üzerinde dönerken yaklaşık olarak paralelForeach göre 2 kat daha yavaş çalıştı
      Enumerable.Range(0, 100000000).ToList().ForEach((item) =>
      {
        double z = Math.Pow(item, 2); // CPU bounded bir işlem.
        double y = Math.Sqrt(item);

        list.Add(z * y);
      });
      sp.Stop();
      Console.Out.WriteLine("ForeachBenchMark:" + sp.ElapsedMilliseconds);



      return Ok();

    }



    // Not: Rece Condition Multi Thread Programlamada Threadler içerisinde hesaplanan değerlerin istenilen sonuçları döndürememesi durumu. Bir Thread'in başka bir Thread ait ortak shared veriyi ezmesi durumu.

    [HttpGet("raceCondition")]
    public IActionResult RaceCondition()
    {
      // Sonuç: sıralı senkron bir şekilde birbirlerini bekleyerek çalıştırlar. Fakat foreach yada for içerisindeki kodlar ise paralelde birden fazla thread bölünererek çalıştı.

      int counter = 0;
      int value = 0;
      //int counter2 = 0;
      /*object counterLockObject = new object();*/ // eski yöntem, bu yöntem performası olumsuz etkiliyor.

      ConcurrentBag<double> pows = new(); // sırası bir şekilde çalışır. Unordered

      Parallel.ForEach(Enumerable.Range(0, 100000), (item) =>
      {
        double z = Math.Pow(item, 2); // CPU bounded bir işlem.
        double y = Math.Sqrt(item);

        pows.Add(z); // eğer ki object list veya list ile paralel kodlar içerisindeki verileri race condition olmadan listeye düzgün bir şekilde eklemek istersek. Concurent Collections Thread Safe koleksiyonları kullanıyoruz.

        //lock (counterLockObject)
        //{
        //  counter++;
        //}

        //counter++;  // sorunumuz thread Safe çalışamadığımızdan race condition durumu meydana geldi.
        // Sayısal ifadelerin paralel kodlar içerisinde hesaplanmasında tercih ettiğimi thread safe bir sınıf.
        Interlocked.Increment(ref counter); // Thread Safe Code 
        Interlocked.Exchange(ref value, 10); // Veriyi güncelledik
      });


      return Ok(new { counter, value, pows });

    }



    [HttpGet("blockingCollection")]
    public IActionResult BlockingCollection()
    {
      // Sonuç: sıralı senkron bir şekilde birbirlerini bekleyerek çalıştırlar. Fakat foreach yada for içerisindeki kodlar ise paralelde birden fazla thread bölünererek çalıştı.
      Stopwatch sp = new Stopwatch();
      int counter = 0;
      int value = 0;
      //int counter2 = 0;
      // object counterLockObject = new object(); // eski yöntem, bu yöntem performası olumsuz etkiliyor.

      BlockingCollection<double> results = new(); // sırası bir şekilde çalışır. Unordered

      // private readonly System.Threading.Lock _balanceLock = new(); Use `dont use lock object` use System.Threading.Lock C#13 

      sp.Start();
      Parallel.ForEach(Enumerable.Range(0, 100), (item) =>
      {
        double z = Math.Pow(item, 2); // CPU bounded bir işlem.
        double y = Math.Sqrt(item);

        results.Add(z * y); 

       

      });

      sp.Stop();

      Console.Out.WriteLine("BlockingCollection" + sp.ElapsedMilliseconds);


      return Ok(new { results });

    }

    // Unordered olarak Non-Blocking çalıştığı için  BlockingCollection göre çok hızlı çalışır.
    // Not: ConcurentBag Blocking Collectiondan 2 temel farkı var. Blocking Collectiondan listeye eleman eklerken ParalelForeach içerisindeki Thread'in listeye eklemesini bitirmeden diğer Threading listeye müdehaleni bloke eder. Buda performans kaybına sebebiyet verir. Ama bu tarz hassas listeki her bir item'ın hesaplanması gereken durumlarda daha güvenli bir veri yakaşımı sağlar. ConcurentBag istemeyen race condition durumları logical olarak gözlemlenebilir. ConcurentBag farkı threadlerin aynı anda listeye eleman eklemesine izin verdiğinden dolayı, geliştirici bu listedeki elemanlar üzerinden bir hesaplama işlemi yapıyorsa, burada logical hesaplama hataları meydana gelebilir. Bunun dışında BlockingCollection'ın kapasitesi ve eleman eklemesinin durudulması işlemin bitiş anını yakalama gibi ekstra özellikleri vardır. ConcurentBag bu özellikleri göstermez.

    [HttpGet("ConcurentBag")]
    public IActionResult ConcurentBag()
    {
    
      Stopwatch sp = new Stopwatch();
      int counter = 0;
      int value = 0;

      ConcurrentBag<double> results = new();

      sp.Start();
      Parallel.ForEach(Enumerable.Range(0, 100), (item) =>
      {
        double z = Math.Pow(item, 2); // CPU bounded bir işlem.
        double y = Math.Sqrt(item);

        results.Add(z * y);

      });
      sp.Stop();

      Console.Out.WriteLine("ConcurentBag" + sp.ElapsedMilliseconds);

      return Ok(new { results});

    }

  }
}
