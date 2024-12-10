namespace AsyncMultiThreadProgramming.Services
{
  public record ProductDto
  {
    public int Id { get; set; }
    public string Name { get; set; }

  }
  public interface IAsyncService
  {
    Task<ProductDto> HandleAsync(int Id, CancellationToken cancellationToken);
  }


  public class AsyncService : IAsyncService
  {
    public  async Task<ProductDto> HandleAsync(int Id, CancellationToken cancellationToken)
    {
      Thread.SpinWait(100000000);

      // async code bloğu
      // Not: Asenkron kod blokları içerisinde gerçekleşen hata durumlarını yakalamak için task.Fromexception döndürmek zorundayız. Senkron kod gibi asenkron kod hatalarında catch otomatik bloğu tetiklenemez.
      var tasks1 = Task.Run(() =>
      {
        int a = 0;
        int b = 10;

        int z = b / a; // 0 bölünme logic hatası
      });

      var tasks2 = Task.Run(() =>
      {
        int[] numbers = new int[1];
        numbers[0] = 1;
        numbers[1] = 2;

      });

    

      if (cancellationToken.IsCancellationRequested)
      {
        // controller tarafında bu işleme ait bir cancelation durumu oluştutuğunu belirtmek için bu dönüş tipini kullandık.
        return await Task.FromCanceled<ProductDto>(cancellationToken);
      }


      // await tasks1; // running state kurtamamız lazım, tasks1.IsFaulted state geçmesi için işlemin bitmesini bekle.
       // when all non-blocking olarak çalışır. Tüm taskların aynı anda çözülmesini sağlar.

      var taskResult = Task.WhenAll(tasks1, tasks2); // multiple task single failure point
      await taskResult;

      if (taskResult.IsFaulted) // Exceptional Caselerin takibini ise IsFaulted ile yapacağız.
      {
        return await Task.FromException<ProductDto>(taskResult.Exception); // bu sayede controllerdaki catch'i tetikleyebiliriz. Kendi custom servisimizde exception durumlarının yönetimini yapmak zorundayız.
      }
      else
      {
        return await Task.FromResult(new ProductDto { Id = Id, Name = "Test" });
      }

    


      
    }
  }
}
