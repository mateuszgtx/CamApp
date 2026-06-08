namespace CamApp;

public static partial class PhotoDownloadService
{
    public static string CleanFileName(string? fileName)
    {
        fileName = string.IsNullOrWhiteSpace(fileName) ? $"photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg" : fileName;

        foreach (var invalid in System.IO.Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(invalid, '_');

        if (!System.IO.Path.HasExtension(fileName))
            fileName += ".jpg";

        return fileName;
    }

    public static async Task SaveToPhoneAsync(string? fileName, byte[] bytes)
    {
        fileName = CleanFileName(fileName);

#if ANDROID
        await SaveToAndroidGalleryAsync(fileName, bytes);
#else
        var targetPath = System.IO.Path.Combine(FileSystem.AppDataDirectory, fileName);
        await File.WriteAllBytesAsync(targetPath, bytes);
#endif
    }

#if ANDROID
    private static async Task SaveToAndroidGalleryAsync(string fileName, byte[] bytes)
    {
        var context = Android.App.Application.Context;
        var resolver = context.ContentResolver ?? throw new InvalidOperationException("Brak ContentResolver.");
        var mimeType = GetMimeType(fileName);

        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q)
        {
            var values = new Android.Content.ContentValues();
            values.Put(Android.Provider.MediaStore.IMediaColumns.DisplayName, fileName);
            values.Put(Android.Provider.MediaStore.IMediaColumns.MimeType, mimeType);
            values.Put(Android.Provider.MediaStore.IMediaColumns.RelativePath, Android.OS.Environment.DirectoryPictures + "/CamApp");
            values.Put(Android.Provider.MediaStore.IMediaColumns.IsPending, 1);

            var uri = resolver.Insert(Android.Provider.MediaStore.Images.Media.ExternalContentUri, values)
                ?? throw new IOException("Nie udało się utworzyć pliku w galerii.");

            try
            {
                await using var output = resolver.OpenOutputStream(uri)
                    ?? throw new IOException("Nie udało się otworzyć pliku do zapisu.");
                await output.WriteAsync(bytes);

                values.Clear();
                values.Put(Android.Provider.MediaStore.IMediaColumns.IsPending, 0);
                resolver.Update(uri, values, null, null);
            }
            catch
            {
                resolver.Delete(uri, null, null);
                throw;
            }

            return;
        }

        var pictures = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures)?.AbsolutePath;
        if (string.IsNullOrWhiteSpace(pictures))
            throw new IOException("Nie znaleziono folderu Pictures.");

        var folder = System.IO.Path.Combine(pictures, "CamApp");
        Directory.CreateDirectory(folder);
        var path = System.IO.Path.Combine(folder, fileName);
        await File.WriteAllBytesAsync(path, bytes);

        var valuesLegacy = new Android.Content.ContentValues();
        valuesLegacy.Put(Android.Provider.MediaStore.IMediaColumns.Data, path);
        valuesLegacy.Put(Android.Provider.MediaStore.IMediaColumns.DisplayName, fileName);
        valuesLegacy.Put(Android.Provider.MediaStore.IMediaColumns.MimeType, mimeType);
        resolver.Insert(Android.Provider.MediaStore.Images.Media.ExternalContentUri, valuesLegacy);
    }

    private static string GetMimeType(string fileName)
    {
        var ext = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };
    }
#endif
}
