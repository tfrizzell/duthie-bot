namespace Duthie.Bot.Extensions;

public static class IntExtensions
{
    public static string Ordinal(this int number)
    {
        var num = number.ToString();
        number %= 100;

        if ((number >= 11) && (number <= 13))
            return $"{num}th";

        switch (number % 10)
        {
            case 1: return $"{num}st";
            case 2: return $"{num}nd";
            case 3: return $"{num}rd";
            default: return $"{num}th";
        }
    }
}