using System.Web;

namespace MusicChatbot.Services
{
    public class FileService
    {
        public string GetBinaryUrl(string fileName)
        {
            string absoluteUri = HttpContext.Current.Request.Url.AbsoluteUri + $"/Binaries/" + fileName;
            return absoluteUri.Replace("api/messages/", "");
        }

        public string GetFilePath(string fileName)
        {
            return HttpContext.Current.Server.MapPath("/Binaries/" + fileName);
        }
    }
}