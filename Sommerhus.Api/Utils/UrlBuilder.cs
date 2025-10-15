namespace Sommerhus.Api.Utils;

public static class UrlBuilder
{
    public static string AreaImageWebPath(Guid areaId, string fileName)
        => $"/uploads/areas/{areaId}/{fileName}";

    public static string HouseImageWebPath(Guid houseId, string fileName)
        => $"/uploads/houses/{houseId}/{fileName}";

    public static string CityImageWebPath(Guid cityId, string fileName)
        => $"/uploads/cities/{cityId}/{fileName}";

    // WEB-sti (til databasen) – brug aldrig Path.Combine til URLs
    public static string FeatureIconWebPath(Guid featureId, string fileName)
        => $"/uploads/features/{featureId}/{Uri.EscapeDataString(fileName)}";

    // DISK-sti (til filesystem)
    public static string FeatureIconDiskPath(IWebHostEnvironment env, Guid featureId, string fileName)
        => Path.Combine(env.WebRootPath ?? "wwwroot", "uploads", "features", featureId.ToString(), fileName);

    // Absolut URL til klienter
    public static string ToAbsolute(HttpRequest req, string relativeWebPath)
    {
        // håndter evtl. basePath (reverse proxy)
        var basePath = string.IsNullOrEmpty(req.PathBase) ? "" : req.PathBase.Value!.TrimEnd('/');
        var rel = relativeWebPath.StartsWith("/") ? relativeWebPath : "/" + relativeWebPath;
        return $"{req.Scheme}://{req.Host}{basePath}{rel}";
    }
}

