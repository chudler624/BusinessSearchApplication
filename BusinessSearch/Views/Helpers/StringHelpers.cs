using System.Text.RegularExpressions;

namespace BusinessSearch.Helpers
{
    public static class StringHelpers
    {
        public static string FormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return phoneNumber;

            phoneNumber = Regex.Replace(phoneNumber, @"[^\d]", "");

            if (phoneNumber.Length == 10)
                return $"({phoneNumber.Substring(0, 3)}) {phoneNumber.Substring(3, 3)}-{phoneNumber.Substring(6)}";
            else if (phoneNumber.Length == 11 && phoneNumber.StartsWith("1"))
                return $"({phoneNumber.Substring(1, 3)}) {phoneNumber.Substring(4, 3)}-{phoneNumber.Substring(7)}";
            else
                return phoneNumber;
        }
    }
}
