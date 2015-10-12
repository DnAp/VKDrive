using System;
using System.Text.RegularExpressions;
namespace VKDrive.Files
{
    public abstract class Download : VFile
    {
        public string Url = String.Empty;

        public Download(string name) : base(name) { }
        
        public static string EscapeFileName(string fileName)
        {
            fileName = fileName.Replace("&#39;", "'");
            Regex regexp = new Regex("&(#[0-9]+|lt|gt|quot);");
            fileName = regexp.Replace(fileName, "");
            
            //regexp = new Regex("[^a-z0-9а-я_ #$%&'*()+,.=@[\\]^`{}~-]", RegexOptions.IgnoreCase);
            regexp = new Regex("[><|?*/\\\\:\"]+");

            return regexp.Replace(fileName, "").TrimEnd('.').Trim();
        }

        public abstract bool update();
        //public abstract string getUniqueId();

        public abstract int[] getUniqueId();
    }
}
