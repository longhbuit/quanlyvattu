using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace QuanLyVatTu;

public static class ConnectionConfig
{
    private static readonly Dictionary<BranchSite, string> _map;
    private static readonly object _lock = new();

    static ConnectionConfig()
    {
        _map = LoadInternal();
    }

    private static Dictionary<BranchSite, string> LoadInternal()
    {
        var defaults = new Dictionary<BranchSite, string>
        {
            { BranchSite.CongTy, "Server=localhost,14331;Database=CTY;TrustServerCertificate=True;" },
            { BranchSite.ChiNhanh1, "Server=localhost,14332;Database=CN1;TrustServerCertificate=True;" },
            { BranchSite.ChiNhanh2, "Server=localhost,14333;Database=CN2;TrustServerCertificate=True;" }
        };
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "ConnectionStrings.json");
            if (!File.Exists(path)) return defaults; // use defaults
            var json = File.ReadAllText(path);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            foreach (var kv in dict)
            {
                if (Enum.TryParse<BranchSite>(kv.Key, true, out var branch))
                {
                    defaults[branch] = kv.Value;
                }
            }
            return defaults;
        }
        catch
        {
            return defaults; // fallback
        }
    }

    public static string? GetBase(BranchSite site)
    {
        lock (_lock)
        {
            return _map.TryGetValue(site, out var v) ? v : null;
        }
    }
}

