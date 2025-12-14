using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using OpenMetaverse;
using OpenMetaverse.Assets;
using Serilog;
using SkiaSharp;

namespace GalaxyViewer.Services
{
    public class ProfileImageService
    {
        private readonly GridClient _client;
        private readonly ILogger _log;
        private const int RequestTimeoutMs = 10000;

        public ProfileImageService(GridClient client, ILogger? log = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _log = log ?? Log.Logger;
        }

        /// <summary>
        /// Requests a profile image asset and returns an Avalonia Bitmap or null on failure.
        /// TODO: Future improvements:
        /// - Implement image caching to avoid redundant asset server requests, and cache invalidation
        /// - Add image scaling/resizing for efficient UI display
        /// </summary>
        public async Task<Bitmap?> GetProfileImageAsync(UUID imageId, CancellationToken ct = default)
        {
            if (imageId == UUID.Zero)
                return null;

            try
            {
                var asset = await RequestImageAssetAsync(imageId, ct).ConfigureAwait(false);
                if (asset == null)
                    return null;

                var managedImage = DecodeTextureAsset(asset, imageId);
                return managedImage == null ? null : ConvertManagedImageToAvaloniaBitmap(managedImage, imageId);
            }
            catch (OperationCanceledException)
            {
                _log.Warning("[ProfileImageService] Request timed out for image {ImageId}", imageId);
                return null;
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "[ProfileImageService] Failed to load image {ImageId}", imageId);
                return null;
            }
        }

        /// <summary>
        /// Requests an image asset from the asset server asynchronously.
        /// </summary>
        private async Task<AssetTexture?> RequestImageAssetAsync(UUID imageId, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<(TextureRequestState state, Asset? asset)>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            _client.Assets.RequestImage(imageId, (state, asset) =>
            {
                tcs.TrySetResult((state, asset));
            });

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(RequestTimeoutMs);

            var (state, asset) = await tcs.Task.WaitAsync(cts.Token).ConfigureAwait(false);

            if (state != TextureRequestState.Finished || asset is not AssetTexture texAsset)
            {
                _log.Debug("[ProfileImageService] Asset request failed: state={State}", state);
                return null;
            }

            return texAsset;
        }

        /// <summary>
        /// Decodes a texture asset into a ManagedImage, validating dimensions.
        /// </summary>
        private OpenMetaverse.Imaging.ManagedImage? DecodeTextureAsset(AssetTexture asset, UUID imageId)
        {
            if (!asset.Decode())
            {
                _log.Debug("[ProfileImageService] Failed to decode texture {ImageId}", imageId);
                return null;
            }

            var image = asset.Image;
            if (image == null)
            {
                _log.Debug("[ProfileImageService] Asset decode succeeded but image is null {ImageId}", imageId);
                return null;
            }

            if (image.Width <= 0 || image.Height <= 0)
            {
                _log.Debug("[ProfileImageService] Invalid image dimensions: {Width}x{Height} for {ImageId}",
                    image.Width, image.Height, imageId);
                return null;
            }

            return image;
        }

        /// <summary>
        /// Converts a ManagedImage to an Avalonia Bitmap via SkiaSharp.
        /// </summary>
        private Bitmap? ConvertManagedImageToAvaloniaBitmap(
            OpenMetaverse.Imaging.ManagedImage managedImage, UUID imageId)
        {
            SKBitmap? skBitmap = null;
            try
            {
                skBitmap = managedImage.ExportBitmap();
                if (skBitmap == null)
                {
                    _log.Debug("[ProfileImageService] ExportBitmap returned null for {ImageId}", imageId);
                    return null;
                }

                using var skImage = SKImage.FromBitmap(skBitmap);
                if (skImage == null)
                {
                    _log.Debug("[ProfileImageService] SKImage creation failed for {ImageId}", imageId);
                    return null;
                }

                using var encodedData = skImage.Encode(SKEncodedImageFormat.Png, 90);
                if (encodedData == null)
                {
                    _log.Debug("[ProfileImageService] Image encoding failed for {ImageId}", imageId);
                    return null;
                }

                var imageBytes = encodedData.ToArray();
                using var memoryStream = new MemoryStream(imageBytes);
                return new Bitmap(memoryStream);
            }
            finally
            {
                skBitmap?.Dispose();
            }
        }
    }
}
