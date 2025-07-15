using FarmScout.Models;
using Microsoft.Maui.Graphics.Platform;

namespace FarmScout.Services
{
    public class PhotoService
    {
        //private const int MaxPhotoWidth = 1920;
        //private const int MaxPhotoHeight = 1080;

        private const int MaxPhotoWidth = 1280;
        private const int MaxPhotoHeight = 720;

        private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
        private const float JpegQuality = 0.85f;

        /// <summary>
        /// Captures a photo and returns it as an ObservationPhoto object ready for database storage
        /// </summary>
        public static async Task<ObservationPhoto?> CapturePhotoAsync(Guid observationId, string description = "")
        {
            try
        {
                if (!MediaPicker.Default.IsCaptureSupported)
            {
                    App.Log("Photo capture is not supported on this device");
                    return null;
                }

                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo == null)
                {
                    App.Log("No photo was captured");
                    return null;
                }

                return await ProcessPhotoFileAsync(photo, observationId, description);
            }
            catch (Exception ex)
            {
                App.Log($"Error capturing photo: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Picks a photo from the gallery and returns it as an ObservationPhoto object
        /// </summary>
        public static async Task<ObservationPhoto?> PickPhotoAsync(Guid observationId, string description = "")
        {
            try
            {
                var photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo == null)
                {
                    App.Log("No photo was selected");
                    return null;
                }

                return await ProcessPhotoFileAsync(photo, observationId, description);
            }
            catch (Exception ex)
            {
                App.Log($"Error picking photo: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Processes a photo file and creates an ObservationPhoto object
        /// </summary>
        private static async Task<ObservationPhoto?> ProcessPhotoFileAsync(FileResult photo, Guid observationId, string description)
        {
            try
            {
                    using var stream = await photo.OpenReadAsync();
                var originalBytes = new byte[stream.Length];
                await stream.ReadAsync(originalBytes, 0, (int)stream.Length);

                // Get image info
                var imageInfo = await GetImageInfoAsync(originalBytes);
                if (imageInfo == null)
                {
                    App.Log("Could not read image information");
                    return null;
                }

                // Process and compress the image
                var processedResult = await ProcessImageAsync(originalBytes, imageInfo.Value);
                if (processedResult == null)
                {
                    App.Log("Failed to process image");
                    return null;
                }

                // Create ObservationPhoto object
                var observationPhoto = new ObservationPhoto
                {
                    ObservationId = observationId,
                    PhotoData = processedResult.Value.Bytes,
                    MimeType = "image/jpeg", // Always convert to JPEG for consistency
                    OriginalFileName = photo.FileName ?? "photo.jpg",
                    FileSize = processedResult.Value.Bytes.Length,
                    Width = processedResult.Value.Width,
                    Height = processedResult.Value.Height,
                    Description = description,
                    Timestamp = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsActive = true
                };

                App.Log($"Photo processed successfully: {processedResult.Value.Bytes.Length} bytes, {processedResult.Value.Width}x{processedResult.Value.Height}");
                return observationPhoto;
            }
            catch (Exception ex)
            {
                App.Log($"Error processing photo file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets image information from byte array
        /// </summary>
        private static async Task<(int Width, int Height, string Format)?> GetImageInfoAsync(byte[] imageData)
        {
            try
            {
                using var stream = new MemoryStream(imageData);
                var image = PlatformImage.FromStream(stream);
                
                if (image == null)
                    return null;

                return ((int)image.Width, (int)image.Height, "unknown");
            }
            catch (Exception ex)
            {
                App.Log($"Error getting image info: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Processes and compresses an image
        /// </summary>
        private static async Task<(byte[] Bytes, int Width, int Height)?> ProcessImageAsync(byte[] originalData, (int Width, int Height, string Format) imageInfo)
        {
            try
            {
                using var originalStream = new MemoryStream(originalData);
                var originalImage = PlatformImage.FromStream(originalStream);
                
                if (originalImage == null)
                    return null;

                // Resize if necessary
                Microsoft.Maui.Graphics.IImage resizedImage = originalImage;
                int finalWidth = imageInfo.Width;
                int finalHeight = imageInfo.Height;
                
                if (imageInfo.Width > MaxPhotoWidth || imageInfo.Height > MaxPhotoHeight)
                {
                    resizedImage = await ResizeImageAsync(originalImage, MaxPhotoWidth, MaxPhotoHeight);
                    finalWidth = (int)resizedImage.Width;
                    finalHeight = (int)resizedImage.Height;
                }

                // Convert to JPEG and compress
                using var outputStream = new MemoryStream();
                await resizedImage.SaveAsync(outputStream, ImageFormat.Jpeg, JpegQuality);
                
                var processedBytes = outputStream.ToArray();

                // Check file size
                if (processedBytes.Length > MaxFileSizeBytes)
                {
                    App.Log($"Image is still too large ({processedBytes.Length} bytes), attempting further compression");
                    var furtherCompressed = await CompressImageFurtherAsync(processedBytes);
                    if (furtherCompressed != null)
                    {
                        return (furtherCompressed, finalWidth, finalHeight);
                    }
                    return null;
                }

                return (processedBytes, finalWidth, finalHeight);
            }
            catch (Exception ex)
            {
                App.Log($"Error processing image: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Resizes an image to fit within specified dimensions while maintaining aspect ratio
        /// </summary>
        private static async Task<Microsoft.Maui.Graphics.IImage> ResizeImageAsync(Microsoft.Maui.Graphics.IImage originalImage, int maxWidth, int maxHeight)
        {
            try
            {
                var scaleX = (float)maxWidth / originalImage.Width;
                var scaleY = (float)maxHeight / originalImage.Height;
                var scale = Math.Min(scaleX, scaleY);

                var newWidth = (int)(originalImage.Width * scale);
                var newHeight = (int)(originalImage.Height * scale);

                var resizedImage = originalImage.Resize(newWidth, newHeight, ResizeMode.Fit);
                return resizedImage;
            }
            catch (Exception ex)
            {
                App.Log($"Error resizing image: {ex.Message}");
                return originalImage; // Return original if resize fails
            }
        }

        /// <summary>
        /// Further compresses an image by reducing quality
        /// </summary>
        private static async Task<byte[]?> CompressImageFurtherAsync(byte[] imageData)
        {
            try
            {
                using var inputStream = new MemoryStream(imageData);
                var image = PlatformImage.FromStream(inputStream);
                
                if (image == null)
                    return null;

                using var outputStream = new MemoryStream();
                await image.SaveAsync(outputStream, ImageFormat.Jpeg, 60); // Lower quality
                
                return outputStream.ToArray();
            }
            catch (Exception ex)
            {
                App.Log($"Error compressing image further: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Converts an ObservationPhoto to a Stream for display
        /// </summary>
        public static Stream GetPhotoStream(ObservationPhoto photo)
        {
            return new MemoryStream(photo.PhotoData);
        }

        /// <summary>
        /// Converts an ObservationPhoto to a byte array
        /// </summary>
        public static byte[] GetPhotoBytes(ObservationPhoto photo)
        {
            return photo.PhotoData;
        }

        /// <summary>
        /// Gets a thumbnail version of a photo for display in lists
        /// </summary>
        public static async Task<byte[]?> GetThumbnailAsync(ObservationPhoto photo, int maxSize = 150)
        {
            try
            {
                using var stream = new MemoryStream(photo.PhotoData);
                var image = PlatformImage.FromStream(stream);
                
                if (image == null)
                    return null;

                // Calculate thumbnail dimensions
                var scaleX = (float)maxSize / image.Width;
                var scaleY = (float)maxSize / image.Height;
                var scale = Math.Min(scaleX, scaleY);

                var thumbWidth = (int)(image.Width * scale);
                var thumbHeight = (int)(image.Height * scale);

                var thumbnail = image.Resize(thumbWidth, thumbHeight, ResizeMode.Fit);
                
                using var outputStream = new MemoryStream();
                await thumbnail.SaveAsync(outputStream, ImageFormat.Jpeg, 80);
                
                return outputStream.ToArray();
                }
            catch (Exception ex)
            {
                App.Log($"Error creating thumbnail: {ex.Message}");
            return null;
            }
        }

        /// <summary>
        /// Validates if a photo meets the requirements
        /// </summary>
        public static bool ValidatePhoto(ObservationPhoto photo)
        {
            if (photo.PhotoData == null || photo.PhotoData.Length == 0)
            {
                App.Log("Photo data is empty");
                return false;
            }

            if (photo.PhotoData.Length > MaxFileSizeBytes)
            {
                App.Log($"Photo file size ({photo.FileSizeDisplay}) exceeds maximum allowed size");
                return false;
            }

            if (photo.Width <= 0 || photo.Height <= 0)
            {
                App.Log("Photo dimensions are invalid");
                return false;
            }

            return true;
        }
    }
} 