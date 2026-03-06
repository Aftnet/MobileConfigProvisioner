using System.Security.Cryptography.X509Certificates;

namespace Provisioner.Shared.Extensions
{
    public static class X509Certificate2Extensions
    {
        public const string CommonNameMarker = "CN=";

        public static string GetCommonName(this X509Certificate2 cert)
        {
            var subj = cert.Subject;
            if (!string.IsNullOrEmpty(subj))
            {
                var cnString = subj.Split(",").FirstOrDefault(d => d.StartsWith(CommonNameMarker));
                if (cnString != null)
                {
                    subj = cnString.Substring(CommonNameMarker.Length);
                }
            }

            return subj;
        }
    }
}
