using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class cf
{
     public static string Base64UrlDecode(string base64Url)
    {
        string base64 = base64Url.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return base64;
    }

    public static string CalculateSHA256Hash(string input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            // Convert the input string to a byte array
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);

            // Calculate the SHA-256 hash of the byte array
            byte[] hashBytes = sha256.ComputeHash(inputBytes);

            // Convert the byte array to a hexadecimal string
            StringBuilder hashString = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                hashString.AppendFormat("{0:x2}", b);
            }

            return hashString.ToString();
        }
    }



}