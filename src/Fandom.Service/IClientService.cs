namespace Fandom.Service
{
    public interface IClientService: IDisposable
    {
        Task<ApiResponse<T>> SendGetRequest<T>(
            string address,
            Dictionary<string, string>? queryArguments = null,
            CancellationToken cancellationToken = default)
            where T : class;
    }
}
