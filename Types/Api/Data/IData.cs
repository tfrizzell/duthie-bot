using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Duthie.Types.Api.Data;

public interface IApiData
{
    string GetHash();
}