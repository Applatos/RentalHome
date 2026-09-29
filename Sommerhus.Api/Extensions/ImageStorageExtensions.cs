using Microsoft.Extensions.FileProviders;
using Sommerhus.Api.Infrastructure.Storage;

namespace Sommerhus.Api.Extensions;

public static class ImageStorageExtensions
{
    public static WebApplication UseImageFiles(this WebApplication app)
    {
        // Publish excludes uploads, so a fresh installation may have no wwwroot. The
        // host then installs a NullFileProvider which cannot see later uploads.
        var webRoot = PhysicalImageStorage.GetWebRoot(app.Environment);
        Directory.CreateDirectory(webRoot);

        var provider = new PhysicalFileProvider(webRoot);
        app.Lifetime.ApplicationStopped.Register(provider.Dispose);
        app.UseStaticFiles(new StaticFileOptions { FileProvider = provider });
        return app;
    }
}
