using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Duthie.Bot.Utils;

public static class EnumUtils
{
    public static string GetDescription<T>(T enumValue) where T : Enum =>
        enumValue.GetType()
            .GetMember(enumValue.ToString())
            .FirstOrDefault()
            ?.GetCustomAttribute<DescriptionAttribute>(true)
            ?.Description
        ?? string.Empty;

    public static string GetName<T>(T enumValue) where T : Enum =>
        enumValue.GetType()
            .GetMember(enumValue.ToString())
            .FirstOrDefault()
            ?.GetCustomAttribute<DisplayAttribute>()
            ?.GetName()
        ?? enumValue.ToString();
}