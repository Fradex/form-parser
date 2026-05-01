namespace FormSqlTranslator.Services;

public sealed class SqlTranslatorClient(HttpClient httpClient)
{
    public async Task<string> TranslateAsync(string translatorUrl, string sql, CancellationToken ct)
    {
        var delay = TimeSpan.FromMilliseconds(200);
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

            try
            {
                using var content = new StringContent(sql);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
                var response = await httpClient.PostAsync(translatorUrl, content, timeoutCts.Token);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync(ct);
                }
            }
            catch when (attempt < 3)
            {
            }

            await Task.Delay(delay, ct);
            delay *= 2;
        }

        throw new InvalidOperationException("Failed to translate SQL after retries.");
    }
}
