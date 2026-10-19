using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace KPI.Web.Controllers;

/// <summary>
/// Quản lý tối ưu hóa công cụ tìm kiếm (SEO Controller)
/// Phục vụ sitemap.xml và robots.txt cho bot tìm kiếm
/// </summary>
public class SeoController : Controller
{
    /// <summary>
    /// Phát sinh sitemap.xml chuẩn Schema 0.9 cho các trang công khai
    /// </summary>
    [HttpGet("/sitemap.xml")]
    [Produces("application/xml")]
    public IActionResult Sitemap()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var sb = new StringBuilder();

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        // Các URL trang chính
        var publicUrls = new[]
        {
            new { Loc = "/", Priority = "1.0", ChangeFreq = "daily" },
            new { Loc = "/Auth/Login", Priority = "0.9", ChangeFreq = "monthly" },
            new { Loc = "/Home/About", Priority = "0.8", ChangeFreq = "monthly" },
            new { Loc = "/Home/UserGuide", Priority = "0.8", ChangeFreq = "weekly" },
            new { Loc = "/Health", Priority = "0.7", ChangeFreq = "always" },
            new { Loc = "/swagger", Priority = "0.6", ChangeFreq = "monthly" }
        };

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        foreach (var url in publicUrls)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{baseUrl}{url.Loc}</loc>");
            sb.AppendLine($"    <lastmod>{today}</lastmod>");
            sb.AppendLine($"    <changefreq>{url.ChangeFreq}</changefreq>");
            sb.AppendLine($"    <priority>{url.Priority}</priority>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");

        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }

    /// <summary>
    /// File robots.txt điều hướng bot tìm kiếm
    /// </summary>
    [HttpGet("/robots.txt")]
    [Produces("text/plain")]
    public IActionResult Robots()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var sb = new StringBuilder();

        sb.AppendLine("User-agent: *");
        sb.AppendLine("Allow: /");
        sb.AppendLine("Disallow: /api/");
        sb.AppendLine("Disallow: /admin/");
        sb.AppendLine("Disallow: /hubs/");
        sb.AppendLine();
        sb.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");

        return Content(sb.ToString(), "text/plain", Encoding.UTF8);
    }
}
