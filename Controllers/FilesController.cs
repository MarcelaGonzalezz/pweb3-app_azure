using System.Security.Claims;
using FileShareApp.Data;
using FileShareApp.Models;
using FileShareApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FileShareApp.Controllers;

[Authorize]
public class FilesController : Controller
{
    private const long MaxFileSize = 50L * 1024 * 1024; // 50 MB

    private readonly AppDbContext _db;
    private readonly BlobStorageService _blobs;

    public FilesController(AppDbContext db, BlobStorageService blobs)
    {
        _db = db;
        _blobs = blobs;
    }

    public async Task<IActionResult> Explorar()
    {
        var files = await _db.Files
            .OrderByDescending(f => f.UploadedAtUtc)
            .ToListAsync();
        return View(files);
    }
    public async Task<IActionResult> MyFiles() {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var files = await _db.Files
            .Where(f => f.UploadedByUserId == userId)
            .OrderByDescending(f => f.UploadedAtUtc)
            .ToListAsync();
        return View(files);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
    public async Task<IActionResult> Upload(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Seleccioná un archivo.";
            return RedirectToAction(nameof(Explorar));
        }
        if (file.Length > MaxFileSize)
        {
            TempData["Error"] = "El archivo supera los 50 MB.";
            return RedirectToAction(nameof(Explorar));
        }

        var blobName = Guid.NewGuid().ToString("N");
        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream" : file.ContentType;

        // 1) Contenido -> Blob Storage
        await using (var stream = file.OpenReadStream())
        {
            await _blobs.UploadAsync(blobName, stream, contentType);
        }

        // 2) Metadatos -> Azure SQL
        try
        {
            _db.Files.Add(new StoredFile
            {
                OriginalName = Path.GetFileName(file.FileName),
                BlobName = blobName,
                ContentType = contentType,
                SizeBytes = file.Length,
                UploadedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                UploadedByEmail = User.Identity?.Name ?? ""
            });
            await _db.SaveChangesAsync();
        }
        catch
        {
            await _blobs.DeleteAsync(blobName); // no dejar blobs huérfanos
            throw;
        }

        TempData["Success"] = "Archivo subido correctamente.";
        return RedirectToAction(nameof(Explorar));
    }

    public async Task<IActionResult> Download(Guid id)
    {
        var meta = await _db.Files.FindAsync(id);
        if (meta is null) return NotFound();

        var stream = await _blobs.OpenReadAsync(meta.BlobName);
        return File(stream, meta.ContentType, meta.OriginalName);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var meta = await _db.Files.FindAsync(id);
        if (meta is null) return NotFound();

        if (meta.UploadedByUserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            return Forbid();

        await _blobs.DeleteAsync(meta.BlobName);
        _db.Files.Remove(meta);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Archivo eliminado.";
        return RedirectToAction(nameof(MyFiles));
    }
}