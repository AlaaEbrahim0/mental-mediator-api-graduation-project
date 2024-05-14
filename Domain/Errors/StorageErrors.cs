using Shared;

namespace Domain.Errors;
public static class StorageErrors
{
	public static Error UnsupportedPhotoExtension(string extension) =>
		Error.Validation("Storage.UnsupportedPhotoExtension", $"The photo you uploaded has an extension [{extension}] that isn't supported, only jgp, jpeg, png are the supported extensions");

	public static Error FileSizeExceededMaximumSize() =>
		Error.Validation("Storage.PhotoSizeExceededMaximumSize", "The size of the photo is greater than the max photo size: 5MB");
}
