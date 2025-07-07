using System.Net.Http.Headers;

namespace Tobiso.Web.App.Handlers;

public sealed class HttpLoggingHandler : DelegatingHandler
{
	 private readonly IHttpContextAccessor _contextAccessor;
	 private readonly ILogger _logger;
	 public HttpLoggingHandler(IHttpContextAccessor contextAccessor, ILogger<HttpLoggingHandler> logger)
	 {
		  _contextAccessor = contextAccessor;
		  _logger = logger;
	 }

	 async protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	 {
		  var req = request;
		  var id = Guid.NewGuid().ToString();
		  var msg = $"[{id}]";

		  _logger.LogDebug(msg);

		  _logger.LogDebug($"{msg} ====== Start API {req.Method} {request.RequestUri.ToString()} ========");
		  _logger.LogDebug($"{msg} ");


		  foreach (var header in req.Headers)
		  {
				_logger.LogDebug($"{msg} {header.Key}: {string.Join(", ", header.Value)}");
		  }

		  if (req.Content != null)
		  {
				foreach (var header in req.Content.Headers)
				{
					 _logger.LogDebug($"{msg} {header.Key}: {string.Join(", ", header.Value)}");
				}

				if (req.Content is StringContent || IsTextBasedContentType(req.Headers) || IsTextBasedContentType(req.Content.Headers))
				{
					 string result = await req.Content.ReadAsStringAsync();

					 _logger.LogDebug($"{msg} Content:");
					 _logger.LogDebug($"{msg} {string.Join("", result.Cast<char>().Take(4096))}...");
				}
		  }

		  var start = DateTime.Now;
		  var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
		  var end = DateTime.Now;

		  _logger.LogDebug($"{msg} Duration: {end - start}");

		  msg = $"[{id}]";
		  _logger.LogDebug($"{msg} === Start Request ===");

		  HttpResponseMessage resp = response;

		  _logger.LogDebug($"{msg} {req.RequestUri.Scheme.ToUpper()}/{resp.Version} {(int)resp.StatusCode} {resp.ReasonPhrase}");

		  foreach (var header in resp.Headers)
		  {
				_logger.LogDebug($"{msg} {header.Key}: {string.Join(", ", header.Value)}");
		  }
		  
		  _logger.LogDebug($"{msg} === End Request ===");

		  if (resp.Content != null)
		  {
				foreach (var header in resp.Content.Headers)
				{
					 _logger.LogDebug($"{msg} Key/Value: {header.Key}: {string.Join(", ", header.Value)}");
				}

				if (resp.Content is StringContent || this.IsTextBasedContentType(resp.Headers) || this.IsTextBasedContentType(resp.Content.Headers))
				{
					 start = DateTime.Now;
					 var result = await resp.Content.ReadAsStringAsync();
					 end = DateTime.Now;

					 _logger.LogDebug($"{msg} Duration: {end - start}. Content:");
					 _logger.LogDebug($"{msg} {string.Join("", result.Cast<char>())}...");
				}
		  }

		  _logger.LogDebug($"{msg} ======== End API ========");
		  _logger.LogDebug($"");

		  return response;
	 }

	 readonly string[] types = ["html", "text", "xml", "json", "txt", "x-www-form-urlencoded"];

	 bool IsTextBasedContentType(HttpHeaders headers)
	 {
		  IEnumerable<string> values;
		  if (!headers.TryGetValues("Content-Type", out values))
		  {
				return false;
		  }

		  var header = string.Join(" ", values).ToLowerInvariant();

		  return types.Any(t => header.Contains(t));
	 }
}