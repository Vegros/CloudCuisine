namespace mattias_tonna_swd63a_pftc.interfaces;

public interface IBucketStorageService
{
        Task<string> UploadFileAsync(IFormFile file, string fileNameForStorage);
        
        Task DeleteFileAsync(string fileName); 
    
}