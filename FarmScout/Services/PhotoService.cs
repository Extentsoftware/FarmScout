using System.Threading.Tasks;
using Microsoft.Maui.Media;

namespace FarmScout.Services
{
    public class PhotoService
    {
        public async Task<string?> CapturePhotoAsync()
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                {
                    var newFile = Path.Combine(FileSystem.AppDataDirectory, photo.FileName);
                    using var stream = await photo.OpenReadAsync();
                    using var newStream = File.OpenWrite(newFile);
                    await stream.CopyToAsync(newStream);
                    return newFile;
                }
            }
            return null;
        }
    }
} 