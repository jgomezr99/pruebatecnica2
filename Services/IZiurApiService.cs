using ZiurInventario.Models;

namespace ZiurInventario.Services;

public interface IZiurApiService
{
    Task<ApiTableData> GetDocumentsAsync(CancellationToken cancellationToken = default);
}
