namespace MCMPTools;

internal class Downloader : IDisposable
{
    private readonly HttpClient _httpClient = new();
    private readonly SemaphoreSlim _semaphore = new(5);

    private readonly string _destDir;

    //public event Action<DownloaderProgress> ProgressChanged;

    public Downloader(string destinationDir)
    {
        _destDir = destinationDir;
    }

    public async Task DownloadFiles(List<DownloaderFile> files, Action<DownloaderProgress> progressChanged = null)
    {
        if (files == null) return;

        var tasks = new List<Task>();
        var i = 1;
        var total = files.Count;
        foreach (var file in files) {
            await _semaphore.WaitAsync();

            tasks.Add(DownloadFile(file).ContinueWith(t => {
                _semaphore.Release();
                if (!t.IsCompletedSuccessfully) return;
                Interlocked.Increment(ref i);
                progressChanged?.Invoke(new DownloaderProgress {
                    Current = i,
                    Total = total
                });
            }));
        }

        await Task.WhenAll(tasks);
    }

    private async Task DownloadFile(DownloaderFile file)
    {
        using var res = await _httpClient.GetAsync(file.Url);
        res.EnsureSuccessStatusCode();
        await using var stream = await res.Content.ReadAsStreamAsync();
        await using var fs = File.Create(Path.Combine(_destDir, file.FileName));
        await stream.CopyToAsync(fs);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _semaphore?.Dispose();
    }
}

internal class DownloaderFile
{
    public string Url { get; set; }
    public string FileName { get; set; }
}

internal class DownloaderProgress
{
    public int Current { get; set; }
    public int Total { get; set; }
}